import React, { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useQueue } from "../../contexts/QueueContext";
import OutletService from "../../services/OutletService";

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
        outletId: outletIdFromQR || "",
        customerName: "",
        customerPhone: "",
        customerEmail: "",
        partySize: 2,
        specialRequests: ""
    });

    // State for outlet data
    const [outlets, setOutlets] = useState([]);
    
    // State for loading outlets
    const [loadingOutlets, setLoadingOutlets] = useState(true);
    const [outletError, setOutletError] = useState(null);

    // Local state for validation
    const [hasEstimation, setHasEstimation] = useState(false);
    const [estimationError, setEstimationError] = useState(null);

    // Fetch outlets from the API
    useEffect(() => {
        const fetchOutlets = async () => {
            try {
                setLoadingOutlets(true);
                const response = await OutletService.getAllOutlets();
                if (response && response.data) {
                    setOutlets(response.data);
                    
                    // If no outlet selected yet, select the first one
                    if (!formData.outletId && response.data.length > 0) {
                        setFormData(prev => ({
                            ...prev,
                            outletId: response.data[0].id
                        }));
                    }
                }
                setOutletError(null);
            } catch (error) {
                console.error("Error fetching outlets:", error);
                setOutletError("Failed to load restaurants. Please try again later.");
                
                // Fallback to mock data if API fails
                const mockData = OutletService.getMockOutlets();
                if (mockData && mockData.outlets) {
                    setOutlets(mockData.outlets);
                    
                    // If no outlet selected yet, select the first one
                    if (!formData.outletId && mockData.outlets.length > 0) {
                        setFormData(prev => ({
                            ...prev,
                            outletId: mockData.outlets[0].id
                        }));
                    }
                }
            } finally {
                setLoadingOutlets(false);
            }
        };

        fetchOutlets();
    }, []);

    // Get wait time estimation when party size or outlet changes
    useEffect(() => {
        const fetchEstimation = async () => {
            if (!formData.outletId || !formData.partySize) return;
            
            try {
                setEstimationError(null);
                await getQueueEstimation(formData.outletId, formData.partySize);
                setHasEstimation(true);
            } catch (error) {
                console.error("Error fetching estimation:", error);
                setEstimationError("Failed to get wait time estimation. Please try again.");
                setHasEstimation(false);
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
            // Navigate to the queue status page with the queue code
            navigate(`/queue/status/${response.queueCode}`);
        } catch (error) {
            console.error("Error joining queue:", error);
            // Error is handled by the context
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

                {/* Outlet Error Message */}
                {outletError && (
                    <div className="bg-yellow-100 border border-yellow-400 text-yellow-700 px-4 py-3 rounded mb-6" role="alert">
                        <span className="block sm:inline">{outletError}</span>
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
                                <span className="font-medium">Current Wait Time: approximately {queueEstimation.estimatedWaitMinutes || queueEstimation.estimatedWaitTime} minutes</span>
                            </div>
                            
                            {queueEstimation.currentQueueLength && (
                                <p className="text-sm">There are currently {queueEstimation.currentQueueLength} parties in the queue.</p>
                            )}

                            {queueEstimation.isHighDemand && (
                                <p className="text-sm text-red-600 mt-2">
                                    This is a busy time with high demand. Wait times may be longer than usual.
                                </p>
                            )}
                        </div>
                    )}

                    {/* Wait Time Estimation Error */}
                    {estimationError && (
                        <div className="mb-6 p-4 rounded-lg bg-yellow-50 border border-yellow-200">
                            <p className="text-sm text-yellow-800">{estimationError}</p>
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        {/* Restaurant Selection - Only show if not from QR code */}
                        {!outletIdFromQR && (
                            <div className="mb-4">
                                <label htmlFor="outletId" className="block text-sm font-medium text-gray-700 mb-1">
                                    Restaurant <span className="text-red-500">*</span>
                                </label>
                                {loadingOutlets ? (
                                    <div className="w-full px-3 py-2 border rounded-md bg-gray-100">
                                        Loading restaurants...
                                    </div>
                                ) : (
                                    <select
                                        id="outletId"
                                        name="outletId"
                                        value={formData.outletId}
                                        onChange={handleChange}
                                        className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                        required
                                    >
                                        {outlets.length === 0 && (
                                            <option value="">No restaurants available</option>
                                        )}
                                        {outlets.map(outlet => (
                                            <option key={outlet.id} value={outlet.id}>{outlet.name}</option>
                                        ))}
                                    </select>
                                )}
                            </div>
                        )}

                        {/* Show selected outlet name if from QR code */}
                        {outletIdFromQR && (
                            <div className="mb-4 bg-gray-50 p-4 rounded-lg">
                                <p className="text-gray-600">
                                    <strong>Restaurant:</strong> {loadingOutlets ? 'Loading...' : (outlets.find(o => o.id === outletIdFromQR)?.name || "Selected Restaurant")}
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

                        <FormInput
                            label="Email Address"
                            type="email"
                            name="customerEmail"
                            value={formData.customerEmail}
                            onChange={handleChange}
                            placeholder="john@example.com"
                        />

                        {/* Special Requests */}
                        <div className="mb-6">
                            <label htmlFor="specialRequests" className="block text-sm font-medium text-gray-700 mb-1">
                                Special Requests
                            </label>
                            <textarea
                                id="specialRequests"
                                name="specialRequests"
                                value={formData.specialRequests}
                                onChange={handleChange}
                                rows="3"
                                placeholder="Any special requests or preferences..."
                                className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            ></textarea>
                        </div>

                        {/* Submit Button */}
                        <button
                            type="submit"
                            className={`w-full py-3 px-4 rounded-md text-white font-semibold ${loading ? 'bg-gray-400 cursor-not-allowed' : 'bg-green-600 hover:bg-green-700'}`}
                            disabled={loading}
                        >
                            {loading ? 'Joining Queue...' : 'Join Queue'}
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default QueueForm;