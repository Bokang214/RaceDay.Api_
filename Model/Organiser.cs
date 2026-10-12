namespace RaceDay.Api.Models;

// Matches Organisers table in database_schema.sql
public class Organiser
{
    public int OrganiserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
