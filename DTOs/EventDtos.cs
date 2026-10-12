using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EventListItemResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class EventDetailResponse : EventListItemResponse
{
    public string? Description { get; set; }
    public int OrganiserId { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();
}

public class CreateEventRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.01, 99999.99)]
    public decimal Distance { get; set; }

    /// <summary>Run, Walk or Cycle (case-insensitive).</summary>
    [Required, RegularExpression(EventTypes.Pattern, ErrorMessage = "eventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public class UpdateEventRequest
{
    [StringLength(150, MinimumLength = 1)]
    public string? EventName { get; set; }
    public string? Description { get; set; }
    public DateOnly? EventDate { get; set; }
    [StringLength(200, MinimumLength = 1)]
    public string? Location { get; set; }
    [Range(0.01, 99999.99)]
    public decimal? Distance { get; set; }
    /// <summary>Run, Walk or Cycle (case-insensitive).</summary>
    [RegularExpression(EventTypes.Pattern, ErrorMessage = "eventType must be Run, Walk or Cycle.")]
    public string? EventType { get; set; }
}

/// <summary>Allowed event types (Part 2 brief: run, walk or cycle).</summary>
public static class EventTypes
{
    public const string Pattern = "^(?i:run|walk|cycle)$";

    public static string Normalise(string value) => value.Trim().ToLowerInvariant() switch
    {
        "run" => "Run",
        "walk" => "Walk",
        "cycle" => "Cycle",
        _ => value
    };
}
