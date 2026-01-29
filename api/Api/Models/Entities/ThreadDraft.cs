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

    // --- Feedback tracking fields ---

    /// <summary>
    /// User rating 1-5 stars (null if not rated)
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Number of times this thread was regenerated
    /// </summary>
    public int RegenerationCount { get; set; } = 0;

    /// <summary>
    /// Whether the user kept this as their final version
    /// </summary>
    public bool WasFinalVersion { get; set; } = false;

    /// <summary>
    /// Feedback tags (comma-separated): too_generic, too_long, weak_hook, not_engaging, too_marketing, off_topic
    /// </summary>
    public string? FeedbackTags { get; set; }

    /// <summary>
    /// ID of the parent thread if this was a regeneration
    /// </summary>
    public Guid? ParentThreadId { get; set; }
}
