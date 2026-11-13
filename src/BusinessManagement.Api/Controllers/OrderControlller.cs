using BusinessManagement.Application.Orders;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/orders")]
public class OrdersController(OrderService orders) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var lines = request.Items.Select(i => new OrderLine(i.ProductId, i.Quantity)).ToList();
        var dto = await orders.CreateAsync(request.CustomerId, lines, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
        => Ok(await orders.GetAsync(id, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await orders.CancelAsync(id, ct));
}