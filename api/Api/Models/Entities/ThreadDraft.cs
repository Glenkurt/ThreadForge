using System;

namespace Api.Models.Entities;

public class ThreadDraft
{
    public Guid Id { get; set; }

    // Anonymous identifier provided by client (no auth for MVP)
    public string ClientId { get; set; } = string.Empty;

    // Stored for later viewing/re-use; do not log this value
    public string PromptJson { get; set; } = string.Empty;

    // Canonical stored output (array of tweet strings, plus metadata)
    public string OutputJson { get; set; } = string.Empty;

    public string Provider { get; set; } = "xai";
    public string Model { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
