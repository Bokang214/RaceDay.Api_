using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string CategoryName { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? CategoryName { get; set; }
}
