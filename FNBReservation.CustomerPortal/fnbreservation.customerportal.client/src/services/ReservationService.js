import api from './api';

const API_BASE_URL = '/api/v1/reservations';

class ReservationService {
    // Get nearby outlets based on user location
    async getNearbyOutlets(locationParams = null) {
        try {
            let url = `${API_BASE_URL}/GetNearbyOutlets`;

            // Add location params if available
            if (locationParams) {
                url += `?latitude=${locationParams.latitude}&longitude=${locationParams.longitude}`;
            }

            const response = await api.get(url);
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
                outletId: availabilityParams.outletId,
                partySize: parseInt(availabilityParams.partySize),
                date: availabilityParams.date,
                preferredTime: availabilityParams.preferredTime,
                earliestTime: availabilityParams.earliestTime || null,
                latestTime: availabilityParams.latestTime || null
            };

            const response = await api.post(`${API_BASE_URL}/check-availability`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Check availability with nearby outlets if the selected outlet is not available
    async checkAvailabilityWithNearby(availabilityParams) {
        try {
            const payload = {
                outletId: availabilityParams.outletId,
                partySize: parseInt(availabilityParams.partySize),
                date: availabilityParams.date,
                preferredTime: availabilityParams.preferredTime,
                earliestTime: availabilityParams.earliestTime || null,
                latestTime: availabilityParams.latestTime || null,
                userLatitude: availabilityParams.latitude,
                userLongitude: availabilityParams.longitude
            };

            const response = await api.post(`${API_BASE_URL}/check-availability-with-nearby`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Hold tables for a reservation
    async holdTables(holdParams) {
        try {
            const payload = {
                outletId: holdParams.outletId,
                partySize: parseInt(holdParams.partySize),
                date: holdParams.date,
                time: holdParams.time
            };

            const response = await api.post(`${API_BASE_URL}/hold-tables`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Release a table hold
    async releaseHold(holdId) {
        try {
            const response = await api.post(`${API_BASE_URL}/release-hold/${holdId}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get alternative times for a hold
    async getAlternativeTimesForHold(holdId) {
        try {
            const response = await api.get(`${API_BASE_URL}/alternative-times-for-hold/${holdId}`);
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
                outletId: reservationData.outletId,
                customerName: reservationData.customerName,
                customerPhone: reservationData.customerPhone,
                customerEmail: reservationData.customerEmail,
                partySize: parseInt(reservationData.partySize),
                reservationDate: reservationData.date,
                reservationTime: reservationData.time,
                specialRequests: reservationData.specialRequests || "",
                holdId: reservationData.holdId || null
            };

            const response = await api.post(`${API_BASE_URL}`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get reservation details by ID
    async getReservationById(id) {
        try {
            const response = await api.get(`${API_BASE_URL}/${id}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get reservation details by code
    async getReservationByCode(code) {
        try {
            const response = await api.get(`${API_BASE_URL}/code/${code}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get all reservations for a phone number
    async getReservationsByPhone(phone) {
        try {
            const phoneWithoutFormatting = phone.replace(/\s+/g, '');
            const response = await api.get(`${API_BASE_URL}/phone/${encodeURIComponent(phoneWithoutFormatting)}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Update an existing reservation
    async updateReservation(reservationData) {
        try {
            const response = await api.put(`${API_BASE_URL}/UpdateReservation`, reservationData);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Cancel a reservation
    async cancelReservation(reservationId) {
        try {
            const response = await api.post(`${API_BASE_URL}/${reservationId}/cancel`);
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