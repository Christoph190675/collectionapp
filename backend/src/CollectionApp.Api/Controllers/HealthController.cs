using CollectionApp.Application.Health;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollectionApp.Api.Controllers
{
    [ApiController]
    [Route("api/health")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HealthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Health of the API and of the database: 200 when the database is ok, 503 when it is ko.
        /// The body has the same format in both cases.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<HealthStatusDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<HealthStatusDto>(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var status = await _mediator.Send(new GetHealthStatusQuery(), cancellationToken);

            // JsonResult (not ObjectResult): HttpExceptionFilter replaces the body of any ObjectResult >= 400
            // with a generic error, whereas the 503 must keep the health body (FR-005).
            return new JsonResult(status)
            {
                StatusCode = status.Database == HealthStatusDto.Ok
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable,
            };
        }
    }
}
