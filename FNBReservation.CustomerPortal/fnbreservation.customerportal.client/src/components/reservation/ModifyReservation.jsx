import React, { useState, useEffect } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, addDays } from "date-fns";

const ModifyReservation = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const location = useLocation();
    const reservationDataFromNav = location.state?.reservationData;

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
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",
        specialRequests: ""
    });

    // Original form data to track changes
    const [originalFormData, setOriginalFormData] = useState({
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",
        specialRequests: ""
    });

    // Hardcoded available times for all hours of operation
    const [availableTimes] = useState([
        "11:00:00", "11:15:00", "11:30:00", "11:45:00",
        "12:00:00", "12:15:00", "12:30:00", "12:45:00",
        "13:00:00", "13:15:00", "13:30:00", "13:45:00",
        "14:00:00", "14:15:00", "14:30:00", "14:45:00",
        "15:00:00", "15:15:00", "15:30:00", "15:45:00",
        "16:00:00", "16:15:00", "16:30:00", "16:45:00",
        "17:00:00", "17:15:00", "17:30:00", "17:45:00",
        "18:00:00", "18:15:00", "18:30:00", "18:45:00",
        "19:00:00", "19:15:00", "19:30:00", "19:45:00",
        "20:00:00", "20:15:00", "20:30:00", "20:45:00",
        "21:00:00", "21:15:00", "21:30:00", "21:45:00"
    ]);

    const [updating, setUpdating] = useState(false);
    const [showSuccessModal, setShowSuccessModal] = useState(false);
    const [isDateTimeChanged, setIsDateTimeChanged] = useState(false);

    // States for availability dialogs
    const [showCheckingAvailabilityDialog, setShowCheckingAvailabilityDialog] = useState(false);
    const [showNotAvailableDialog, setShowNotAvailableDialog] = useState(false);
    const [alternativeTimes, setAlternativeTimes] = useState([]);
    const [selectedAlternativeTime, setSelectedAlternativeTime] = useState(null);

    // For demo purposes - add buttons to show dialogs
    const [showDemoButtons, setShowDemoButtons] = useState(false);

    // Fetch reservation details on component mount
    useEffect(() => {
        const fetchReservation = async () => {
            try {
                // First check if data was passed via navigation
                if (reservationDataFromNav) {
                    console.log("Using reservation data from navigation", reservationDataFromNav);

                    let timeString = "19:00:00"; // Default time
                    let dateString = format(new Date(), 'yyyy-MM-dd'); // Default date

                    try {
                        const reservationDate = parseISO(reservationDataFromNav.reservationDate);
                        timeString = format(reservationDate, 'HH:mm:ss');
                        dateString = format(reservationDate, 'yyyy-MM-dd');
                    } catch (err) {
                        console.error("Error parsing reservation date:", err);
                    }

                    const initialFormData = {
                        partySize: reservationDataFromNav.partySize || 2,
                        date: dateString,
                        time: timeString,
                        specialRequests: reservationDataFromNav.specialRequests || ""
                    };

                    console.log("Setting form data to:", initialFormData);
                    setFormData(initialFormData);
                    setOriginalFormData(initialFormData);
                } else {
                    // Use the id from params, or default to "12345" for demo/testing
                    const reservationId = id || "12345";
                    console.log("Fetching reservation with ID:", reservationId);

                    const reservationData = await getReservationById(reservationId);

                    if (reservationData) {
                        let timeString = "19:00:00"; // Default time
                        let dateString = format(new Date(), 'yyyy-MM-dd'); // Default date

                        try {
                            const reservationDate = parseISO(reservationData.reservationDate);
                            timeString = format(reservationDate, 'HH:mm:ss');
                            dateString = format(reservationDate, 'yyyy-MM-dd');
                        } catch (err) {
                            console.error("Error parsing reservation date:", err);
                        }

                        const initialFormData = {
                            partySize: reservationData.partySize || 2,
                            date: dateString,
                            time: timeString,
                            specialRequests: reservationData.specialRequests || ""
                        };

                        console.log("Setting form data to:", initialFormData);
                        setFormData(initialFormData);
                        setOriginalFormData(initialFormData);
                    }
                }

                // For demo purposes - show the demo controls
                setShowDemoButtons(true);
            } catch (err) {
                console.error('Failed to fetch reservation', err);
            }
        };

        fetchReservation();

        // Cleanup
        return () => {
            clearError();
        };
    }, [id, getReservationById, clearError, reservationDataFromNav]);

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

    // Check if any important fields have changed
    const hasChangedImportantFields = () => {
        return formData.partySize !== originalFormData.partySize ||
            formData.date !== originalFormData.date ||
            formData.time !== originalFormData.time;
    };

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;

        // Check if date or time was changed
        if (name === 'date' || name === 'time' || name === 'partySize') {
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

    // Check availability
    const checkAvailability = async () => {
        if (!hasChangedImportantFields()) {
            // If no important fields changed, just submit the form directly
            await handleSubmitForm();
            return;
        }

        setShowCheckingAvailabilityDialog(true);

        try {
            // In a real system, this would call the actual availability API
            // For this demo, we'll simulate an API call with random results
            await new Promise(resolve => setTimeout(resolve, 2000));

            // Simulate availability check with 50% chance of availability
            const isAvailable = Math.random() > 0.5;

            if (isAvailable) {
                // If available, continue with submission
                setShowCheckingAvailabilityDialog(false);
                await handleSubmitForm();
            } else {
                // Generate alternative times
                const [hours, minutes] = formData.time.split(':').map(Number);
                const generatedAlternatives = [];

                // Generate alternative times on the same date
                for (let i = 1; i <= 4; i++) {
                    let newHours = hours;
                    let newMinutes = minutes + (i * 30);

                    // Handle time overflow
                    while (newMinutes >= 60) {
                        newHours += 1;
                        newMinutes -= 60;
                    }

                    // Only include if within restaurant hours
                    if (newHours >= 11 && newHours < 22) {
                        const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                        generatedAlternatives.push(timeString);
                    }
                }

                setAlternativeTimes(generatedAlternatives);
                setSelectedAlternativeTime(null);
                setShowCheckingAvailabilityDialog(false);
                setShowNotAvailableDialog(true);
            }
        } catch (err) {
            console.error('Failed to check availability', err);
            setShowCheckingAvailabilityDialog(false);
        }
    };

    // Submit form after availability check
    const handleSubmitForm = async () => {
        setUpdating(true);

        try {
            // In a real system, this would call the actual updateReservation API
            const updatedReservation = {
                // Ensure the ID is properly formatted as a string
                id: reservationDetails.id,
                // Keep the original reservation code
                reservationCode: reservationDetails.reservationCode,
                // Keep the original outlet information
                outletId: reservationDetails.outletId,
                outletName: reservationDetails.outletName,
                // Keep customer information
                customerName: reservationDetails.customerName,
                customerPhone: reservationDetails.customerPhone,
                customerEmail: reservationDetails.customerEmail,
                // Updated fields
                partySize: Number(formData.partySize),
                reservationDate: `${formData.date}T${formData.time}+08:00`,
                specialRequests: formData.specialRequests,
                // Keep original status
                status: reservationDetails.status
            };

            console.log("Sending update with data:", updatedReservation);
            await updateReservation(updatedReservation);
            setShowSuccessModal(true);
        } catch (err) {
            console.error('Failed to update reservation', err);
        } finally {
            setUpdating(false);
        }
    };

    // Submit with alternative time
    const submitWithAlternativeTime = async () => {
        if (!selectedAlternativeTime) return;

        setFormData({
            ...formData,
            time: selectedAlternativeTime
        });

        setShowNotAvailableDialog(false);
        await handleSubmitForm();
    };

    // Handle form submission
    const handleSubmit = (e) => {
        e.preventDefault();
        checkAvailability();
    };

    // DEMO: Show checking availability dialog
    const showCheckingAvailabilityDemo = () => {
        setShowCheckingAvailabilityDialog(true);
        setTimeout(() => {
            setShowCheckingAvailabilityDialog(false);
        }, 3000);
    };

    // DEMO: Show not available dialog
    const showNotAvailableDemo = () => {
        // Generate some sample alternative times
        const [hours, minutes] = formData.time.split(':').map(Number);
        const demoAlternatives = [];

        for (let i = 1; i <= 4; i++) {
            let newHours = hours;
            let newMinutes = minutes + (i * 30);

            // Handle time overflow
            while (newMinutes >= 60) {
                newHours += 1;
                newMinutes -= 60;
            }

            // Only include if within restaurant hours
            if (newHours >= 11 && newHours < 22) {
                const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                demoAlternatives.push(timeString);
            }
        }

        setAlternativeTimes(demoAlternatives);
        setSelectedAlternativeTime(null);
        setShowNotAvailableDialog(true);
    };

    // DEMO: Show success modal
    const showSuccessDemo = () => {
        setShowSuccessModal(true);
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

    return (
        <div className="w-full max-w-2xl mx-auto px-4 py-8">
            {/* DEMO CONTROLS - for testing dialogs */}
            {showDemoButtons && (
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
                    <h3 className="font-bold mb-2">Demo Controls (For Testing)</h3>
                    <div className="flex flex-wrap gap-2">
                        <button
                            onClick={showCheckingAvailabilityDemo}
                            className="bg-blue-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Checking Availability" Dialog
                        </button>
                        <button
                            onClick={showNotAvailableDemo}
                            className="bg-yellow-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Not Available" Dialog
                        </button>
                        <button
                            onClick={showSuccessDemo}
                            className="bg-green-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Success" Dialog
                        </button>
                    </div>
                </div>
            )}

            {/* Header section */}
            <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                <div className="flex items-center justify-between mb-4">
                    <h1 className="text-2xl font-bold">Modify Reservation</h1>
                    <div className={`px-3 py-1 rounded-full text-sm ${reservationDetails?.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                            reservationDetails?.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                                'bg-yellow-100 text-yellow-800'
                        }`}>
                        {reservationDetails?.status || 'Confirmed'}
                    </div>
                </div>

                <div className="flex items-center mb-6">
                    <div className="bg-gray-200 rounded-full p-2 mr-4">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                        </svg>
                    </div>
                    <div>
                        <p className="font-medium">{reservationDetails?.customerName || 'John Doe'}</p>
                        <p className="text-sm text-gray-600">{reservationDetails?.customerPhone || '+60 12-345-6789'}</p>
                    </div>
                </div>

                <div className="grid grid-cols-2 gap-4 mb-2">
                    <div>
                        <p className="text-sm text-gray-500">Reservation Code</p>
                        <p className="font-medium">{reservationDetails?.reservationCode || 'RES9299'}</p>
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Restaurant</p>
                        <p className="font-medium">{reservationDetails?.outletName || 'Main Branch'}</p>
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

            {/* Checking Availability Dialog */}
            {showCheckingAvailabilityDialog && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                        <div className="flex justify-center mb-6">
                            <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
                        </div>
                        <h3 className="text-xl font-bold text-center mb-2">Checking Availability</h3>
                        <p className="text-gray-600 text-center">
                            Please wait while we check if a table is available for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} on {format(new Date(formData.date), 'EEEE, MMMM d, yyyy')} at {formatDisplayTime(formData.time)}.
                        </p>
                    </div>
                </div>
            )}

            {/* Not Available Dialog */}
            {showNotAvailableDialog && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6">
                        <div className="flex justify-center mb-4">
                            <div className="rounded-full bg-red-100 p-3">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </div>
                        </div>

                        <h3 className="text-xl font-bold text-center mb-2">Time Not Available</h3>
                        <p className="text-gray-600 text-center mb-6">
                            Sorry, we don't have availability for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} on {format(new Date(formData.date), 'EEEE, MMMM d, yyyy')} at {formatDisplayTime(formData.time)}.
                        </p>

                        <div className="mb-6">
                            <h4 className="font-medium mb-2">Alternative times available on the same day:</h4>
                            <div className="grid grid-cols-2 gap-2">
                                {alternativeTimes.map((time, index) => (
                                    <button
                                        key={index}
                                        type="button"
                                        onClick={() => setSelectedAlternativeTime(time)}
                                        className={`py-2 px-3 rounded text-center text-sm ${selectedAlternativeTime === time
                                            ? 'bg-green-600 text-white'
                                            : 'border border-gray-300 hover:bg-gray-100'
                                            }`}
                                    >
                                        {formatDisplayTime(time)}
                                    </button>
                                ))}
                            </div>
                        </div>

                        <div className="flex justify-between">
                            <button
                                onClick={() => setShowNotAvailableDialog(false)}
                                className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                            >
                                Cancel
                            </button>

                            <button
                                onClick={submitWithAlternativeTime}
                                disabled={!selectedAlternativeTime}
                                className={`bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded ${!selectedAlternativeTime ? 'opacity-50 cursor-not-allowed' : ''}`}
                            >
                                Book Selected Time
                            </button>
                        </div>
                    </div>
                </div>
            )}

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

                        <div className="bg-gray-50 p-4 rounded-lg mb-6">
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <p className="text-sm text-gray-500">Date</p>
                                    <p className="font-medium">{format(new Date(formData.date), 'EEEE, MMMM d, yyyy')}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Time</p>
                                    <p className="font-medium">{formatDisplayTime(formData.time)}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Party Size</p>
                                    <p className="font-medium">{formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Reservation Code</p>
                                    <p className="font-medium">{reservationDetails?.reservationCode || 'RES9299'}</p>
                                </div>
                            </div>
                        </div>

                        <div className="flex justify-center">
                            <button
                                onClick={() => navigate(`/reservation/${reservationDetails?.id || '12345'}`)}
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