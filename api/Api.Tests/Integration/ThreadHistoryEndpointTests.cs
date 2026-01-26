using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Data;
using Api.Models.DTOs;
using Api.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Integration;

public class ThreadHistoryEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task History_List_ReturnsNewestFirst()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.ThreadDrafts.Add(new ThreadDraft
            {
                Id = olderId,
                ClientId = "test",
                PromptJson = JsonSerializer.Serialize(new GenerateThreadRequestDto(
                    Topic: "Older topic",
                    Tone: null,
                    Audience: null,
                    TweetCount: 3,
                    KeyPoints: null,
                    Feedback: null), JsonOptions),
                OutputJson = JsonSerializer.Serialize(new { tweets = new[] { "Older tweet 1", "Older tweet 2" } }, JsonOptions),
                Provider = "xai",
                Model = "grok-test",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            });

            db.ThreadDrafts.Add(new ThreadDraft
            {
                Id = newerId,
                ClientId = "test",
                PromptJson = JsonSerializer.Serialize(new GenerateThreadRequestDto(
                    Topic: "Newer topic",
                    Tone: null,
                    Audience: null,
                    TweetCount: 3,
                    KeyPoints: null,
                    Feedback: null), JsonOptions),
                OutputJson = JsonSerializer.Serialize(new { tweets = new[] { "Newer tweet 1", "Newer tweet 2", "Newer tweet 3" } }, JsonOptions),
                Provider = "xai",
                Model = "grok-test",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/threads/history?limit=20&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<ThreadHistoryListItemDto[]>();
        Assert.NotNull(items);
        Assert.True(items!.Length >= 2);

        Assert.Equal(newerId, items[0].Id);
        Assert.Equal("Newer topic", items[0].TopicPreview);
        Assert.Equal(3, items[0].TweetCount);
        Assert.StartsWith("Newer tweet 1", items[0].FirstTweetPreview);

        Assert.Equal(olderId, items[1].Id);
    }

    [Fact]
    public async Task History_Detail_UnknownId_ReturnsNotFound()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/threads/history/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("Thread not found", body!.Message);
    }

    [Fact]
    public async Task History_List_LimitOver100_ReturnsBadRequest()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/threads/history?limit=101&offset=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("Limit must not exceed 100", body!.Message);
    }
}
