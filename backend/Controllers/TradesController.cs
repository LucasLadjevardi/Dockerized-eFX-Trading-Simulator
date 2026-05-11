using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TradesController : ControllerBase
{
    private readonly ExecutionService _executionService;
    private readonly TradeService _tradeService;

    public TradesController(
        ExecutionService executionService,
        TradeService tradeService)
    {
        _executionService = executionService;
        _tradeService = tradeService;
    }

    [HttpPost]
    public async Task<ActionResult<Trade>> ExecuteTrade([FromBody] TradeRequest request)
    {
        try
        {
            var trade = await _executionService.ExecuteTradeAsync(request);
            return Ok(trade);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Ok(new
            {
                status = "REJECTED",
                reason = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<Trade>>> GetTrades()
    {
        var trades = await _tradeService.GetLatestTradesAsync();
        return Ok(trades);
    }
}
