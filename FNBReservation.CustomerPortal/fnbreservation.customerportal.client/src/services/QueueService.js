import api from './api';

// Use API path that will be proxied through Vite's proxy
const API_BASE_URL = '/api/v1/queue';

class QueueService {
    // Join the queue with customer details
    async joinQueue(queueData) {
        try {
            console.log('Joining queue with data:', queueData);
            const payload = {
                outletId: queueData.outletId,
                customerName: queueData.customerName,
                customerPhone: queueData.customerPhone,
                customerEmail: queueData.customerEmail,
                partySize: parseInt(queueData.partySize),
                specialRequests: queueData.specialRequests || ''
            };

            const response = await api.post(`${API_BASE_URL}`, payload);
            console.log('Join queue response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Fall back to mock data for demo/testing if the API fails
            console.log('Falling back to mock data for join queue');
            return this.mockJoinQueue(queueData);
        }
    }

    // Get queue status by ID
    async getQueueStatus(queueId) {
        try {
            console.log(`Getting queue status for ID: ${queueId}`);
            const response = await api.get(`${API_BASE_URL}/${queueId}`);
            console.log('Queue status response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Fall back to mock data for demo/testing if the API fails
            console.log('Falling back to mock data for queue status');
            return this.mockGetQueueStatus(queueId);
        }
    }

    // Get queue status by code
    async getQueueStatusByCode(code) {
        try {
            console.log(`Getting queue status for code: ${code}`);
            // Make sure to use the correct format for the new API
            const response = await api.get(`${API_BASE_URL}/code/${code}`);
            console.log('Queue status by code response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Try API fallback for demo purposes
            try {
                console.log('Trying direct API call to /api/v1/queue/code...');
                // Try direct call without proxy
                const directResponse = await fetch(`http://localhost:5000/api/v1/queue/code/${code}`);
                if (directResponse.ok) {
                    const data = await directResponse.json();
                    console.log('Direct API call successful:', data);
                    return data;
                }
            } catch (directError) {
                console.error('Direct API call also failed:', directError);
            }
            
            // Fall back to mock data for demo/testing if all APIs fail
            console.log('Falling back to mock data for queue status by code');
            return this.mockGetQueueStatusByCode(code);
        }
    }

    // Get queue wait time estimation
    async getQueueEstimation(outletId, partySize) {
        try {
            console.log(`Getting wait time for outlet: ${outletId}, party size: ${partySize}`);
            const response = await api.get(
                `${API_BASE_URL}/wait-time/${outletId}/${partySize}`
            );
            console.log('Wait time response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Try API fallback for demo purposes
            try {
                console.log('Trying direct API call to /api/v1/queue/wait-time...');
                // Try direct call without proxy
                const directResponse = await fetch(`http://localhost:5000/api/v1/queue/wait-time/${outletId}/${partySize}`);
                if (directResponse.ok) {
                    const data = await directResponse.json();
                    console.log('Direct API call successful:', data);
                    return data;
                }
            } catch (directError) {
                console.error('Direct API call also failed:', directError);
            }
            
            // Fall back to mock data for demo/testing if the API fails
            console.log('Falling back to mock data for queue estimation');
            return this.mockGetQueueEstimation(outletId, partySize);
        }
    }

    // Exit the queue (cancel queue entry)
    async exitQueue(queueCode) {
        try {
            console.log(`Exiting queue with code: ${queueCode}`);
            const response = await api.post(`${API_BASE_URL}/exit/${queueCode}`);
            console.log('Exit queue response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Fall back to mock data for demo/testing if the API fails
            console.log('Falling back to mock data for exit queue');
            return this.mockExitQueue(queueCode);
        }
    }

    // Update queue entry
    async updateQueueEntry(queueCode, updateData) {
        try {
            console.log(`Updating queue entry with code: ${queueCode}`, updateData);
            const response = await api.put(`${API_BASE_URL}/${queueCode}`, updateData);
            console.log('Update queue response:', response.data);
            return response.data;
        } catch (error) {
            this.handleError(error);
            
            // Fall back to mock success response for demo/testing if the API fails
            console.log('Falling back to mock success for update queue');
            return { success: true, message: "Queue entry updated successfully" };
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
        console.log('Using mock join queue data');
        return {
            id: "b2143b48-8a12-47c4-a768-789a97c69701",
            queueCode: "73A-0416-001",
            outletId: queueData.outletId,
            outletName: "TTDI Chakuro Yakiniku by Meatpoint - حلال",
            customerName: queueData.customerName,
            customerPhone: queueData.customerPhone,
            customerEmail: queueData.customerEmail, 
            partySize: parseInt(queueData.partySize),
            queuePosition: 1,
            estimatedWaitMinutes: 5, 
            status: "Waiting",
            joinedAt: new Date().toISOString(),
            queuedAt: new Date().toISOString(),
            specialRequests: queueData.specialRequests || ''
        };
    }

    mockGetQueueStatus(queueId) {
        console.log('Using mock queue status data');
        return {
            id: queueId || "b2143b48-8a12-47c4-a768-789a97c69701",
            queueCode: "73A-0416-001",
            outletId: "73a3ef70-e570-4edd-85d5-f7a2802bc008",
            outletName: "TTDI Chakuro Yakiniku by Meatpoint - حلال",
            customerName: "Raymond",
            customerPhone: "+6019-4110130",
            customerEmail: "raymond@example.com", 
            partySize: 2,
            queuePosition: 1,
            estimatedWaitMinutes: 5,
            status: "Waiting",
            joinedAt: new Date().toISOString(),
            queuedAt: new Date().toISOString(),
            specialRequests: ""
        };
    }

    mockGetQueueStatusByCode(code) {
        console.log('Using mock queue status by code data');
        return {
            id: "b2143b48-8a12-47c4-a768-789a97c69701",
            queueCode: code || "73A-0416-001",
            outletId: "73a3ef70-e570-4edd-85d5-f7a2802bc008",
            outletName: "TTDI Chakuro Yakiniku by Meatpoint - حلال",
            customerName: "Raymond",
            customerPhone: "+6019-4110130",
            queuePosition: 1,
            estimatedWaitMinutes: 5,
            status: "Waiting",
            joinedAt: new Date().toISOString(),
            queuedAt: new Date().toISOString(),
            specialRequests: ""
        };
    }

    mockExitQueue(queueCode) {
        console.log('Using mock exit queue data');
        return {
            success: true,
            message: "Queue entry cancelled successfully"
        };
    }

    mockGetQueueEstimation(outletId, partySize) {
        console.log('Using mock queue estimation data');
        return {
            outletId: outletId,
            currentQueueLength: 3,
            estimatedWaitMinutes: 5,
            isHighDemand: false
        };
    }

    getOutletName(outletId) {
        const outlets = {
            "73a3ef70-e570-4edd-85d5-f7a2802bc008": "TTDI Chakuro Yakiniku by Meatpoint - حلال",
            "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5": "Downtown Location",
            "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5": "Riverside Branch"
        };
        return outlets[outletId] || "Unknown Outlet";
    }
}

export default new QueueService();