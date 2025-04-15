import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api/v1/public/outlets';

// Configure axios with default headers
axios.defaults.headers.common['Accept'] = 'application/json';
axios.interceptors.request.use(config => {
    console.log(`Making ${config.method.toUpperCase()} request to: ${config.url}`);
    return config;
});

class OutletService {
    // Get all outlets
    async getAllOutlets() {
        try {
            console.log('Fetching all outlets from:', API_BASE_URL);
            const response = await axios.get(API_BASE_URL);
            console.log('Outlets response data:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get outlet by ID
    async getOutletById(id) {
        try {
            console.log(`Fetching outlet with ID: ${id}`);
            const response = await axios.get(`${API_BASE_URL}/${id}`);
            console.log('Outlet details response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get outlets near a specific location
    async getNearbyOutlets(latitude, longitude, radius = 10) {
        try {
            // First get all outlets
            const allOutlets = await this.getAllOutlets();
            console.log(`Finding outlets within ${radius}km of [${latitude}, ${longitude}]`);
            
            // Calculate distance and filter based on radius
            const nearbyOutlets = allOutlets.filter(outlet => {
                if (!outlet.latitude || !outlet.longitude) return false;
                
                const distance = this.calculateDistance(
                    latitude, 
                    longitude, 
                    outlet.latitude, 
                    outlet.longitude
                );
                
                return distance <= radius;
            });
            
            console.log(`Found ${nearbyOutlets.length} outlets within ${radius}km`);
            return nearbyOutlets;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Calculate distance between two coordinates using Haversine formula
    calculateDistance(lat1, lon1, lat2, lon2) {
        const R = 6371; // Radius of the earth in km
        const dLat = this.deg2rad(lat2 - lat1);
        const dLon = this.deg2rad(lon2 - lon1);
        const a =
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(this.deg2rad(lat1)) * Math.cos(this.deg2rad(lat2)) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        const distance = R * c; // Distance in km
        return distance;
    }

    deg2rad(deg) {
        return deg * (Math.PI / 180);
    }

    // Error handling helper method
    handleError(error) {
        // Log the error
        console.error('Outlet API Error:', error);

        // Additional error handling logic can be added here
        // E.g., Tracking errors, showing notifications, etc.

        if (error.response) {
            // Server responded with a status code outside of 2xx range
            console.error('Error response:', error.response.data);
            console.error('Status:', error.response.status);
            console.error('Headers:', error.response.headers);
        } else if (error.request) {
            // Request was made but no response was received
            console.error('No response received:', error.request);
        } else {
            // Error in setting up the request
            console.error('Request setup error:', error.message);
        }

        // Log network details if available
        if (error.config) {
            console.error('Request Config:', {
                url: error.config.url,
                method: error.config.method,
                headers: error.config.headers,
                data: error.config.data
            });
        }
    }

    // This is a mock method for development that returns sample data
    // To be removed in production when API is ready
    getMockOutlets() {
        return {
            success: true,
            outlets: [
                {
                    id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
                    name: "Sunset Grill & Cafe",
                    location: "12, Jalan Pantai, 75000 Melaka, Malaysia",
                    operatingHours: "04:00 PM - 12:00 AM",
                    contact: "+60 19-876 5432",
                    description: "Experience coastal dining at its finest with panoramic sea views and a menu focused on fresh seafood and international favorites.",
                    image: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80",
                    latitude: 2.1896,
                    longitude: 102.2501
                },
                {
                    id: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5",
                    name: "Urban Spice Kitchen",
                    location: "45, Jalan Petaling, 50000 Kuala Lumpur, Malaysia",
                    operatingHours: "11:30 AM - 10:00 PM",
                    contact: "+60 13-456 7890",
                    description: "A contemporary dining experience in the heart of KL offering fusion dishes that blend Asian spices with modern culinary techniques.",
                    image: "https://images.unsplash.com/photo-1544148103-0773bf10d330?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80",
                    latitude: 3.1412,
                    longitude: 101.6933
                },
                {
                    id: "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5",
                    name: "Highland Garden Restaurant",
                    location: "88, Jalan Hujan, 70300 Cameron Highlands, Malaysia",
                    operatingHours: "08:00 AM - 08:00 PM",
                    contact: "+60 17-222 3333",
                    description: "Nestled in the cool highlands, our restaurant offers farm-to-table dining with ingredients sourced from our own organic garden.",
                    image: "https://images.unsplash.com/photo-1515003197210-e0cd71810b5f?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80",
                    latitude: 4.4718,
                    longitude: 101.3756
                }
            ]
        };
    }
}

export default new OutletService();