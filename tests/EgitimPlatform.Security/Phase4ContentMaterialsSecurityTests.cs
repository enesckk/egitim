using System.Security;
using System.Text;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Content.Entities;
using EgitimPlatform.Modules.Content.Features.ManageMaterials;
using EgitimPlatform.Modules.Content.Services;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Teachers.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Phase4ContentMaterialsSecurityTests
{
    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private IFormFile CreateMockFormFile(string fileName = "test_material.pdf", string contentType = "application/pdf", byte[]? content = null)
    {
        content ??= Encoding.UTF8.GetBytes("%PDF-1.4 Mock PDF Content for testing");
        var stream = new MemoryStream(content);
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.Length.Returns(content.Length);
        file.ContentType.Returns(contentType);
        file.OpenReadStream().Returns(stream);
        file.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(async (ci) =>
            {
                var targetStream = ci.Arg<Stream>();
                await stream.CopyToAsync(targetStream);
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
}
