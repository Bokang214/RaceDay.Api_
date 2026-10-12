namespace RaceDay.Api.Models;

// Matches Users table in database_schema.sql.

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Participant";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateOnly? DateOfBirth { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
