using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace FNBReservation.SharedKernel.Health
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var isPrimaryHealthy = false;
            var isReplicaHealthy = false;
            var isReplicationWorking = false;

            try
            {
                // Check primary database health
                var primaryConnection = _configuration.GetConnectionString("DefaultConnection");
                isPrimaryHealthy = await CheckDatabaseConnectionAsync(primaryConnection, "Primary");
                data.Add("PrimaryDatabaseStatus", isPrimaryHealthy ? "Healthy" : "Unhealthy");

                // Check read replica health if enabled
                var useReadReplica = _configuration.GetValue<bool>("DatabaseOptions:EnableReadReplica", false);

                if (useReadReplica)
                {
                    var replicaConnection = _configuration.GetConnectionString("ReadOnlyConnection");
                    if (!string.IsNullOrEmpty(replicaConnection))
                    {
                        isReplicaHealthy = await CheckDatabaseConnectionAsync(replicaConnection, "Replica");
                        data.Add("ReplicaDatabaseStatus", isReplicaHealthy ? "Healthy" : "Unhealthy");

                        // Check replication lag if both databases are healthy
                        if (isPrimaryHealthy && isReplicaHealthy)
                        {
                            var replicationLag = await CheckReplicationLagAsync(primaryConnection, replicaConnection);
                            data.Add("ReplicationLagSeconds", replicationLag);
                            isReplicationWorking = replicationLag >= 0 && replicationLag <= 60; // Consider healthy if lag is under 60 seconds
                            data.Add("ReplicationStatus", isReplicationWorking ? "Healthy" : "Lagging");
                        }
                    }
                    else
                    {
                        // If read replica is enabled but connection string is missing
                        _logger.LogWarning("Read replica is enabled but ReadOnlyConnection string is missing");
                        data.Add("ReplicaDatabaseStatus", "Not Configured");
                    }
                }
                else
                {
                    // Read replica not enabled
                    data.Add("ReplicaDatabaseStatus", "Not Enabled");
                    isReplicaHealthy = true; // Don't fail health check if replica is not enabled
                    isReplicationWorking = true;
                }

                // Determine overall health
                if (!isPrimaryHealthy)
                {
                    return HealthCheckResult.Unhealthy("Primary database is unhealthy", null, data);
                }
                else if (useReadReplica && !isReplicaHealthy)
                {
                    return HealthCheckResult.Degraded("Read replica is unhealthy", null, data);
                }
                else if (useReadReplica && !isReplicationWorking)
                {
                    return HealthCheckResult.Degraded("Replication is lagging", null, data);
                }

                return HealthCheckResult.Healthy("Database connections are healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during database health check");
                return HealthCheckResult.Unhealthy("Database health check failed", ex, data);
            }
        }

        private async Task<bool> CheckDatabaseConnectionAsync(string connectionString, string dbType)
        {
            try
            {
                using var connection = new MySqlConnector.MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Execute a simple query to check if database is responsive
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();

                _logger.LogDebug("{DbType} database connection test successful", dbType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {DbType} database", dbType);
                return false;
            }
        }

        private async Task<int> CheckReplicationLagAsync(string primaryConnectionString, string replicaConnectionString)
        {
            try
            {
                // This is a simple approach to check replication lag by creating a temporary marker table
                // with a timestamp on the primary and checking when it appears on the replica
                var testTableName = $"replication_test_{DateTime.UtcNow.Ticks}";
                var timestamp = DateTime.UtcNow;

                // Create test table on primary
                using (var primaryConnection = new MySqlConnector.MySqlConnection(primaryConnectionString))
                {
                    await primaryConnection.OpenAsync();
                    using var command = primaryConnection.CreateCommand();
                    command.CommandText = $@"
                        CREATE TABLE {testTableName} (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            created_at DATETIME NOT NULL
                        );
                        INSERT INTO {testTableName} (created_at) VALUES (UTC_TIMESTAMP());";
                    await command.ExecuteNonQueryAsync();
                }

                // Wait for it to appear on replica
                using (var replicaConnection = new MySqlConnector.MySqlConnection(replicaConnectionString))
                {
                    await replicaConnection.OpenAsync();
                    var startTime = DateTime.UtcNow;
                    var timeout = TimeSpan.FromSeconds(60);

                    while (DateTime.UtcNow - startTime < timeout)
                    {
                        using var command = replicaConnection.CreateCommand();
                        command.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{testTableName}'";
                        var result = await command.ExecuteScalarAsync();

                        if (Convert.ToInt32(result) > 0)
                        {
                            // Table exists on replica, replication is working
                            var lagTime = (int)(DateTime.UtcNow - timestamp).TotalSeconds;
                            _logger.LogInformation("Replication lag: {LagSeconds} seconds", lagTime);

                            // Clean up test table
                            await CleanupTestTableAsync(primaryConnectionString, testTableName);
                            return lagTime;
                        }

                        // Wait and try again
                        await Task.Delay(500);
                    }

                    // Timed out waiting for replication
                    _logger.LogWarning("Replication lag check timed out after 60 seconds");

                    // Clean up test table even if we timed out
                    await CleanupTestTableAsync(primaryConnectionString, testTableName);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking replication lag");
                return -1;
            }
        }

        private async Task CleanupTestTableAsync(string connectionString, string tableName)
        {
            try
            {
                using var connection = new MySqlConnector.MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up test table {TableName}", tableName);
            }
        }
    }
}