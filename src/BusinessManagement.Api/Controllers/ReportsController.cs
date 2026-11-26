using System.ComponentModel.DataAnnotations;
using BusinessManagement.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController(ReportService reports) : ControllerBase
{
    [HttpGet("top-customers")]
    public async Task<ActionResult<IReadOnlyList<TopCustomerDto>>> TopCustomers(
        [FromQuery, Range(1, 100)] int top = 10, CancellationToken ct = default)
        => Ok(await reports.TopCustomersAsync(top, ct));
}