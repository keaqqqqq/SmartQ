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

            console.log("Checking availability with payload:", payload);

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
                
                // Add the flag to check nearby outlets - this is critical
                checkNearbyOutlets: true,
                
                // Add location permission flag
                hasLocationPermission: true,
                
                // Include coordinates if available
                latitude: availabilityParams.latitude,
                longitude: availabilityParams.longitude,
                
                // Additional parameters to control nearby search
                maxNearbyOutlets: 5 // Request up to 5 nearby outlets
            };

            console.log("Checking availability with nearby with payload:", payload);

            const response = await api.post(`${API_BASE_URL}/check-availability-with-nearby`, payload);
            
            // Log the response for debugging
            console.log("Check availability with nearby response:", response.data);
            
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Hold tables for a reservation
    async holdTables(holdParams, sessionId = null) {
        try {
            // Generate a session ID if none is provided
            const actualSessionId = sessionId || this.generateSessionId();
            
            // Format the date and time into a proper reservationDateTime
            let reservationDateTime;
            try {
                // Try to combine date and time into a full ISO datetime string
                const dateStr = holdParams.date;
                const timeStr = holdParams.time;
                
                // Create proper ISO string format
                reservationDateTime = `${dateStr}T${timeStr}`;
                console.log("Created reservationDateTime:", reservationDateTime);
            } catch (error) {
                console.error("Error formatting reservationDateTime:", error);
                // Fallback to separate date and time if formatting fails
            }
            
            // Ensure all parameters are in the correct format
            const payload = {
                outletId: holdParams.outletId,
                partySize: parseInt(holdParams.partySize),
                reservationDateTime: reservationDateTime,
                date: holdParams.date, // Keep for backward compatibility
                time: holdParams.time, // Keep for backward compatibility
                sessionId: actualSessionId
            };

            console.log("Sending hold tables request with payload:", payload);

            const response = await api.post(`${API_BASE_URL}/hold-tables`, payload);
            
            // Log the response for debugging
            console.log("Hold tables response:", response.data);
            
            // Store the holdId in localStorage for backup retrieval if needed
            if (response.data && (response.data.holdId || response.data.id)) {
                const holdId = response.data.holdId || response.data.id;
                localStorage.setItem('reservation_hold_id', holdId);
                console.log("Stored holdId in localStorage:", holdId);
            }
            
            return response;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Generate a random session ID for table holds
    generateSessionId() {
        // Generate a random string that can be used as a session ID
        return 'session_' + Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
    }

    // Release a table hold
    async releaseHold(holdId) {
        try {
            const response = await api.post(`${API_BASE_URL}/release-hold/${holdId}`);
            return response;
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
            // Make sure reservationDate is properly formatted
            let reservationDate = reservationData.reservationDate;
            
            // Log the original value for debugging
            console.log("Original reservationDate:", reservationDate);
            
            // Format the payload
            const payload = {
                outletId: reservationData.outletId,
                customerName: reservationData.customerName,
                customerPhone: reservationData.customerPhone,
                customerEmail: reservationData.customerEmail,
                partySize: parseInt(reservationData.partySize),
                reservationDate: reservationDate, // Already formatted by the calling component
                specialRequests: reservationData.specialRequests || "",
                holdId: reservationData.holdId || null,
                sessionId: reservationData.sessionId || null
            };

            console.log("Sending create reservation request with payload:", payload);

            const response = await api.post(`${API_BASE_URL}`, payload);
            
            // Log the response for debugging
            console.log("Create reservation API response:", response.data);
            
            return response;
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
            
            // Log more details about validation errors
            if (error.response.data && error.response.data.errors) {
                console.error('Validation errors:', error.response.data.errors);
            }
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