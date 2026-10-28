using System.Net;
using CollectionApp.Domain.Exceptions;

namespace CollectionApp.Api.Http.Exceptions
{
    public static class ExceptionStatusMapper
    {
        /// <summary>
        /// Get the Http status corresponding to the specified exception
        /// </summary>
        /// <param name="exception">An Exception</param>
        /// <returns>A HttpStatusCode</returns>
        public static HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                AlreadyExistException => HttpStatusCode.Conflict,
                BadInputException => HttpStatusCode.BadRequest,
                BadRequestException => HttpStatusCode.BadRequest,
                ForbiddenException => HttpStatusCode.Forbidden,
                LockedException => HttpStatusCode.Locked,
                NotAcceptableException => HttpStatusCode.NotAcceptable,
                NotFoundException => HttpStatusCode.NotFound,
                PreconditionException => HttpStatusCode.PreconditionFailed,
                TooLargeException => HttpStatusCode.RequestEntityTooLarge,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                UnsupportedMediaTypeException => HttpStatusCode.UnsupportedMediaType,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
