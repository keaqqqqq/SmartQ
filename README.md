# FNB Reservation System

## API Integration Details

This project integrates a React frontend with .NET Core backend for a Reservation and Queue Management System.

### API Endpoints

The following API endpoints have been integrated:

- **Queue Management**:
  - `http://localhost:5000/api/v1/queue` - Create a new queue entry (POST)
  - `http://localhost:5000/api/v1/queue/code/{code}` - Get queue entry by code (GET)
  - `http://localhost:5000/api/v1/queue/wait-time/{outletId}/{partySize}` - Get estimated wait time (GET)
  - `http://localhost:5000/api/v1/queue/exit/{code}` - Exit queue (POST)
  - `http://localhost:5000/api/v1/queue/{code}` - Update queue entry (PUT)

### Frontend Components

- **CustomerPortal**: React-based portal for customers to interact with the queue system
  - Queue Form - Join a queue
  - Queue Status - Check status of a queue entry
  - Queue Confirmation - Confirmation screen after arrival

### Setup and Configuration

1. The frontend communicates with the API via Vite's proxy configuration:
   - All API requests are forwarded to the appropriate backend endpoints
   - See `vite.config.js` for proxy settings

2. To run the application:
   - Start the backend server on port 5000
   - Start the frontend with `npm start`

### Development

- Mock data is provided for development when backend is not available
- API services are structured to easily switch between real and mock data
