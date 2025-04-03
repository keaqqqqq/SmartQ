import React from 'react';
import { Link } from 'react-router-dom';
import { useLocation } from '../contexts/LocationContext';

const Home = () => {
    const { locationStatus, requestLocationAccess } = useLocation();

    // No longer showing location dialog here since it's now in ReservationForm
    // We'll just show status indicators if location has already been determined

    return (
        <div className="fixed inset-0 overflow-hidden">
            {/* Video Background with Dark Filter */}
            <div className="absolute inset-0 z-0">
                <div className="absolute inset-0 bg-black bg-opacity-70 z-10"></div>
                <video
                    autoPlay
                    loop
                    muted
                    className="object-cover w-full h-full"
                    poster="/images/restaurant-poster.jpg"
                >
                    <source src="/videos/restaurant-ambience.mp4" type="video/mp4" />
                    {/* Fallback image if video fails */}
                    Your browser does not support the video tag.
                </video>
            </div>

            {/* Content Section */}
            <div className="relative z-20 w-full h-full flex items-center justify-center">
                <div className="text-center max-w-xl px-6 py-12 bg-black bg-opacity-50 backdrop-blur-sm rounded-lg">
                    <h2 className="text-gray-300 text-sm uppercase tracking-widest mb-2">WELCOME TO SmartQ</h2>
                    <h1 className="text-white text-4xl md:text-5xl font-bold mb-8 leading-tight">
                        Savor the Moment
                        <hr className="my-2 border-t-2 border-gray-400 w-1/2 mx-auto" />
                        Reserve Now.
                    </h1>

                    <div className="flex justify-center gap-4">
                        <Link to="/reservation/new">
                            <button className="bg-transparent hover:bg-green-700 text-white border border-white hover:border-green-700 font-medium py-3 px-8 rounded-md transition duration-300 w-full md:w-64">
                                BOOK A TABLE
                            </button>
                        </Link>

                        <Link to="/reservation/lookup">
                            <button className="bg-transparent hover:bg-white text-white hover:text-gray-900 border border-white font-medium py-3 px-8 rounded-md transition duration-300 w-full md:w-64">
                                FIND MY RESERVATION
                            </button>
                        </Link>
                    </div>
                </div>
            </div>

            {/* Location status indicators - shown discreetly at the top if status is already determined */}
            {locationStatus === 'requesting' && (
                <div className="fixed top-4 right-4 bg-blue-50 border border-blue-200 text-blue-700 p-2 rounded-lg z-30 shadow-md">
                    <div className="flex items-center">
                        <div className="animate-spin rounded-full h-4 w-4 border-t-2 border-b-2 border-blue-500 mr-2"></div>
                        <span className="text-sm">Finding restaurants near you...</span>
                    </div>
                </div>
            )}

            {locationStatus === 'denied' && (
                <div className="fixed top-4 right-4 bg-yellow-50 border border-yellow-200 text-yellow-700 p-2 rounded-lg z-30 shadow-md">
                    <div className="flex items-center text-sm">
                        <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
                        </svg>
                        <span>Location access denied</span>
                        <button
                            onClick={() => {
                                requestLocationAccess();
                                localStorage.removeItem('locationPermission');
                            }}
                            className="ml-2 text-blue-600 hover:text-blue-800 underline"
                        >
                            Enable
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
};

export default Home;