using System.Net;
using CPAWeb.API.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace CPAWeb.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Անսպասելի սխալ է տեղի ունեցել: {Message}", exception.Message);

            // 2. Որոշում ենք Status Code-ը և Title-ը ըստ Exception-ի տեսակի
            var (statusCode, title) = exception switch
            {
                ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Not Found"),
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),
                _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Title = title,
                Message = exception.Message 
            };

            // 3. Ուղարկում ենք HTTP պատասխանը
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            // Վերադարձնում ենք true, ինչը նշանակում է, որ exception-ը մշակված է (handled)
            return true;
        }
    }
}