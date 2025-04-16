import React, { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import QueueService from "../../services/QueueService";

const QueueConfirmation = () => {
    const { id } = useParams(); // This is the queue code
    const navigate = useNavigate();
    
    const [queueDetails, setQueueDetails] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [countdown, setCountdown] = useState(300); // Countdown for automatic redirection

    // Fetch queue status
    const fetchQueueStatus = useCallback(async () => {
        if (!id) return;
        
        setLoading(true);
        try {
            const data = await QueueService.getQueueStatusByCode(id);
            setQueueDetails(data);
            setError(null);
        } catch (error) {
            console.error("Error fetching queue status:", error);
            setError("Failed to load queue status. Please try again.");
        } finally {
            setLoading(false);
        }
    }, [id]);

    // Fetch queue status on mount
    useEffect(() => {
        fetchQueueStatus();
    }, [fetchQueueStatus]);

    // Countdown timer
    useEffect(() => {
        if (countdown > 0) {
            const timer = setTimeout(() => {
                setCountdown(countdown - 1);
            }, 1000);

            return () => clearTimeout(timer);
        } else {
            // Redirect to home when countdown reaches 0
            navigate('/');
        }
    }, [countdown, navigate]);

    // Loading state
    if (loading) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

    // Error state
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

    // If no queue details found
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
        <div className="max-w-2xl mx-auto px-4 py-12">
            <div className="bg-white rounded-lg shadow-md p-8 text-center">
                <div className="flex justify-center mb-6">
                    <div className="bg-green-100 rounded-full p-4">
                        <svg className="h-16 w-16 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                        </svg>
                    </div>
                </div>

                <h1 className="text-3xl font-bold text-green-600 mb-4">Arrival Confirmed!</h1>
                <p className="text-xl mb-6">Thank you for confirming your arrival.</p>

                <div className="mb-8 p-6 bg-gray-50 rounded-lg">
                    <h2 className="text-lg font-semibold mb-4">Booking Details</h2>
                    <div className="grid grid-cols-2 gap-4 text-left">
                        <div>
                            <p className="text-sm text-gray-600">Name</p>
                            <p className="font-medium">{queueDetails.customerName}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Party Size</p>
                            <p className="font-medium">{queueDetails.partySize} {queueDetails.partySize === 1 ? 'person' : 'people'}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Restaurant</p>
                            <p className="font-medium">{queueDetails.outletName}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-600">Queue Code</p>
                            <p className="font-medium">{queueDetails.queueCode}</p>
                        </div>
                    </div>
                </div>

                <p className="text-gray-600 mb-6">
                    The staff has been notified of your arrival. 
                    Please wait to be seated by the restaurant staff.
                </p>

                <p className="text-sm text-gray-500 mb-8">
                    You will be redirected to the home page in {Math.floor(countdown / 60)}:{(countdown % 60).toString().padStart(2, '0')}
                </p>

                <div className="flex space-x-4 justify-center">
                    <button
                        onClick={() => navigate('/')}
                        className="bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-8 rounded-lg"
                    >
                        Return to Home
                    </button>
                </div>
            </div>
        </div>
    );
};

export default QueueConfirmation;