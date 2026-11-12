using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using SurveyBasket.API.Contracts.Authentication;
using SurveyBasket.API.Contracts.Users;
using SurveyBasket.API.Email;
using System.Text;

namespace SurveyBasket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("[controller]")]
public class AuthController(
    IAuthService authService,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!CanSendEmail())
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Email delivery is not configured." });

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(new ValidationProblemDetails(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description })));

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
        var confirmationLink = BuildEmailLink("confirm-email", new("userId", user.Id), new("token", encodedToken));

        try
        {
            await emailSender.SendAsync(user.Email!, "Confirm your SurveyBasket account",
                $"Open this link to confirm your account: {confirmationLink}", cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or SmtpException)
        {
            await userManager.DeleteAsync(user);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The confirmation email could not be sent. Try again later." });
        }

        return StatusCode(StatusCodes.Status202Accepted, new { message = "Check your email to confirm your account." });
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid confirmation token." });
        }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        return result.Succeeded ? Ok(new { message = "Email confirmed. You can now sign in." }) : BadRequest(result.Errors);
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!CanSendEmail())
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Email delivery is not configured." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return Accepted(new { message = "If the account needs confirmation, a new link has been sent." });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = BuildEmailLink("confirm-email", new("userId", user.Id), new("token", encodedToken));
        try
        {
            await emailSender.SendAsync(user.Email!, "Confirm your SurveyBasket account",
                $"Open this link to confirm your account: {confirmationLink}", cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or SmtpException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The confirmation email could not be sent. Try again later." });
        }

        return Accepted(new { message = "If the account needs confirmation, a new link has been sent." });
    }

    [HttpPost]
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var authResult = await authService.GetTokenAsync(request.Email, request.Password, cancellationToken);
        return authResult is null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(authResult);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null
            ? NotFound()
            : Ok(new UserResponse(user.Id, user.FirstName, user.LastName, user.Email!,
                user.LockoutEnd > DateTimeOffset.UtcNow, (await userManager.GetRolesAsync(user)).ToArray()));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length > 100)
            return BadRequest(new { message = "First and last names are required and must not exceed 100 characters." });

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded ? NoContent() : BadRequest(new ValidationProblemDetails(
            result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description })));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!CanSendEmail())
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Email delivery is not configured." });

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
            return Accepted(new { message = "If the account exists and is confirmed, password reset instructions have been sent." });

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
        var resetEndpoint = BuildEmailLink("reset-password");
        try
        {
            await emailSender.SendAsync(user.Email!, "Reset your SurveyBasket password",
                $"Submit a POST request to {resetEndpoint} with email, token, and your new password.\nEmail: {user.Email}\nToken: {encodedToken}",
                cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOperationException or SmtpException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Password reset email could not be sent. Try again later." });
        }

        return Accepted(new { message = "If the account exists and is confirmed, password reset instructions have been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] SurveyBasket.API.Contracts.Authentication.ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
            return BadRequest(new { message = "The password reset request is invalid or expired." });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "The password reset request is invalid or expired." });
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        return result.Succeeded ? NoContent() : BadRequest(new { message = "The password reset request is invalid or expired." });
    }

    private bool CanSendEmail() => emailOptions.Value.Enabled &&
        !string.IsNullOrWhiteSpace(emailOptions.Value.Host) &&
        !string.IsNullOrWhiteSpace(emailOptions.Value.From) &&
        Uri.TryCreate(emailOptions.Value.BaseUrl, UriKind.Absolute, out var baseUri) &&
        (baseUri.Scheme == Uri.UriSchemeHttps ||
            (baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback));

    private string BuildEmailLink(string endpoint, params KeyValuePair<string, string?>[] queryValues)
    {
        var baseUrl = new Uri(emailOptions.Value.BaseUrl, UriKind.Absolute).AbsoluteUri.TrimEnd('/');
        return $"{baseUrl}/api/auth/{endpoint}{QueryString.Create(queryValues)}";
    }
}
