using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// This code is for Migration

namespace IWX_CloudZen.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> // Factory that creates AppDbContext
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>(); // Preparing database configuration

            optionsBuilder.UseSqlServer(connectionString); // Use SQL Server with this connection string

            return new AppDbContext(optionsBuilder.Options); 
        }
    }
}
