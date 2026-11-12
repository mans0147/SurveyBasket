namespace SurveyBasket.API.Entities;

public class Vote
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime SubmittedAtUtc { get; set; }
    public ICollection<VoteAnswer> Answers { get; set; } = new List<VoteAnswer>();
}
