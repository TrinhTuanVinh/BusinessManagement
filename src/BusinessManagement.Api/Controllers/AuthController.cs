
using BusinessManagement.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request, CancellationToken ct)
        => StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request.Email, request.Password, ct));

    [HttpPost("login"), AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var token = await auth.LoginAsync(request.Email, request.Password, ct);
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc));
    }

    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirst("sub")?.Value,
        email = User.FindFirst("email")?.Value,
        role = User.FindFirst("role")?.Value
    });
}