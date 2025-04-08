import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQueue } from "../../contexts/QueueContext";

const QueueConfirmation = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const { queueDetails, getQueueStatus, loading, error } = useQueue();

    const [countdown, setCountdown] = useState(300); // Countdown for automatic redirection

    // Fetch queue status on mount
    useEffect(() => {
        const fetchQueueStatus = async () => {
            if (id) {
                await getQueueStatus(id);
            }
        };

        fetchQueueStatus();
    }, [id, getQueueStatus]);

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

    // Validation: check if queue entry exists and is in the correct state
    if (!queueDetails || (queueDetails.status !== 'Arrived' && queueDetails.status !== 'Seated')) {
        return (
            <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
                <h2 className="text-xl font-bold mb-4">Invalid Queue Status</h2>
                <p className="mb-4">This queue entry is not in the correct state for confirmation. Please check your queue status.</p>
                <button
                    onClick={() => navigate(`/queue/status/${id}`)}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Check Queue Status
                </button>
            </div>
        );
    }

    return (
        <div className="max-w-2xl mx-auto px-4 py-8">
            <div className="bg-white rounded-lg shadow-md p-6 text-center">
                <div className="w-20 h-20 bg-green-100 rounded-full mx-auto flex items-center justify-center mb-6">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-10 w-10 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                </div>

                <h1 className="text-2xl font-bold mb-4">Your Table is Ready!</h1>

                <div className="mb-6">
                    <p className="text-lg mb-2">
                        Please approach the host stand and show this screen.
                    </p>
                    <p className="text-gray-600">
                        A staff member will escort you to your table.
                    </p>
                </div>

                <div className="bg-gray-50 p-6 rounded-lg mb-6 text-left">
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <p className="text-sm text-gray-500">Queue Code</p>
                            <p className="text-2xl font-bold">{queueDetails.queueCode}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-500">Restaurant</p>
                            <p className="font-medium">{queueDetails.outletName}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-500">Name</p>
                            <p className="font-medium">{queueDetails.customerName}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-500">Party Size</p>
                            <p className="font-medium">{queueDetails.partySize} {queueDetails.partySize === 1 ? 'person' : 'people'}</p>
                        </div>
                        <div>
                            <p className="text-sm text-gray-500">Email</p>
                            <p className="font-medium">{queueDetails.customerEmail || "Not provided"}</p>
                        </div>
                        {queueDetails.tableNumber && (
                            <div className="col-span-2">
                                <p className="text-sm text-gray-500">Table Number</p>
                                <p className="text-xl font-bold text-green-600">{queueDetails.tableNumber}</p>
                            </div>
                        )}
                    </div>
                </div>

                <p className="text-sm text-gray-500 mb-6">
                    This page will automatically close in {countdown} seconds
                </p>

                <button
                    onClick={() => navigate('/')}
                    className="bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-8 rounded"
                >
                    Return to Home
                </button>
            </div>
        </div>
    );
};

export default QueueConfirmation;