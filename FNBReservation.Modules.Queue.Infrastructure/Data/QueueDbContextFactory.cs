using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FNBReservation.Modules.Queue.Infrastructure.Data
{
    public class QueueDbContextFactory : IDesignTimeDbContextFactory<QueueDbContext>
    {
        public QueueDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<QueueDbContext>();
            optionsBuilder.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );

            return new QueueDbContext(optionsBuilder.Options);
        }
    }
}