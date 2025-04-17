-- Create replication user with appropriate privileges
CREATE USER IF NOT EXISTS 'replicator'@'%' IDENTIFIED BY 'replication_password';
GRANT REPLICATION SLAVE ON *.* TO 'replicator'@'%';
FLUSH PRIVILEGES;

-- This helps identify the current binary log position
-- which will be needed when setting up the replica
SHOW MASTER STATUS;