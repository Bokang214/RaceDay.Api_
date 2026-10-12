using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

/// <summary>Details needed to create an account.</summary>
public class RegisterRequest
{
    /// <summary>"Organiser" or "Participant".</summary>
    [Required]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "role must be 'Organiser' or 'Participant'.")]
    public string Role { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    // Required for a Participant, ignored for an Organiser.
    public DateOnly? DateOfBirth { get; set; }
}

public class RegisterResponse
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = "Logged in. A server-side session has been created.";
}
