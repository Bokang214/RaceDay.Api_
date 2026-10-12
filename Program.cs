using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Database (EF Core, Code-First against SQL Server) ----------------
// The unit-test project replaces this registration with an in-memory database.
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayContext")));

// ---------------- Session management + authentication ----------------
// Login stores the user's id and role in SERVER-SIDE session state; the client only
// receives an opaque session cookie. SessionAuthenticationHandler turns that session
// back into a ClaimsPrincipal on every request.
var idleMinutes = builder.Configuration.GetValue<int?>("Session:IdleTimeoutMinutes") ?? 120;
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
});

builder.Services
    .AddAuthentication(SessionAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationDefaults.Scheme, _ => { });

builder.Services.AddAuthorization();

// ---------------- MVC / Controllers ----------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system: Authentication, User Profile, " +
                      "Events, Categories, Event Enrolments and Results. There are two roles, Organiser and Participant. " +
                      "To test protected endpoints, call POST /api/auth/login first - the server-side session cookie " +
                      "is then sent automatically by the browser on every request made from this page."
    });

    // Use the XML doc comments on the controllers/DTOs as endpoint descriptions.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ---------------- CORS (open for the MVC front end in Part 3) ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Create the database on first run (Code-First). If EF migrations have been added
// (dotnet ef migrations add InitialCreate) they are applied; otherwise the schema is
// created straight from the model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
    if (db.Database.IsRelational()) // skipped by the unit tests (in-memory provider)
    {
        if (db.Database.GetMigrations().Any())
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();
    }
}

// Swagger stays enabled in every environment so the API can always be verified in a browser.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.DocumentTitle = "RaceDay API";
});

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Default");

app.UseSession();          // must come before authentication
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes Program for WebApplicationFactory<Program> in the test project.
public partial class Program { }
