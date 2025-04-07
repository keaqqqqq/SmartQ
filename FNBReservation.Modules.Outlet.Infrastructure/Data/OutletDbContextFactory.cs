// FNBReservation.Modules.Outlet.Infrastructure/Data/OutletDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FNBReservation.Modules.Outlet.Infrastructure.Data
{
    public class OutletDbContextFactory : IDesignTimeDbContextFactory<OutletDbContext>
    {
        public OutletDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<OutletDbContext>();
            optionsBuilder.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );

            return new OutletDbContext(optionsBuilder.Options);
        }
    }
}