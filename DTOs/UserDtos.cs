using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ProfileResponse
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; } // Participant only
}

public class UpdateProfileRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? FullName { get; set; }

    // Participant only.
    public DateOnly? DateOfBirth { get; set; }
}
