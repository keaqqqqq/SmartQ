import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO } from "date-fns";

const ReservationDetail = () => {
    const { id, code } = useParams();
    const navigate = useNavigate();
    const [isLoading, setIsLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const {
        reservationDetails,
        loading,
        error,
        getReservationById,
        getReservationByCode,
        cancelReservation,
        clearError
    } = useReservation();

    // Fetch reservation details
    useEffect(() => {
        const fetchReservation = async () => {
            setIsLoading(true);
            try {
                if (id) {
                    await getReservationById(id);
                } else if (code) {
                    await getReservationByCode(code);
                }
            } catch (err) {
                console.error('Failed to fetch reservation', err);
            } finally {
                setIsLoading(false);
            }
        };

        fetchReservation();

        // Cleanup
        return () => {
            clearError();
        };
    }, [id, code, getReservationById, getReservationByCode, clearError]);

    // Handle reservation cancellation
    const handleCancelReservation = async () => {
        try {
            await cancelReservation(reservationDetails.id, "Cancelled by customer through web portal");
            setIsModalOpen(false);
            // Show temporary success message
            alert('Your reservation has been successfully cancelled.');
            navigate('/');
        } catch (err) {
            console.error('Failed to cancel reservation', err);
        }
    };

    if (isLoading || loading) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

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

    if (!reservationDetails) {
        return (
            <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
                <h2 className="text-xl font-bold mb-4">Reservation Not Found</h2>
                <p className="mb-4">Sorry, we couldn't find the reservation you're looking for.</p>
                <button
                    onClick={() => navigate('/')}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Back to Home
                </button>
            </div>
        );
    }

    // Format date
    const formatReservationDate = (dateString) => {
        try {
            const date = parseISO(dateString);
            return format(date, 'MMMM d, yyyy h:mm a');
        } catch (error) {
            console.error('Date formatting error:', error);
            return dateString; // Return original if parsing fails
        }
    };

    // Check if reservation is upcoming
    const isUpcoming = () => {
        try {
            const reservationDate = parseISO(reservationDetails.reservationDate);
            return new Date() < reservationDate;
        } catch (error) {
            console.error('Date comparison error:', error);
            return false;
        }
    };

    return (
        <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
            <h1 className="text-2xl font-bold mb-6">Reservation Details</h1>

            {/* Reservation status indicator */}
            <div className={`px-4 py-2 rounded-md inline-block mb-6 ${reservationDetails.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                    reservationDetails.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                        'bg-yellow-100 text-yellow-800'
                }`}>
                {reservationDetails.status}
            </div>

            <div className="bg-gray-50 p-5 rounded-lg mb-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                        <p className="text-gray-500 text-sm">Reservation Code</p>
                        <p className="font-medium">{reservationDetails.reservationCode}</p>
                    </div>

                    <div>
                        <p className="text-gray-500 text-sm">Date & Time</p>
                        <p className="font-medium">{formatReservationDate(reservationDetails.reservationDate)}</p>
                    </div>

                    <div>
                        <p className="text-gray-500 text-sm">Outlet</p>
                        <p className="font-medium">{reservationDetails.outletName}</p>
                    </div>

                    <div>
                        <p className="text-gray-500 text-sm">Party Size</p>
                        <p className="font-medium">{reservationDetails.partySize} {reservationDetails.partySize === 1 ? 'person' : 'people'}</p>
                    </div>

                    <div>
                        <p className="text-gray-500 text-sm">Name</p>
                        <p className="font-medium">{reservationDetails.customerName}</p>
                    </div>

                    <div>
                        <p className="text-gray-500 text-sm">Phone</p>
                        <p className="font-medium">{reservationDetails.customerPhone}</p>
                    </div>

                    <div className="md:col-span-2">
                        <p className="text-gray-500 text-sm">Special Requests</p>
                        <p className="font-medium">{reservationDetails.specialRequests || 'None'}</p>
                    </div>
                </div>
            </div>

            {/* Action buttons */}
            <div className="flex flex-col md:flex-row gap-3">
                <button
                    onClick={() => navigate('/')}
                    className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                >
                    Back to Home
                </button>

                {isUpcoming() && reservationDetails.status === 'Confirmed' && (
                    <>
                        <button
                            onClick={() => navigate(`/update-reservation/${reservationDetails.id}`)}
                            className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Modify Reservation
                        </button>

                        <button
                            onClick={() => setIsModalOpen(true)}
                            className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Cancel Reservation
                        </button>
                    </>
                )}
            </div>

            {/* Cancel Confirmation Modal */}
            {isModalOpen && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                    <div className="bg-white p-6 rounded-lg shadow-lg max-w-md w-full">
                        <h3 className="text-xl font-bold mb-4">Cancel Reservation</h3>
                        <p className="mb-6">Are you sure you want to cancel your reservation for {formatReservationDate(reservationDetails.reservationDate)}?</p>

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => setIsModalOpen(false)}
                                className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                            >
                                Keep Reservation
                            </button>

                            <button
                                onClick={handleCancelReservation}
                                className="bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-6 rounded"
                            >
                                Cancel Reservation
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ReservationDetail;