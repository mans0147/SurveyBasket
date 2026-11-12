using Microsoft.AspNetCore.Authorization;
using SurveyBasket.API.Authorization;
using SurveyBasket.API.Contracts.Polls;

namespace SurveyBasket.API.Controllers;

[ApiController]
[Route("api/polls/{pollId:int}/questions")]
[Authorize(Roles = SurveyBasketRoles.Admin)]
public class QuestionsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromRoute] int pollId, [FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Polls.AnyAsync(poll => poll.Id == pollId, cancellationToken))
            return NotFound(new { message = "Poll was not found." });

        if (await dbContext.Votes.AnyAsync(vote => vote.PollId == pollId, cancellationToken))
            return Conflict(new { message = "Questions cannot be added after voting has started." });

        var question = new Question
        {
            PollId = pollId,
            Content = request.Content.Trim(),
            Answers = request.Answers.Select(answer => new Answer { Content = answer.Trim() }).ToList()
        };

        dbContext.Questions.Add(question);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { pollId, questionId = question.Id }, ToResponse(question));
    }

    [HttpGet("{questionId:int}")]
    public async Task<IActionResult> Get([FromRoute] int pollId, [FromRoute] int questionId, CancellationToken cancellationToken)
    {
        var question = await dbContext.Questions.AsNoTracking()
            .Include(question => question.Answers)
            .SingleOrDefaultAsync(question => question.Id == questionId && question.PollId == pollId, cancellationToken);
        return question is null ? NotFound() : Ok(ToResponse(question));
    }

    [HttpPut("{questionId:int}")]
    public async Task<IActionResult> Update([FromRoute] int pollId, [FromRoute] int questionId,
        [FromBody] QuestionRequest request, CancellationToken cancellationToken)
    {
        var question = await dbContext.Questions.Include(question => question.Answers)
            .SingleOrDefaultAsync(question => question.Id == questionId && question.PollId == pollId, cancellationToken);
        if (question is null)
            return NotFound();

        if (await dbContext.VoteAnswers.AnyAsync(voteAnswer => voteAnswer.Answer.QuestionId == questionId, cancellationToken))
            return Conflict(new { message = "Questions with recorded votes cannot be edited." });

        question.Content = request.Content.Trim();
        dbContext.Answers.RemoveRange(question.Answers);
        question.Answers = request.Answers.Select(answer => new Answer { Content = answer.Trim() }).ToList();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(question));
    }

    [HttpDelete("{questionId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int pollId, [FromRoute] int questionId, CancellationToken cancellationToken)
    {
        var question = await dbContext.Questions.SingleOrDefaultAsync(
            question => question.Id == questionId && question.PollId == pollId, cancellationToken);
        if (question is null)
            return NotFound();

        if (await dbContext.VoteAnswers.AnyAsync(voteAnswer => voteAnswer.Answer.QuestionId == questionId, cancellationToken))
            return Conflict(new { message = "Questions with recorded votes cannot be deleted." });

        dbContext.Questions.Remove(question);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static QuestionResponse ToResponse(Question question) => new(
        question.Id,
        question.Content,
        question.Answers.Select(answer => new AnswerResponse(answer.Id, answer.Content)).ToArray());
}
