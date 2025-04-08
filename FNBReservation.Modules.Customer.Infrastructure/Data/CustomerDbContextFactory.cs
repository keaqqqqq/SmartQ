using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FNBReservation.Modules.Customer.Infrastructure.Data
{
    public class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
    {
        public CustomerDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
            optionsBuilder.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );

            return new CustomerDbContext(optionsBuilder.Options);
        }
    }
}