namespace RaceDay.Api.DTOs;

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
}

public class EnrolmentDetailResponse : EnrolmentResponse
{
    public string ParticipantName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}
