
using Microsoft.AspNetCore.Http;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine(
            "Запрос: " +
            context.Request.Method + " " +
            context.Request.Path);

        await _next(context);

        Console.WriteLine(
            "Статус ответа: " +
            context.Response.StatusCode);
    }
}
