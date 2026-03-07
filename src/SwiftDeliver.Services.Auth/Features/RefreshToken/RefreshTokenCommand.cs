using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<RefreshTokenTokens>>;