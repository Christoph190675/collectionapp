using CollectionApp.Application.Security;
using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Users;
using MediatR;

namespace CollectionApp.Application.Auth
{
    /// <summary>
    /// The authenticated user and its rights (session check at startup, US2-AC7)
    /// </summary>
    public sealed record GetCurrentUserQuery : IRequest<UserDto>;

    public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IUserRepository _userRepository;

        public GetCurrentUserQueryHandler(ICurrentUser currentUser, IUserRepository userRepository)
        {
            _currentUser = currentUser;
            _userRepository = userRepository;
        }

        public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = _currentUser.UserId is int id
                ? await _userRepository.GetByIdWithClaimsAsync(id, cancellationToken)
                : null;

            return user != null ? UserDto.From(user) : throw new UnauthorizedException("Utilisateur inconnu");
        }
    }
}
