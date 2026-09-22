using Asp.Versioning;
using MarketplaceAdvisory.Contracts.Identity;
using MarketplaceAdvisory.Core.Api.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceAdvisory.Core.Api.Controllers;

/// <summary>
/// Demonstrates the Core API acting as a client of the Identity provider: it calls the Identity
/// endpoints to obtain a JWT that can then be used against the protected Core endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity")]
[Produces("application/json")]
public sealed class IdentityProxyController(IIdentityAuthApi identityAuthApi) : ControllerBase
{
    [HttpPost("token/user")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> GetUserToken(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await identityAuthApi.LoginAsUserAsync(request, cancellationToken));

    [HttpPost("token/manager")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TokenResponse>> GetManagerToken(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await identityAuthApi.LoginAsManagerAsync(request, cancellationToken));
}
