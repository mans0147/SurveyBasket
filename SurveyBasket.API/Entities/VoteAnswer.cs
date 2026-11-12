namespace SurveyBasket.API.Entities;

public class VoteAnswer
{
    public int VoteId { get; set; }
    public Vote Vote { get; set; } = null!;
    public int AnswerId { get; set; }
    public Answer Answer { get; set; } = null!;
}
