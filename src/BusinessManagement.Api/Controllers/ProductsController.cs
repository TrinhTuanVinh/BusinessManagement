using BusinessManagement.Api.Contracts.Requests;
using BusinessManagement.Application.Common;
using BusinessManagement.Application.Products;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _products;
    public ProductsController(ProductService products) => _products = products;

    [HttpGet]
    public async Task<ActionResult<PageResult<ProductDTO>>> List([FromQuery] ProductListRequest request, CancellationToken ct)
        => Ok(await _products.ListAsync(request.ToQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDTO>> Get(Guid id, CancellationToken ct)
        => Ok(await _products.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ProductDTO>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var dto = await _products.CreateAsync(request.Name, request.Category, request.Price, request.Stock, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);          // 201 + Location
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDTO>> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
        => Ok(await _products.UpdateAsync(id, request.Name, request.Category, request.Price, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return NoContent();
    }
}