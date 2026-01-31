using System;
using Api.Data;
using Api.Models.DTOs;
using Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

/// <summary>
/// Controller for global brand guideline settings.
/// </summary>
[ApiController]
[Route("api/v1/brand-guidelines")]
public sealed class BrandGuidelinesController : ControllerBase
{
    private readonly AppDbContext _db;

    public BrandGuidelinesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get the global brand guideline text.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BrandGuidelineDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandGuidelineDto>> Get(CancellationToken cancellationToken)
    {
        var record = await _db.BrandGuidelines
            .AsNoTracking()
            .OrderByDescending(b => b.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new BrandGuidelineDto(record?.Text ?? string.Empty));
    }

    /// <summary>
    /// Save the global brand guideline text.
    /// </summary>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(BrandGuidelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BrandGuidelineDto>> Upsert(
        [FromBody] BrandGuidelineDto request,
        CancellationToken cancellationToken)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length > 1500)
        {
            return BadRequest(new ErrorResponseDto("Brand guideline must not exceed 1500 characters"));
        }

        var record = await _db.BrandGuidelines.FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            if (record is not null)
            {
                _db.BrandGuidelines.Remove(record);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Ok(new BrandGuidelineDto(string.Empty));
        }

        if (record is null)
        {
            record = new BrandGuideline
            {
                Id = Guid.NewGuid()
            };
            _db.BrandGuidelines.Add(record);
        }

        record.Text = text;
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new BrandGuidelineDto(record.Text));
    }
}