using BusinessManagement.Api.Contracts.Requests;
using BusinessManagement.Application.Common;
using BusinessManagement.Application.Customers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customers;
    public CustomersController(CustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<PageResult<CustomerDto>>> List([FromQuery] CustomerListRequest request, CancellationToken ct)
        => Ok(await _customers.ListAsync(request.ToQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _customers.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request, CancellationToken ct)
    {
        var dto = await _customers.CreateAsync(request.FullName, request.Email, request.Phone, request.Address, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);          // 201 + Location
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, CustomerRequest request, CancellationToken ct)
        => Ok(await _customers.UpdateAsync(id, request.FullName, request.Email, request.Phone, request.Address, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<CustomerDto>> Deactivate(Guid id, CancellationToken ct)
     => Ok(await _customers.DeactivateAsync(id, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<CustomerDto>> Activate(Guid id, CancellationToken ct)
         => Ok(await _customers.ActivateAsync(id, ct));

}