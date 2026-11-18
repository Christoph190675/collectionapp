using CollectionApp.Application.Security;
using CollectionApp.Domain.Users;
using FluentValidation;
using MediatR;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// Log out of the current device: revokes its refresh token only (research R4). Never fails.
    /// </summary>
    public sealed record LogoutCommand(string RefreshToken) : IRequest;

    public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(c => c.RefreshToken).NotEmpty().WithMessage("Le jeton de session est obligatoire");
        }
    }

    public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _timeProvider;

        public LogoutCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            ICurrentUser currentUser,
            TimeProvider timeProvider)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var token = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);

            // Someone else's session is left untouched, without telling it apart from an unknown token
            if (token != null && token.UserId == _currentUser.UserId)
            {
                token.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
