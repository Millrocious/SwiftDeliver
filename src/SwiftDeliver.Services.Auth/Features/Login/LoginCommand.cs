using FluentResults;
using Mediator;

namespace SwiftDeliver.Auth.Features.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<Result<LoginTokensDto>>;