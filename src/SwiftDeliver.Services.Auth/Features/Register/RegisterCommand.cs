using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.Register;

public sealed record RegisterCommand(string Email, string Password) : ICommand<Result<RegisterTokensDto>>;