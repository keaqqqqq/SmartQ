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
            // Remove spaces to format the phone for the API
            const phoneWithoutFormatting = phone.replace(/\s+/g, '');
            
            console.log(`Searching for reservations with phone number: ${phoneWithoutFormatting}`);
            
            // Make the API call
            const response = await api.get(`${API_BASE_URL}/phone/${encodeURIComponent(phoneWithoutFormatting)}`);
            
            // Log response for debugging
            console.log("API Response from phone search:", response.data);
            
            // For compatibility, ensure we maintain a consistent response format
            // Response could be an array or an object with reservations property
            if (Array.isArray(response.data)) {
                console.log(`Found ${response.data.length} reservations from array response`);
                return response.data;
            } else if (response.data && response.data.reservations) {
                console.log(`Found ${response.data.reservations.length} reservations from object response`);
                return response.data;
            } else {
                console.log("No reservations found or unexpected response format");
                return []; // Return empty array for consistency
            }
        } catch (error) {
            this.handleError(error);
            console.error("Error searching reservations by phone:", error);
            throw error;
        }
    }

    // Update an existing reservation
    async updateReservation(reservationData) {
        try {
            // Log the data being sent to help debugging
            console.log("Update reservation payload:", JSON.stringify(reservationData, null, 2));
            
            // Make sure all required fields are present
            if (!reservationData.id) {
                throw new Error("Reservation ID is required for updates");
            }
            
            // Ensure the payload matches the API expectations
            const payload = {
                customerName: reservationData.customerName,
                customerPhone: reservationData.customerPhone,
                customerEmail: reservationData.customerEmail || "",
                partySize: Number(reservationData.partySize),
                reservationDate: reservationData.reservationDate,
                specialRequests: reservationData.specialRequests || "",
                // Add hold and session ID if provided
                holdId: reservationData.holdId || null,
                sessionId: reservationData.sessionId || null
            };
            
            // Use the correct endpoint format: PUT /api/v1/reservations/{id}
            const response = await api.put(`${API_BASE_URL}/${reservationData.id}`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Cancel a reservation
    async cancelReservation(reservationId, reason = "Cancelled by customer") {
        try {
            // Create a proper cancelReservationDto object
            const cancelReservationDto = {
                reason: reason
            };
            
            const response = await api.put(`${API_BASE_URL}/${reservationId}/cancel`, cancelReservationDto);
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