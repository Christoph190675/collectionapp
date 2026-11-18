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
    }
}
