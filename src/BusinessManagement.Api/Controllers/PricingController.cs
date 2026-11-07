
using BusinessManagement.Application.Checkout.Pricing;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly PricingService _service;
    public PricingController(PricingService service) => _service = service;

    [HttpPost("quote")]
    [ProducesResponseType<QuoteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public ActionResult<QuoteResponse> Quote(QuoteRequest request)
    {
        var quote = _service.Quote(request.OrderTotal, request.CustomerType, request.VoucherCode);
        return Ok(new QuoteResponse(quote.OrderTotal, quote.Discount, quote.FinalAmount));
    }
}