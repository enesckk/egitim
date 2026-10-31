using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Content.Controllers;
using EgitimPlatform.Modules.Content.Entities;
using EgitimPlatform.Modules.Content.Features.ManageMaterials;
using EgitimPlatform.Modules.Content.Services;
using EgitimPlatform.Modules.Exams.Controllers;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Services;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Phase4ContentMaterialsSecurityTests
{
    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private IFormFile CreateMockFormFile(string fileName = "test_material.pdf", string contentType = "application/pdf", byte[]? content = null, long? overrideLength = null)
    {
        content ??= Encoding.UTF8.GetBytes("%PDF-1.4 Mock PDF Content for testing");
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.Length.Returns(overrideLength ?? content.Length);
        file.ContentType.Returns(contentType);
        file.OpenReadStream().Returns(_ => new MemoryStream(content));
        file.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(async (ci) =>
            {
                var targetStream = ci.Arg<Stream>();
                await targetStream.WriteAsync(content);
            });
        return file;
    }

    [Fact]
    public async Task Teacher_Can_Upload_Material_And_Assign_To_Classroom()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = teacherUserId,
            FirstName = "Ali",
            LastName = "Hoca"
        };
        db.Set<Teacher>().Add(teacher);

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            Name = "12-A Sayısal",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().Add(classroom);

        var classTeacher = new ClassroomTeacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            TeacherId = teacher.Id,
            ClassroomId = classroom.Id
        };
        db.Set<ClassroomTeacher>().Add(classTeacher);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Teacher]);

        var storage = Substitute.For<IFileStorageService>();
        storage.SaveFileAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("inst_1/teacher_1/safe_file.pdf");

        var handler = new MaterialsHandler(db, currentUser, storage, Substitute.For<IAuditService>());

        var result = await handler.CreateMaterialAsync(new CreateMaterialRequest(
            "Türev Fasikülü",
            "Matematik",
            "Türev Alma Kuralları",
            "12. Sınıf",
            "fasikul",
            "Açıklama notu",
            24,
            50,
            60,
            CreateMockFormFile(),
            new List<Guid> { classroom.Id }
        ));

        Assert.NotNull(result);
        Assert.Equal("Türev Fasikülü", result.Title);

        var savedMaterial = await db.Set<ContentMaterial>().Include(m => m.Assignments).FirstOrDefaultAsync(m => m.Id == result.Id);
        Assert.NotNull(savedMaterial);
        Assert.Single(savedMaterial.Assignments);
        Assert.Equal(classroom.Id, savedMaterial.Assignments.First().ClassroomId);
    }

    [Fact]
    public async Task Student_Can_Access_Material_Assigned_To_Their_Classroom()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            Name = "11-B",
            GradeLevel = 11,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().Add(classroom);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = studentUserId,
            FirstName = "Ayşe",
            LastName = "Kaya"
        };
        db.Set<Student>().Add(student);

        db.Set<ClassroomStudent>().Add(new ClassroomStudent
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ClassroomId = classroom.Id,
            StudentId = student.Id
        });

        var material = new ContentMaterial
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            TeacherId = Guid.NewGuid(),
            Title = "Dalgalar Testi",
            Subject = "Fizik",
            Topic = "Yay Dalgaları",
            Grade = "11. Sınıf",
            MaterialType = "test",
            OriginalFileName = "dalgalar.pdf",
            StoragePath = "path/dalgalar.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024
        };
        material.Assignments.Add(new ContentMaterialAssignment
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            MaterialId = material.Id,
            ClassroomId = classroom.Id,
            IsActive = true
        });
        db.Set<ContentMaterial>().Add(material);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(studentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Student]);

        var storage = Substitute.For<IFileStorageService>();
        storage.GetFileAsync("path/dalgalar.pdf", "dalgalar.pdf", "application/pdf")
            .Returns((new MemoryStream(Encoding.UTF8.GetBytes("file content")), "application/pdf", "dalgalar.pdf"));

        var handler = new MaterialsHandler(db, currentUser, storage, Substitute.For<IAuditService>());

        var list = await handler.GetStudentMaterialsAsync();
        Assert.Single(list);
        Assert.Equal("Dalgalar Testi", list[0].Title);

        var (stream, ct, fn) = await handler.DownloadMaterialAsync(material.Id);
        Assert.NotNull(stream);
        Assert.Equal("dalgalar.pdf", fn);
    }

    [Fact]
    public async Task Revoked_Classroom_Assignment_Denies_Student_Access_FailClosed()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            Name = "11-B",
            GradeLevel = 11,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().Add(classroom);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = studentUserId,
            FirstName = "Ayşe",
            LastName = "Kaya"
        };
        db.Set<Student>().Add(student);

        db.Set<ClassroomStudent>().Add(new ClassroomStudent
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            ClassroomId = classroom.Id,
            StudentId = student.Id
        });

        var material = new ContentMaterial
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            TeacherId = Guid.NewGuid(),
            Title = "Gizli Fasikül",
            Subject = "Fizik",
            Topic = "Optik",
            Grade = "11. Sınıf",
            MaterialType = "fasikul",
            OriginalFileName = "gizli.pdf",
            StoragePath = "path/gizli.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024
        };
        material.Assignments.Add(new ContentMaterialAssignment
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            MaterialId = material.Id,
            ClassroomId = classroom.Id,
            IsActive = false // REVOKED!
        });
        db.Set<ContentMaterial>().Add(material);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(studentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Student]);

        var storage = Substitute.For<IFileStorageService>();
        var handler = new MaterialsHandler(db, currentUser, storage, Substitute.For<IAuditService>());

        var list = await handler.GetStudentMaterialsAsync();
        Assert.Empty(list);

        await Assert.ThrowsAsync<SecurityException>(() => handler.DownloadMaterialAsync(material.Id));
    }

    [Fact]
    public async Task Cross_Tenant_Student_Cannot_Access_Material_From_Another_Institution()
    {
        using var db = CreateDb();
        var instA = Guid.NewGuid();
        var instB = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var classroomB = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB,
            Name = "12-C",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().Add(classroomB);

        var studentB = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB,
            UserId = studentUserId,
            FirstName = "Ahmet",
            LastName = "Demir"
        };
        db.Set<Student>().Add(studentB);

        db.Set<ClassroomStudent>().Add(new ClassroomStudent
        {
            Id = Guid.NewGuid(),
            InstitutionId = instB,
            ClassroomId = classroomB.Id,
            StudentId = studentB.Id
        });

        // Material belongs to Institution A
        var materialA = new ContentMaterial
        {
            Id = Guid.NewGuid(),
            InstitutionId = instA,
            TeacherId = Guid.NewGuid(),
            Title = "Institution A Özel Materyal",
            Subject = "Matematik",
            Topic = "Polinomlar",
            Grade = "10. Sınıf",
            MaterialType = "fasikul",
            OriginalFileName = "instA.pdf",
            StoragePath = "path/instA.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048
        };
        db.Set<ContentMaterial>().Add(materialA);
        await db.SaveChangesAsync();

        // Student is in Institution B
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(studentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instB);
        currentUser.Roles.Returns([Roles.Student]);

        var storage = Substitute.For<IFileStorageService>();
        var handler = new MaterialsHandler(db, currentUser, storage, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.DownloadMaterialAsync(materialA.Id));
    }

    [Fact]
    public async Task Teacher_Cannot_Assign_Material_To_Unassigned_Classroom()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = teacherUserId,
            FirstName = "Murat",
            LastName = "Hoca"
        };
        db.Set<Teacher>().Add(teacher);

        var unassignedClassroom = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            Name = "12-D Başka Şube",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        db.Set<Classroom>().Add(unassignedClassroom);

        var material = new ContentMaterial
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            TeacherId = teacher.Id,
            Title = "Organik Kimya",
            Subject = "Kimya",
            Topic = "Alkanlar",
            Grade = "12. Sınıf",
            MaterialType = "fasikul",
            OriginalFileName = "organik.pdf",
            StoragePath = "path/organik.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024
        };
        db.Set<ContentMaterial>().Add(material);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Teacher]);

        var storage = Substitute.For<IFileStorageService>();
        var handler = new MaterialsHandler(db, currentUser, storage, Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<SecurityException>(() => handler.AssignToClassroomAsync(
            material.Id,
            new AssignMaterialRequest(unassignedClassroom.Id, "İzinsiz atama")
        ));
    }

    [Fact]
    public async Task Unauthorized_File_Extension_Throws_InvalidOperationException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorageService(tempFolder);

        var exeFile = CreateMockFormFile("malware.exe", "application/x-msdownload", new byte[] { 0x4D, 0x5A });

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveFileAsync(exeFile, "subfolder"));

        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
    }

    [Fact]
    public async Task Spoofed_Pdf_Magic_Bytes_Throws_InvalidOperationException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileStorageService(tempFolder);
            var spoofedPdf = CreateMockFormFile("spoofed.pdf", "application/pdf", Encoding.UTF8.GetBytes("MZ-not-a-pdf-payload"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveFileAsync(spoofedPdf, "subfolder"));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task Path_Traversal_In_GetFileAsync_Is_Sanitized_And_Cannot_Escape_BaseStorage()
    {
        var outerFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var baseStorage = Path.Combine(outerFolder, "Storage", "Materials");
        Directory.CreateDirectory(outerFolder);
        var secretOutsidePath = Path.Combine(outerFolder, "secret.pdf");
        await File.WriteAllTextAsync(secretOutsidePath, "%PDF-1.4 secret outside base storage");

        try
        {
            var storage = new LocalFileStorageService(baseStorage);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                storage.GetFileAsync("../../secret.pdf", "secret.pdf", "application/pdf"));
        }
        finally
        {
            if (Directory.Exists(outerFolder)) Directory.Delete(outerFolder, true);
        }
    }

    [Fact]
    public void Upload_Endpoints_Enforce_Multipart_And_RequestSizeLimits_Before_Handler_Execution()
    {
        var createMaterialMethod = typeof(ContentMaterialsController).GetMethod(nameof(ContentMaterialsController.CreateMaterial))!;
        var createReqSize = createMaterialMethod.GetCustomAttribute<RequestSizeLimitAttribute>();
        var createFormLimits = createMaterialMethod.GetCustomAttribute<RequestFormLimitsAttribute>();

        Assert.NotNull(createReqSize);
        Assert.NotNull(createFormLimits);
        Assert.Equal(LocalFileStorageService.MaxFileSize, createFormLimits.MultipartBodyLengthLimit);

        foreach (var actionName in new[] { nameof(ExamResultImportsController.PreviewImport), nameof(ExamResultImportsController.CommitImport) })
        {
            var method = typeof(ExamResultImportsController).GetMethod(actionName)!;
            var reqSize = method.GetCustomAttribute<RequestSizeLimitAttribute>();
            var formLimits = method.GetCustomAttribute<RequestFormLimitsAttribute>();

            Assert.NotNull(reqSize);
            Assert.NotNull(formLimits);
            Assert.Equal(ImportFileValidator.MaxFileSizeBytes, formLimits.MultipartBodyLengthLimit);
        }
    }

    [Fact]
    public async Task Oversized_Material_Upload_Exceeding_25MB_Throws_InvalidOperationException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileStorageService(tempFolder);
            var oversizedPdf = CreateMockFormFile(
                "oversized.pdf",
                "application/pdf",
                Encoding.UTF8.GetBytes("%PDF-1.4 header"),
                overrideLength: LocalFileStorageService.MaxFileSize + 1);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.SaveFileAsync(oversizedPdf, "inst_1/teacher_1"));

            Assert.Contains("25MB", ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task Malformed_Truncated_Or_Corrupt_File_Header_Throws_InvalidOperationException()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileStorageService(tempFolder);

            // Truncated 2-byte PDF shorter than 4-byte %PDF signature
            var truncatedPdf = CreateMockFormFile("truncated.pdf", "application/pdf", new byte[] { 0x25, 0x50 });
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.SaveFileAsync(truncatedPdf, "inst_1/teacher_1"));

            // Corrupt PNG header
            var corruptPng = CreateMockFormFile("corrupt.png", "image/png", new byte[] { 0x89, 0x50, 0x00, 0x00 });
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.SaveFileAsync(corruptPng, "inst_1/teacher_1"));
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public async Task Path_Traversal_In_SaveFileAsync_DeleteFileAsync_And_FileName_Cannot_Escape_BaseStorage()
    {
        var outerFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var baseStorage = Path.Combine(outerFolder, "Storage", "Materials");
        Directory.CreateDirectory(outerFolder);
        var protectedOutsideFile = Path.Combine(outerFolder, "protected.pdf");
        await File.WriteAllTextAsync(protectedOutsideFile, "%PDF-1.4 do not delete");

        try
        {
            var storage = new LocalFileStorageService(baseStorage);
            var validPdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 valid content");
            var traversalFile = CreateMockFormFile(@"..\..\outside\evil.pdf", "application/pdf", validPdfBytes);

            var relativeSaved = await storage.SaveFileAsync(traversalFile, @"..\..\escaped_folder");
            Assert.DoesNotContain("..", relativeSaved);

            var (_, _, downloadedName) = await storage.GetFileAsync(relativeSaved, @"..\..\outside\evil.pdf", "application/pdf");
            Assert.Equal("evil.pdf", downloadedName);

            // Attempt to delete file outside baseStorage via path traversal must not delete protectedOutsideFile
            await storage.DeleteFileAsync("../../protected.pdf");
            Assert.True(File.Exists(protectedOutsideFile));
        }
        finally
        {
            if (Directory.Exists(outerFolder)) Directory.Delete(outerFolder, true);
        }
    }

    [Fact]
    public async Task CreateMaterialAsync_Serializes_Valid_Escaped_Audit_Metadata_Json()
    {
        using var db = CreateDb();
        var instId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = teacherUserId,
            FirstName = "Zeynep",
            LastName = "Hoca"
        };
        db.Set<Teacher>().Add(teacher);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.Roles.Returns([Roles.Teacher]);

        var storage = Substitute.For<IFileStorageService>();
        storage.SaveFileAsync(Arg.Any<IFormFile>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("inst_1/teacher_1/safe.pdf");

        var auditService = new AuditService(db);
        var handler = new MaterialsHandler(db, currentUser, storage, auditService);

        var specialTitle = "Türev \"Özel\" Fasikülü \\ Bölüm 1\nSatır 2";
        var rawFileName = @"..\..\path\turev_""ozel""_notlar.pdf";

        var created = await handler.CreateMaterialAsync(new CreateMaterialRequest(
            specialTitle,
            "Matematik",
            "Türev",
            "12. Sınıf",
            "fasikul",
            null,
            10,
            20,
            40,
            CreateMockFormFile(rawFileName, "application/pdf"),
            null
        ));

        Assert.Equal("turev_\"ozel\"_notlar.pdf", created.OriginalFileName);

        var auditLog = await db.Set<AuditLog>().SingleAsync(a => a.Action == "CreateMaterial" && a.EntityId == created.Id.ToString());
        Assert.NotNull(auditLog.MetadataJson);

        using var doc = JsonDocument.Parse(auditLog.MetadataJson!);
        Assert.Equal(specialTitle, doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("turev_\"ozel\"_notlar.pdf", doc.RootElement.GetProperty("fileName").GetString());
    }
}
