using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CreateResultRequest
{
    [Required]
    public TimeOnly FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

public class UpdateResultRequest
{
    public TimeOnly? FinishTime { get; set; }
    [Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class LeaderboardEntryResponse
{
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? Position { get; set; }
}
