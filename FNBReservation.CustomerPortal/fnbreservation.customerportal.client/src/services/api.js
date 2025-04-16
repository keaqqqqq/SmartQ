import axios from 'axios';

// Create a base axios instance with default settings
const api = axios.create({
    // No baseURL - we'll use Vite's proxy instead
    timeout: 30000, // 30 seconds
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
});

// Request interceptor
api.interceptors.request.use(
    (config) => {
        // Add any auth token here if needed in the future
        // const token = localStorage.getItem('token');
        // if (token) {
        //     config.headers.Authorization = `Bearer ${token}`;
        // }

        // Log requests in development
        if (process.env.NODE_ENV === 'development') {
            console.log(`API Request: ${config.method.toUpperCase()} ${config.url}`, config.data || '');
        }
        
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor
api.interceptors.response.use(
    (response) => {
        // Log responses in development
        if (process.env.NODE_ENV === 'development') {
            console.log(`API Response from ${response.config.url}:`, response.data);
        }
        return response;
    },
    (error) => {
        // Handle common API errors here
        if (error.response) {
            console.error('API Error Response:', {
                status: error.response.status,
                data: error.response.data,
                url: error.config?.url
            });
            
            // Handle specific status codes if needed
            switch (error.response.status) {
                case 401:
                    // Handle unauthorized
                    console.error('Unauthorized access');
                    break;
                case 404:
                    // Handle not found
                    console.error('Resource not found');
                    break;
                case 500:
                    // Handle server errors
                    console.error('Server error');
                    break;
                default:
                    break;
            }
        } else if (error.request) {
            // Request was made but no response received
            console.error('No response received:', error.request);
            console.error('Request details:', error.config);
        } else {
            // Something happened in setting up the request
            console.error('Request setup error:', error.message);
        }
        
        return Promise.reject(error);
    }
);

export default api; 