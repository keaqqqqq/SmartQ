using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace FNBReservation.SharedKernel.Data
{
    /// <summary>
    /// DbContext factory that creates and manages separate contexts for read and write operations,
    /// enabling the use of read replicas for horizontal scaling.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to create</typeparam>
    public class DbContextFactory<TContext> where TContext : DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly Action<DbContextOptionsBuilder, string> _optionsAction;
        private readonly ILogger<DbContextFactory<TContext>> _logger;

        public DbContextFactory(
            IConfiguration configuration,
            Action<DbContextOptionsBuilder, string> optionsAction,
            ILogger<DbContextFactory<TContext>> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _optionsAction = optionsAction ?? throw new ArgumentNullException(nameof(optionsAction));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a DbContext connected to the primary database for write operations
        /// </summary>
        public TContext CreateWriteContext()
        {
            var options = new DbContextOptionsBuilder<TContext>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger.LogInformation("[WRITE] Using primary database: {ConnectionString}",
                       connectionString.Substring(0, connectionString.IndexOf(';')));

            _optionsAction(options, connectionString);

            return (TContext)Activator.CreateInstance(typeof(TContext), options.Options);
        }

        /// <summary>
        /// Creates a DbContext connected to the read replica for read operations
        /// </summary>
        public TContext CreateReadContext()
        {
            var options = new DbContextOptionsBuilder<TContext>();

            // Use the read replica connection if available, otherwise fall back to primary
            var connectionString = _configuration.GetConnectionString("ReadOnlyConnection")
                ?? _configuration.GetConnectionString("DefaultConnection");
            _logger.LogInformation("[READ] Using replica database: {ConnectionString}",
                     connectionString.Substring(0, connectionString.IndexOf(';')));

            _optionsAction(options, connectionString);

            return (TContext)Activator.CreateInstance(typeof(TContext), options.Options);
        }
    }
}