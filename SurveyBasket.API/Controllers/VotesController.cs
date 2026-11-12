using Microsoft.AspNetCore.Authorization;
using SurveyBasket.API.Contracts.Votes;
using System.Security.Claims;

namespace SurveyBasket.API.Controllers;

[ApiController]
[Route("api/polls/{pollId:int}")]
[Authorize]
public class VotesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("votes")]
    public async Task<IActionResult> Submit([FromRoute] int pollId, [FromBody] SubmitVoteRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var poll = await dbContext.Polls.Include(poll => poll.Questions)
            .SingleOrDefaultAsync(poll => poll.Id == pollId && poll.IsPublished &&
                poll.StartsAt <= today && poll.EndsAt >= today, cancellationToken);
        if (poll is null)
            return NotFound(new { message = "No active poll was found." });

        if (await dbContext.Votes.AnyAsync(vote => vote.PollId == pollId && vote.UserId == userId, cancellationToken))
            return Conflict(new { message = "You have already voted in this poll." });

        var answerIds = request.AnswerIds.Distinct().ToArray();
        if (answerIds.Length != request.AnswerIds.Count || answerIds.Length != poll.Questions.Count)
            return BadRequest(new { message = "Choose exactly one answer for every question." });

        var answers = await dbContext.Answers.Include(answer => answer.Question)
            .Where(answer => answerIds.Contains(answer.Id) && answer.Question.PollId == pollId)
            .ToListAsync(cancellationToken);
        if (answers.Count != answerIds.Length || answers.Select(answer => answer.QuestionId).Distinct().Count() != poll.Questions.Count)
            return BadRequest(new { message = "One or more selected answers do not belong to a question in this poll." });

        var vote = new Vote
        {
            PollId = pollId,
            UserId = userId,
            SubmittedAtUtc = DateTime.UtcNow,
            Answers = answers.Select(answer => new VoteAnswer { AnswerId = answer.Id }).ToList()
        };

        dbContext.Votes.Add(vote);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(vote).State = EntityState.Detached;
            if (await dbContext.Votes.AnyAsync(existing => existing.PollId == pollId && existing.UserId == userId, cancellationToken))
                return Conflict(new { message = "You have already voted in this poll." });
            throw;
        }

        return CreatedAtAction(nameof(GetResults), new { pollId }, new { vote.Id, vote.SubmittedAtUtc });
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults([FromRoute] int pollId, CancellationToken cancellationToken)
    {
        var poll = await dbContext.Polls.AsNoTracking().Include(poll => poll.Questions)
            .ThenInclude(question => question.Answers)
            .SingleOrDefaultAsync(poll => poll.Id == pollId, cancellationToken);
        if (poll is null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(SurveyBasket.API.Authorization.SurveyBasketRoles.Admin) &&
            (userId is null || !await dbContext.Votes.AnyAsync(vote => vote.PollId == pollId && vote.UserId == userId, cancellationToken)))
            return Forbid();

        var totalVotes = await dbContext.Votes.CountAsync(vote => vote.PollId == pollId, cancellationToken);
        var counts = await dbContext.VoteAnswers
            .Where(voteAnswer => voteAnswer.Vote.PollId == pollId)
            .GroupBy(voteAnswer => voteAnswer.AnswerId)
            .Select(group => new { AnswerId = group.Key, Votes = group.Count() })
            .ToDictionaryAsync(item => item.AnswerId, item => item.Votes, cancellationToken);

        var response = new VoteResultResponse(
            poll.Id,
            poll.Title,
            totalVotes,
            poll.Questions.Select(question => new QuestionResultResponse(
                question.Id,
                question.Content,
                question.Answers.Select(answer =>
                {
                    var answerVotes = counts.GetValueOrDefault(answer.Id);
                    var percentage = totalVotes == 0 ? 0 : Math.Round(answerVotes * 100d / totalVotes, 2);
                    return new AnswerResultResponse(answer.Id, answer.Content, answerVotes, percentage);
                }).ToArray())).ToArray());

        return Ok(response);
    }
}
