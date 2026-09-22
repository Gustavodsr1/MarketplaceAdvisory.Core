using MediatR;

namespace MarketplaceAdvisory.Core.Application.Common.Messaging;

/// <summary>
/// Marker for a CQRS query. Isolates MediatR behind an application-owned abstraction.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
