using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

// Collect configuration, logging and dependency registrations
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' is missing.");

builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddProblemDetails();

var app = builder.Build();

// Global middlewares
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (HttpContext context, RequestDelegate next) =>
{
    context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;
    await next(context);
});

app.MapGet("/debug/throw", () =>
{
    throw new InvalidOperationException("Intentional teaching error");
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/todos", async (
      TodoDbContext db,
      CancellationToken ct) =>
{
    var todos = await db.Todos
    .AsNoTracking()
    .OrderBy(todo => todo.Id)
    .Select(todo => new TodoResponse(todo.Id, todo.Title, todo.IsComplete))
    .ToListAsync(ct);

    return Results.Ok(todos);
});

app.MapGet("/todos/{id:int}", async (
      int id,
      TodoDbContext db,
      CancellationToken ct) =>
{
    var todo = await db.Todos
    .AsNoTracking()
    .Where(todo => todo.Id == id)
    .Select(todo => new TodoResponse(todo.Id, todo.Title, todo.IsComplete))
    .SingleOrDefaultAsync(ct);

    return todo is null ? Results.NotFound() : Results.Ok(todo);
});

app.MapPost("/todos", async (
      CreateTodoRequest request,
      TodoDbContext db,
      CancellationToken ct) =>
{
    var title = request.Title.Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["title"] = ["Title cannot be blank."]
        });
    }

    var todo = new Todo { Title = title };
    db.Todos.Add(todo);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/todos/{todo.Id}", new TodoResponse(todo.Id, todo.Title, todo.IsComplete));
});

app.MapPut("/todos/{id:int}", async (
      int id,
      UpdateTodoRequest request,
      TodoDbContext db,
      CancellationToken ct) =>
{
    // SingleOrDefaultAsync() means to fetch a single row from the DB. If there are
    // no rows, then return null. However, if there are more than one rows, then
    // throw an exception. `ct` is the default cancellation token injected by
    // the HTTP call so that the DB operation can be aborted if the request is
    // aborted.
    var todo = await db.Todos.SingleOrDefaultAsync(todo => todo.Id == id, ct);
    if (todo is null)
    {
        return Results.NotFound();
    }

    var title = request.Title.Trim();
    if (string.IsNullOrWhiteSpace(title))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["title"] = ["Title cannot be blank."]
        });
    }

    todo.Title = title;
    todo.IsComplete = request.IsComplete;
    await db.SaveChangesAsync(ct);

    return Results.Ok(new TodoResponse(todo.Id, todo.Title, todo.IsComplete));
});

app.MapDelete("/todos/{id:int}", async (
      int id,
      TodoDbContext db,
      CancellationToken ct) =>
{
    var todo = await db.Todos.SingleOrDefaultAsync(ct);
    if (todo is null)
    {
        return Results.NotFound();
    }

    // This pattern is called change-tracking in EFCore. Change-tracking stays 
    // limited to the DbContext and then once SaveChangesAsync() is called then 
    // all the changes are flushed to the DB in a single go.
    db.Todos.Remove(todo);
    await db.SaveChangesAsync(ct);

    return Results.NoContent();
});

// app.MapGet("/todos", () => Results.Ok(todos.Values.OrderBy(todo => todo.Id)));
//
// app.MapGet("/todos/{id:int}", (int id) => todos.TryGetValue(id, out var todo)
//     ? Results.Ok(todo)
//     : Results.NotFound()
//     );
//
// app.MapPost("/todos", (CreateTodoRequest request) =>
// {
//     var title = request.Title.Trim();
//     if (string.IsNullOrWhiteSpace(title))
//     {
//         return Results.ValidationProblem(new Dictionary<string, string[]>
//         {
//             ["title"] = ["Title cannot be blank."]
//         });
//     }
//
//     var todo = new Todo(Interlocked.Increment(ref nextId), title, false);
//     todos[todo.Id] = todo;
//
//     return Results.Created($"/todos/{todo.Id}", todo);
// });
//
// app.MapPut("/todos/{id:int}", (int id, UpdateTodoRequest request) =>
// {
//     if (!todos.TryGetValue(id, out var existing))
//     {
//         return Results.NotFound();
//     }
//
//     var title = request.Title.Trim();
//     if (string.IsNullOrWhiteSpace(title))
//     {
//         return Results.ValidationProblem(new Dictionary<string, string[]>
//         {
//             ["title"] = ["Title cannot be blank."]
//         });
//     }
//
//     var updated = existing with { Title = title, IsComplete = request.IsComplete };
//     todos[id] = updated;
//
//     return Results.Ok(updated);
// });
//
// app.MapDelete("/todos/{id:int}", (int id) => todos.TryRemove(id, out _)
//     ? Results.NoContent()
//     : Results.NotFound()
//     );


// Starts Kestrel and blocks until shutdown. Kestrel is the default 
// cross-platform web server included with ASP.NET Core.
app.Run();

// Record is  shorthand for a reference-type data container with:
//
// - A constructor: `new Todo(1, "Learn APIs", false)`.
// - Read-only/init-only properties: `Id`, `Title`, `IsComplete`.
// - Useful `ToString()` output.
// - Value-based equality: two Todos with the same three values compare equal, even if they are separate objects.
// - Support for the `with` expression.

// public record Todo(int Id, string Title, bool IsComplete);
public record CreateTodoRequest(string Title);
public record UpdateTodoRequest(string Title, bool IsComplete);
public record TodoResponse(int Id, string Title, bool IsComplete);
