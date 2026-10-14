using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Questions.Constants;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Questions.Features.CreateQuestion;
using EgitimPlatform.Modules.Questions.Features.GetQuestionById;
using EgitimPlatform.Modules.Questions.Features.GetQuestions;
using EgitimPlatform.Modules.Questions.Features.UpdateQuestion;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class SprintAQuestionStyleUnitTests
{
    private static ApplicationDbContext ModelContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static List<QuestionOptionDto> FiveValidOptions() =>
    [
        new("A", "Seçenek A", true),
        new("B", "Seçenek B", false),
        new("C", "Seçenek C", false),
        new("D", "Seçenek D", false),
        new("E", "Seçenek E", false),
    ];

    [Fact]
    public void ControlledStyles_ContainsAllInitialStylesWithStableCodesAndTurkishNames()
    {
        var styles = QuestionStyleDefaults.ControlledStyles;
        styles.Should().HaveCount(8);

        var expectedCodes = new[]
        {
            "NEW_GENERATION", "CONCEPTUAL", "CALCULATION", "INTERPRETATION",
            "GRAPH_TABLE", "PROBLEM", "VISUAL", "MULTI_STEP"
        };

        styles.Select(s => s.Code).Should().BeEquivalentTo(expectedCodes);
        styles.Select(s => s.Id).Distinct().Should().HaveCount(8);
        styles.Select(s => s.DisplayOrder).Should().BeInAscendingOrder();

        var newGen = styles.First(s => s.Code == "NEW_GENERATION");
        newGen.Name.Should().Be("Yeni Nesil");

        var graphTable = styles.First(s => s.Code == "GRAPH_TABLE");
        graphTable.Name.Should().Be("Grafik / Tablo");
    }

    [Fact]
    public void Model_ContainsQuestionStyle_And_CompositeKeyAssignment()
    {
        using var db = ModelContext();

        var styleType = db.Model.FindEntityType(typeof(QuestionStyle));
        styleType.Should().NotBeNull();
        styleType!.FindProperty("Code").Should().NotBeNull();
        styleType.FindIndex(styleType.FindProperty("Code")!)!.IsUnique.Should().BeTrue();

        var assignmentType = db.Model.FindEntityType(typeof(QuestionStyleAssignment));
        assignmentType.Should().NotBeNull();
        var primaryKey = assignmentType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Select(p => p.Name).Should().BeEquivalentTo(["QuestionId", "QuestionStyleId"]);
    }

    [Fact]
    public void CreateQuestionCommand_DuplicateStyleIds_FailsValidation()
    {
        var duplicateId = Guid.NewGuid();
        var command = new CreateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Soru metni", null, FiveValidOptions(),
            QuestionStatus.Draft, false,
            [duplicateId, duplicateId]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate question style IDs"));
    }

    [Fact]
    public void UpdateQuestionCommand_DuplicateStyleIds_FailsValidation()
    {
        var duplicateId = Guid.NewGuid();
        var command = new UpdateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Soru metni", null, FiveValidOptions(),
            QuestionStatus.Draft,
            [duplicateId, duplicateId]
        );

        var validator = new UpdateQuestionCommandValidator();
        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate question style IDs"));
    }

    [Fact]
    public async Task CreateQuestion_InvalidStyleId_ThrowsValidationException()
    {
        using var db = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.SubjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        catalog.ValidateSubjectTopicsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var audit = Substitute.For<IAuditService>();

        var handler = new CreateQuestionHandler(db, currentUser, catalog, audit);

        var nonExistentStyleId = Guid.NewGuid();
        var command = new CreateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Soru metni", null, FiveValidOptions(),
            QuestionStatus.Draft, true,
            [nonExistentStyleId]
        );

        var act = () => handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"*Question style with ID '{nonExistentStyleId}' does not exist.*");
    }

    [Fact]
    public async Task CreateQuestion_InactiveStyle_ThrowsValidationException()
    {
        using var db = CreateInMemoryDbContext();
        var inactiveStyle = new QuestionStyle
        {
            Id = Guid.NewGuid(),
            Code = "INACTIVE_STYLE",
            Name = "Pasif Tarz",
            IsActive = false
        };
        db.Set<QuestionStyle>().Add(inactiveStyle);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.SubjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        catalog.ValidateSubjectTopicsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var audit = Substitute.For<IAuditService>();

        var handler = new CreateQuestionHandler(db, currentUser, catalog, audit);

        var command = new CreateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Soru metni", null, FiveValidOptions(),
            QuestionStatus.Draft, true,
            [inactiveStyle.Id]
        );

        var act = () => handler.HandleAsync(command);
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"*Question style 'Pasif Tarz' (INACTIVE_STYLE) is inactive and cannot be assigned.*");
    }

    [Fact]
    public async Task CreateQuestion_WithMultipleStyles_PersistsAtomically()
    {
        using var db = CreateInMemoryDbContext();
        var style1 = new QuestionStyle { Id = Guid.NewGuid(), Code = "NEW_GEN", Name = "Yeni Nesil", IsActive = true };
        var style2 = new QuestionStyle { Id = Guid.NewGuid(), Code = "PROBLEM", Name = "Problem", IsActive = true };
        db.Set<QuestionStyle>().AddRange(style1, style2);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.SubjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        catalog.ValidateSubjectTopicsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var audit = Substitute.For<IAuditService>();

        var handler = new CreateQuestionHandler(db, currentUser, catalog, audit);

        var command = new CreateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Hard, QuestionSourceType.InstitutionCustom, null,
            "Geometri sorusu", null, FiveValidOptions(),
            QuestionStatus.Draft, true,
            [style1.Id, style2.Id]
        );

        var questionId = await handler.HandleAsync(command);

        var created = await db.Set<Question>()
            .Include(q => q.StyleAssignments)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        created.Should().NotBeNull();
        created!.StyleAssignments.Should().HaveCount(2);
        created.StyleAssignments.Select(sa => sa.QuestionStyleId).Should().BeEquivalentTo([style1.Id, style2.Id]);
    }

    [Fact]
    public async Task CreateQuestion_ZeroStyles_RemainsValidForBackwardCompatibility()
    {
        using var db = CreateInMemoryDbContext();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.SubjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        catalog.ValidateSubjectTopicsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var audit = Substitute.For<IAuditService>();

        var handler = new CreateQuestionHandler(db, currentUser, catalog, audit);

        // Without specifying styles (null)
        var command = new CreateQuestionCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            DifficultyLevel.Easy, QuestionSourceType.InstitutionCustom, null,
            "Eski tarz soru", null, FiveValidOptions(),
            QuestionStatus.Draft, true,
            null
        );

        var questionId = await handler.HandleAsync(command);

        var created = await db.Set<Question>()
            .Include(q => q.StyleAssignments)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        created.Should().NotBeNull();
        created!.StyleAssignments.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateQuestion_PublishedOrUsedInExam_RejectsStyleChangesWithConflictException()
    {
        using var db = CreateInMemoryDbContext();
        var style = new QuestionStyle { Id = Guid.NewGuid(), Code = "STYLE_A", Name = "Tarz A", IsActive = true };
        db.Set<QuestionStyle>().Add(style);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            TopicId = Guid.NewGuid(),
            Content = "Immutable Published Question",
            Status = QuestionStatus.Published // Published!
        };
        db.Set<Question>().Add(question);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        var audit = Substitute.For<IAuditService>();
        var usageChecker = Substitute.For<IQuestionUsageChecker>();
        usageChecker.IsQuestionLinkedToExamAsync(question.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new UpdateQuestionHandler(db, currentUser, catalog, audit, usageChecker);

        var updateCommand = new UpdateQuestionCommand(
            question.SubjectId, question.TopicId, null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Yeni metin", null, FiveValidOptions(),
            QuestionStatus.Published,
            [style.Id]
        );

        var act = () => handler.HandleAsync(question.Id, updateCommand);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*immutable*");
    }

    [Fact]
    public async Task UpdateQuestion_DraftQuestion_SuccessfullyUpdatesStyles()
    {
        using var db = CreateInMemoryDbContext();
        var style1 = new QuestionStyle { Id = Guid.NewGuid(), Code = "STYLE_1", Name = "Tarz 1", IsActive = true };
        var style2 = new QuestionStyle { Id = Guid.NewGuid(), Code = "STYLE_2", Name = "Tarz 2", IsActive = true };
        db.Set<QuestionStyle>().AddRange(style1, style2);

        var question = new Question
        {
            Id = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            TopicId = Guid.NewGuid(),
            Content = "Draft Question",
            Status = QuestionStatus.Draft
        };
        question.StyleAssignments.Add(new QuestionStyleAssignment
        {
            QuestionId = question.Id,
            QuestionStyleId = style1.Id
        });
        db.Set<Question>().Add(question);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);
        var catalog = Substitute.For<IAcademicCatalog>();
        catalog.SubjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        catalog.ValidateSubjectTopicsAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        var audit = Substitute.For<IAuditService>();
        var usageChecker = Substitute.For<IQuestionUsageChecker>();
        usageChecker.IsQuestionLinkedToExamAsync(question.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new UpdateQuestionHandler(db, currentUser, catalog, audit, usageChecker);

        // Update style from style1 to style2
        var updateCommand = new UpdateQuestionCommand(
            question.SubjectId, question.TopicId, null, null,
            DifficultyLevel.Medium, QuestionSourceType.InstitutionCustom, null,
            "Draft Question Updated", null, FiveValidOptions(),
            QuestionStatus.Draft,
            [style2.Id]
        );

        await handler.HandleAsync(question.Id, updateCommand);

        var updated = await db.Set<Question>()
            .Include(q => q.StyleAssignments)
            .FirstOrDefaultAsync(q => q.Id == question.Id);

        updated.Should().NotBeNull();
        updated!.StyleAssignments.Should().HaveCount(1);
        updated.StyleAssignments.First().QuestionStyleId.Should().Be(style2.Id);
    }
}
