using System.Net;
using System.Text.Json;

namespace TaskManager.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                int statusCode = StatusCodes.Status500InternalServerError;

                if (ex is ArgumentException)
                {
                    statusCode = StatusCodes.Status400BadRequest;
                }

                context.Response.StatusCode = statusCode;

                var response = new
                {
                    success = false,
                    message = statusCode == 400 ? "Bad Request" : "Something went wrong",
                    error = ex.InnerException?.Message ?? ex.Message
                };

                var json = JsonSerializer.Serialize(response);

                await context.Response.WriteAsync(json);
            }
        }
    }
}