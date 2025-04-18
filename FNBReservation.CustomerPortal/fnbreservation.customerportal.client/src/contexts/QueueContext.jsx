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
    const [updateInterval, setUpdateInterval] = useState(null);

    // Join queue
    const joinQueue = useCallback(async (queueData) => {
        setLoading(true);
        setError(null);

        try {
            // Use real API method
            const response = await QueueService.joinQueue(queueData);
            setQueueDetails(response);
            return response;
        } catch (err) {
            setError('Failed to join queue. Please try again.');
            console.error('Error joining queue:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get queue status (by ID)
    const getQueueStatus = useCallback(async (queueId) => {
        setLoading(true);
        setError(null);

        try {
            // Use real API method
            const response = await QueueService.getQueueStatus(queueId);
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
            // Use real API method
            const response = await QueueService.getQueueStatusByCode(code);
            setQueueDetails(response);
            return response;
        } catch (err) {
            setError('Failed to get queue status. Please try again.');
            console.error('Error getting queue status by code:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Exit queue (cancel queue entry)
    const exitQueue = useCallback(async (queueCode) => {
        setLoading(true);
        setError(null);

        try {
            // Use real API method
            const response = await QueueService.exitQueue(queueCode);

            // Update queue details if it's the current viewed queue
            if (queueDetails && queueDetails.queueCode === queueCode) {
                setQueueDetails({
                    ...queueDetails,
                    status: 'Cancelled'
                });
            }

            // Update user queue entries if it exists in the list
            setUserQueueEntries(prevEntries =>
                prevEntries.map(entry =>
                    entry.queueCode === queueCode
                        ? { ...entry, status: 'Cancelled' }
                        : entry
                )
            );

            return response;
        } catch (err) {
            setError('Failed to exit queue. Please try again.');
            console.error('Error exiting queue:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, [queueDetails]);

    // Update queue entry
    const updateQueueEntry = useCallback(async (queueCode, updateData) => {
        setLoading(true);
        setError(null);

        try {
            // Use real API method
            const response = await QueueService.updateQueueEntry(queueCode, updateData);

            // Update queue details if it's the current viewed queue
            if (queueDetails && queueDetails.queueCode === queueCode) {
                setQueueDetails({
                    ...queueDetails,
                    ...updateData
                });
            }

            // Update user queue entries if it exists in the list
            setUserQueueEntries(prevEntries =>
                prevEntries.map(entry =>
                    entry.queueCode === queueCode
                        ? { ...entry, ...updateData }
                        : entry
                )
            );

            return response;
        } catch (err) {
            setError('Failed to update queue entry. Please try again.');
            console.error('Error updating queue entry:', err);
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
            // Use real API method
            const response = await QueueService.getQueueEstimation(outletId, partySize);
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
            stopPollingUpdates();
        };
    }, [stopPollingUpdates]);

    // Context value
    const value = {
        queueDetails,
        userQueueEntries,
        queueEstimation,
        loading,
        error,
        setError,
        joinQueue,
        getQueueStatus,
        getQueueStatusByCode,
        exitQueue,
        updateQueueEntry,
        getQueueEstimation,
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