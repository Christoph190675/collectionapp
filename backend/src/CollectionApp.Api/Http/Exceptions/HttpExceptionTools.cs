using System.Net;
using CollectionApp.Domain.Exceptions;
using Newtonsoft.Json;

namespace CollectionApp.Api.Http.Exceptions
{
    public static class HttpExceptionTools
    {
        /// <summary>
        /// Set the Http response for an exception
        /// </summary>
        /// <param name="context">An HttpContext</param>
        /// <param name="body">An HttpExceptionBody</param>
        public static async Task SetExceptionResponse(HttpContext context, HttpExceptionBody body)
        {
            context.Response.StatusCode = (int)body.Status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(body.Error));
        }
        
        /// <summary>
        /// Set the Http response for an exception
        /// </summary>
        /// <param name="context">An HttpContext</param>
        /// <param name="statusCode">A forced status code (ot the context's one if it is not specified</param>
        public static async Task SetExceptionResponse(HttpContext context, int? statusCode = null)
        {
            statusCode = statusCode ?? context.Response.StatusCode;
            await SetExceptionResponse(context, BuildHttpException(context, statusCode.Value).Body);
        }

        /// <summary>
        /// Build an Exception
        /// </summary>
        /// <param name="context">An Httpcontext</param>
        /// <param name="status">A status</param>
        /// <param name="code">An error code</param>
        /// <param name="message">An error message</param>
        /// <returns>An HttpException</returns>
        public static HttpException BuildHttpException(HttpContext context, int status, string code = null, string message = null)
        {
            HttpStatusCode httpStatusCode = Enum.Parse<HttpStatusCode>(status.ToString(), true);
            switch (httpStatusCode)
            {
                case HttpStatusCode.NotAcceptable:
                    return new HttpException(context, new BadRequestException(message));
                case HttpStatusCode.MethodNotAllowed:
                    return new HttpException(context, new BadRequestException(message));
                case HttpStatusCode.Unauthorized:
                    return new HttpException(context, new UnauthorizedException(message));
                case HttpStatusCode.Forbidden:
                    return new HttpException(context, new ForbiddenException(message));
                case HttpStatusCode.NotFound:
                    return new HttpException(context, new NotFoundException(message));
                case HttpStatusCode.BadRequest:
                    return new HttpException(context, new BadRequestException(message));
                case HttpStatusCode.Ambiguous:
                    return new HttpException(context, new AlreadyExistException(message));
                default: //Other error => Default exception
                    return new HttpException(context, new InternalException( message ?? httpStatusCode.ToString()));
            }
        }
    }
}