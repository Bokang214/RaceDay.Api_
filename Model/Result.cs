namespace RaceDay.Api.Models;

// Matches Results table in database_schema.sql
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}
