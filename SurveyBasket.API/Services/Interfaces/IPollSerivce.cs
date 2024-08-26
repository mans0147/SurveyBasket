namespace SurveyBasket.API.Services.Interfaces;

public interface IPollSerivce
{
    IEnumerable<Poll> GetAll();
    Poll? Get(int id);
    Poll Create(Poll poll);
    bool Update(int id, Poll poll);
    bool Delete(int id);
}
