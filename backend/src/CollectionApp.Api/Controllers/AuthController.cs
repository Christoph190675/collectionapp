using CollectionApp.Api.Http.Exceptions;
using CollectionApp.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollectionApp.Api.Controllers
{
    /// <summary>
    /// Registration, login, session renewal, logout and current user (contracts/auth.openapi.yaml)
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Create an account with the "User" right. No session is opened.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _mediator.Send(command, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, user);
        }

        /// <summary>
        /// Log in. Wrong password and unknown e-mail get the same 401; 423 (with Retry-After) after 5 failures.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType<AuthSessionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status423Locked)]
        public async Task<ActionResult<AuthSessionDto>> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            return await _mediator.Send(command, cancellationToken);
        }

        /// <summary>
        /// Renew the session: new access token and new refresh token, the previous one is revoked
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType<AuthSessionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthSessionDto>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            return await _mediator.Send(command, cancellationToken);
        }

        /// <summary>
        /// Log out of the current device (idempotent)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// The authenticated user and its rights
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<HttpExceptionBodyError>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
        {
            return await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        }
    }
}
