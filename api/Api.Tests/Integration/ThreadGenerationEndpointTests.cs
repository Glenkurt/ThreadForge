using System.Net;
using System.Net.Http.Json;
using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests.Integration;

public class ThreadGenerationEndpointTests
{
    [Fact]
    public async Task Generate_ReturnsOk()
    {
        await using var factory = CreateFactoryWithFakeGenerator();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Client-Id", "test-client");

        var request = new GenerateThreadRequestDto(
            Topic: "Test topic",
            Tone: "clear",
            Audience: "builders",
            TweetCount: 5,
            KeyPoints: new[] { "Point A" },
            Feedback: null);

        var response = await client.PostAsJsonAsync("/api/v1/threads/generate", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<GenerateThreadResponseDto>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.NotEmpty(body.Tweets);
    }

    [Fact]
    public async Task Generate_IsLimitedTo20PerDayPerClientId()
    {
        await using var factory = CreateFactoryWithFakeGenerator();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Client-Id", "rate-limit-client");

        var request = new GenerateThreadRequestDto(
            Topic: "Test topic",
            Tone: null,
            Audience: null,
            TweetCount: 3,
            KeyPoints: null,
            Feedback: null);

        for (var i = 0; i < 20; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/threads/generate", request);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/v1/threads/generate", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithFakeGenerator()
    {
        return new CustomWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IThreadGenerationService));
                services.AddScoped<IThreadGenerationService, FakeThreadGenerationService>();
            });
        });
    }

    private sealed class FakeThreadGenerationService : IThreadGenerationService
    {
        public Task<GenerateThreadResponseDto> GenerateAsync(
            GenerateThreadRequestDto request,
            string clientId,
            CancellationToken cancellationToken)
        {
            var dto = new GenerateThreadResponseDto(
                Guid.NewGuid(),
                new[] { "Tweet 1", "Tweet 2" },
                DateTime.UtcNow,
                "xai",
                "grok-test");

            return Task.FromResult(dto);
        }
    }
}
