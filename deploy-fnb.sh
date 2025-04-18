#!/bin/bash

# FNB Reservation System Deployment Script
# Usage: ./deploy-fnb.sh [--scale N] [--env ENV] [--restart]
# Options:
#   --scale N       : Scale API instances to N replicas
#   --env ENV       : Set environment (Development, Staging, Production)
#   --restart       : Restart all services
#   --update        : Rebuild and update services
#   --logs          : View logs of running containers
#   --help          : Show this help message

# Default values
SCALE=2
ENV_FILE=".env"
ACTION="start"

# Process command line arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        --scale)
            SCALE="$2"
            shift 2
            ;;
        --env)
            case "$2" in
                "Development"|"Staging"|"Production")
                    sed -i "s/^ENVIRONMENT=.*/ENVIRONMENT=$2/" "$ENV_FILE"
                    echo "Environment set to $2"
                    ;;
                *)
                    echo "Invalid environment: $2. Use Development, Staging, or Production."
                    exit 1
                    ;;
            esac
            shift 2
            ;;
        --restart)
            ACTION="restart"
            shift
            ;;
        --update)
            ACTION="update"
            shift
            ;;
        --logs)
            ACTION="logs"
            shift
            ;;
        --help)
            echo "FNB Reservation System Deployment Script"
            echo "Usage: ./deploy-fnb.sh [--scale N] [--env ENV] [--restart] [--update] [--logs] [--help]"
            echo "Options:"
            echo "  --scale N       : Scale API instances to N replicas"
            echo "  --env ENV       : Set environment (Development, Staging, Production)"
            echo "  --restart       : Restart all services"
            echo "  --update        : Rebuild and update services"
            echo "  --logs          : View logs of running containers"
            echo "  --help          : Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown parameter: $1"
            echo "Use --help for usage information."
            exit 1
            ;;
    esac
done

# Update API_REPLICAS in .env file
sed -i "s/^API_REPLICAS=.*/API_REPLICAS=$SCALE/" "$ENV_FILE"
echo "API instances scaled to $SCALE"

# Get current environment
CURRENT_ENV=$(grep "^ENVIRONMENT=" "$ENV_FILE" | cut -d= -f2)
echo "Current environment: $CURRENT_ENV"

# Create required directories for volume mounting in production
if [ "$CURRENT_ENV" == "Production" ]; then
    echo "Creating data directories for Production..."
    mkdir -p /var/data/fnb/mariadb
    mkdir -p /var/data/fnb/mariadb-replica
    mkdir -p /var/data/fnb/redis
    mkdir -p /var/data/fnb/ssl
    
    # Check if we need to create self-signed SSL certificates
    if [ ! -f "/var/data/fnb/ssl/cert.pem" ] || [ ! -f "/var/data/fnb/ssl/key.pem" ]; then
        echo "Generating self-signed SSL certificates..."
        openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
            -keyout /var/data/fnb/ssl/key.pem \
            -out /var/data/fnb/ssl/cert.pem \
            -subj "/C=US/ST=State/L=City/O=Organization/CN=fnbreservation.local"
        echo "Self-signed certificates created. Replace with valid certificates for production."
    fi
    
    # Set proper permissions
    chmod -R 755 /var/data/fnb
    
    # Use the production override
    COMPOSE_FILE="-f docker-compose.yml -f docker-compose.override.yml"
else
    # Use just the main compose file for non-production
    COMPOSE_FILE="-f docker-compose.yml"
fi

# Execute the requested action
case "$ACTION" in
    "start")
        echo "Starting FNB Reservation System with $SCALE API instances..."
        docker-compose $COMPOSE_FILE up -d
        ;;
    "restart")
        echo "Restarting FNB Reservation System services..."
        docker-compose $COMPOSE_FILE restart
        ;;
    "update")
        echo "Updating FNB Reservation System services..."
        docker-compose $COMPOSE_FILE build
        docker-compose $COMPOSE_FILE up -d
        ;;
    "logs")
        echo "Viewing logs (Ctrl+C to exit)..."
        docker-compose $COMPOSE_FILE logs -f
        ;;
esac

# Display running containers
echo -e "\nRunning containers:"
docker-compose $COMPOSE_FILE ps

if [ "$ACTION" != "logs" ]; then
    echo -e "\nSystem is running. Access points:"
    echo "- API: http://localhost:${API_PORT:-5000}"
    echo "- Portal: http://localhost:${PORTAL_HTTPS_PORT:-5003}"
    echo "- Swagger API Documentation: http://localhost:${API_PORT:-5000}/swagger"
    echo -e "\nTo view logs, run: ./deploy-fnb.sh --logs"
fi