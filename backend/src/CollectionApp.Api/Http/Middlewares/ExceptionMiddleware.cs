using CollectionApp.Api.Http.Exceptions;
using CollectionApp.Domain.Exceptions;

namespace CollectionApp.Api.Http.Middlewares
{
    public class ExceptionMiddleware
    {
        public const string HEADER_IMPERSONATE = "hxg-impersonate";

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<ExceptionMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        }

        public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
        {
            try
            {
                
                await _next(context);
            }
            catch (Exception contextException)
            {
                HttpException httpException = null;
                // Log error
                if (contextException is AggregateException aggregateException)
                {
                    foreach (var ex in aggregateException.InnerExceptions)
                    {
                        httpException = new HttpException(context, ex);
                        httpException.Log(_logger);
                    }
                    await HttpExceptionTools.SetExceptionResponse(context, httpException.Body);
                }
                else if (contextException is NotAcceptableException validationException)
                {
                    httpException = new HttpException(context, validationException);
                    await HttpExceptionTools.SetExceptionResponse(context, httpException.Body);
                }
                else
                {
                    contextException = contextException.InnerException ?? contextException;
                    httpException = new HttpException(context, contextException);
                    httpException.Log(_logger);
                    await HttpExceptionTools.SetExceptionResponse(context, httpException.Body);
                }

            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExceptionRedactionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }

}
