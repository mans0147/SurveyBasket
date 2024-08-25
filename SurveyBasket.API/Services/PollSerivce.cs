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
}
