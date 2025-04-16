import api from './api';

// Use API path that will be proxied through Vite's proxy
const API_BASE_URL = '/api/v1/queue';

class QueueService {
    // Join the queue with customer details
    async joinQueue(queueData) {
        try {
            const payload = {
                outletId: queueData.outletId,
                customerName: queueData.customerName,
                customerPhone: queueData.customerPhone,
                customerEmail: queueData.customerEmail,
                partySize: parseInt(queueData.partySize),
                specialRequests: queueData.specialRequests || ''
            };

            const response = await api.post(`${API_BASE_URL}`, payload);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get queue status by ID
    async getQueueStatus(queueId) {
        try {
            const response = await api.get(`${API_BASE_URL}/${queueId}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get queue status by code
    async getQueueStatusByCode(code) {
        try {
            const response = await api.get(`${API_BASE_URL}/code/${code}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Get queue wait time estimation
    async getQueueEstimation(outletId, partySize) {
        try {
            const response = await api.get(
                `${API_BASE_URL}/wait-time/${outletId}/${partySize}`
            );
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Exit the queue (cancel queue entry)
    async exitQueue(queueCode) {
        try {
            const response = await api.post(`${API_BASE_URL}/exit/${queueCode}`);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Update queue entry
    async updateQueueEntry(queueCode, updateData) {
        try {
            const response = await api.put(`${API_BASE_URL}/${queueCode}`, updateData);
            return response.data;
        } catch (error) {
            this.handleError(error);
            throw error;
        }
    }

    // Error handling helper method
    handleError(error) {
        // Log the error
        console.error('Queue API Error:', error);

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

    // ----------------------
    // Mock methods for development without backend
    // These can be used as fallbacks if the API is not available
    // ----------------------

    mockJoinQueue(queueData) {
        return new Promise((resolve) => {
            setTimeout(() => {
                resolve({
                    id: "q-" + Math.random().toString(36).substr(2, 9),
                    queueCode: "Q" + Math.floor(1000 + Math.random() * 9000),
                    outletId: queueData.outletId,
                    outletName: this.getOutletName(queueData.outletId),
                    customerName: queueData.customerName,
                    customerPhone: queueData.customerPhone,
                    customerEmail: queueData.customerEmail, 
                    partySize: parseInt(queueData.partySize),
                    position: Math.floor(1 + Math.random() * 10), 
                    estimatedWaitTime: Math.floor(10 + Math.random() * 50), 
                    status: "Waiting",
                    joinedAt: new Date().toISOString(),
                    specialRequests: queueData.specialRequests || ''
                });
            }, 1000);
        });
    }

    mockGetQueueStatus(queueId) {
        return new Promise((resolve) => {
            setTimeout(() => {
                // Decrease position and wait time to simulate queue progress
                const position = Math.floor(1 + Math.random() * 5); // Random position between 1-5
                resolve({
                    id: queueId,
                    queueCode: "Q" + queueId.substr(-4),
                    outletId: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
                    outletName: "Main Branch",
                    customerName: "John Doe",
                    customerPhone: "+60 12-345 6789",
                    customerEmail: "john.doe@example.com", 
                    partySize: 4,
                    position: position,
                    estimatedWaitTime: position * 8, // Each position is about 8 mins
                    status: "Waiting",
                    joinedAt: new Date(Date.now() - 20 * 60000).toISOString(), // Joined 20 mins ago
                    specialRequests: "Prefer window seat"
                });
            }, 800);
        });
    }

    mockGetQueueStatusByCode(code) {
        return this.mockGetQueueStatus("q-" + code.substr(-4));
    }

    mockExitQueue(queueCode) {
        return new Promise((resolve) => {
            setTimeout(() => {
                resolve({
                    success: true,
                    message: "Queue entry cancelled successfully"
                });
            }, 800);
        });
    }

    mockGetQueueEstimation(outletId, partySize) {
        return new Promise((resolve) => {
            setTimeout(() => {
                const baseWait = 15; // Base wait time in minutes
                const partyFactor = Math.max(1, partySize / 2); // Larger parties may wait longer
                const randomFactor = Math.random() * 10; // Add some randomness

                resolve({
                    outletId: outletId,
                    currentQueueLength: Math.floor(5 + Math.random() * 15),
                    estimatedWaitTime: Math.floor(baseWait * partyFactor + randomFactor),
                    isHighDemand: Math.random() > 0.7 // 30% chance it's high demand time
                });
            }, 600);
        });
    }

    getOutletName(outletId) {
        const outlets = {
            "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5": "Main Branch",
            "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5": "Downtown Location",
            "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5": "Riverside Branch"
        };
        return outlets[outletId] || "Unknown Outlet";
    }
}

export default new QueueService();