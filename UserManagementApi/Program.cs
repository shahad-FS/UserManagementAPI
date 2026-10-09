using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register application services
builder.Services.AddSingleton<UserStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// 2. Read JWT configuration
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured.");

var issuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is not configured.");

var audience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is not configured.");

// 3. Configure JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

var app = builder.Build();

// 4. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// 5. Centralized exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(
            exceptionFeature?.Error,
            "An unhandled exception occurred.");

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new { error = "Internal server error." });
    });
});

// 6. HTTPS and routing
app.UseHttpsRedirection();
app.UseRouting();

// 7. Authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Development-only token endpoint for testing
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/auth/token", (
        LoginRequest request,
        IConfiguration configuration) =>
    {
        var expectedUsername =
            configuration["DemoAuth:Username"];

        var expectedPassword =
            configuration["DemoAuth:Password"];

        if (string.IsNullOrWhiteSpace(expectedUsername) ||
            string.IsNullOrWhiteSpace(expectedPassword))
        {
            return Results.Problem(
                title: "Demo credentials are not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (request.Username != expectedUsername ||
            request.Password != expectedPassword)
        {
            return Results.Unauthorized();
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        var tokenValue =
            new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new
        {
            accessToken = tokenValue,
            tokenType = "Bearer",
            expiresIn = 1800
        });
    })
    .AllowAnonymous();
}

// 9. Public endpoint
app.MapGet("/api/public", () =>
{
    return Results.Ok(new
    {
        message = "The API is running."
    });
})
.AllowAnonymous();

// 10. Get all users with pagination
app.MapGet("/api/users", (
    int? page,
    int? pageSize,
    UserStore store) =>
{
    var currentPage = page ?? 1;
    var currentPageSize = pageSize ?? 10;

    if (currentPage < 1 ||
        currentPageSize < 1 ||
        currentPageSize > 100)
    {
        return Results.BadRequest(new
        {
            error = "Page must be at least 1, and pageSize must be between 1 and 100."
        });
    }

    var result = store.GetPage(
        currentPage,
        currentPageSize);

    return Results.Ok(new
    {
        items = result.Items,
        totalCount = result.TotalCount,
        page = currentPage,
        pageSize = currentPageSize
    });
})
.RequireAuthorization();

// 11. Get one user by ID
app.MapGet("/api/users/{id:int}", (
    int id,
    UserStore store) =>
{
    var user = store.GetById(id);

    return user is null
        ? Results.NotFound(new { error = "User not found." })
        : Results.Ok(user);
})
.RequireAuthorization();

// 12. Create a user
app.MapPost("/api/users", (
    UserRequest request,
    UserStore store) =>
{
    var validationError = UserValidation.Validate(
        request.Name,
        request.Email);

    if (validationError is not null)
    {
        return Results.BadRequest(new
        {
            error = validationError
        });
    }

    var created = store.TryCreate(
        request.Name!.Trim(),
        request.Email!.Trim(),
        out var user);

    if (!created)
    {
        return Results.Conflict(new
        {
            error = "A user with this email already exists."
        });
    }

    return Results.Created(
        $"/api/users/{user!.Id}",
        user);
})
.RequireAuthorization();

// 13. Update a user
app.MapPut("/api/users/{id:int}", (
    int id,
    UserRequest request,
    UserStore store) =>
{
    var validationError = UserValidation.Validate(
        request.Name,
        request.Email);

    if (validationError is not null)
    {
        return Results.BadRequest(new
        {
            error = validationError
        });
    }

    var result = store.TryUpdate(
        id,
        request.Name!.Trim(),
        request.Email!.Trim(),
        out var user);

    return result switch
    {
        UpdateUserResult.NotFound =>
            Results.NotFound(new { error = "User not found." }),

        UpdateUserResult.DuplicateEmail =>
            Results.Conflict(new
            {
                error = "A user with this email already exists."
            }),

        UpdateUserResult.Updated =>
            Results.Ok(user),

        _ => Results.Problem()
    };
})
.RequireAuthorization();

// 14. Delete a user
app.MapDelete("/api/users/{id:int}", (
    int id,
    UserStore store) =>
{
    return store.Delete(id)
        ? Results.NoContent()
        : Results.NotFound(new { error = "User not found." });
})
.RequireAuthorization();

// 15. Development-only exception test
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test-error", () =>
    {
        throw new InvalidOperationException(
            "Intentional exception for testing.");
    })
    .AllowAnonymous();
}

app.Run();

public record LoginRequest(string Username, string Password);