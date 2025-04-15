import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useLocation } from '../../contexts/LocationContext';

const Outlets = () => {
    const [outlets, setOutlets] = useState([
        {
            id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
            name: "Sunset Grill & Cafe",
            location: "12, Jalan Pantai, 75000 Melaka, Malaysia",
            operatingHours: "04:00 PM - 12:00 AM",
            contact: "+60 19-876 5432",
            latitude: 2.1896,
            longitude: 102.2501
        },
        {
            id: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5",
            name: "Urban Spice Kitchen",
            location: "45, Jalan Petaling, 50000 Kuala Lumpur, Malaysia",
            operatingHours: "11:30 AM - 10:00 PM",
            contact: "+60 13-456 7890",
            latitude: 3.1412,
            longitude: 101.6933
        },
        {
            id: "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5",
            name: "Highland Garden Restaurant",
            location: "88, Jalan Hujan, 70300 Cameron Highlands, Malaysia",
            operatingHours: "08:00 AM - 08:00 PM",
            contact: "+60 17-222 3333",
            latitude: 4.4718,
            longitude: 101.3756
        }
    ]);

    const [loading, setLoading] = useState(false);
    const { locationStatus, userLocation } = useLocation();

    // Function to calculate distance between two points using Haversine formula
    const calculateDistance = (lat1, lon1, lat2, lon2) => {
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

    // Sort outlets by distance if user location is available
    useEffect(() => {
        if (locationStatus === 'granted' && userLocation) {
            const sortedOutlets = [...outlets].sort((a, b) => {
                const distanceA = calculateDistance(
                    userLocation.latitude,
                    userLocation.longitude,
                    a.latitude,
                    a.longitude
                );
                const distanceB = calculateDistance(
                    userLocation.latitude,
                    userLocation.longitude,
                    b.latitude,
                    b.longitude
                );
                return distanceA - distanceB;
            });
            setOutlets(sortedOutlets);
        }
    }, [locationStatus, userLocation]);

    // Get directions to outlet
    const getDirections = (outlet) => {
        const address = encodeURIComponent(outlet.location);
        window.open(`https://www.google.com/maps/dir/?api=1&destination=${address}`, '_blank');
    };

    return (
        <div className="container mx-auto px-4 py-8">
            <div className="text-center mb-12">
                <h1 className="text-4xl font-bold text-gray-800 mb-4">Our Locations</h1>
                <p className="text-gray-600 max-w-2xl mx-auto">
                    Discover our restaurants across Malaysia. Each location offers a unique dining experience with our signature hospitality.
                </p>
            </div>

            {/* Location permission status indicators */}
            {locationStatus === 'requesting' && (
                <div className="bg-blue-50 border border-blue-200 text-blue-700 p-4 rounded-lg mb-6 max-w-4xl mx-auto">
                    <div className="flex items-center">
                        <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-blue-500 mr-3"></div>
                        <span>Finding nearest restaurants to you...</span>
                    </div>
                </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 max-w-7xl mx-auto">
                {outlets.map((outlet) => (
                    <div
                        key={outlet.id}
                        className="bg-white rounded-lg overflow-hidden shadow-lg hover:shadow-xl transition-shadow duration-300"
                    >
                        <div className="p-6">
                            <h2 className="text-2xl font-bold text-gray-800 mb-4">{outlet.name}</h2>

                            <div className="border-t border-gray-200 pt-4">
                                <div className="flex items-start mb-2">
                                    <svg className="w-5 h-5 text-gray-500 mr-2 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                                    </svg>
                                    <span className="text-gray-700">{outlet.location}</span>
                                </div>

                                <div className="flex items-center mb-2">
                                    <svg className="w-5 h-5 text-gray-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                                    </svg>
                                    <span className="text-gray-700">{outlet.operatingHours}</span>
                                </div>

                                <div className="flex items-center mb-4">
                                    <svg className="w-5 h-5 text-gray-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"></path>
                                    </svg>
                                    <span className="text-gray-700">{outlet.contact}</span>
                                </div>

                                <div className="grid grid-cols-2 gap-2">
                                    <Link
                                        to={`/reservation/new?outlet=${outlet.id}`}
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