import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, addDays } from "date-fns";

const ModifyReservation = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const {
        reservationDetails,
        loading,
        error,
        getReservationById,
        updateReservation,
        clearError
    } = useReservation();

    // Local state for form fields
    const [formData, setFormData] = useState({
        partySize: 2,
        date: "",
        time: "",
        specialRequests: ""
    });

    const [availableTimes, setAvailableTimes] = useState([]);
    const [updating, setUpdating] = useState(false);
    const [showSuccessModal, setShowSuccessModal] = useState(false);
    const [isDateTimeChanged, setIsDateTimeChanged] = useState(false);

    // Fetch reservation details on component mount
    useEffect(() => {
        const fetchReservation = async () => {
            try {
                if (id) {
                    const reservationData = await getReservationById(id);

                    if (reservationData) {
                        const reservationDate = parseISO(reservationData.reservationDate);

                        setFormData({
                            partySize: reservationData.partySize,
                            date: format(reservationDate, 'yyyy-MM-dd'),
                            time: format(reservationDate, 'HH:mm:ss'),
                            specialRequests: reservationData.specialRequests || ""
                        });

                        // Generate some available times around the original time
                        generateAvailableTimes(format(reservationDate, 'HH:mm:ss'));
                    }
                }
            } catch (err) {
                console.error('Failed to fetch reservation', err);
            }
        };

        fetchReservation();

        // Cleanup
        return () => {
            clearError();
        };
    }, [id, getReservationById, clearError]);

    // Generate time options around the original booking time
    const generateAvailableTimes = (originalTime) => {
        // Parse the original time
        const [hours, minutes] = originalTime.split(':').map(Number);

        // Generate time slots 30 minutes before and after the original time
        const timeSlots = [];

        // This is a simplified version - in a real system, you'd check actual availability
        for (let i = -2; i <= 2; i++) {
            let newHours = hours;
            let newMinutes = minutes + (i * 30);

            // Handle minute overflow
            while (newMinutes >= 60) {
                newHours += 1;
                newMinutes -= 60;
            }

            // Handle minute underflow
            while (newMinutes < 0) {
                newHours -= 1;
                newMinutes += 60;
            }

            // Handle hour overflow/underflow and restaurant hours
            if (newHours >= 11 && newHours < 22) {
                const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                timeSlots.push(timeString);
            }
        }

        setAvailableTimes(timeSlots);
    };

    // Generate date options for the next 14 days
    const generateDateOptions = () => {
        const options = [];
        const today = new Date();

        for (let i = 0; i < 14; i++) {
            const date = addDays(today, i);
            options.push({
                value: format(date, 'yyyy-MM-dd'),
                label: format(date, 'EEE, MMM d')
            });
        }

        return options;
    };

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;

        // Check if date or time was changed
        if (name === 'date' || name === 'time') {
            setIsDateTimeChanged(true);
        }

        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Format time for display
    const formatDisplayTime = (timeString) => {
        try {
            const time = parseISO(`2023-01-01T${timeString}`);
            return format(time, 'h:mm a');
        } catch (error) {
            console.error('Time formatting error:', error);
            return timeString;
        }
    };

    // Submit form
    const handleSubmit = async (e) => {
        e.preventDefault();
        setUpdating(true);

        try {
            // In a real system, this would call the actual updateReservation API
            // For this demo, we'll simulate an API call delay
            await new Promise(resolve => setTimeout(resolve, 1000));

            if (reservationDetails) {
                const updatedReservation = {
                    ...reservationDetails,
                    partySize: Number(formData.partySize),
                    reservationDate: `${formData.date}T${formData.time}+08:00`,
                    specialRequests: formData.specialRequests
                };

                await updateReservation(updatedReservation);
                setShowSuccessModal(true);
            }
        } catch (err) {
            console.error('Failed to update reservation', err);
        } finally {
            setUpdating(false);
        }
    };

    if (loading) {
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
                    onClick={() => navigate(-1)}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Go Back
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
                    onClick={() => navigate('/reservations')}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Back to My Reservations
                </button>
            </div>
        );
    }

    return (
        <div className="w-full max-w-2xl mx-auto px-4 py-8">
            {/* Header section */}
            <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                <div className="flex items-center justify-between mb-4">
                    <h1 className="text-2xl font-bold">Modify Reservation</h1>
                    <div className={`px-3 py-1 rounded-full text-sm ${reservationDetails.status === 'Confirmed' ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'
                        }`}>
                        {reservationDetails.status}
                    </div>
                </div>

                <div className="flex items-center mb-6">
                    <div className="bg-gray-200 rounded-full p-2 mr-4">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                        </svg>
                    </div>
                    <div>
                        <p className="font-medium">{reservationDetails.customerName}</p>
                        <p className="text-sm text-gray-600">{reservationDetails.customerPhone}</p>
                    </div>
                </div>

                <div className="grid grid-cols-2 gap-4 mb-2">
                    <div>
                        <p className="text-sm text-gray-500">Reservation Code</p>
                        <p className="font-medium">{reservationDetails.reservationCode}</p>
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Restaurant</p>
                        <p className="font-medium">{reservationDetails.outletName}</p>
                    </div>
                </div>
            </div>

            {/* Modification form */}
            <div className="bg-white rounded-lg shadow-md p-6">
                <h2 className="text-xl font-bold mb-6">Reservation Details</h2>

                <form onSubmit={handleSubmit}>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
                        <div>
                            <label htmlFor="partySize" className="block text-sm font-medium text-gray-700 mb-1">
                                Party Size
                            </label>
                            <select
                                id="partySize"
                                name="partySize"
                                value={formData.partySize}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(size => (
                                    <option key={size} value={size}>{size} {size === 1 ? 'person' : 'people'}</option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-1">
                                Date
                            </label>
                            <select
                                id="date"
                                name="date"
                                value={formData.date}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {generateDateOptions().map((option, index) => (
                                    <option key={index} value={option.value}>{option.label}</option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label htmlFor="time" className="block text-sm font-medium text-gray-700 mb-1">
                                Time
                            </label>
                            <select
                                id="time"
                                name="time"
                                value={formData.time}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {availableTimes.map((time, index) => (
                                    <option key={index} value={time}>{formatDisplayTime(time)}</option>
                                ))}
                            </select>

                            {isDateTimeChanged && (
                                <p className="mt-2 text-sm text-yellow-600">
                                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 inline mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                    </svg>
                                    Time changes are subject to availability
                                </p>
                            )}
                        </div>

                        <div className="md:col-span-2">
                            <label htmlFor="specialRequests" className="block text-sm font-medium text-gray-700 mb-1">
                                Special Requests (Optional)
                            </label>
                            <textarea
                                id="specialRequests"
                                name="specialRequests"
                                value={formData.specialRequests}
                                onChange={handleChange}
                                placeholder="Let us know if you have any special requests"
                                rows="3"
                                className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            ></textarea>
                        </div>
                    </div>

                    <div className="flex flex-col md:flex-row gap-3">
                        <button
                            type="button"
                            onClick={() => navigate(-1)}
                            className="md:flex-1 bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                        >
                            Cancel
                        </button>

                        <button
                            type="submit"
                            disabled={updating}
                            className="md:flex-1 bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                        >
                            {updating ? (
                                <span className="flex items-center justify-center">
                                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Updating...
                                </span>
                            ) : "Save Changes"}
                        </button>
                    </div>
                </form>
            </div>

            {/* Success Modal */}
            {showSuccessModal && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6">
                        <div className="flex justify-center mb-4">
                            <div className="rounded-full bg-green-100 p-3">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                </svg>
                            </div>
                        </div>

                        <h3 className="text-xl font-bold text-center mb-2">Reservation Updated</h3>
                        <p className="text-gray-600 text-center mb-6">
                            Your reservation has been successfully modified. We've sent an updated confirmation to your phone.
                        </p>

                        <div className="flex justify-center">
                            <button
                                onClick={() => navigate(`/reservation/${id}`)}
                                className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                            >
                                View Reservation
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ModifyReservation;