using System.Net;
using System.Text;
using CollectionApp.Domain.Exceptions;
using CollectionApp.Domain.Security;
using Newtonsoft.Json;

namespace CollectionApp.Api.Http.Exceptions
{
    public class HttpException
    {
        public HttpContext Context { get; set; }
        public HttpExceptionBody Body { get; set; }

        private Exception _baseException;

        public HttpException(HttpContext context, Exception exception)
        {
            _baseException = exception;
            Context = context;
            Body = ToJson();

            Initialize(context, exception);
        }

        /// <summary>
        /// Initializer
        /// </summary>
        /// <param name="context">An HttpContext</param>
        /// <param name="exception">An Exception</param>
        private void Initialize(HttpContext context, Exception exception)
        {
            _baseException = exception;
            Context = context;
            Body = ToJson();
        }

        /// <summary>
        /// Log the exception
        /// </summary>
        /// <param name="logger"></param>
        public void Log(ILogger logger)
        {
            try
            {
                LogLevel level = LogLevel.Error;
                if (Body.Status == HttpStatusCode.InternalServerError)
                {
                    level = LogLevel.Critical;
                }

                //Log with Serilog
                logger.Log(level, _baseException, Body.Error.Message);

            }
            catch (Exception e2)
            {
                logger.LogCritical(e2.Message);
            }
        }

        /// <summary>
        /// Get the Http status From the specified exception
        /// </summary>
        /// <param name="exception">An Exception</param>
        /// <returns>A HttpStatusCode</returns>
        private HttpStatusCode GetStatus()
        {
            if (_baseException is AbstractException)
                return ((AbstractException)_baseException).Code;
        
            return HttpStatusCode.InternalServerError;
        }


        /// <summary>
        /// Get a JSON body from the specifie dexcpetion
        /// </summary>
        /// <param name="exception">An Exception</param>
        /// <returns>HttpExceptionBody</returns>
        private HttpExceptionBody ToJson()
        {          
            var status = GetStatus();
            return new HttpExceptionBody()
            {
                Status = GetStatus(),
                Error = new HttpExceptionBodyError()
                {
                    Code = _baseException.GetType().Name,
                    Message = _baseException.Message,
                    Status = (int)GetStatus(),
                    Details = GetDetails(),
                    StackTrace = FullStackTrace()
                }
            };

        }

        /// <summary>
        /// Get full stack trace for the specified exception
        /// </summary>
        /// <param name="e">En Exception</param>
        /// <returns>A string</returns>
        public string FullStackTrace()
        {
            StringBuilder sb = new StringBuilder();
            Exception? e = _baseException;
            while (e != null)
            {
                sb.AppendLine(e.Message);
                sb.AppendLine(e.StackTrace);
                sb.AppendLine("-----------");
                e = e.InnerException;
            }

            return sb.ToString();
        }


        private string? GetDetails()
        {
            if(Context?.User?.HasClaim(ClaimPrincipalValues.LogRead) ?? false)
            {
                return _baseException.InnerException?.Message ?? _baseException.Message;
            }
            return null;
        }

        private string? GetStackTrace(Exception exception)
        {
            if (Context?.User?.HasClaim(ClaimPrincipalValues.LogRead) ?? false)
            {
                return exception.StackTrace;
            }
            return null;
        }
    }

    public class HttpExceptionBody
    {
        [JsonProperty("status")]
        public HttpStatusCode Status { get; set; }

        [JsonProperty("error")]
        public HttpExceptionBodyError Error { get; set; }
    }

    public class HttpExceptionBodyError
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty(PropertyName = "details", NullValueHandling = NullValueHandling.Ignore)]
        public string? Details { get; set; }
        [JsonProperty(PropertyName = "stackTrace", NullValueHandling = NullValueHandling.Ignore)]
        public string? StackTrace { get; set; }
    }
}
