using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace F1Fantasy.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(
        RequestDelegate next,
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);
            
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            // 400 Bad Request - Client sent invalid data
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            
            // 401 Unauthorized - Missing or invalid authentication
            System.Security.Authentication.AuthenticationException => HttpStatusCode.Unauthorized,
            
            // 403 Forbidden - Authenticated but not authorized
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            
            // 404 Not Found - Resource doesn't exist
            KeyNotFoundException => HttpStatusCode.NotFound,
            
            // 409 Conflict - Resource conflict (e.g., duplicate)
            DbUpdateConcurrencyException => HttpStatusCode.Conflict,
            
            // 422 Unprocessable Entity - Database validation failed
            DbUpdateException => HttpStatusCode.UnprocessableEntity,
            
            // 501 Not Implemented
            NotImplementedException => HttpStatusCode.NotImplemented,
            
            // 500 Internal Server Error - Everything else
            _ => HttpStatusCode.InternalServerError
        };

        // Customize message based on exception type
        var message = statusCode switch
        {
            HttpStatusCode.InternalServerError => 
                "An internal server error occurred. Please try again later.",
            
            HttpStatusCode.UnprocessableEntity when exception is DbUpdateException dbEx => 
                GetDatabaseErrorMessage(dbEx),
            
            _ => exception.Message
        };

        var errorResponse = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Detail = _env.IsDevelopment() ? exception.Message : null,
            StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _env.IsDevelopment()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    /// <summary>
    /// Extracts user-friendly message from database exceptions
    /// </summary>
    private string GetDatabaseErrorMessage(DbUpdateException dbEx)
    {
        // Check for common database constraint violations
        var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

        if (innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
        {
            return "A record with this value already exists.";
        }

        if (innerMessage.Contains("foreign key constraint", StringComparison.OrdinalIgnoreCase))
        {
            return "Cannot complete operation due to related data constraints.";
        }

        if (innerMessage.Contains("check constraint", StringComparison.OrdinalIgnoreCase))
        {
            return "Data validation failed. Please check your input.";
        }

        // Generic database error
        return "A database error occurred. Please check your input and try again.";
    }
}
