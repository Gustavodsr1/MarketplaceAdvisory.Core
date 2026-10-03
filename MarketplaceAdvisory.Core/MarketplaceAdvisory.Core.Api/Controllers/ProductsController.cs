using Asp.Versioning;
using ErrorOr;
using MarketplaceAdvisory.Core.Application.Catalog.Commands.CreateProduct;
using MarketplaceAdvisory.Core.Application.Catalog.Queries.GetProductById;
using MarketplaceAdvisory.SharedKernel.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceAdvisory.Core.Api.Controllers;

/// <summary>
/// Catalog endpoints. Every action requires an authenticated caller; creating products is
/// additionally restricted to the "Manager" role to demonstrate RBAC.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
[Authorize]
[Produces("application/json")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(product => Ok(product), ToProblem);
    }

    // RBAC demonstration: only a JWT carrying the "Manager" role may create a product.
    [HttpPost]
    [Authorize(Roles = AppRoles.Manager)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.Match<IActionResult>(
            product => CreatedAtAction(nameof(GetById), new { id = product.Id, version = "1" }, product),
            ToProblem);
    }

    private IActionResult ToProblem(List<Error> errors)
    {
        var problemDetails = ApiProblemDetails.Create(errors, HttpContext);
        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }
}
