import React, { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQueue } from "../../contexts/QueueContext";

const QueueStatus = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const {
        queueDetails,
        getQueueStatus,
        cancelQueue,
        confirmArrival,
        clearQueueDetails,
        loading,
        error
    } = useQueue();

    const [showCancelModal, setShowCancelModal] = useState(false);
    const [showConfirmModal, setShowConfirmModal] = useState(false);
    const [timeLeft, setTimeLeft] = useState(60); // Countdown timer for confirmation (60 seconds)
    const [showTableReadyNotification, setShowTableReadyNotification] = useState(false);
    const [isConfirmingArrival, setIsConfirmingArrival] = useState(false);

    // Check queue status on mount and set up polling
    useEffect(() => {
        const fetchStatus = async () => {
            if (id) {
                await getQueueStatus(id);
            }
        };

        fetchStatus();

        // Cleanup function will handle unsubscribing from WebSocket or stopping polling
        return () => {
            clearQueueDetails();
        };
    }, [id, getQueueStatus, clearQueueDetails]);

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

    // Handle cancel queue
    const handleCancelQueue = async () => {
        try {
            await cancelQueue(id);
            setShowCancelModal(false);
            // After successful cancellation, show a success message or navigate elsewhere
            navigate("/");
        } catch (error) {
            console.error("Error cancelling queue:", error);
        }
    };

    // Handle confirm arrival
    const handleConfirmArrival = async () => {
        setIsConfirmingArrival(true);
        try {
            await confirmArrival(id);
            setShowConfirmModal(false);
            setShowTableReadyNotification(false);
            // After confirmation, navigate to the confirmation page
            navigate(`/queue/confirm/${id}`);
        } catch (error) {
            console.error("Error confirming arrival:", error);
        } finally {
            setIsConfirmingArrival(false);
        }
    };

    // Render loading state
    if (loading && !queueDetails) {
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

                {/* Queue Position and Wait Time */}
                {queueDetails.status === 'Waiting' && (
                    <div className="mb-6">
                        <div className="grid grid-cols-2 gap-4">
                            <div className="bg-gray-50 p-4 rounded-lg text-center">
                                <h3 className="text-gray-500 text-sm mb-1">Position</h3>
                                <p className="text-4xl font-bold text-green-600">{queueDetails.position}</p>
                            </div>
                            <div className="bg-gray-50 p-4 rounded-lg text-center">
                                <h3 className="text-gray-500 text-sm mb-1">Estimated Wait</h3>
                                <p className="text-4xl font-bold text-green-600">{queueDetails.estimatedWaitTime}</p>
                                <p className="text-xs text-gray-500">minutes</p>
                            </div>
                        </div>

                        <div className="mt-4 bg-blue-50 border border-blue-200 text-blue-700 p-3 rounded text-sm">
                            <p>
                                <span className="font-medium">Keep this page open</span> to receive real-time updates.
                                You'll also receive WhatsApp notifications when your queue status changes.
                            </p>
                        </div>
                    </div>
                )}

                {/* Table Ready Status */}
                {queueDetails.status === 'Ready' && (
                    <div className="mb-6 bg-green-50 border border-green-200 p-4 rounded-lg">
                        <h3 className="font-bold text-lg text-green-800 mb-2">Your Table is Ready!</h3>
                        <p className="text-green-700 mb-4">
                            Please confirm your arrival within {formatTime(timeLeft)} or your spot may be given to the next person in line.
                        </p>
                        <button
                            onClick={() => setShowConfirmModal(true)}
                            className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded"
                        >
                            Confirm Arrival
                        </button>
                    </div>
                )}

                {/* Seated Status */}
                {queueDetails.status === 'Seated' && (
                    <div className="mb-6 bg-purple-50 border border-purple-200 p-4 rounded-lg">
                        <h3 className="font-bold text-lg text-purple-800 mb-2">You've Been Seated</h3>
                        <p className="text-purple-700">
                            Enjoy your meal! Your table number is {queueDetails.tableNumber || 'being prepared'}.
                        </p>
                    </div>
                )}

                {/* Cancelled Status */}
                {queueDetails.status === 'Cancelled' && (
                    <div className="mb-6 bg-red-50 border border-red-200 p-4 rounded-lg">
                        <h3 className="font-bold text-lg text-red-800 mb-2">Queue Cancelled</h3>
                        <p className="text-red-700 mb-4">
                            This queue entry has been cancelled.
                        </p>
                        <button
                            onClick={() => navigate('/queue/join')}
                            className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded"
                        >
                            Join a New Queue
                        </button>
                    </div>
                )}

                {/* Customer Details */}
                <div className="bg-gray-50 p-5 rounded-lg mb-6">
                    <h3 className="font-medium mb-3">Details</h3>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <p className="text-gray-500 text-sm">Name</p>
                            <p className="font-medium">{queueDetails.customerName}</p>
                        </div>
                        <div>
                            <p className="text-gray-500 text-sm">Phone</p>
                            <p className="font-medium">{queueDetails.customerPhone}</p>
                        </div>
                        <div>
                            <p className="text-gray-500 text-sm">Email</p>
                            <p className="font-medium">{queueDetails.customerEmail || "Not provided"}</p>
                        </div>
                        <div>
                            <p className="text-gray-500 text-sm">Party Size</p>
                            <p className="font-medium">{queueDetails.partySize} {queueDetails.partySize === 1 ? 'person' : 'people'}</p>
                        </div>
                        <div>
                            <p className="text-gray-500 text-sm">Joined At</p>
                            <p className="font-medium">
                                {new Date(queueDetails.joinedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                            </p>
                        </div>
                        {queueDetails.specialRequests && (
                            <div className="col-span-2">
                                <p className="text-gray-500 text-sm">Special Requests</p>
                                <p className="font-medium">{queueDetails.specialRequests}</p>
                            </div>
                        )}
                    </div>
                </div>

                {/* Action Buttons */}
                <div className="flex flex-col md:flex-row space-y-2 md:space-y-0 md:space-x-3">
                    <button
                        onClick={() => navigate("/")}
                        className="md:flex-1 bg-white border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                    >
                        Back to Home
                    </button>

                    {queueDetails.status === 'Waiting' && (
                        <button
                            onClick={() => setShowCancelModal(true)}
                            className="md:flex-1 bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded"
                        >
                            Cancel Queue
                        </button>
                    )}
                </div>
            </div>

            {/* Cancel Confirmation Modal */}
            {showCancelModal && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white p-6 rounded-lg shadow-lg max-w-md w-full">
                        <h3 className="text-xl font-bold mb-4">Cancel Queue</h3>
                        <p className="mb-6">Are you sure you want to cancel your position in the queue? You'll lose your current spot.</p>

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => setShowCancelModal(false)}
                                className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                            >
                                Keep My Spot
                            </button>

                            <button
                                onClick={handleCancelQueue}
                                className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-6 rounded"
                            >
                                Cancel Queue
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Confirm Arrival Modal */}
            {showConfirmModal && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white p-6 rounded-lg shadow-lg max-w-md w-full">
                        <h3 className="text-xl font-bold mb-4">Confirm Arrival</h3>
                        <p className="mb-6">Please confirm that you have arrived at the restaurant and are ready to be seated.</p>

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => setShowConfirmModal(false)}
                                className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                                disabled={isConfirmingArrival}
                            >
                                Not Yet
                            </button>

                            <button
                                onClick={handleConfirmArrival}
                                disabled={isConfirmingArrival}
                                className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                            >
                                {isConfirmingArrival ? (
                                    <span className="flex items-center">
                                        <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                        </svg>
                                        Confirming...
                                    </span>
                                ) : "I'm Here"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default QueueStatus;