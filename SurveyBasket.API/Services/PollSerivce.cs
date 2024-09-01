using SurveyBasket.API.Entities;

namespace SurveyBasket.API.Services;

public class PollSerivce(ApplicationDbContext context) : IPollSerivce
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IEnumerable<Poll>> GetAllAsync(CancellationToken cancellationToken = default) =>  
        await _context.Polls.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Poll?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Polls.FindAsync(id, cancellationToken);

    public async Task<Poll> CreateAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        await _context.Polls.AddAsync(poll, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return poll;
    }

    public async Task<bool> UpdateAsync(int id, Poll poll, CancellationToken cancellationToken = default)
    {
        var currentPoll = await GetAsync(id, cancellationToken);

        if (currentPoll is null)
            return false;

        currentPoll.Title = poll.Title;
        currentPoll.Summary = poll.Summary;
        currentPoll.StartsAt = poll.StartsAt;
        currentPoll.EndsAt = poll.EndsAt;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var Poll = await GetAsync(id, cancellationToken);
        if (Poll is null)
            return false;

        _context.Remove(Poll);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> TogglePublishStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        var Poll = await GetAsync(id, cancellationToken);

        if (Poll is null)
            return false;

        Poll.IsPublished = !Poll.IsPublished;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
