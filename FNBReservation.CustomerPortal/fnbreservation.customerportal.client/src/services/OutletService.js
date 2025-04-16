import api from './api';

// Update API base URL to use the public outlets endpoint
const API_BASE_URL = '/api/v1/public/outlets';

class OutletService {
    // Get all outlets
    async getAllOutlets() {
        try {
            const response = await api.get(`${API_BASE_URL}`);
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get outlet by ID
    async getOutletById(id) {
        try {
            const response = await api.get(`${API_BASE_URL}/${id}`);
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get outlets near a specific location
    async getNearbyOutlets(latitude, longitude, limit = 3) {
        try {
            // We'll still use the geolocation endpoint for this specific function
            const response = await api.get(`/api/v1/geolocation/nearest-outlets?latitude=${latitude}&longitude=${longitude}&limit=${limit}`);
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get nearest outlet based on location
    async getNearestOutlet(latitude, longitude) {
        try {
            const response = await api.get(`/api/v1/reservations/nearest-outlet?latitude=${latitude}&longitude=${longitude}`);
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get outlet operating hours
    async getOutletOperatingHours(outletId) {
        try {
            const response = await api.get(`${API_BASE_URL}/${outletId}/operating-hours`);
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Error handling helper method
    handleError(error) {
        // Additional custom error handling if needed
        console.error('OutletService: Error encountered', error);
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