import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useQueue } from "../../contexts/QueueContext";

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

const QueueForm = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { joinQueue, getQueueEstimation, queueEstimation, loading, error } = useQueue();

    // Parse URL parameters (for QR code scan)
    const queryParams = new URLSearchParams(location.search);
    const outletIdFromQR = queryParams.get('outletId');

    // State for the form
    const [formData, setFormData] = useState({
        outletId: outletIdFromQR || "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
        customerName: "",
        customerPhone: "",
        customerEmail: "", // Added email field
        partySize: 2,
        specialRequests: ""
    });

    // State for outlet data
    const [outlets, setOutlets] = useState([
        { id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5", name: "Main Branch", address: "123 Main Street" },
        { id: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5", name: "Downtown Location", address: "456 Center Ave" },
        { id: "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5", name: "Riverside Branch", address: "789 River Road" }
    ]);

    // Local state for validation
    const [hasEstimation, setHasEstimation] = useState(false);

    // Get wait time estimation when party size or outlet changes
    useEffect(() => {
        const fetchEstimation = async () => {
            try {
                await getQueueEstimation(formData.outletId, formData.partySize);
                setHasEstimation(true);
            } catch (error) {
                console.error("Error fetching estimation:", error);
            }
        };

        if (formData.outletId && formData.partySize) {
            fetchEstimation();
        }
    }, [formData.outletId, formData.partySize, getQueueEstimation]);

    // Handle form input changes
    const handleChange = (e) => {
        const { name, value } = e.target;
        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Handle form submission
    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            const response = await joinQueue(formData);
            // Navigate to the queue status page with the queue ID
            navigate(`/queue/status/${response.id}`);
        } catch (error) {
            console.error("Error joining queue:", error);
        }
    };

    return (
        <div className="w-full">
            {/* Header image */}
            <div
                className="w-full h-72 bg-cover bg-center mb-8"
                style={{ backgroundImage: "url('https://images.unsplash.com/photo-1552566626-52f8b828add9?ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80')" }}
            >
                <div className="w-full h-full bg-black bg-opacity-50 flex flex-col items-center justify-center">
                    <h1 className="text-white text-4xl font-bold mb-2">JOIN THE QUEUE</h1>
                    <p className="text-white text-xl italic">Skip the physical line</p>
                </div>
            </div>

            <div className="max-w-2xl mx-auto px-4 pb-12">
                {/* Error Message */}
                {error && (
                    <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6" role="alert">
                        <span className="block sm:inline">{error}</span>
                    </div>
                )}

                {/* Main Form */}
                <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                    <h2 className="text-2xl font-bold mb-6">Join Our Queue</h2>

                    {/* Estimated Wait Time Display */}
                    {hasEstimation && queueEstimation && (
                        <div className={`mb-6 p-4 rounded-lg ${queueEstimation.isHighDemand ? 'bg-red-50 border border-red-200' : 'bg-blue-50 border border-blue-200'}`}>
                            <div className="flex items-center mb-2">
                                <svg className="h-5 w-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                </svg>
                                <span className="font-medium">Current Wait Time: approximately {queueEstimation.estimatedWaitTime} minutes</span>
                            </div>
                            <p className="text-sm">There are currently {queueEstimation.currentQueueLength} parties in the queue.</p>

                            {queueEstimation.isHighDemand && (
                                <p className="text-sm text-red-600 mt-2">
                                    This is a busy time with high demand. Wait times may be longer than usual.
                                </p>
                            )}
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        {/* Restaurant Selection - Only show if not from QR code */}
                        {!outletIdFromQR && (
                            <div className="mb-4">
                                <label htmlFor="outletId" className="block text-sm font-medium text-gray-700 mb-1">
                                    Restaurant <span className="text-red-500">*</span>
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
                        )}

                        {/* Show selected outlet name if from QR code */}
                        {outletIdFromQR && (
                            <div className="mb-4 bg-gray-50 p-4 rounded-lg">
                                <p className="text-gray-600">
                                    <strong>Restaurant:</strong> {outlets.find(o => o.id === outletIdFromQR)?.name || "Selected Restaurant"}
                                </p>
                            </div>
                        )}

                        {/* Party Size */}
                        <div className="mb-4">
                            <label htmlFor="partySize" className="block text-sm font-medium text-gray-700 mb-1">
                                Party Size <span className="text-red-500">*</span>
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

                        {/* Customer Information */}
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
                            label="Phone Number"
                            type="tel"
                            name="customerPhone"
                            value={formData.customerPhone}
                            onChange={handleChange}
                            required
                            placeholder="+60 12-345 6789"
                        />

                        {/* New Email Field */}
                        <FormInput
                            label="Email Address"
                            type="email"
                            name="customerEmail"
                            value={formData.customerEmail}
                            onChange={handleChange}
                            required
                            placeholder="john@example.com"
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
                                placeholder="Any special seating requests or dietary requirements?"
                                rows="3"
                                className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            ></textarea>
                        </div>

                        {/* WhatsApp Notification Consent */}
                        <div className="mb-6 bg-gray-50 p-4 rounded-lg">
                            <h3 className="text-md font-medium mb-2">Notifications</h3>
                            <p className="text-sm text-gray-600 mb-2">
                                We'll send you WhatsApp notifications to keep you updated about your queue status.
                                You'll be notified when:
                            </p>
                            <ul className="text-sm text-gray-600 list-disc pl-5 mb-2">
                                <li>Your queue position changes significantly</li>
                                <li>Your table is almost ready</li>
                                <li>Your table is ready</li>
                            </ul>
                            <p className="text-sm text-gray-600">
                                Make sure your phone number is correct to receive these updates.
                            </p>
                        </div>

                        {/* Submit Button */}
                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                        >
                            {loading ? (
                                <span className="flex items-center justify-center">
                                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Joining Queue...
                                </span>
                            ) : "Join Queue"}
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default QueueForm;