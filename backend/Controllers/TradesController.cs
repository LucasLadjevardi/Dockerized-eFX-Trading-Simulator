using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TradesController : ControllerBase
{
    private readonly TradeService _tradeService;

    public TradesController(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    [HttpPost]
    public async Task<ActionResult<Trade>> ExecuteTrade([FromBody] TradeRequest request)
    {
        try
        {
            var trade = await _tradeService.ExecuteTradeAsync(request);
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