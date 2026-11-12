namespace SurveyBasket.API.Email;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IPollNotificationService
{
    Task NotifyPublishedAsync(Poll poll, CancellationToken cancellationToken = default);
}
