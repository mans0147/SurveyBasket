namespace SurveyBasket.API.Entities;

public class Question
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
