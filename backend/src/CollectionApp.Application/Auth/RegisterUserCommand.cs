using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using FluentValidation;
using MediatR;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// Create an account with the default right (User Story 1)
    /// </summary>
    public sealed record RegisterUserCommand(
        string LastName,
        string FirstName,
        string Email,
        string Password,
        string PasswordConfirmation) : IRequest<UserDto>;

    public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 254;
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;

        public RegisterUserCommandValidator()
        {
            RuleFor(c => c.LastName)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Le nom est obligatoire")
                .Must(name => (name ?? "").Trim().Length <= MaxNameLength).WithMessage($"Le nom ne doit pas dépasser {MaxNameLength} caractères");
            RuleFor(c => c.FirstName)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Le prénom est obligatoire")
                .Must(name => (name ?? "").Trim().Length <= MaxNameLength).WithMessage($"Le prénom ne doit pas dépasser {MaxNameLength} caractères");
            RuleFor(c => c.Email)
                .Must(email => !string.IsNullOrWhiteSpace(email)).WithMessage("L'adresse e-mail est obligatoire")
                .Must(email => (email ?? "").Trim().Length <= MaxEmailLength).WithMessage($"L'adresse e-mail ne doit pas dépasser {MaxEmailLength} caractères")
                .Must(email => IsValidEmail((email ?? "").Trim())).WithMessage("L'adresse e-mail n'est pas valide");
            RuleFor(c => c.Password)
                .Must(IsCompliantPassword)
                .WithMessage($"Le mot de passe doit contenir entre {MinPasswordLength} et {MaxPasswordLength} caractères, dont une lettre et un chiffre");
            RuleFor(c => c.PasswordConfirmation)
                .Equal(c => c.Password).WithMessage("La confirmation ne correspond pas au mot de passe");
        }

        private static bool IsValidEmail(string email)
        {
            var at = email.IndexOf('@');
            return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && !email.Any(char.IsWhiteSpace);
        }

        private static bool IsCompliantPassword(string? password)
        {
            return password != null
                && password.Length is >= MinPasswordLength and <= MaxPasswordLength
                && password.Any(char.IsLetter)
                && password.Any(char.IsDigit);
        }
    }

    public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Fast answer for the usual case; concurrent registrations are decided by the unique index
            if (await _userRepository.ExistsByEmailAsync(User.NormalizeEmail(request.Email), cancellationToken))
            {
                throw new AlreadyExistException(EmailAlreadyUsedMessage);
            }

            var defaultClaim = await _userRepository.GetClaimByNameAsync(ClaimValue.DefaultClaimName, cancellationToken)
                ?? throw new InternalException($"Reference claim '{ClaimValue.DefaultClaimName}' is missing: apply the migrations");

            var user = User.Create(request.LastName, request.FirstName, request.Email, _passwordHasher.Hash(request.Password));
            user.UserClaims.Add(new UserClaim { User = user, Claim = defaultClaim });

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return UserDto.From(user);
        }

        public const string EmailAlreadyUsedMessage = "Cette adresse e-mail est déjà utilisée";
    }
}
