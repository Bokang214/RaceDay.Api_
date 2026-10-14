using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

// Spins up the real API pipeline (routing, auth, controllers) in-memory,
// swapping the SQL Server DbContext for a fresh EF Core InMemory database
// per factory instance so tests never touch a real SQL Server.
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
