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
    public async Task<ActionResult<List<Position>>> GetPositions()
    {
        var positions = await _positionService.GetAllPositionsAsync();
        return Ok(positions);
    }

    [HttpGet("{pair}")]
    public async Task<ActionResult<Position>> GetPosition(string pair)
    {
        var position = await _positionService.GetPositionAsync(pair);

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