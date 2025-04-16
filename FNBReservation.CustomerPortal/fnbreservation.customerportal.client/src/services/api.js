import axios from 'axios';

// Create a base axios instance with default settings
const api = axios.create({
    timeout: 30000, // 30 seconds
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    }
});

// Request interceptor
api.interceptors.request.use(
    (config) => {
        // You can add auth token here if needed
        // const token = localStorage.getItem('token');
        // if (token) {
        //     config.headers.Authorization = `Bearer ${token}`;
        // }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor
api.interceptors.response.use(
    (response) => {
        return response;
    },
    (error) => {
        // Handle common API errors here
        if (error.response) {
            console.error('API Error Response:', {
                status: error.response.status,
                data: error.response.data
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
        } else {
            // Something happened in setting up the request
            console.error('Request setup error:', error.message);
        }
        
        return Promise.reject(error);
    }
);

export default api; 