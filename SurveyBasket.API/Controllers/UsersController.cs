using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SurveyBasket.API.Authorization;
using SurveyBasket.API.Contracts.Users;
using System.Security.Claims;

namespace SurveyBasket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SurveyBasketRoles.Admin)]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var response = new List<UserResponse>(users.Count);
        foreach (var user in users)
            response.Add(await ToResponseAsync(user));

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get([FromRoute] string id)
    {
        var user = await userManager.FindByIdAsync(id);
        return user is null ? NotFound() : Ok(await ToResponseAsync(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var role = NormalizeRole(request.Role);
        if (role is null)
            return BadRequest(new { message = "Role must be Admin or User." });

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return BadRequest(created.Errors);

        var assigned = await userManager.AddToRoleAsync(user, role);
        if (!assigned.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest(assigned.Errors);
        }

        return CreatedAtAction(nameof(Get), new { id = user.Id }, await ToResponseAsync(user));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] string id, [FromBody] UpdateUserRequest request)
    {
        var role = NormalizeRole(request.Role);
        if (role is null)
            return BadRequest(new { message = "Role must be Admin or User." });

        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier) && (request.IsLocked || role != SurveyBasketRoles.Admin))
            return Conflict(new { message = "You cannot lock or remove administrator access from your own account." });

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if ((request.IsLocked || role != SurveyBasketRoles.Admin) &&
            await userManager.IsInRoleAsync(user, SurveyBasketRoles.Admin) &&
            (await userManager.GetUsersInRoleAsync(SurveyBasketRoles.Admin)).Count <= 1)
            return Conflict(new { message = "The last administrator cannot be locked or demoted." });

        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length > 100)
            return BadRequest(new { message = "First and last names are required and must not exceed 100 characters." });

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.LockoutEnabled = true;
        user.LockoutEnd = request.IsLocked ? DateTimeOffset.MaxValue : null;

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
            return BadRequest(updated.Errors);

        var currentRoles = await userManager.GetRolesAsync(user);
        var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removed.Succeeded)
            return BadRequest(removed.Errors);

        var assigned = await userManager.AddToRoleAsync(user, role);
        return assigned.Succeeded ? Ok(await ToResponseAsync(user)) : BadRequest(assigned.Errors);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Conflict(new { message = "You cannot delete your own account." });

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (await userManager.IsInRoleAsync(user, SurveyBasketRoles.Admin) &&
            (await userManager.GetUsersInRoleAsync(SurveyBasketRoles.Admin)).Count <= 1)
            return Conflict(new { message = "The last administrator cannot be deleted." });

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword([FromRoute] string id, [FromBody] AdminResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    private async Task<UserResponse> ToResponseAsync(ApplicationUser user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.Email!,
        user.LockoutEnd > DateTimeOffset.UtcNow,
        (await userManager.GetRolesAsync(user)).ToArray());

    private static string? NormalizeRole(string role) =>
        string.Equals(role, SurveyBasketRoles.Admin, StringComparison.OrdinalIgnoreCase) ? SurveyBasketRoles.Admin :
        string.Equals(role, SurveyBasketRoles.User, StringComparison.OrdinalIgnoreCase) ? SurveyBasketRoles.User : null;
}
