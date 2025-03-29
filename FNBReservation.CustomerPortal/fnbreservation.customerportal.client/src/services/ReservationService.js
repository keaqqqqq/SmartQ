import axios from 'axios';

const API_BASE_URL = '/api/CustomerReservation';

class ReservationService {
    // Get nearby outlets based on user location
    async getNearbyOutlets(locationParams = null) {
        try {
            let url = `${API_BASE_URL}/GetNearbyOutlets`;

            // Add location params if available
            if (locationParams) {
                url += `?latitude=${locationParams.latitude}&longitude=${locationParams.longitude}`;
            }

            const response = await axios.get(url);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Check availability based on provided parameters
    async checkAvailability(availabilityParams) {
        try {
            const payload = {
                OutletId: availabilityParams.outletId,
                PartySize: parseInt(availabilityParams.partySize),
                Date: `${availabilityParams.date}T00:00:00Z`,
                PreferredTime: availabilityParams.preferredTime,
                EarliestTime: availabilityParams.earliestTime,
                LatestTime: availabilityParams.latestTime
            };

            const response = await axios.post(`${API_BASE_URL}/CheckAvailability`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Create a reservation with customer details
    async createReservation(reservationData) {
        try {
            const payload = {
                OutletId: reservationData.outletId,
                CustomerName: reservationData.customerName,
                CustomerPhone: reservationData.customerPhone,
                CustomerEmail: reservationData.customerEmail,
                PartySize: parseInt(reservationData.partySize),
                ReservationDate: reservationData.reservationDate,
                SpecialRequests: reservationData.specialRequests
            };

            const response = await axios.post(`${API_BASE_URL}/CreateReservation`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get reservation details by ID
    async getReservationById(id) {
        try {
            const response = await axios.get(`${API_BASE_URL}/GetReservationById?id=${id}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get reservation details by code
    async getReservationByCode(code) {
        try {
            const response = await axios.get(`${API_BASE_URL}/GetReservationByCode?code=${code}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get all reservations for a phone number
    async getReservationsByPhone(phone) {
        try {
            const response = await axios.get(`${API_BASE_URL}/GetReservationsByPhone?phone=${encodeURIComponent(phone)}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Update an existing reservation
    async updateReservation(reservationData) {
        try {
            const response = await axios.put(`${API_BASE_URL}/UpdateReservation`, reservationData);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Cancel a reservation
    async cancelReservation(reservationId) {
        try {
            const response = await axios.put(`${API_BASE_URL}/CancelReservation?id=${reservationId}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Error handling helper method
    handleError(error) {
        // Log the error
        console.error('Reservation API Error:', error);

        // Additional error handling logic can be added here
        // E.g., Tracking errors, showing notifications, etc.

        if (error.response) {
            // Server responded with a status code outside of 2xx range
            console.error('Error response:', error.response.data);
            console.error('Status:', error.response.status);
        } else if (error.request) {
            // Request was made but no response was received
            console.error('No response received:', error.request);
        } else {
            // Error in setting up the request
            console.error('Request setup error:', error.message);
        }
    }
}

export default new ReservationService();