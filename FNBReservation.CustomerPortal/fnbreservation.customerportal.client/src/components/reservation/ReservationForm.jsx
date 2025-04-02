import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { format, addDays, parseISO } from "date-fns";
import axios from "axios";

// Reusable Input Component
const FormInput = ({ label, type, name, value, onChange, required, placeholder, className }) => (
    <div className="mb-6">
        <label htmlFor={name} className="block text-sm font-medium text-gray-700 mb-2">
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
    const [step, setStep] = useState(1); // 1: Initial Check, 2: Personal Details, 3: Confirmation
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [availableSlots, setAvailableSlots] = useState([]);
    const [alternativeOutlets, setAlternativeOutlets] = useState([]);
    const [selectedSlot, setSelectedSlot] = useState(null);
    const [reservationCode, setReservationCode] = useState(null);

    // Form State
    const [formData, setFormData] = useState({
        // Initial Check
        outletId: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",

        // Personal Details
        customerName: "",
        customerPhone: "",
        customerEmail: "",
        specialRequests: ""
    });

    // Outlet data
    const [outlets, setOutlets] = useState([
        { id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5", name: "Main Branch", address: "123 Main Street" },
        { id: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5", name: "Downtown Location", address: "456 Center Ave" },
        { id: "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5", name: "Riverside Branch", address: "789 River Road" }
    ]);

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Format date for display
    const formatDisplayDate = (dateString) => {
        try {
            const date = new Date(dateString);
            return format(date, 'EEEE, MMMM d, yyyy');
        } catch (error) {
            return dateString;
        }
    };

    // Check availability
    const checkAvailability = async (e) => {
        e?.preventDefault();
        setLoading(true);
        setError(null);

        try {
            // Simulating API call for available time slots
            // In a real implementation, this would call your actual API
            setTimeout(() => {
                // These would come from the API
                const timeSlots = [
                    { time: "18:00:00", available: true },
                    { time: "18:15:00", available: true },
                    { time: "18:30:00", available: true },
                    { time: "18:45:00", available: true },
                    { time: formData.time, available: true },
                    { time: "19:15:00", available: true },
                    { time: "19:30:00", available: true },
                    { time: "19:45:00", available: true },
                    { time: "20:00:00", available: true }
                ];

                // Alternative outlets would also come from the API
                const alternatives = outlets
                    .filter(outlet => outlet.id !== formData.outletId)
                    .map(outlet => {
                        return {
                            ...outlet,
                            availableTimes: [
                                { time: "18:15:00", available: true },
                                { time: "18:30:00", available: true },
                                { time: "18:45:00", available: true },
                                { time: "19:00:00", available: true },
                                { time: "19:15:00", available: true },
                                { time: "20:15:00", available: true }
                            ]
                        };
                    });

                setAvailableSlots(timeSlots);
                setAlternativeOutlets(alternatives);
                setSelectedSlot(formData.time);
                setStep(2); // Move to personal details step
                setLoading(false);
            }, 1000);
        } catch (err) {
            setError("Failed to check availability. Please try again.");
            console.error("Availability check error:", err);
            setLoading(false);
        }
    };

    // Create reservation
    const createReservation = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            // Simulating API call for creating a reservation
            setTimeout(() => {
                setReservationCode("RES" + Math.floor(10000 + Math.random() * 90000));
                setStep(3); // Success step
                setLoading(false);
            }, 1500);
        } catch (err) {
            setError("Failed to create reservation. Please try again.");
            console.error("Reservation creation error:", err);
            setLoading(false);
        }
    };

    // State for timeout dialog
    const [showTimeoutDialog, setShowTimeoutDialog] = useState(false);

    // Countdown timer implementation
    useEffect(() => {
        if (step !== 2) return;

        // Start with 4:59 (299 seconds)
        let totalSeconds = 10;

        const countdownElement = document.getElementById('countdown-timer');
        if (!countdownElement) return;

        const timer = setInterval(() => {
            const minutes = Math.floor(totalSeconds / 60);
            const seconds = totalSeconds % 60;

            // Format as M:SS
            countdownElement.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

            // Decrease the countdown
            totalSeconds--;

            // When timer reaches 0, show timeout dialog
            if (totalSeconds < 0) {
                clearInterval(timer);
                countdownElement.textContent = "0:00";
                setShowTimeoutDialog(true);
            }
        }, 1000);

        // Clean up timer
        return () => clearInterval(timer);
    }, [step]);

    // Handle timeout dialog close
    const handleTimeoutDialogClose = () => {
        setShowTimeoutDialog(false);
        setStep(1); // Go back to availability check page
    };

    // Select a time slot
    const selectTimeSlot = (slot) => {
        setSelectedSlot(slot);
        setFormData({
            ...formData,
            time: slot
        });
    };

    // Select alternative outlet
    const selectAlternativeOutlet = (outletId, time) => {
        setFormData({
            ...formData,
            outletId: outletId,
            time: time
        });
        setSelectedSlot(time);
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

    // Generate time options
    const generateTimeOptions = () => {
        return [
            { value: "11:00:00", label: "11:00 AM" },
            { value: "11:15:00", label: "11:15 AM" },
            { value: "11:30:00", label: "11:30 AM" },
            { value: "11:45:00", label: "11:45 AM" },
            { value: "12:00:00", label: "12:00 PM" },
            { value: "12:15:00", label: "12:15 PM" },
            { value: "12:30:00", label: "12:30 PM" },
            { value: "12:45:00", label: "12:45 PM" },
            { value: "13:00:00", label: "1:00 PM" },
            { value: "13:15:00", label: "1:15 PM" },
            { value: "13:30:00", label: "1:30 PM" },
            { value: "13:45:00", label: "1:45 PM" },
            { value: "17:00:00", label: "5:00 PM" },
            { value: "17:15:00", label: "5:15 PM" },
            { value: "17:30:00", label: "5:30 PM" },
            { value: "17:45:00", label: "5:45 PM" },
            { value: "18:00:00", label: "6:00 PM" },
            { value: "18:15:00", label: "6:15 PM" },
            { value: "18:30:00", label: "6:30 PM" },
            { value: "18:45:00", label: "6:45 PM" },
            { value: "19:00:00", label: "7:00 PM" },
            { value: "19:15:00", label: "7:15 PM" },
            { value: "19:30:00", label: "7:30 PM" },
            { value: "19:45:00", label: "7:45 PM" },
            { value: "20:00:00", label: "8:00 PM" },
            { value: "20:15:00", label: "8:15 PM" },
            { value: "20:30:00", label: "8:30 PM" },
            { value: "20:45:00", label: "8:45 PM" },
            { value: "21:00:00", label: "9:00 PM" }
        ];
    };

    // Format time for display
    const formatDisplayTime = (timeString) => {
        try {
            const time = parseISO(`2023-01-01T${timeString}`);
            return format(time, 'h:mm a');
        } catch (error) {
            return timeString;
        }
    };

    return (
        <div className="w-full">
            {/* Full-width header image */}
            <div
                className="w-full h-72 bg-cover bg-center relative"
                style={{ backgroundImage: "url('https://images.unsplash.com/photo-1414235077428-338989a2e8c0?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80')" }}
            >
                <div className="absolute inset-0 bg-black bg-opacity-50 flex flex-col items-center justify-center">
                    <h1 className="text-white text-4xl font-bold mb-2">RESERVATION</h1>
                    <p className="text-white text-xl italic">Book A Table</p>
                </div>
            </div>

            <div className="max-w-5xl mx-auto px-4 py-8">
                {/* Error Message */}
                {error && (
                    <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6" role="alert">
                        <span className="block sm:inline">{error}</span>
                    </div>
                )}

                {/* Step 1: Initial availability check */}
                {step === 1 && (
                    <div className="bg-white rounded-lg shadow-md p-6 mb-8">
                        <h2 className="text-2xl font-bold mb-6">Find a Table</h2>

                        <form onSubmit={checkAvailability} className="grid md:grid-cols-4 gap-4">
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
                                    {generateTimeOptions().map((option, index) => (
                                        <option key={index} value={option.value}>{option.label}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label htmlFor="outletId" className="block text-sm font-medium text-gray-700 mb-1">
                                    Location
                                </label>
                                <select
                                    id="outletId"
                                    name="outletId"
                                    value={formData.outletId}
                                    onChange={handleChange}
                                    className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                    required
                                >
                                    {outlets.map(outlet => (
                                        <option key={outlet.id} value={outlet.id}>{outlet.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="md:col-span-4 mt-2">
                                <button
                                    type="submit"
                                    disabled={loading}
                                    className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                                >
                                    {loading ? "Checking..." : "Find a Table"}
                                </button>
                            </div>
                        </form>
                    </div>
                )}

                {/* Step 2: Personal Details */}
                {step === 2 && (
                    <div className="grid md:grid-cols-12 gap-6">
                        {/* Timeout Dialog */}
                        {showTimeoutDialog && (
                            <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                                <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                                    <div className="flex justify-between items-start mb-4">
                                        <h2 className="text-xl font-semibold text-gray-800">Time Expired</h2>
                                    </div>

                                    <div className="mb-6">
                                        <div className="flex justify-center mb-4">
                                            <svg className="w-16 h-16 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                            </svg>
                                        </div>
                                        <p className="text-gray-600">
                                            We're sorry, but your table hold has expired. To continue making a reservation, you'll need to check availability again.
                                        </p>
                                    </div>

                                    <div className="flex justify-center">
                                        <button
                                            onClick={handleTimeoutDialogClose}
                                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded"
                                        >
                                            Check Availability Again
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Main reservation form */}
                        <div className="md:col-span-8">
                            <div className="bg-white rounded-lg shadow-md p-6 mb-4">
                                <h2 className="text-2xl font-bold mb-6">Complete Your Reservation</h2>

                                <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded mb-6">
                                    <div className="flex items-center mb-2">
                                        <svg className="h-5 w-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                                        </svg>
                                        <span>
                                            Table for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} at {formatDisplayTime(formData.time)} on {formatDisplayDate(formData.date)}
                                        </span>
                                    </div>
                                    <div className="flex items-center text-sm">
                                        <svg className="h-4 w-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                        </svg>
                                        <span>We're holding this table for you for <span className="font-bold" id="countdown-timer">4:59</span></span>
                                    </div>
                                </div>

                                <form onSubmit={createReservation}>
                                    <FormInput
                                        label="Full Name"
                                        type="text"
                                        name="customerName"
                                        value={formData.customerName}
                                        onChange={handleChange}
                                        required
                                        placeholder="John Smith"
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

                                    <FormInput
                                        label="Phone Number"
                                        type="tel"
                                        name="customerPhone"
                                        value={formData.customerPhone}
                                        onChange={handleChange}
                                        required
                                        placeholder="+60 12-345 6789"
                                    />

                                    <div className="mb-6">
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

                                    <div className="grid grid-cols-2 gap-4">
                                        <button
                                            type="button"
                                            onClick={() => setStep(1)}
                                            className="w-full border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                                        >
                                            Back
                                        </button>

                                        <button
                                            type="submit"
                                            disabled={loading}
                                            className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                                        >
                                            {loading ? "Confirming..." : "Complete Reservation"}
                                        </button>
                                    </div>
                                </form>
                            </div>
                        </div>

                        {/* Sidebar with reservation info and alternative times */}
                        <div className="md:col-span-4">
                            <div className="bg-white rounded-lg shadow-md p-6 mb-4">
                                <h3 className="font-bold text-lg mb-4">Reservation Details</h3>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Restaurant</p>
                                    <p className="font-medium">{outlets.find(o => o.id === formData.outletId)?.name}</p>
                                    <p className="text-sm text-gray-600">{outlets.find(o => o.id === formData.outletId)?.address}</p>
                                </div>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Date & Time</p>
                                    <p className="font-medium">{formatDisplayDate(formData.date)}</p>
                                    <p className="font-medium">{formatDisplayTime(formData.time)}</p>
                                </div>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Party Size</p>
                                    <p className="font-medium">{formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                                </div>
                            </div>

                            {/* Alternative time slots for current outlet */}
                            <div className="bg-white rounded-lg shadow-md p-6 mb-4">
                                <h3 className="font-bold text-lg mb-4">Other Available Times</h3>
                                <div className="grid grid-cols-3 gap-2">
                                    {availableSlots.map((slot, index) => (
                                        <button
                                            key={index}
                                            onClick={() => selectTimeSlot(slot.time)}
                                            className={`py-2 px-3 rounded text-center text-sm ${slot.time === selectedSlot
                                                    ? 'bg-green-600 text-white'
                                                    : 'border border-gray-300 hover:bg-gray-100'
                                                }`}
                                        >
                                            {formatDisplayTime(slot.time)}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Alternative outlets */}
                            {alternativeOutlets.length > 0 && (
                                <div className="bg-white rounded-lg shadow-md p-6">
                                    <h3 className="font-bold text-lg mb-4">Other Available Restaurants</h3>

                                    {alternativeOutlets.map((outlet, outletIndex) => (
                                        <div key={outletIndex} className="mb-4 pb-4 border-b border-gray-200 last:border-0 last:mb-0 last:pb-0">
                                            <p className="font-medium mb-2">{outlet.name}</p>
                                            <p className="text-sm text-gray-600 mb-2">{outlet.address}</p>

                                            <div className="grid grid-cols-3 gap-2">
                                                {outlet.availableTimes.map((timeSlot, timeIndex) => (
                                                    <button
                                                        key={timeIndex}
                                                        onClick={() => selectAlternativeOutlet(outlet.id, timeSlot.time)}
                                                        className="py-2 px-3 rounded text-center text-sm border border-gray-300 hover:bg-gray-100"
                                                    >
                                                        {formatDisplayTime(timeSlot.time)}
                                                    </button>
                                                ))}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Step 3: Confirmation */}
                {step === 3 && reservationCode && (
                    <div className="bg-white rounded-lg shadow-md p-8 max-w-xl mx-auto text-center">
                        <div className="w-20 h-20 bg-green-100 rounded-full mx-auto flex items-center justify-center mb-6">
                            <svg xmlns="http://www.w3.org/2000/svg" className="h-10 w-10 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                        </div>

                        <h2 className="text-2xl font-bold mb-4">Reservation Confirmed!</h2>
                        <p className="mb-6 text-gray-600">Your reservation has been successfully created. A confirmation has been sent to your phone and email.</p>

                        <div className="bg-gray-50 p-6 rounded-lg mb-6 text-left">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <p className="text-sm text-gray-500">Reservation Code</p>
                                    <p className="font-medium">{reservationCode}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Restaurant</p>
                                    <p className="font-medium">{outlets.find(o => o.id === formData.outletId)?.name}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Date</p>
                                    <p className="font-medium">{formatDisplayDate(formData.date)}</p>
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
                                    <p className="text-sm text-gray-500">Name</p>
                                    <p className="font-medium">{formData.customerName}</p>
                                </div>
                            </div>
                        </div>

                        <div className="flex flex-col sm:flex-row justify-center gap-4">
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
                                className="border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                            >
                                Make Another Reservation
                            </button>

                            <button
                                onClick={() => navigate('/')}
                                className="bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-6 rounded"
                            >
                                Return Home
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ReservationForm;