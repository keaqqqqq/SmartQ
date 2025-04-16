import React, { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQueue } from "../../contexts/QueueContext";
import QueueService from "../../services/QueueService";

const QueueStatus = () => {
    const { id } = useParams(); // This will be the queue code now
    const navigate = useNavigate();
    const { 
        loading, 
        error, 
        setError 
    } = useQueue();

    const [queueDetails, setQueueDetails] = useState(null);
    const [showCancelModal, setShowCancelModal] = useState(false);
    const [showConfirmModal, setShowConfirmModal] = useState(false);
    const [timeLeft, setTimeLeft] = useState(60); // Countdown timer for confirmation (60 seconds)
    const [showTableReadyNotification, setShowTableReadyNotification] = useState(false);
    const [isConfirmingArrival, setIsConfirmingArrival] = useState(false);
    const [loadingStatus, setLoadingStatus] = useState(true);

    // Fetch queue status
    const fetchQueueStatus = useCallback(async () => {
        if (!id) return;
        
        setLoadingStatus(true);
        try {
            const data = await QueueService.getQueueStatusByCode(id);
            setQueueDetails(data);
            setError(null);
        } catch (error) {
            console.error("Error fetching queue status:", error);
            setError("Failed to load queue status. Please try again.");
        } finally {
            setLoadingStatus(false);
        }
    }, [id, setError]);

    // Check queue status on mount and set up polling
    useEffect(() => {
        fetchQueueStatus();
        
        // Set up polling (check every 30 seconds)
        const intervalId = setInterval(() => {
            fetchQueueStatus();
        }, 30000);

        // Cleanup function will handle stopping polling
        return () => {
            clearInterval(intervalId);
        };
    }, [fetchQueueStatus]);

    // Listen for changes in queueDetails to show table ready notification
    useEffect(() => {
        if (queueDetails && queueDetails.status === 'Ready') {
            setShowTableReadyNotification(true);

            // Request browser notification permission if not already granted
            if (Notification.permission !== 'granted' && Notification.permission !== 'denied') {
                Notification.requestPermission();
            }

            // Show browser notification
            if (Notification.permission === 'granted') {
                new Notification('Your table is ready!', {
                    body: 'Please confirm your arrival within 5 minutes.',
                    icon: '/images/logo.png' // Make sure this path exists
                });
            }

            // Start countdown timer for confirmation
            setTimeLeft(300); // 5 minutes (300 seconds)
        }
    }, [queueDetails]);

    // Handle countdown timer
    useEffect(() => {
        let timer;
        if (queueDetails && queueDetails.status === 'Ready' && timeLeft > 0) {
            timer = setInterval(() => {
                setTimeLeft(prev => prev - 1);
            }, 1000);
        } else if (timeLeft === 0 && queueDetails && queueDetails.status === 'Ready') {
            // Auto-cancel when time expires
            handleCancelQueue();
        }

        return () => {
            if (timer) clearInterval(timer);
        };
    }, [timeLeft, queueDetails]);

    // Format time display
    const formatTime = (seconds) => {
        const minutes = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${minutes}:${secs < 10 ? '0' : ''}${secs}`;
    };

    // Handle cancel queue (exit queue)
    const handleCancelQueue = async () => {
        try {
            await QueueService.exitQueue(id);
            setShowCancelModal(false);
            // After successful cancellation, update queue details
            fetchQueueStatus();
            // Show success message or navigate elsewhere
            navigate("/", { state: { message: "Successfully exited the queue" } });
        } catch (error) {
            console.error("Error exiting queue:", error);
            setError("Failed to exit queue. Please try again.");
        }
    };

    // Handle confirm arrival
    const handleConfirmArrival = async () => {
        setIsConfirmingArrival(true);
        try {
            // Since we're using a different API structure, update the approach
            const updatedData = {
                status: "Seated"
            };
            await QueueService.updateQueueEntry(id, updatedData);
            setShowConfirmModal(false);
            setShowTableReadyNotification(false);
            // After confirmation, update the queue details
            fetchQueueStatus();
            navigate(`/queue/confirm/${id}`);
        } catch (error) {
            console.error("Error confirming arrival:", error);
            setError("Failed to confirm arrival. Please try again.");
        } finally {
            setIsConfirmingArrival(false);
        }
    };

    // Render loading state
    if (loadingStatus && !queueDetails) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return (
            <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4" role="alert">
                    <span className="block sm:inline">{error}</span>
                </div>
                <button
                    onClick={() => navigate('/')}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Back to Home
                </button>
            </div>
        );
    }

    // Render no data state
    if (!queueDetails) {
        return (
            <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
                <h2 className="text-xl font-bold mb-4">Queue Entry Not Found</h2>
                <p className="mb-4">Sorry, we couldn't find the queue entry you're looking for.</p>
                <button
                    onClick={() => navigate('/queue/join')}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Join a New Queue
                </button>
            </div>
        );
    }

    return (
        <div className="max-w-2xl mx-auto px-4 py-8">
            {/* Table Ready Notification */}
            {showTableReadyNotification && (
                <div className="fixed inset-x-0 top-0 z-50 p-4 flex justify-center">
                    <div className="bg-green-600 text-white rounded-lg shadow-xl p-4 w-full max-w-md animate-bounce">
                        <div className="flex items-center">
                            <svg className="h-8 w-8 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <div>
                                <h3 className="font-bold text-lg">Your Table is Ready!</h3>
                                <p className="text-sm">Please confirm your arrival within {formatTime(timeLeft)}</p>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Main Content */}
            <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                <div className="flex items-center justify-between mb-6">
                    <h1 className="text-2xl font-bold">Queue Status</h1>
                    <div className={`px-3 py-1 rounded-full text-sm font-medium ${queueDetails.status === 'Waiting' ? 'bg-blue-100 text-blue-800' :
                        queueDetails.status === 'Ready' ? 'bg-green-100 text-green-800' :
                            queueDetails.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                                queueDetails.status === 'Seated' ? 'bg-purple-100 text-purple-800' :
                                    'bg-gray-100 text-gray-800'
                        }`}>
                        {queueDetails.status}
                    </div>
                </div>

                {/* Restaurant Info */}
                <div className="flex items-center mb-6">
                    <div className="bg-gray-200 rounded-full p-2 mr-4">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                        </svg>
                    </div>
                    <div>
                        <p className="font-medium">{queueDetails.outletName}</p>
                        <p className="text-sm text-gray-600">Queue Code: {queueDetails.queueCode}</p>
                    </div>
                </div>

                {/* Queue Position Card */}
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-6 text-center">
                    {queueDetails.status === 'Waiting' ? (
                        <>
                            <h2 className="text-xl font-bold mb-2">Your Position in Queue</h2>
                            <div className="text-5xl font-bold text-blue-700 mb-2">{queueDetails.position}</div>
                            <p className="text-gray-700 mb-2">Estimated Wait Time</p>
                            <div className="text-3xl font-bold text-blue-600">
                                {queueDetails.estimatedWaitTime ? `${queueDetails.estimatedWaitTime} mins` : 'Calculating...'}
                            </div>
                        </>
                    ) : queueDetails.status === 'Ready' ? (
                        <>
                            <h2 className="text-xl font-bold mb-4 text-green-700">Your Table is Ready!</h2>
                            <p className="text-gray-700 mb-2">Please proceed to the restaurant.</p>
                            <p className="font-medium">Time remaining to confirm: {formatTime(timeLeft)}</p>
                        </>
                    ) : queueDetails.status === 'Seated' ? (
                        <>
                            <h2 className="text-xl font-bold mb-4 text-purple-700">You're Seated!</h2>
                            <p className="text-gray-700">Enjoy your meal!</p>
                        </>
                    ) : queueDetails.status === 'Cancelled' ? (
                        <>
                            <h2 className="text-xl font-bold mb-4 text-red-700">Queue Entry Cancelled</h2>
                            <p className="text-gray-700">Your queue entry has been cancelled.</p>
                        </>
                    ) : (
                        <p className="text-xl">Queue status: {queueDetails.status}</p>
                    )}
                </div>

                {/* Queue Details */}
                <div className="mb-6">
                    <h2 className="text-lg font-semibold mb-3">Booking Details</h2>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <p className="text-sm text-gray-600">Name</p>
                            <p className="font-medium">{queueDetails.customerName}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Party Size</p>
                            <p className="font-medium">{queueDetails.partySize} {queueDetails.partySize === 1 ? 'person' : 'people'}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Phone</p>
                            <p className="font-medium">{queueDetails.customerPhone}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Joined At</p>
                            <p className="font-medium">{new Date(queueDetails.joinedAt).toLocaleTimeString()}</p>
                        </div>
                    </div>
                </div>

                {/* Special Requests */}
                {queueDetails.specialRequests && (
                    <div className="mb-6">
                        <h2 className="text-lg font-semibold mb-2">Special Requests</h2>
                        <p className="bg-gray-50 p-3 rounded">{queueDetails.specialRequests}</p>
                    </div>
                )}

                {/* Action Buttons */}
                <div className="mt-8 flex flex-col space-y-3">
                    {queueDetails.status === 'Waiting' && (
                        <button
                            onClick={() => setShowCancelModal(true)}
                            className="w-full py-3 px-4 border border-red-500 text-red-500 rounded-md hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500"
                        >
                            Exit Queue
                        </button>
                    )}

                    {queueDetails.status === 'Ready' && (
                        <button
                            onClick={() => setShowConfirmModal(true)}
                            className="w-full py-3 px-4 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500"
                        >
                            Confirm Arrival
                        </button>
                    )}

                    <button
                        onClick={() => window.location.reload()}
                        className="w-full py-3 px-4 border border-blue-500 text-blue-500 rounded-md hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                        Refresh Status
                    </button>
                </div>
            </div>

            {/* Cancel Modal */}
            {showCancelModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
                    <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md">
                        <h3 className="text-xl font-bold mb-4">Exit Queue?</h3>
                        <p className="mb-6">Are you sure you want to exit the queue? This action cannot be undone.</p>
                        <div className="flex space-x-3">
                            <button
                                onClick={() => setShowCancelModal(false)}
                                className="flex-1 py-2 px-4 border border-gray-300 rounded-md hover:bg-gray-50"
                            >
                                No, Stay in Queue
                            </button>
                            <button
                                onClick={handleCancelQueue}
                                className="flex-1 py-2 px-4 bg-red-600 text-white rounded-md hover:bg-red-700"
                            >
                                Yes, Exit Queue
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Confirm Arrival Modal */}
            {showConfirmModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
                    <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md">
                        <h3 className="text-xl font-bold mb-4">Confirm Your Arrival</h3>
                        <p className="mb-6">Please confirm that you have arrived at the restaurant and are ready to be seated.</p>
                        <div className="flex space-x-3">
                            <button
                                onClick={() => setShowConfirmModal(false)}
                                className="flex-1 py-2 px-4 border border-gray-300 rounded-md hover:bg-gray-50"
                                disabled={isConfirmingArrival}
                            >
                                Cancel
                            </button>
                            <button
                                onClick={handleConfirmArrival}
                                className="flex-1 py-2 px-4 bg-green-600 text-white rounded-md hover:bg-green-700"
                                disabled={isConfirmingArrival}
                            >
                                {isConfirmingArrival ? 'Confirming...' : 'Confirm Arrival'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default QueueStatus;