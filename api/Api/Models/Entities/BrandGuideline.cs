using System;

namespace Api.Models.Entities;

public class BrandGuideline
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}