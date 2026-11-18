using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using FluentValidation;
using MediatR;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// Renew the session: the refresh token is rotated, the end of the session is kept (research R3)
    /// </summary>
    public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthSessionDto>;

    public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(c => c.RefreshToken).NotEmpty().WithMessage("Le jeton de session est obligatoire");
        }
    }

    public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthSessionDto>
    {
        public const string InvalidSessionMessage = "Session expirée ou invalide";

        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly TimeProvider _timeProvider;

        public RefreshTokenCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            ITokenService tokenService,
            TimeProvider timeProvider)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _tokenService = tokenService;
            _timeProvider = timeProvider;
        }

        public async Task<AuthSessionDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var current = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);
            if (current == null || !current.IsActive(now))
            {
                throw new UnauthorizedException(InvalidSessionMessage);
            }

            var user = await _userRepository.GetByIdWithClaimsAsync(current.UserId, cancellationToken)
                ?? throw new UnauthorizedException(InvalidSessionMessage);

            // Rotation: the old token can no longer be used, the session still ends at the same date (FR-013)
            current.Revoke(now);
            var newValue = _tokenService.CreateRefreshTokenValue();
            var renewed = RefreshToken.Issue(user.Id, _tokenService.HashRefreshToken(newValue), now, current.ExpiresAt);
            await _refreshTokenRepository.AddAsync(renewed, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // Current rights of the user, not the ones of the previous access token
            var accessToken = _tokenService.CreateAccessToken(user, user.UserClaims.Select(uc => uc.Claim.Name), now);
            return new AuthSessionDto(accessToken.Value, accessToken.ExpiresAt, newValue, renewed.ExpiresAt, UserDto.From(user));
        }
    }
}
