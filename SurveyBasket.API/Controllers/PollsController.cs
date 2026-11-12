using Microsoft.AspNetCore.Authorization;
using SurveyBasket.API.Authorization;
using SurveyBasket.API.Contracts.Polls;

namespace SurveyBasket.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PollsController(IPollService pollService, IPollNotificationService notifications, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailable(CancellationToken cancellationToken)
    {
        var polls = await pollService.GetAllAsync(cancellationToken: cancellationToken);
        return Ok(polls.Select(ToResponse));
    }

    [HttpGet("manage")]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var polls = await pollService.GetAllAsync(includeDrafts: true, cancellationToken);
        return Ok(polls.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        var poll = await pollService.GetAsync(id, cancellationToken: cancellationToken);
        return poll is null ? NotFound() : Ok(ToResponse(poll));
    }

    [HttpGet("manage/{id:int}")]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> GetForManagement([FromRoute] int id, CancellationToken cancellationToken)
    {
        var poll = await pollService.GetAsync(id, includeDrafts: true, cancellationToken);
        return poll is null ? NotFound() : Ok(ToResponse(poll));
    }

    [HttpPost]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] PollRequest request, CancellationToken cancellationToken)
    {
        if (request.IsPublished && request.Questions is not { Count: > 0 })
            return BadRequest(new { message = "A poll needs at least one question before it can be published." });

        var title = request.Title.Trim();
        if (await dbContext.Polls.AnyAsync(poll => poll.Title == title, cancellationToken))
            return Conflict(new { message = "A poll with this title already exists." });

        var poll = new Poll
        {
            Title = title,
            Summary = request.Summary.Trim(),
            IsPublished = request.IsPublished,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Questions = request.Questions?.Select(ToEntity).ToList() ?? []
        };

        var created = await pollService.CreateAsync(poll, cancellationToken);
        if (created.IsPublished)
            await notifications.NotifyPublishedAsync(created, cancellationToken);
        return CreatedAtAction(nameof(GetForManagement), new { id = created.Id }, ToResponse(created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PollRequest request, CancellationToken cancellationToken)
    {
        if (request.IsPublished)
        {
            var currentPoll = await pollService.GetAsync(id, includeDrafts: true, cancellationToken);
            if (currentPoll is null)
                return NotFound();
            if (currentPoll.Questions.Count == 0)
                return BadRequest(new { message = "A poll needs at least one question before it can be published." });
        }

        var current = await pollService.GetAsync(id, includeDrafts: true, cancellationToken);
        if (current is null)
            return NotFound();

        var title = request.Title.Trim();
        if (await dbContext.Polls.AnyAsync(poll => poll.Id != id && poll.Title == title, cancellationToken))
            return Conflict(new { message = "A poll with this title already exists." });

        var shouldNotify = !current.IsPublished && request.IsPublished;
        var updated = await pollService.UpdateAsync(id, new Poll
        {
            Title = title,
            Summary = request.Summary.Trim(),
            IsPublished = request.IsPublished,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        }, cancellationToken);

        if (!updated)
            return NotFound();
        if (shouldNotify)
        {
            current.IsPublished = true;
            await notifications.NotifyPublishedAsync(current, cancellationToken);
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken) =>
        await pollService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("{id:int}/publish")]
    [Authorize(Roles = SurveyBasketRoles.Admin)]
    public async Task<IActionResult> TogglePublish([FromRoute] int id, CancellationToken cancellationToken)
    {
        var poll = await pollService.GetAsync(id, includeDrafts: true, cancellationToken);
        if (poll is null)
            return NotFound();
        if (!poll.IsPublished && poll.Questions.Count == 0)
            return BadRequest(new { message = "A poll needs at least one question before it can be published." });

        await pollService.TogglePublishStatusAsync(id, cancellationToken);
        if (!poll.IsPublished)
        {
            poll.IsPublished = true;
            await notifications.NotifyPublishedAsync(poll, cancellationToken);
        }
        return NoContent();
    }

    private static Question ToEntity(QuestionRequest request) => new()
    {
        Content = request.Content.Trim(),
        Answers = request.Answers.Select(answer => new Answer { Content = answer.Trim() }).ToList()
    };

    private static PollResponse ToResponse(Poll poll) => new(
        poll.Id,
        poll.Title,
        poll.Summary,
        poll.IsPublished,
        poll.StartsAt,
        poll.EndsAt,
        poll.Questions.Select(question => new QuestionResponse(
            question.Id,
            question.Content,
            question.Answers.Select(answer => new AnswerResponse(answer.Id, answer.Content)).ToArray())).ToArray());
}
