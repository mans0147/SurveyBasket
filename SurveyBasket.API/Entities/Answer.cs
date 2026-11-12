namespace SurveyBasket.API.Entities;

public class Answer
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public ICollection<VoteAnswer> VoteAnswers { get; set; } = new List<VoteAnswer>();
}
