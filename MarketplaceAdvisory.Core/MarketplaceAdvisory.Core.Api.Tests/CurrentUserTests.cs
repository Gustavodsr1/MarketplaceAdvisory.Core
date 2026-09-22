using System.Security.Claims;
using FluentAssertions;
using MarketplaceAdvisory.Core.Api.Authentication;
using MarketplaceAdvisory.SharedKernel.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MarketplaceAdvisory.Core.Api.Tests;

public sealed class CurrentUserTests
{
    [Fact]
    public void CurrentUser_ReflectsClaimsFromHttpContext()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(AppClaimTypes.Subject, "user-1"),
            new Claim(AppClaimTypes.Role, AppRoles.Manager)
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var currentUser = new CurrentUser(accessor.Object);

        // Act & Assert
        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.UserId.Should().Be("user-1");
        currentUser.Roles.Should().Contain(AppRoles.Manager);
    }
}
