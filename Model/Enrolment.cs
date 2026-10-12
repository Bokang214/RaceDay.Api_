namespace RaceDay.Api.Models;

// Matches Enrolments table in database_schema.sql.
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}
