using System.ComponentModel.DataAnnotations;

public record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password);

public record LoginRequest(
    [Required, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);
