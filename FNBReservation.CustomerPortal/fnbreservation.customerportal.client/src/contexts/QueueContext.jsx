import React, { createContext, useState, useContext, useCallback, useEffect } from 'react';
import QueueService from '../services/QueueService';

// Create context
const QueueContext = createContext();

export const useQueue = () => useContext(QueueContext);

export const QueueProvider = ({ children }) => {
    // Queue state
    const [queueDetails, setQueueDetails] = useState(null);
    const [userQueueEntries, setUserQueueEntries] = useState([]);
    const [queueEstimation, setQueueEstimation] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [websocket, setWebsocket] = useState(null);
    const [isConnected, setIsConnected] = useState(false);
    const [updateInterval, setUpdateInterval] = useState(null);

    // Create queue service instance
    const queueService = QueueService;

    // Join queue
    const joinQueue = useCallback(async (queueData) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockJoinQueue(queueData);
            setQueueDetails(response);

            // Start polling for updates until WebSocket is set up
            startPollingUpdates(response.id);

            return response;
        } catch (err) {
            setError('Failed to join queue. Please try again.');
            console.error('Error joining queue:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get queue status
    const getQueueStatus = useCallback(async (queueId) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockGetQueueStatus(queueId);
            setQueueDetails(response);
            return response;
        } catch (err) {
            setError('Failed to get queue status. Please try again.');
            console.error('Error getting queue status:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get queue status by code
    const getQueueStatusByCode = useCallback(async (code) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockGetQueueStatusByCode(code);
            setQueueDetails(response);

            // Start polling for updates
            startPollingUpdates(response.id);

            return response;
        } catch (err) {
            setError('Failed to get queue status. Please try again.');
            console.error('Error getting queue status by code:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get queue by phone
    const getQueueByPhone = useCallback(async (phone) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockGetQueueByPhone(phone);
            setUserQueueEntries(response.queueEntries || []);
            return response;
        } catch (err) {
            setError('Failed to get queue entries. Please try again.');
            console.error('Error getting queue by phone:', err);
            return { queueEntries: [] };
        } finally {
            setLoading(false);
        }
    }, []);

    // Cancel queue
    const cancelQueue = useCallback(async (queueId) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockCancelQueue(queueId);

            // Update queue details if it's the current viewed queue
            if (queueDetails && queueDetails.id === queueId) {
                setQueueDetails({
                    ...queueDetails,
                    status: 'Cancelled'
                });
            }

            // Update user queue entries if it exists in the list
            setUserQueueEntries(prevEntries =>
                prevEntries.map(entry =>
                    entry.id === queueId
                        ? { ...entry, status: 'Cancelled' }
                        : entry
                )
            );

            // Stop polling for updates
            stopPollingUpdates();

            return response;
        } catch (err) {
            setError('Failed to cancel queue. Please try again.');
            console.error('Error cancelling queue:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, [queueDetails]);

    // Confirm arrival
    const confirmArrival = useCallback(async (queueId) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockConfirmArrival(queueId);

            // Update queue details if it's the current viewed queue
            if (queueDetails && queueDetails.id === queueId) {
                setQueueDetails({
                    ...queueDetails,
                    status: 'Arrived',
                    tableNumber: response.tableNumber
                });
            }

            // Update user queue entries if it exists in the list
            setUserQueueEntries(prevEntries =>
                prevEntries.map(entry =>
                    entry.id === queueId
                        ? { ...entry, status: 'Arrived', tableNumber: response.tableNumber }
                        : entry
                )
            );

            // Stop polling for updates
            stopPollingUpdates();

            return response;
        } catch (err) {
            setError('Failed to confirm arrival. Please try again.');
            console.error('Error confirming arrival:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, [queueDetails]);

    // Get queue estimation
    const getQueueEstimation = useCallback(async (outletId, partySize) => {
        setLoading(true);
        setError(null);

        try {
            // Use mock method for development
            const response = await queueService.mockGetQueueEstimation(outletId, partySize);
            setQueueEstimation(response);
            return response;
        } catch (err) {
            setError('Failed to get queue estimation. Please try again.');
            console.error('Error getting queue estimation:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Setup WebSocket connection for real-time updates
    const setupWebSocket = useCallback((queueId) => {
        // Placeholder for actual WebSocket implementation
        // In a real app, you would establish a WebSocket connection here

        console.log('Setting up WebSocket for queue:', queueId);

        // For now, we'll just simulate with a flag
        setIsConnected(true);
    }, []);

    // Close WebSocket connection
    const closeWebSocket = useCallback(() => {
        if (websocket) {
            // Placeholder for actual WebSocket closure
            // websocket.close();
            setWebsocket(null);
        }
        setIsConnected(false);
    }, [websocket]);

    // Start polling for updates (until WebSocket is implemented)
    const startPollingUpdates = useCallback((queueId) => {
        // Clear any existing intervals
        stopPollingUpdates();

        // Set up new interval to poll for updates every 10 seconds
        const interval = setInterval(async () => {
            if (queueId) {
                try {
                    await getQueueStatus(queueId);
                } catch (error) {
                    console.error('Error polling for updates:', error);
                }
            }
        }, 10000); // Poll every 10 seconds

        setUpdateInterval(interval);
    }, [getQueueStatus]);

    // Stop polling for updates
    const stopPollingUpdates = useCallback(() => {
        if (updateInterval) {
            clearInterval(updateInterval);
            setUpdateInterval(null);
        }
    }, [updateInterval]);

    // Clear queue details
    const clearQueueDetails = useCallback(() => {
        setQueueDetails(null);
        stopPollingUpdates();
    }, [stopPollingUpdates]);

    // Clear error
    const clearError = useCallback(() => {
        setError(null);
    }, []);

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            closeWebSocket();
            stopPollingUpdates();
        };
    }, [closeWebSocket, stopPollingUpdates]);

    // Context value
    const value = {
        queueDetails,
        userQueueEntries,
        queueEstimation,
        loading,
        error,
        isConnected,
        joinQueue,
        getQueueStatus,
        getQueueStatusByCode,
        getQueueByPhone,
        cancelQueue,
        confirmArrival,
        getQueueEstimation,
        setupWebSocket,
        clearQueueDetails,
        clearError
    };

    return (
        <QueueContext.Provider value={value}>
            {children}
        </QueueContext.Provider>
    );
};

export default QueueContext;