using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.RefreshToken;

public sealed record RefreshTokenEndpointCommand(string RefreshToken) : ICommand<Result<RefreshTokenEndpointTokens>>;