import React, { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useLocation } from '../contexts/LocationContext';

const Home = () => {
    const { locationStatus, requestLocationAccess } = useLocation();

    // No need for the useEffect to show the dialog now, as we're handling it with conditional rendering

    // Close the dialog
    const closeDialog = () => {
        document.getElementById('location-dialog').classList.add('hidden');
    };

    // Allow location access and close dialog
    const handleAllowLocation = () => {
        requestLocationAccess();
        closeDialog();
    };

    // Deny location access and close dialog
    const handleDenyLocation = () => {
        localStorage.setItem('locationPermission', 'denied');
        closeDialog();
    };

    return (
        <div className="max-w-5xl mx-auto p-8">
            <div className="text-center mb-12">
                <h1 className="text-4xl font-bold text-gray-800 mb-4">FNB Reservation System</h1>
                <p className="text-xl text-gray-600">Manage your restaurant reservations with ease</p>
            </div>

            {/* Location Permission Dialog */}
            <div id="location-dialog" className={`fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center ${locationStatus !== 'initial' || localStorage.getItem('locationPermission') ? 'hidden' : ''}`}>
                <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                    <div className="flex justify-between items-start mb-4">
                        <h2 className="text-xl font-semibold text-gray-800">Enable Location Services</h2>
                        <button onClick={closeDialog} className="text-gray-400 hover:text-gray-600">
                            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                                <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd"></path>
                            </svg>
                        </button>
                    </div>

                    <div className="mb-5">
                        <div className="flex justify-center mb-4">
                            <svg className="w-16 h-16 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                            </svg>
                        </div>
                        <p className="text-gray-600">
                            To help you find the nearest restaurants, we need your permission to access your location.
                            This helps us provide better recommendations based on where you are.
                        </p>
                    </div>

                    <div className="flex flex-col space-y-2">
                        <button
                            onClick={handleAllowLocation}
                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded"
                        >
                            Allow Location Access
                        </button>
                        <button
                            onClick={handleDenyLocation}
                            className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                        >
                            Not Now
                        </button>
                    </div>
                </div>
            </div>

            {/* Location status indicators */}
            {locationStatus === 'requesting' && (
                <div className="bg-blue-50 border border-blue-200 text-blue-700 p-4 rounded-lg mb-8">
                    <div className="flex items-center">
                        <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-blue-500 mr-3"></div>
                        Requesting location access...
                    </div>
                </div>
            )}

            {locationStatus === 'granted' && (
                <div className="bg-green-50 border border-green-200 text-green-700 p-4 rounded-lg mb-8">
                    <div className="flex items-center">
                        <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path>
                        </svg>
                        Location access granted! We'll show you the nearest restaurants.
                    </div>
                </div>
            )}

            {locationStatus === 'denied' && (
                <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 p-4 rounded-lg mb-8">
                    <p>You've declined location access. You can still browse all restaurants, but we won't be able to show you the nearest options.</p>
                    <button
                        onClick={() => {
                            requestLocationAccess();
                            localStorage.removeItem('locationPermission');
                        }}
                        className="text-blue-600 hover:text-blue-800 underline mt-2"
                    >
                        Enable Location Access
                    </button>
                </div>
            )}

            {locationStatus === 'unavailable' && (
                <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg mb-8">
                    Location services are not available in your browser.
                </div>
            )}

            {/* Main content cards */}
            <div className="grid md:grid-cols-2 gap-6">
                <Link to="/reservation/new" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
                    <h2 className="text-2xl font-semibold text-gray-800 mb-2">Make a Reservation</h2>
                    <p className="text-gray-600 mb-4">Book a table at one of our restaurants</p>
                    <div className="text-green-600 font-medium">Get Started →</div>
                </Link>

                <Link to="/reservation/lookup" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
                    <h2 className="text-2xl font-semibold text-gray-800 mb-2">Find a Reservation</h2>
                    <p className="text-gray-600 mb-4">Check or modify your existing reservations</p>
                    <div className="text-green-600 font-medium">Look Up →</div>
                </Link>

                <Link to="/reservations" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
                    <h2 className="text-2xl font-semibold text-gray-800 mb-2">My Reservations</h2>
                    <p className="text-gray-600 mb-4">View all your current reservations</p>
                    <div className="text-green-600 font-medium">View All →</div>
                </Link>

                <div className="block bg-white p-6 rounded-lg shadow-md">
                    <h2 className="text-2xl font-semibold text-gray-800 mb-2">Need Help?</h2>
                    <p className="text-gray-600 mb-4">Contact our support team for assistance</p>
                    <div className="text-gray-700 mb-1">Phone: +60 12-345 6789</div>
                    <div className="text-gray-700">Email: supporeet@fnbreservation.com</div>
                </div>
            </div>
        </div>
    );
};

export default Home;