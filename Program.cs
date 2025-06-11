using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// JWT config
var jwtKey = "super_secret_jwt_key_iman_giahi";
var jwtIssuer = "UserManagementAPI";
var jwtAudience = "UserManagementAPIUsers";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = signingKey
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// In-memory user store
var users = new List<User>();
var nextId = 1;

// Login endpoint (returns JWT)
app.MapPost("/login", (string username, string password) =>
{
    // For demo: accept any username/password
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        return Results.BadRequest("Username and password required.");

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, username)
    };
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds
    );
    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = jwt });
});

// Get all users
app.MapGet("/users", () => Results.Ok(users))
    .RequireAuthorization();

// Get user by id
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is null ? Results.NotFound($"User with ID {id} not found.") : Results.Ok(user);
}).RequireAuthorization();

// Create user
app.MapPost("/users", (UserCreateDto userDto) =>
{
    var user = new User
    {
        Id = nextId++,
        FullName = userDto.FullName,
        Email = userDto.Email,
        Department = userDto.Department
    };
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
}).RequireAuthorization();

// Update user
app.MapPut("/users/{id:int}", (int id, UserCreateDto userDto) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
        return Results.NotFound($"User with ID {id} not found.");
    user.FullName = userDto.FullName;
    user.Email = userDto.Email;
    user.Department = userDto.Department;
    return Results.NoContent();
}).RequireAuthorization();

// Delete user
app.MapDelete("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
        return Results.NotFound($"User with ID {id} not found.");
    users.Remove(user);
    return Results.NoContent();
}).RequireAuthorization();

// Error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ErrorHandlingMiddleware");
        logger.LogError(ex, "Unhandled exception occurred.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var errorResponse = System.Text.Json.JsonSerializer.Serialize(new { error = "Internal server error." });
        await context.Response.WriteAsync(errorResponse);
    }
});

// Logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("LoggingMiddleware");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var request = context.Request;
    logger.LogInformation("Incoming Request: {Method} {Path}", request.Method, request.Path);

    var originalBodyStream = context.Response.Body;
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;

    await next();

    stopwatch.Stop();
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    logger.LogInformation("Response Status: {StatusCode} | Duration: {Duration}ms", context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    await responseBody.CopyToAsync(originalBodyStream);
});

app.Run();