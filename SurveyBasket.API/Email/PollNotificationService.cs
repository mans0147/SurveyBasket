using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SurveyBasket.API.Email;

public sealed class PollNotificationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<PollNotificationService> logger) : IPollNotificationService
{
    public async Task NotifyPublishedAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        if (!emailOptions.Value.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(emailOptions.Value.Host) || string.IsNullOrWhiteSpace(emailOptions.Value.From))
        {
            logger.LogWarning("Poll {PollId} was published but email notifications are not configured.", poll.Id);
            return;
        }

        var recipients = await userManager.Users
            .Where(user => user.EmailConfirmed && user.Email != null)
            .Select(user => user.Email!)
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            try
            {
                await emailSender.SendAsync(recipient, $"New survey: {poll.Title}",
                    $"A new survey is available.\n\n{poll.Title}\n{poll.Summary}\n\nOpen the SurveyBasket API to participate.", cancellationToken);
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.Net.Mail.SmtpException)
            {
                logger.LogWarning(exception, "Failed to notify a confirmed account about poll {PollId}.", poll.Id);
            }
        }
    }
}
