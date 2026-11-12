using SurveyBasket.API.Entities;

namespace SurveyBasket.API.Services;

public class PollService(ApplicationDbContext context) : IPollService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IEnumerable<Poll>> GetAllAsync(bool includeDrafts = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Polls.AsNoTracking()
            .Include(poll => poll.Questions)
            .ThenInclude(question => question.Answers)
            .AsQueryable();
        if (!includeDrafts)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(poll => poll.IsPublished && poll.StartsAt <= today && poll.EndsAt >= today);
        }

        return await query.OrderBy(poll => poll.StartsAt).ToListAsync(cancellationToken);
    }

    public async Task<Poll?> GetAsync(int id, bool includeDrafts = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Polls.AsNoTracking().Where(poll => poll.Id == id);
        if (!includeDrafts)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(poll => poll.IsPublished && poll.StartsAt <= today && poll.EndsAt >= today);
        }

        return await query.Include(poll => poll.Questions)
            .ThenInclude(question => question.Answers)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Poll> CreateAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        await _context.Polls.AddAsync(poll, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return poll;
    }

    public async Task<bool> UpdateAsync(int id, Poll poll, CancellationToken cancellationToken = default)
    {
        var currentPoll = await _context.Polls.SingleOrDefaultAsync(poll => poll.Id == id, cancellationToken);

        if (currentPoll is null)
            return false;

        currentPoll.Title = poll.Title;
        currentPoll.Summary = poll.Summary;
        currentPoll.IsPublished = poll.IsPublished;
        currentPoll.StartsAt = poll.StartsAt;
        currentPoll.EndsAt = poll.EndsAt;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var poll = await _context.Polls.SingleOrDefaultAsync(poll => poll.Id == id, cancellationToken);
        if (poll is null)
            return false;

        var voteIds = await _context.Votes.Where(vote => vote.PollId == id).Select(vote => vote.Id).ToListAsync(cancellationToken);
        var voteAnswers = await _context.VoteAnswers.Where(answer => voteIds.Contains(answer.VoteId)).ToListAsync(cancellationToken);
        _context.VoteAnswers.RemoveRange(voteAnswers);
        var votes = await _context.Votes.Where(vote => vote.PollId == id).ToListAsync(cancellationToken);
        _context.Votes.RemoveRange(votes);
        _context.Polls.Remove(poll);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TogglePublishStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var poll = await _context.Polls.SingleOrDefaultAsync(poll => poll.Id == id, cancellationToken);

        if (poll is null)
            return false;

        poll.IsPublished = !poll.IsPublished;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
