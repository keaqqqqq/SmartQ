#!/bin/bash
set -e

echo "Waiting for MariaDB to be ready..."
while ! mysqladmin ping -h"localhost" -u"root" -p"$MYSQL_ROOT_PASSWORD" --silent; do
    sleep 1
done

echo "Starting replication..."
mysql -u root -p$MYSQL_ROOT_PASSWORD -e "START SLAVE;"

echo "Checking replication status..."
mysql -u root -p$MYSQL_ROOT_PASSWORD -e "SHOW SLAVE STATUS\G"