using EgitimPlatform.Modules.Coaching.Features.ManageNotes;
using EgitimPlatform.Modules.Notebook.Features.ManageNotebook;
using FluentValidation.TestHelper;
using Xunit;

namespace EgitimPlatform.Unit.Validators;

public class NotebookAndCoachNotesValidatorTests
{
    private readonly CreateNotebookEntryValidator _createNotebookValidator = new();
    private readonly UpdateNotebookEntryValidator _updateNotebookValidator = new();
    private readonly CreateCoachPrivateNoteValidator _createCoachNoteValidator = new();
    private readonly UpdateCoachPrivateNoteValidator _updateCoachNoteValidator = new();

    [Fact]
    public void CreateNotebookEntry_WithValidData_ShouldPassValidation()
    {
        var command = new CreateNotebookEntryCommand(
            Title: "TYT Matematik Formülleri",
            Content: "İkinci dereceden denklemler: ax^2 + bx + c = 0, delta = b^2 - 4ac",
            Tags: "Matematik, TYT");

        var result = _createNotebookValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateNotebookEntry_WithEmptyTitle_ShouldFailValidation()
    {
        var command = new CreateNotebookEntryCommand(
            Title: "",
            Content: "Bazı içerikler...");

        var result = _createNotebookValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateNotebookEntry_WithEmptyContent_ShouldFailValidation()
    {
        var command = new CreateNotebookEntryCommand(
            Title: "Ders Notu",
            Content: "");

        var result = _createNotebookValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void UpdateNotebookEntry_WithEmptyId_ShouldFailValidation()
    {
        var command = new UpdateNotebookEntryCommand(
            Id: Guid.Empty,
            Title: "Başlık",
            Content: "İçerik");

        var result = _updateNotebookValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("Akademik")]
    [InlineData("Motivasyon")]
    [InlineData("Veli Görüşmesi")]
    [InlineData("Genel")]
    public void CreateCoachPrivateNote_WithValidCategories_ShouldPassValidation(string category)
    {
        var command = new CreateCoachPrivateNoteCommand(
            StudentId: Guid.NewGuid(),
            Category: category,
            Content: "Öğrenci son denemede netlerini 10 net artırdı.");

        var result = _createCoachNoteValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCoachPrivateNote_WithInvalidCategory_ShouldFailValidation()
    {
        var command = new CreateCoachPrivateNoteCommand(
            StudentId: Guid.NewGuid(),
            Category: "BilinmeyenKategori",
            Content: "Not içeriği...");

        var result = _createCoachNoteValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void CreateCoachPrivateNote_WithEmptyContent_ShouldFailValidation()
    {
        var command = new CreateCoachPrivateNoteCommand(
            StudentId: Guid.NewGuid(),
            Category: "Akademik",
            Content: "");

        var result = _createCoachNoteValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void UpdateCoachPrivateNote_WithValidData_ShouldPassValidation()
    {
        var command = new UpdateCoachPrivateNoteCommand(
            NoteId: Guid.NewGuid(),
            Category: "Motivasyon",
            Content: "Öğrencinin sınav kaygısı hafifledi.");

        var result = _updateCoachNoteValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
