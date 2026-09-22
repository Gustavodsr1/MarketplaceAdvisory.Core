using MarketplaceAdvisory.Contracts.Identity;
using Refit;

namespace MarketplaceAdvisory.Core.Api.Authentication;

/// <summary>
/// Typed client the Core API uses to call the Identity provider ("hit and get a token").
/// </summary>
public interface IIdentityAuthApi
{
    [Post("/api/v1/auth/login/user")]
    Task<TokenResponse> LoginAsUserAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);

    [Post("/api/v1/auth/login/manager")]
    Task<TokenResponse> LoginAsManagerAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);
}
