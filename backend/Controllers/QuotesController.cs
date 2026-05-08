using EfxSimulator.Api.Models;
using EfxSimulator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EfxSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QuotesController : ControllerBase
{
    private readonly QuoteService _quoteService;

    public QuotesController(QuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    [HttpPost]
    public async Task<ActionResult<Quote>> CreateQuote([FromBody] QuoteRequest request)
    {
        try
        {
            var quote = await _quoteService.CreateQuoteAsync(request);
            return Ok(quote);
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
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }
}