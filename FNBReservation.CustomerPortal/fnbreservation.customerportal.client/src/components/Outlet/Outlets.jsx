import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useLocation } from '../../contexts/LocationContext';
import OutletService from '../../services/OutletService';

const Outlets = () => {
    const [outlets, setOutlets] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const { locationStatus, userCoordinates } = useLocation();

    // Function to calculate distance between two points using Haversine formula
    const calculateDistance = (lat1, lon1, lat2, lon2) => {
        if (!lat1 || !lon1 || !lat2 || !lon2) return Number.MAX_VALUE;
        
        const R = 6371; // Radius of the earth in km
        const dLat = deg2rad(lat2 - lat1);
        const dLon = deg2rad(lon2 - lon1);
        const a =
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(deg2rad(lat1)) * Math.cos(deg2rad(lat2)) *
            Math.sin(dLon / 2) * Math.sin(dLon / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        const distance = R * c; // Distance in km
        return distance;
    };

    const deg2rad = (deg) => {
        return deg * (Math.PI / 180);
    };

    // Fetch outlets from API
    useEffect(() => {
        const fetchOutlets = async () => {
            setLoading(true);
            setError(null);
            
            try {
                const response = await OutletService.getAllOutlets();
                console.log('API Response:', response);
                
                // Handle the response based on the actual API response format
                if (response && response.data) {
                    // The backend returns an array directly
                    if (Array.isArray(response.data)) {
                        setOutlets(response.data);
                    }
                    // Or it might return an object with outlets property
                    else if (response.data && Array.isArray(response.data.outlets)) {
                        setOutlets(response.data.outlets);
                    }
                    // Or it might return success and data properties
                    else if (response.data && response.data.success && Array.isArray(response.data.data)) {
                        setOutlets(response.data.data);
                    }
                    // Fallback
                    else {
                        console.warn('Unexpected response format:', response.data);
                        // If all else fails, try the mock data
                        const mockData = OutletService.getMockOutlets();
                        setOutlets(mockData.outlets);
                    }
                } else {
                    setOutlets([]);
                    console.warn('Empty response or missing data:', response);
                }
            } catch (err) {
                console.error('Error fetching outlets:', err);
                setError('Failed to load outlets. Please try again later.');
                
                // Fallback to mock data in development
                if (process.env.NODE_ENV === 'development') {
                    console.log('Using mock data as fallback');
                    const mockData = OutletService.getMockOutlets();
                    setOutlets(mockData.outlets);
                }
            } finally {
                setLoading(false);
            }
        };
        
        fetchOutlets();
    }, []);

    // Sort outlets by distance if user location is available
    useEffect(() => {
        if (locationStatus === 'granted' && userCoordinates && outlets.length > 0) {
            const sortedOutlets = [...outlets].sort((a, b) => {
                // Handle different property case conventions (camelCase vs PascalCase)
                const aLat = a.latitude || a.Latitude;
                const aLon = a.longitude || a.Longitude;
                const bLat = b.latitude || b.Latitude;
                const bLon = b.longitude || b.Longitude;
                
                const distanceA = calculateDistance(
                    userCoordinates.latitude,
                    userCoordinates.longitude,
                    aLat,
                    aLon
                );
                const distanceB = calculateDistance(
                    userCoordinates.latitude,
                    userCoordinates.longitude,
                    bLat,
                    bLon
                );
                return distanceA - distanceB;
            });
            setOutlets(sortedOutlets);
        }
    }, [locationStatus, userCoordinates, outlets.length]);

    // Get directions to outlet
    const getDirections = (outlet) => {
        const address = encodeURIComponent(outlet.location || outlet.Location || '');
        window.open(`https://www.google.com/maps/dir/?api=1&destination=${address}`, '_blank');
    };

    // Extract property value considering different casing conventions
    const getProp = (obj, propName) => {
        return obj[propName] || obj[propName.charAt(0).toUpperCase() + propName.slice(1)] || '';
    };

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="text-center mb-12">
                <h1 className="text-4xl font-bold text-gray-800 mb-4">Our Locations</h1>
                <p className="text-gray-600 max-w-2xl mx-auto">
                    Discover our restaurants across Malaysia. Each location offers a unique dining experience with our signature hospitality.
                </p>
            </div>

            {/* Loading indicator */}
            {loading && (
                <div className="bg-blue-50 border border-blue-200 text-blue-700 p-4 rounded-lg mb-6 max-w-4xl mx-auto">
                    <div className="flex items-center">
                        <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-blue-500 mr-3"></div>
                        <span>Loading restaurants...</span>
                    </div>
                </div>
            )}

            {/* Error message */}
            {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg mb-6 max-w-4xl mx-auto">
                    <div className="flex items-center">
                        <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                        </svg>
                        <span>{error}</span>
                    </div>
                </div>
            )}

            {/* Location permission status indicators */}
            {locationStatus === 'requesting' && (
                <div className="bg-blue-50 border border-blue-200 text-blue-700 p-4 rounded-lg mb-6 max-w-4xl mx-auto">
                    <div className="flex items-center">
                        <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-blue-500 mr-3"></div>
                        <span>Finding nearest restaurants to you...</span>
                    </div>
                </div>
            )}

            {/* No outlets found message */}
            {!loading && !error && outlets.length === 0 && (
                <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 p-4 rounded-lg mb-6 max-w-4xl mx-auto">
                    <div className="flex items-center">
                        <svg className="w-5 h-5 mr-3" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zm-1 9a1 1 0 11-2 0 1 1 0 012 0z" clipRule="evenodd" />
                        </svg>
                        <span>No restaurants found. Please try again later.</span>
                    </div>
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 max-w-7xl mx-auto">
                {outlets.map((outlet) => (
                    <div
                        key={getProp(outlet, 'id')}
                        className="bg-white rounded-lg overflow-hidden shadow-lg hover:shadow-xl transition-shadow duration-300"
                    >
                        <div className="p-6">
                            <h2 className="text-2xl font-bold text-gray-800 mb-4">{getProp(outlet, 'name')}</h2>

                            <div className="border-t border-gray-200 pt-4">
                                <div className="flex items-start mb-2">
                                    <svg className="w-5 h-5 text-gray-500 mr-2 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                                    </svg>
                                    <span className="text-gray-700">{getProp(outlet, 'location')}</span>
                                </div>

                                <div className="flex items-center mb-2">
                                    <svg className="w-5 h-5 text-gray-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                                    </svg>
                                    <span className="text-gray-700">{getProp(outlet, 'operatingHours')}</span>
                                </div>

                                <div className="flex items-center mb-4">
                                    <svg className="w-5 h-5 text-gray-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"></path>
                                    </svg>
                                    <span className="text-gray-700">{getProp(outlet, 'contact')}</span>
                                </div>

                                {(outlet.queueEnabled || outlet.QueueEnabled) && (
                                    <div className="flex items-center mb-4">
                                        <svg className="w-5 h-5 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                                        </svg>
                                        <span className="text-green-700">Queue Available</span>
                                    </div>
                                )}

                                <div className="grid grid-cols-2 gap-2">
                                    <Link
                                        to={`/reservation/new?outlet=${getProp(outlet, 'id')}`}
                                        className="bg-green-600 hover:bg-green-700 text-white text-center py-2 px-4 rounded-md transition-colors"
                                    >
                                        Reserve Table
                                    </Link>

                                    <button
                                        onClick={() => getDirections(outlet)}
                                        className="border border-green-600 text-green-600 hover:bg-green-50 text-center py-2 px-4 rounded-md transition-colors"
                                    >
                                        Get Directions
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default Outlets;