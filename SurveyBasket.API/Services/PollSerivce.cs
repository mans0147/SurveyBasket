namespace SurveyBasket.API.Services;

public class PollSerivce : IPollSerivce
{
    private static readonly List<Poll> _polls = [
       new Poll{
            Id = 1,
            Title = "Poll One",
            Description = "Poll One Description"
       },
        new Poll{
            Id = 2,
            Title = "Poll Two",
            Description = "Poll Two Description"
        }

       ];

    public IEnumerable<Poll> GetAll() => _polls;

    public Poll? Get(int id) => _polls.SingleOrDefault(pol => pol.Id == id);

    public Poll Create(Poll poll)
    {
        poll.Id = _polls.Count + 1;
        _polls.Add(poll);
        return poll;
    }

    public bool Update(int id, Poll poll)
    {
        var currentPoll = Get(id);

        if(currentPoll is null)
            return false;

        currentPoll.Title = poll.Title;
        currentPoll.Description = poll.Description;

        return true;
    }

    public bool Delete(int id)
    {
        var Poll = Get(id);
        if(Poll is null)
            return false;

        _polls.Remove(Poll);
            return true;
    }
}
