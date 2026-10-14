# RaceDay.Api

RaceDay API
Part 2 – RESTful API Development
ASP.NET Core Web API (.NET 10) for the RaceDay event system, built with SQL Server and Entity Framework Core (Code-First). It uses role-based session authentication, Swagger UI, xUnit tests and GitHub Actions CI.

Roles
•	Organiser – creates, updates and deletes their own events and categories, views enrolments for their events, and captures or corrects results.
•	Participant – browses events, enrols in an event by choosing a category, withdraws, and views their own results.
Both roles can register, log in and manage their own profile. Using the other role's functions returns 403 Forbidden; no session returns 401 Unauthorized.

Authentication
Passwords are hashed with PBKDF2. On login the server creates a session storing the user ID, role and email; the client only holds an HttpOnly session cookie. A custom authentication handler enforces [Authorize] and role checks from the session on every request.

Main endpoints
•	Auth: /api/auth/register, /api/auth/login, /api/auth/logout
•	Profile: /api/users/me
•	Events: /api/events (Run, Walk or Cycle) and /api/events/{id}
•	Categories: /api/events/{eventId}/categories and /api/categories/{id}
•	Enrolments: /api/categories/{categoryId}/enrolments, /api/enrolments/me, /api/events/{eventId}/enrolments
•	Results: /api/enrolments/{id}/results, /api/results/{id}, /api/results/me, /api/events/{eventId}/results
Every endpoint is described and testable in Swagger UI at /swagger.
Setup and run

Prerequisites: .NET 10 SDK, Visual Studio 2026 and SQL Server (Express or LocalDB).
1.	Clone the repository and open the solution in Visual Studio.
git clone <your-repo-url>
2.	Check the connection string in appsettings.json (it defaults to LocalDB).
3.	The database is created from the EF Core InitialCreate migration when the API starts. To apply it manually:
dotnet ef database update
4.	Press F5, or run:
dotnet run --launch-profile https

6.	Open https://localhost:<port>/swagger, register a user, log in, then call the endpoints.
Unit tests
xUnit tests with an in-memory database cover registration and login, event management, role rejection, enrolments, categories, profile and results, each with success and failure cases. No SQL Server is needed.
dotnet test RaceDay.Api.Tests

Continuous integration
The workflow .github/workflows/dotnet-ci.yml runs on every push and pull request. It restores, builds and runs all unit tests.
Green build screenshot: insert your screenshot of the successful run here.
