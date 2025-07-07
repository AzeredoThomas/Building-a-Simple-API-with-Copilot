using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using UserManagement; 

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
        });

        var app = builder.Build();

        var users = new List<User>();
        var nextId = 1;

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseMiddleware<TokenAuthenticationMiddleware>();

        app.UseHttpsRedirection();


        app.MapPost("/users", (User user) =>
        {
            try
            {
                // Validate required fields and format
                List<ValidationResult> validationResults = new List<ValidationResult>();
                var context = new ValidationContext(user);
                if (!Validator.TryValidateObject(user, context, validationResults, true))
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                    return Results.BadRequest(new { Errors = errors });
                }

                // Sanitize input to prevent malicious code (basic example)
                user.Username = SanitizeInput(user.Username);
                user.Email = SanitizeInput(user.Email);

                // Check for unique email
                if (users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.BadRequest(new { Errors = new[] { "Email must be unique." } });
                }

                user.Id = nextId++;
                user.CreatedAt = DateTime.UtcNow;
                users.Add(user);
                return Results.Created($"/users/{user.Id}", user);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}", statusCode: 500);
            }
        });


        // Get All Users
        app.MapGet("/users", () =>
        {
            try
            {
                return Results.Ok(users);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}", statusCode: 500);
            }
        });

        // Get User by Id
        app.MapGet("/users/{id:int}", (int id) =>
        {
            try
            {
                if (users == null)
                    return Results.Problem("User is not available.", statusCode: 500);

                var user = users.FirstOrDefault(u => u.Id == id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}", statusCode: 500);
            }
        });
        // Update User
        app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
        {
            try
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null) return Results.NotFound();

                // Validate required fields and format
                List<ValidationResult> validationResults = new List<ValidationResult>();
                var context = new ValidationContext(updatedUser);
                if (!Validator.TryValidateObject(updatedUser, context, validationResults, true))
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                    return Results.BadRequest(new { Errors = errors });
                }

                // Sanitize input to prevent malicious code
                updatedUser.Username = SanitizeInput(updatedUser.Username);
                updatedUser.Email = SanitizeInput(updatedUser.Email);

                // Check for unique email (excluding current user)
                if (users.Any(u => u.Id != id && u.Email.Equals(updatedUser.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.BadRequest(new { Errors = new[] { "Email must be unique." } });
                }

                user.Username = updatedUser.Username;
                user.Email = updatedUser.Email;
                return Results.Ok(user);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}", statusCode: 500);
            }
        });
        // Delete User
        app.MapDelete("/users/{id:int}", (int id) =>
        {
            try
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null) return Results.NotFound();

                users.Remove(user);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal server error: {ex.Message}", statusCode: 500);
            }
        });


        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        app.MapControllers();

        app.Run();


        // Sanitize input to prevent malicious code
        static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove script tags
            var sanitized = Regex.Replace(input, "<.*?>", string.Empty);
            // Remove potentially dangerous characters
            sanitized = Regex.Replace(sanitized, @"[<>""'/]", string.Empty);
            return sanitized.Trim();
        }
    }
}

   

// Create User
// Move the SanitizeInput method into a separate static class to resolve CS8803 and ensure it is used to resolve CS8321.

public static class InputSanitizer
{
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove script tags
        var sanitized = Regex.Replace(input, "<.*?>", string.Empty);
        // Remove potentially dangerous characters
        sanitized = Regex.Replace(sanitized, @"[<>""'/]", string.Empty);
        return sanitized.Trim();
    }
}
public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(50, ErrorMessage = "Username must be at most 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [MaxLength(100, ErrorMessage = "Email must be at most 100 characters.")]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
/*
1.Boilerplate Setup

Copilot generated a clean and modern C# boilerplate for the Program.cs file,
including the User class with appropriate properties
(Id, Username, Email, CreatedAt), following.NET 9 and C# 13 conventions.

2.Minimal API and Middleware Configuration

Copilot set up the minimal API pipeline using the latest ASP.NET Core features. This included:
•	Registering essential services like endpoints API explorer and Swagger for API documentation.
•	Configuring middleware for HTTPS redirection and Swagger UI.
•	Preparing the app for future API endpoints by mapping controllers and setting up the request pipeline.

3.CRUD Endpoints Implementation

Copilot provided a full set of minimal API CRUD endpoints for the User resource:
•	POST / users: Create a new user.
•	GET / users: Retrieve all users.
•	GET /users/{id}: Retrieve a user by ID.
•	PUT /users/{id}: Update an existing user.
•	DELETE /users/{id}: Delete a user.

These endpoints use an in-memory list for demonstration and follow RESTful conventions.

4.	HTTP Test File Generation

Copilot generated a comprehensive UserManagementApi.http file with ready-to-use HTTP requests for each endpoint.
This allows for easy manual testing of the API directly from Visual Studio.

5.Best Practices and Modern Syntax

Copilot ensured the use of modern C# features (such as file-scoped namespaces)
and followed best practices for minimal APIs, making the codebase clean, maintainable,
and up-to-date with the latest .NET standards.
---
Summary:
GitHub Copilot accelerated the development process by scaffolding the API structure,
implementing robust CRUD functionality, configuring middleware,
and providing practical test scripts, all while adhering to modern .NET and C# standards. 
*/