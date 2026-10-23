using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Notebook.Features.ManageNotebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EgitimPlatform.Modules.Notebook.Controllers;

[ApiController]
[Route("api/v1/notebook")]
[Authorize(Policy = Policies.CanManageNotebook)]
public class NotebookController(NotebookHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedList<NotebookEntryDto>>> List(
        [FromQuery] ListNotebookEntriesQuery query,
        CancellationToken ct)
    {
        var result = await handler.ListAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotebookEntryDto>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await handler.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<NotebookEntryDto>> Create(
        [FromBody] CreateNotebookEntryCommand command,
        CancellationToken ct)
    {
        var result = await handler.CreateAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NotebookEntryDto>> Update(
        Guid id,
        [FromBody] UpdateNotebookEntryRequest request,
        CancellationToken ct)
    {
        var command = new UpdateNotebookEntryCommand(
            id,
            request.Title,
            request.Content,
            request.SubjectId,
            request.TopicId,
            request.Tags,
            request.IsPinned);

        var result = await handler.UpdateAsync(command, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        await handler.DeleteAsync(id, ct);
        return NoContent();
    }
}
