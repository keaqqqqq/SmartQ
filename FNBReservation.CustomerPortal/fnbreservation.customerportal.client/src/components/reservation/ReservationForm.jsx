import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { format, addHours, subHours, parseISO } from "date-fns";
import axios from "axios";

// Reusable Input Component
const FormInput = ({ label, type, name, value, onChange, required, placeholder, className }) => (
    <div className="mb-4">
        <label htmlFor={name} className="block text-sm font-medium text-gray-700 mb-1">
            {label} {required && <span className="text-red-500">*</span>}
        </label>
        <input
            type={type}
            id={name}
            name={name}
            value={value}
            onChange={onChange}
            required={required}
            placeholder={placeholder}
            className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 ${className}`}
        />
    </div>
);

const ReservationForm = () => {
    const navigate = useNavigate();
    const [step, setStep] = useState(1); // 1: Check Availability, 2: Review Availability, 3: Personal Details
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [availableSlots, setAvailableSlots] = useState([]);
    const [selectedSlot, setSelectedSlot] = useState(null);
    const [reservationCode, setReservationCode] = useState(null);

    // Form State
    const [formData, setFormData] = useState({
        // Availability Check
        outletId: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        preferredTime: "19:00:00",

        // Personal Details
        customerName: "",
        customerPhone: "",
        customerEmail: "",
        specialRequests: ""
    });

    // Outlet data would typically come from an API
    const [outlets, setOutlets] = useState([
        { id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5", name: "Main Branch" }
    ]);

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Calculate earliest and latest time (30 min before and 1 hour after preferred time)
    const getEarliestTime = () => {
        try {
            const timeOnly = formData.preferredTime;
            const [hours, minutes] = timeOnly.split(':').map(Number);
            const date = new Date();
            date.setHours(hours, minutes, 0);
            const earliestDate = subHours(date, 0.5);
            return format(earliestDate, 'HH:mm:ss');
        } catch (e) {
            return "18:30:00"; // Default
        }
    };

    const getLatestTime = () => {
        try {
            const timeOnly = formData.preferredTime;
            const [hours, minutes] = timeOnly.split(':').map(Number);
            const date = new Date();
            date.setHours(hours, minutes, 0);
            const latestDate = addHours(date, 1);
            return format(latestDate, 'HH:mm:ss');
        } catch (e) {
            return "20:00:00"; // Default
        }
    };

    // Check availability
    const checkAvailability = async (e) => {
        e?.preventDefault();
        setLoading(true);
        setError(null);

        try {
            const payload = {
                OutletId: formData.outletId,
                PartySize: parseInt(formData.partySize),
                Date: `${formData.date}T00:00:00Z`,
                PreferredTime: formData.preferredTime,
                EarliestTime: getEarliestTime(),
                LatestTime: getLatestTime()
            };

            // API call to check availability
            const response = await axios.post('/api/CustomerReservation/CheckAvailability', payload);

            if (response.data && response.data.availableSlots) {
                setAvailableSlots(response.data.availableSlots);
                setStep(2); // Move to review availability step
            } else {
                setAvailableSlots([]);
                setError("No available slots found. Please try different time or date.");
            }
        } catch (err) {
            setError(err.response?.data?.message || "Failed to check availability. Please try again.");
            console.error("Availability check error:", err);
        } finally {
            setLoading(false);
        }
    };

    // Create reservation
    const createReservation = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            // Format the reservation date in ISO format with timezone
            const reservationDateTime = new Date(`${formData.date}T${selectedSlot || formData.preferredTime}`);
            const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
            const formattedDateTime = reservationDateTime.toLocaleString('en-CA', {
                timeZone: timezone,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false
            }).replace(/(\d+)\/(\d+)\/(\d+)/, '$3-$1-$2');

            const payload = {
                OutletId: formData.outletId,
                CustomerName: formData.customerName,
                CustomerPhone: formData.customerPhone,
                CustomerEmail: formData.customerEmail,
                PartySize: parseInt(formData.partySize),
                ReservationDate: `${formattedDateTime}+08:00`, // Adjust timezone as needed
                SpecialRequests: formData.specialRequests
            };

            // API call to create reservation
            const response = await axios.post('/api/CustomerReservation/CreateReservation', payload);

            if (response.data && response.data.reservationCode) {
                setReservationCode(response.data.reservationCode);
                setStep(4); // Success step
            } else {
                throw new Error("No reservation code received");
            }
        } catch (err) {
            setError(err.response?.data?.message || "Failed to create reservation. Please try again.");
            console.error("Reservation creation error:", err);
        } finally {
            setLoading(false);
        }
    };

    // Select a time slot
    const selectTimeSlot = (slot) => {
        setSelectedSlot(slot);
        setStep(3); // Move to personal details
    };

    // Reset and go back to availability check
    const goBackToAvailability = () => {
        setStep(1);
        setSelectedSlot(null);
    };

    // Fetch nearby outlets on component mount
    useEffect(() => {
        const fetchOutlets = async () => {
            try {
                const response = await axios.get('/api/CustomerReservation/GetNearbyOutlets');
                if (response.data && response.data.outlets) {
                    setOutlets(response.data.outlets);
                }
            } catch (err) {
                console.error("Error fetching outlets:", err);
            }
        };

        fetchOutlets();
    }, []);

    // Generate sample time slots for demo (would come from the API in reality)
    const generateSampleSlots = () => {
        if (availableSlots.length > 0) return availableSlots;

        const baseTime = new Date();
        baseTime.setHours(18, 0, 0);

        return [
            { time: "18:00:00", available: true },
            { time: "18:30:00", available: true },
            { time: formData.preferredTime, available: true },
            { time: "19:30:00", available: true },
            { time: "20:00:00", available: true }
        ];
    };

    return (
        <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
            {/* Header */}
            <h1 className="text-2xl font-bold text-center mb-6">
                {step === 1 ? "Check Table Availability" :
                    step === 2 ? "Select Available Time" :
                        step === 3 ? "Complete Your Reservation" : "Reservation Confirmed"}
            </h1>

            {/* Error Message */}
            {error && (
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4" role="alert">
                    <span className="block sm:inline">{error}</span>
                </div>
            )}

            {/* Step 1: Check Availability Form */}
            {step === 1 && (
                <form onSubmit={checkAvailability}>
                    <div className="mb-4">
                        <label htmlFor="outletId" className="block text-sm font-medium text-gray-700 mb-1">
                            Select Restaurant <span className="text-red-500">*</span>
                        </label>
                        <select
                            id="outletId"
                            name="outletId"
                            value={formData.outletId}
                            onChange={handleChange}
                            className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            required
                        >
                            {outlets.map(outlet => (
                                <option key={outlet.id} value={outlet.id}>{outlet.name}</option>
                            ))}
                        </select>
                    </div>

                    <FormInput
                        label="Date"
                        type="date"
                        name="date"
                        value={formData.date}
                        onChange={handleChange}
                        required
                    />

                    <FormInput
                        label="Preferred Time"
                        type="time"
                        name="preferredTime"
                        value={formData.preferredTime}
                        onChange={handleChange}
                        required
                    />

                    <div className="mb-4">
                        <label htmlFor="partySize" className="block text-sm font-medium text-gray-700 mb-1">
                            Party Size <span className="text-red-500">*</span>
                        </label>
                        <select
                            id="partySize"
                            name="partySize"
                            value={formData.partySize}
                            onChange={handleChange}
                            className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            required
                        >
                            {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(size => (
                                <option key={size} value={size}>{size} {size === 1 ? 'person' : 'people'}</option>
                            ))}
                        </select>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                    >
                        {loading ? "Checking..." : "Check Availability"}
                    </button>
                </form>
            )}

            {/* Step 2: Review Available Time Slots */}
            {step === 2 && (
                <div>
                    <p className="mb-4">
                        Select one of the available time slots for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} on {format(new Date(formData.date), 'MMMM d, yyyy')}:
                    </p>

                    <div className="grid grid-cols-3 gap-3 mb-6">
                        {generateSampleSlots().map((slot, index) => (
                            <button
                                key={index}
                                onClick={() => selectTimeSlot(slot.time)}
                                disabled={!slot.available}
                                className={`py-3 px-4 rounded text-center ${slot.available
                                        ? 'bg-green-100 hover:bg-green-200 text-green-800 border border-green-300'
                                        : 'bg-gray-100 text-gray-400 cursor-not-allowed'
                                    }`}
                            >
                                {format(parseISO(`2023-01-01T${slot.time}`), 'h:mm a')}
                            </button>
                        ))}
                    </div>

                    <button
                        onClick={goBackToAvailability}
                        className="w-full border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                    >
                        Change Selection
                    </button>
                </div>
            )}

            {/* Step 3: Personal Details */}
            {step === 3 && (
                <form onSubmit={createReservation}>
                    <div className="bg-gray-100 p-4 rounded-lg mb-6">
                        <h3 className="font-medium mb-2">Reservation Summary</h3>
                        <p>Date: {format(new Date(formData.date), 'MMMM d, yyyy')}</p>
                        <p>Time: {format(parseISO(`2023-01-01T${selectedSlot}`), 'h:mm a')}</p>
                        <p>Party Size: {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                        <button
                            type="button"
                            onClick={goBackToAvailability}
                            className="text-sm text-blue-600 hover:underline mt-2"
                        >
                            Change
                        </button>
                    </div>

                    <FormInput
                        label="Your Name"
                        type="text"
                        name="customerName"
                        value={formData.customerName}
                        onChange={handleChange}
                        required
                        placeholder="John Smith"
                    />

                    <FormInput
                        label="Phone Number"
                        type="tel"
                        name="customerPhone"
                        value={formData.customerPhone}
                        onChange={handleChange}
                        required
                        placeholder="+60 12-345 6789"
                    />

                    <FormInput
                        label="Email Address"
                        type="email"
                        name="customerEmail"
                        value={formData.customerEmail}
                        onChange={handleChange}
                        required
                        placeholder="your@email.com"
                    />

                    <div className="mb-4">
                        <label htmlFor="specialRequests" className="block text-sm font-medium text-gray-700 mb-1">
                            Special Requests
                        </label>
                        <textarea
                            id="specialRequests"
                            name="specialRequests"
                            value={formData.specialRequests}
                            onChange={handleChange}
                            placeholder="Any special requests or occasions? (Optional)"
                            rows="3"
                            className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                        ></textarea>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 mb-3"
                    >
                        {loading ? "Confirming..." : "Confirm Reservation"}
                    </button>

                    <button
                        type="button"
                        onClick={() => setStep(2)}
                        className="w-full border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                    >
                        Back
                    </button>
                </form>
            )}

            {/* Step 4: Confirmation */}
            {step === 4 && reservationCode && (
                <div className="text-center">
                    <div className="w-16 h-16 bg-green-100 rounded-full mx-auto flex items-center justify-center mb-4">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                    </div>

                    <h2 className="text-xl font-bold mb-2">Reservation Confirmed!</h2>
                    <p className="mb-4">Your reservation has been successfully created. A confirmation has been sent to your phone.</p>

                    <div className="bg-gray-100 p-4 rounded-lg mb-6 text-left">
                        <p className="mb-2"><strong>Reservation Code:</strong> {reservationCode}</p>
                        <p className="mb-2"><strong>Date:</strong> {format(new Date(formData.date), 'MMMM d, yyyy')}</p>
                        <p className="mb-2"><strong>Time:</strong> {format(parseISO(`2023-01-01T${selectedSlot}`), 'h:mm a')}</p>
                        <p className="mb-2"><strong>Party Size:</strong> {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                        <p><strong>Name:</strong> {formData.customerName}</p>
                    </div>

                    <div className="mt-6">
                        <button
                            onClick={() => {
                                // Reset form and go back to step 1
                                setFormData({
                                    ...formData,
                                    customerName: "",
                                    customerPhone: "",
                                    customerEmail: "",
                                    specialRequests: ""
                                });
                                setStep(1);
                                setSelectedSlot(null);
                                setReservationCode(null);
                            }}
                            className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50 mr-3"
                        >
                            Make Another Reservation
                        </button>

                        <button
                            onClick={() => navigate('/')}
                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Home
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ReservationForm;