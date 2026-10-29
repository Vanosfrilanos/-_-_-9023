
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Настройки приложения
builder.Services.AddRouting();
builder.Logging.AddConsole();
builder.Configuration["AppName"] = "Mini ASP.NET Core";

var app = builder.Build();

// Настройка окружения
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Подключение статических файлов
app.UseStaticFiles();

// Собственный middleware
app.UseMiddleware<LoggingMiddleware>();

// Middleware Use
app.Use(async (context, next) =>
{
    Console.WriteLine("Начало: " + context.Request.Path);

    await next(context);

    Console.WriteLine("Конец: " +
        context.Response.StatusCode);
});

// UseWhen: условная ветка с возвратом
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            context.Response.Headers["X-API"] = "UseWhen";

            await next(context);
        });
    }
);

// MapWhen: ветка по условию
app.MapWhen(
    context => context.Request.Path == "/special",
    branch =>
    {
        branch.Run(async context =>
        {
            context.Response.ContentType =
                "text/plain; charset=utf-8";

            await context.Response.WriteAsync(
                "MapWhen работает успешно!");
        });
    }
);

// Map: отдельная ветка по адресу
app.Map("/admin", branch =>
{
    branch.Run(async context =>
    {
        context.Response.ContentType =
            "text/plain; charset=utf-8";

        await context.Response.WriteAsync(
            "Map работает успешно!");
    });
});

// Главная страница
app.MapGet("/", (IWebHostEnvironment env) =>
{
    string webRoot = env.WebRootPath ??
        Path.Combine(env.ContentRootPath, "wwwroot");

    string file = Path.Combine(webRoot, "index.html");

    if (!File.Exists(file))
    {
        return Results.Problem(
            "Не найден файл wwwroot/index.html",
            statusCode: 500);
    }

    return Results.File(
        file,
        contentType: "text/html; charset=utf-8");
});

// Информация о HTTP-запросе
app.MapGet("/request", (HttpContext context) =>
{
    return Results.Json(new
    {
        Method = context.Request.Method,
        Path = context.Request.Path.ToString(),
        Query = context.Request.QueryString.ToString(),
        Headers = context.Request.Headers.ToDictionary(
            x => x.Key, x => x.Value.ToString()),
        Cookies = context.Request.Cookies.ToDictionary(
            x => x.Key, x => x.Value)
    });
});

// HttpResponse
app.MapGet("/response", () =>
{
    return Results.Text(
        "HTTP-ответ успешно получен! Код 200.",
        "text/plain",
        Encoding.UTF8,
        statusCode: 200);
});

// Обработка формы
app.MapPost("/form", async (HttpContext context) =>
{
    if (!context.Request.HasFormContentType)
        return Results.BadRequest("Нужна HTML-форма");

    var form = await context.Request.ReadFormAsync();

    string name = form["name"].ToString();

    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest("Введите имя");

    return Results.Json(new
    {
        Message = $"Привет, {name}!",
        Method = "POST",
        Success = true
    });
});

// Получение JSON
app.MapGet("/api/student", () =>
{
    return Results.Json(new
    {
        Id = 1,
        Name = "Алексей",
        Age = 18,
        Specialty = "Информационные системы",
        Subject = "ASP.NET Core"
    });
});

// Отправка JSON
app.MapPost("/api/student", async (HttpContext context) =>
{
    Student? student;

    try
    {
        student = await context.Request
            .ReadFromJsonAsync<Student>();
    }
    catch (JsonException)
    {
        return Results.BadRequest("Ошибка формата JSON");
    }
    catch (BadHttpRequestException)
    {
        return Results.BadRequest("Неверный запрос");
    }

    if (student == null ||
        string.IsNullOrWhiteSpace(student.Name))
    {
        return Results.BadRequest("Введите имя студента");
    }

    return Results.Json(new
    {
        Message = "JSON успешно обработан",
        Name = student.Name,
        Age = student.Age
    });
});

// Minimal API с параметром
app.MapGet("/api/hello/{name}", (string name) =>
{
    return Results.Json(new
    {
        Message = $"Привет, {name}!"
    });
});

// Загрузка файлов
app.MapPost("/upload", async (
    HttpContext context,
    IWebHostEnvironment env) =>
{
    if (!context.Request.HasFormContentType)
        return Results.BadRequest("Выберите файл");

    var form = await context.Request.ReadFormAsync();

    IFormFile? file = form.Files.GetFile("file");

    if (file == null || file.Length == 0)
        return Results.BadRequest("Файл не выбран");

    if (file.Length > 5 * 1024 * 1024)
        return Results.BadRequest("Максимум 5 МБ");

    string folder = Path.Combine(
        env.ContentRootPath, "Uploads");

    Directory.CreateDirectory(folder);

    string savedName =
        Guid.NewGuid().ToString("N") + ".bin";

    string path = Path.Combine(folder, savedName);

    await using (var stream = File.Create(path))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Json(new
    {
        Message = "Файл успешно загружен!",
        OriginalName = file.FileName,
        FileSize = file.Length,
        SavedAs = savedName
    });
});

// Скачивание файла через SendFileAsync
app.MapGet("/download", async (
    HttpContext context,
    IWebHostEnvironment env) =>
{
    string path = Path.Combine(
        env.ContentRootPath, "example.txt");

    await File.WriteAllTextAsync(
        path,
        "Пример скачивания файла ASP.NET Core",
        Encoding.UTF8);

    context.Response.ContentType =
        "text/plain; charset=utf-8";

    context.Response.Headers.ContentDisposition =
        "attachment; filename=\"example.txt\"";

    await context.Response.SendFileAsync(path);
});

// Временная переадресация 302
app.MapGet("/redirect", () =>
{
    return Results.Redirect("/", permanent: false);
});

// Постоянная переадресация 301
app.MapGet("/redirect-permanent", () =>
{
    return Results.Redirect("/", permanent: true);
});

// Окружение
app.MapGet("/environment",
    (IWebHostEnvironment env) =>
    {
        return Results.Json(new
        {
            Environment = env.EnvironmentName,
            ContentRoot = env.ContentRootPath,
            WebRoot = env.WebRootPath,
            IsDevelopment = env.IsDevelopment(),
            IsProduction = env.IsProduction()
        });
    });

// Терминальное middleware Run
app.Map("/terminal", branch =>
{
    branch.Run(async context =>
    {
        context.Response.ContentType =
            "text/plain; charset=utf-8";

        await context.Response.WriteAsync(
            "Run работает! Конвейер завершён.");
    });
});

// Обработка неизвестных адресов
app.MapFallback(() =>
{
    return Results.Json(new
    {
        Error = "Страница не найдена",
        Code = 404
    }, statusCode: 404);
});

// Запуск веб-сервера
app.Run();

// Модель студента
public record Student(string Name, int Age);
