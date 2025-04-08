using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace FNBReservation.Modules.Reservation.Infrastructure.Data
{
    public class ReservationDbContextFactory : IDesignTimeDbContextFactory<ReservationDbContext>
    {
        public ReservationDbContext CreateDbContext(string[] args)
        {
            // Load configuration from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ReservationDbContext>();
            optionsBuilder.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );

            return new ReservationDbContext(optionsBuilder.Options);
        }
    }
}