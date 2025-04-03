import React, { createContext, useState, useContext, useCallback } from 'react';

// Create context
const LocationContext = createContext();

export const useLocation = () => useContext(LocationContext);

export const LocationProvider = ({ children }) => {
    // Location status: 'initial', 'requesting', 'granted', 'denied', 'unavailable'
    const [locationStatus, setLocationStatus] = useState(() => {
        // Check localStorage for previously saved permission
        const savedPermission = localStorage.getItem('locationPermission');
        if (savedPermission === 'granted') return 'granted';
        if (savedPermission === 'denied') return 'denied';
        return 'initial';
    });

    // Current user location
    const [userLocation, setUserLocation] = useState(null);

    // Request location access
    const requestLocationAccess = useCallback(() => {
        // Skip if already granted or unavailable
        if (locationStatus === 'granted' || locationStatus === 'unavailable') {
            return;
        }

        // Check if geolocation is available
        if (!navigator.geolocation) {
            setLocationStatus('unavailable');
            return;
        }

        // Request location
        setLocationStatus('requesting');

        navigator.geolocation.getCurrentPosition(
            (position) => {
                // Success
                setUserLocation({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
                setLocationStatus('granted');
                localStorage.setItem('locationPermission', 'granted');
            },
            (error) => {
                // Error
                console.error('Location error:', error);
                setLocationStatus('denied');
                localStorage.setItem('locationPermission', 'denied');
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    }, [locationStatus]);

    // Context value
    const value = {
        locationStatus,
        userLocation,
        requestLocationAccess
    };

    return (
        <LocationContext.Provider value={value}>
            {children}
        </LocationContext.Provider>
    );
};

export default LocationContext;