#!/bin/bash
set -e

# Wait for the primary database to be ready
while ! mysqladmin ping -h"mariadb" -P"3306" --silent; do
    echo "Waiting for primary database to be ready..."
    sleep 2
done

# Get the current binary log position from the primary
echo "Getting binary log position from primary..."
MASTER_STATUS=$(mysql -h mariadb -u root -p$MYSQL_ROOT_PASSWORD -e "SHOW MASTER STATUS\G")
MASTER_LOG_FILE=$(echo "$MASTER_STATUS" | grep File | awk '{print $2}')
MASTER_LOG_POS=$(echo "$MASTER_STATUS" | grep Position | awk '{print $2}')

if [ -z "$MASTER_LOG_FILE" ] || [ -z "$MASTER_LOG_POS" ]; then
    echo "Failed to get binary log information from primary"
    exit 1
fi

echo "Primary binary log file: $MASTER_LOG_FILE, position: $MASTER_LOG_POS"

# Configure replica to replicate from primary
mysql -u root -p$MYSQL_ROOT_PASSWORD <<EOF
STOP SLAVE;
CHANGE MASTER TO
    MASTER_HOST='mariadb',
    MASTER_PORT=3306,
    MASTER_USER='replicator',
    MASTER_PASSWORD='replication_password',
    MASTER_LOG_FILE='$MASTER_LOG_FILE',
    MASTER_LOG_POS=$MASTER_LOG_POS;
START SLAVE;
EOF

# Check if replication is working
echo "Checking replication status..."
mysql -u root -p$MYSQL_ROOT_PASSWORD -e "SHOW SLAVE STATUS\G"