import React, { createContext, useState, useContext, useCallback, useEffect } from 'react';

// Create context
const LocationContext = createContext();

export const useLocation = () => useContext(LocationContext);

export const LocationProvider = ({ children }) => {
    // Location status can be: 'initial', 'requesting', 'granted', 'denied', 'unavailable'
    const [locationStatus, setLocationStatus] = useState('initial');
    const [coordinates, setCoordinates] = useState(null);

    // Check if geolocation is available in the browser
    useEffect(() => {
        if (!navigator.geolocation) {
            setLocationStatus('unavailable');
        } else {
            // Check if permission was previously granted/denied
            const savedPermission = localStorage.getItem('locationPermission');
            if (savedPermission === 'granted') {
                requestLocationAccess();
            } else if (savedPermission === 'denied') {
                setLocationStatus('denied');
            }
        }
    }, []);

    // Request location access
    const requestLocationAccess = useCallback(() => {
        if (!navigator.geolocation) {
            setLocationStatus('unavailable');
            return;
        }

        setLocationStatus('requesting');

        navigator.geolocation.getCurrentPosition(
            // Success
            (position) => {
                setCoordinates({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
                setLocationStatus('granted');
                localStorage.setItem('locationPermission', 'granted');
            },
            // Error
            (error) => {
                console.error('Error getting location:', error);
                setLocationStatus('denied');
                localStorage.setItem('locationPermission', 'denied');
            },
            // Options
            {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0
            }
        );
    }, []);

    // Clear location data
    const clearLocation = useCallback(() => {
        setCoordinates(null);
        setLocationStatus('initial');
        localStorage.removeItem('locationPermission');
    }, []);

    // Context value
    const value = {
        locationStatus,
        coordinates,
        requestLocationAccess,
        clearLocation
    };

    return (
        <LocationContext.Provider value={value}>
            {children}
        </LocationContext.Provider>
    );
};

export default LocationContext;