using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// Log in with e-mail and password (User Story 2)
    /// </summary>
    public sealed record LoginCommand(string Email, string Password) : IRequest<AuthSessionDto>;

    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(c => c.Email)
                .Must(email => !string.IsNullOrWhiteSpace(email)).WithMessage("L'adresse e-mail est obligatoire")
                .MaximumLength(RegisterUserCommandValidator.MaxEmailLength).WithMessage($"L'adresse e-mail ne doit pas dépasser {RegisterUserCommandValidator.MaxEmailLength} caractères");
            RuleFor(c => c.Password)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire")
                .MaximumLength(RegisterUserCommandValidator.MaxPasswordLength).WithMessage($"Le mot de passe ne doit pas dépasser {RegisterUserCommandValidator.MaxPasswordLength} caractères");
        }
    }

    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthSessionDto>
    {
        /// <summary>Same message whether the e-mail exists or not (FR-008)</summary>
        public const string InvalidCredentialsMessage = "E-mail ou mot de passe incorrect";

        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly TimeProvider _timeProvider;
        private readonly AuthOptions _options;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            TimeProvider timeProvider,
            IOptions<AuthOptions> options)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _timeProvider = timeProvider;
            _options = options.Value;
        }

        public async Task<AuthSessionDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var user = await _userRepository.GetByEmailWithLoginAndClaimsAsync(User.NormalizeEmail(request.Email), cancellationToken);

            if (user == null)
            {
                // Same cost as a real check, nothing recorded (research R7)
                _passwordHasher.Verify(_passwordHasher.DummyHash, request.Password);
                throw new UnauthorizedException(InvalidCredentialsMessage);
            }

            var login = user.Login ??= new Login { UserId = user.Id, User = user, DateLogin = now };

            // Refused without checking the password nor extending the lock (FR-010)
            if (login.IsLocked(now))
            {
                var retryAfter = login.LockedUntil!.Value - now;
                throw new LockedException(
                    $"Compte temporairement bloqué. Réessayez dans {Math.Ceiling(retryAfter.TotalMinutes)} minutes.",
                    retryAfter);
            }
            login.ResetIfLockExpired(now);

            var check = _passwordHasher.Verify(user.PasswordHash, request.Password);
            if (check == PasswordCheck.Failed)
            {
                login.RegisterFailure(now);
                await _userRepository.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedException(InvalidCredentialsMessage);
            }

            login.RegisterSuccess(now);
            if (check == PasswordCheck.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.Hash(request.Password);
            }

            var claims = user.UserClaims.Select(uc => uc.Claim.Name).ToList();
            var accessToken = _tokenService.CreateAccessToken(user, claims, now);
            var refreshTokenValue = _tokenService.CreateRefreshTokenValue();
            var refreshToken = RefreshToken.Issue(
                user.Id, _tokenService.HashRefreshToken(refreshTokenValue), now, now.AddDays(_options.RefreshTokenDays));

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            // Same DbContext: saves the login state, the hash update and the new session at once
            await _userRepository.SaveChangesAsync(cancellationToken);

            return new AuthSessionDto(accessToken.Value, accessToken.ExpiresAt, refreshTokenValue, refreshToken.ExpiresAt, UserDto.From(user));
        }
    }
}
