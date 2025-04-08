import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";

const ReservationLookup = () => {
    const navigate = useNavigate();
    const { getReservationByCode, loading } = useReservation();

    const [reservationCode, setReservationCode] = useState('');
    const [searchType, setSearchType] = useState('code'); // 'code' or 'phone'
    const [phoneNumber, setPhoneNumber] = useState('');
    const [error, setError] = useState(null);
    const [submitted, setSubmitted] = useState(false);

    // Handle form submission for reservation code
    const handleCodeSubmit = async (e) => {
        e.preventDefault();
        if (!reservationCode) return;

        setError(null);
        setSubmitted(true);

        try {
            const response = await getReservationByCode(reservationCode);
            if (response && response.id) {
                navigate(`/reservation/code/${reservationCode}`);
            } else {
                setError('Reservation not found. Please check your code and try again.');
                setSubmitted(false);
            }
        } catch (err) {
            setError('Failed to find reservation. Please check your code and try again.');
            console.error('Reservation lookup error:', err);
            setSubmitted(false);
        }
    };

    // Handle phone lookup navigation
    const handlePhoneSubmit = (e) => {
        e.preventDefault();
        if (!phoneNumber) return;
        navigate('/reservations');
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8">
                {/* Hero section with image */}
                <div className="text-center">
                    <div className="relative w-full h-40 mb-6 overflow-hidden rounded-xl">
                        <div className="absolute inset-0 bg-gradient-to-r from-green-600 to-green-800 opacity-90"></div>
                        <img
                            src="https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80"
                            alt="Restaurant ambiance"
                            className="w-full h-full object-cover"
                        />
                        <div className="absolute inset-0 flex items-center justify-center">
                            <h1 className="text-3xl font-extrabold text-white tracking-tight">
                                Find Your Reservation
                            </h1>
                        </div>
                    </div>

                    <p className="mt-2 text-sm text-gray-600 max-w-sm mx-auto">
                        Enter your reservation code or phone number to find and manage your reservations
                    </p>
                </div>

                {/* Tab navigation */}
                <div className="bg-white shadow-md rounded-t-lg">
                    <div className="flex border-b">
                        <button
                            onClick={() => setSearchType('code')}
                            className={`flex-1 py-4 px-4 font-medium text-sm focus:outline-none ${searchType === 'code'
                                    ? 'border-b-2 border-green-500 text-green-600'
                                    : 'text-gray-500 hover:text-gray-700'
                                }`}
                        >
                            <div className="flex items-center justify-center space-x-2">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V8a2 2 0 00-2-2h-5m-4 0V5a2 2 0 114 0v1m-4 0a2 2 0 104 0" />
                                </svg>
                                <span>Search by Code</span>
                            </div>
                        </button>
                        <button
                            onClick={() => setSearchType('phone')}
                            className={`flex-1 py-4 px-4 font-medium text-sm focus:outline-none ${searchType === 'phone'
                                    ? 'border-b-2 border-green-500 text-green-600'
                                    : 'text-gray-500 hover:text-gray-700'
                                }`}
                        >
                            <div className="flex items-center justify-center space-x-2">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                                </svg>
                                <span>Search by Phone</span>
                            </div>
                        </button>
                    </div>
                </div>

                {/* Form content */}
                <div className="bg-white shadow-md rounded-b-lg p-8">
                    {error && (
                        <div className="mb-6 flex bg-red-50 border-l-4 border-red-500 p-4 rounded items-center">
                            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-red-500 mr-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                            </svg>
                            <span className="text-red-700">{error}</span>
                        </div>
                    )}

                    {/* Search by Code Form */}
                    {searchType === 'code' && (
                        <form onSubmit={handleCodeSubmit} className="space-y-6">
                            <div>
                                <label htmlFor="reservationCode" className="block text-sm font-medium text-gray-700">
                                    Reservation Code <span className="text-red-500">*</span>
                                </label>
                                <div className="mt-1 relative rounded-md shadow-sm">
                                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 20l4-16m2 16l4-16M6 9h14M4 15h14" />
                                        </svg>
                                    </div>
                                    <input
                                        type="text"
                                        id="reservationCode"
                                        value={reservationCode}
                                        onChange={(e) => setReservationCode(e.target.value)}
                                        placeholder="Enter your reservation code"
                                        className="pl-10 py-3 block w-full rounded-md border border-gray-300 focus:ring-green-500 focus:border-green-500"
                                        required
                                    />
                                </div>
                                <p className="mt-2 text-sm text-gray-500">
                                    The code was sent to you in the confirmation message
                                </p>
                            </div>

                            <div>
                                <button
                                    type="submit"
                                    disabled={loading || !reservationCode || submitted}
                                    className="w-full flex justify-center items-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
                                >
                                    {loading || submitted ? (
                                        <>
                                            <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                            </svg>
                                            Searching...
                                        </>
                                    ) : (
                                        <>
                                            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                            </svg>
                                            Find Reservation
                                        </>
                                    )}
                                </button>
                            </div>
                        </form>
                    )}

                    {/* Search by Phone Form */}
                    {searchType === 'phone' && (
                        <form onSubmit={handlePhoneSubmit} className="space-y-6">
                            <div>
                                <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700">
                                    Phone Number <span className="text-red-500">*</span>
                                </label>
                                <div className="mt-1 relative rounded-md shadow-sm">
                                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                                        </svg>
                                    </div>
                                    <input
                                        type="tel"
                                        id="phoneNumber"
                                        value={phoneNumber}
                                        onChange={(e) => setPhoneNumber(e.target.value)}
                                        placeholder="+60 12-345-6789"
                                        className="pl-10 py-3 block w-full rounded-md border border-gray-300 focus:ring-green-500 focus:border-green-500"
                                        required
                                    />
                                </div>
                                <p className="mt-2 text-sm text-gray-500">
                                    We'll show all reservations associated with this phone number
                                </p>
                            </div>

                            <div>
                                <button
                                    type="submit"
                                    disabled={loading || !phoneNumber}
                                    className="w-full flex justify-center items-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50"
                                >
                                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                    </svg>
                                    Find My Reservations
                                </button>
                            </div>
                        </form>
                    )}

                    {/* Quick Actions */}
                    <div className="mt-8 pt-6 border-t border-gray-200 text-center text-gray-500 text-sm">
                        <p className="mb-4">Need something else?</p>
                        <div className="flex flex-wrap justify-center gap-3">
                            <button
                                onClick={() => navigate('/reservation/new')}
                                className="inline-flex items-center px-4 py-2 rounded-md text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200"
                            >
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 mr-2 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                                </svg>
                                New Reservation
                            </button>
                            <button
                                onClick={() => navigate('/')}
                                className="inline-flex items-center px-4 py-2 rounded-md text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200"
                            >
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 mr-2 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7m-7-7v14" />
                                </svg>
                                Return Home
                            </button>
                        </div>
                    </div>
                </div>
               
            </div>
        </div>
    );
};

export default ReservationLookup;