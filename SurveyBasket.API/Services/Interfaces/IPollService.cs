namespace SurveyBasket.API.Services.Interfaces;

public interface IPollService
{
    Task<IEnumerable<Poll>> GetAllAsync(bool includeDrafts = false, CancellationToken cancellationToken = default);
    Task<Poll?> GetAsync(int id, bool includeDrafts = false, CancellationToken cancellationToken = default);
    Task<Poll> CreateAsync(Poll poll, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, Poll poll, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> TogglePublishStatusAsync(int id, CancellationToken cancellationToken = default);
}
