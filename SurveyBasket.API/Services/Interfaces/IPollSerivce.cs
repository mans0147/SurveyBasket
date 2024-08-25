namespace SurveyBasket.API.Services.Interfaces;

public interface IPollSerivce
{
    IEnumerable<Poll> GetAll();
    Poll? Get(int id);
    Poll Create(Poll poll);
    //Poll Update(Poll poll);
    //void Delete(int id);
}
