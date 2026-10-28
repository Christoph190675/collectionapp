using CollectionApp.Api.Http.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CollectionApp.Api.Http.Filters
{
    public class HttpExceptionFilter : IActionFilter, IOrderedFilter
    {
        //This Order allows other filters to run at the end of the pipeline.
        public int Order => int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                //Exception managed in ExceptionMiddleware
                //=> Do nothing
                return;
            }

            int status = 200;
            if (context.Result is StatusCodeResult)
                status = ((StatusCodeResult)context.Result).StatusCode;
            if (context.Result is ObjectResult)
                status = ((ObjectResult)context.Result).StatusCode ?? 200;

            if (status >= 400)
            {
                HttpException httpException = HttpExceptionTools.BuildHttpException(context.HttpContext, status);
                HttpExceptionTools.SetExceptionResponse(context.HttpContext, httpException.Body).Wait();
                return;
            }
            return;
        }
    }
}
