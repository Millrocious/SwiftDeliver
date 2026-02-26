using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.Login;

public sealed record LoginEndpointCommand(string Email, string Password) : ICommand<Result<LoginEndpointTokens>>;