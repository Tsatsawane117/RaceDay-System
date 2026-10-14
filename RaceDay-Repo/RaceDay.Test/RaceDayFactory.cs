using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration of RaceDayDbContext (and its options).
            var toRemove = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext)))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
