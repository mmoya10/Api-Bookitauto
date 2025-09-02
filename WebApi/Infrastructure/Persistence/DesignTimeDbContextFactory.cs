using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace WebApi.Infrastructure.Persistence
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();
            var cs = config.GetConnectionString("Default") 
                     ?? "Host=ep-soft-queen-a97lrflo-pooler.gwc.azure.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_7zjuceU1bPBO;Ssl Mode=Require;Trust Server Certificate=true";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .Options;

            return new AppDbContext(options);
        }
    }
}
