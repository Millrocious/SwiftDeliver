using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.Register;

public sealed record RegisterEndpointCommand(string Email, string Password) : ICommand<Result<RegisterEndpointTokens>>;