# FNB Reservation Customer Portal

This is the customer-facing portal for the FNB Reservation system, allowing customers to:
- View restaurant outlets
- Make table reservations
- Join virtual queues
- Check reservation/queue status

## Integration with Backend API

### Outlets API Integration

The outlets component has been integrated with the backend API:

- API Endpoint: `http://localhost:5000/api/v1/public/outlets`
- Method: GET
- Response: List of active outlets with details

### API Testing

For development purposes, an API Tester component is available at `/api-tester` to:
- Test API connectivity
- Verify response format
- Debug integration issues

### Property Handling

The integration includes special handling for API property naming conventions:
- Supports both camelCase and PascalCase property names
- Example: `outlet.queueEnabled` or `outlet.QueueEnabled` are both supported

## Development Setup

1. Configure the proxy in `vite.config.js`:
   ```js
   proxy: {
     '/api/v1/public': {
       target: 'http://localhost:5000',
       changeOrigin: true,
       secure: false
     }
   }
   ```

2. Run the development server:
   ```
   npm run dev
   ```

## API Services

The application uses service classes to interact with APIs:

- `OutletService`: Handles outlet-related API calls
- `ReservationService`: Manages reservation operations
- `QueueService`: Handles queue operations

## Next Steps

- [ ] Complete integration of reservation endpoints
- [ ] Complete integration of queue endpoints
- [ ] Add authentication for user login/registration
- [ ] Implement user profile management
