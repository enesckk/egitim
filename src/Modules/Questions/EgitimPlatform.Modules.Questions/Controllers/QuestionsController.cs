using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Questions.Features.ArchiveQuestion;
using EgitimPlatform.Modules.Questions.Features.CreateQuestion;
using EgitimPlatform.Modules.Questions.Features.GetQuestionById;
using EgitimPlatform.Modules.Questions.Features.GetQuestions;
using EgitimPlatform.Modules.Questions.Features.GetQuestionStyles;
using EgitimPlatform.Modules.Questions.Features.UpdateQuestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Questions.Controllers;

[ApiController]
[Route("api/v1/questions")]
public class QuestionsController : ControllerBase
{
    [HttpGet("/api/v1/question-styles")]
    [HttpGet("styles")]
    [Authorize(Policy = Policies.CanViewQuestions)]
    public async Task<ActionResult<List<QuestionStyleDto>>> GetQuestionStyles(
        [FromServices] GetQuestionStylesHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageQuestions)]
    public async Task<ActionResult<Guid>> CreateQuestion(
        [FromBody] CreateQuestionCommand command,
        [FromServices] CreateQuestionHandler handler,
        CancellationToken ct)
    {
        var questionId = await handler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetQuestionById), new { questionId }, questionId);
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewQuestions)]
    public async Task<ActionResult<PaginatedList<QuestionDto>>> GetQuestions(
        [FromQuery] GetQuestionsQuery query,
        [FromServices] GetQuestionsHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{questionId:guid}")]
    [Authorize(Policy = Policies.CanViewQuestions)]
    public async Task<ActionResult<QuestionDetailDto>> GetQuestionById(
        [FromRoute] Guid questionId,
        [FromServices] GetQuestionByIdHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(questionId, ct);
        return Ok(result);
    }

    [HttpPut("{questionId:guid}")]
    [Authorize(Policy = Policies.CanManageQuestions)]
    public async Task<ActionResult> UpdateQuestion(
        [FromRoute] Guid questionId,
        [FromBody] UpdateQuestionCommand command,
        [FromServices] UpdateQuestionHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(questionId, command, ct);
        return NoContent();
    }

    [HttpPost("{questionId:guid}/archive")]
    [Authorize(Policy = Policies.CanManageQuestions)]
    public async Task<ActionResult> ArchiveQuestion(
        [FromRoute] Guid questionId,
        [FromServices] ArchiveQuestionHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(questionId, ct);
        return NoContent();
    }
}
