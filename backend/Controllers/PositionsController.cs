using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PositionsController : ControllerBase
{
    private readonly PositionService _positionService;

    public PositionsController(PositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Position>>> GetPositions(
        [FromHeader(Name = "X-Portfolio-Id")] string? portfolioId)
    {
        var positions = await _positionService.GetAllPositionsAsync(portfolioId);
        return Ok(positions);
    }

    [HttpGet("{pair}")]
    public async Task<ActionResult<Position>> GetPosition(
        string pair,
        [FromHeader(Name = "X-Portfolio-Id")] string? portfolioId)
    {
        var position = await _positionService.GetPositionAsync(pair, portfolioId);

        if (position is null)
        {
            return NotFound(new
            {
                error = $"No position found for {pair.ToUpperInvariant()}"
            });
        }

        return Ok(position);
    }
}
