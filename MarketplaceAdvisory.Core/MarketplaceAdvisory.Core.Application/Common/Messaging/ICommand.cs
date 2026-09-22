using MediatR;

namespace MarketplaceAdvisory.Core.Application.Common.Messaging;

/// <summary>
/// Marker for a CQRS command. Isolates MediatR behind an application-owned abstraction so the
/// mediator can be swapped without touching handlers (licensing/roadmap hedge).
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
