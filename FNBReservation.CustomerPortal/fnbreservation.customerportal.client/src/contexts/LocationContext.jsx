import React, { createContext, useState, useContext, useCallback, useEffect } from 'react';

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

    // Current user location - initialize from sessionStorage if available
    const [userCoordinates, setUserCoordinates] = useState(() => {
        try {
            const savedCoordinates = sessionStorage.getItem('userCoordinates');
            if (savedCoordinates) {
                return JSON.parse(savedCoordinates);
            }
            return null;
        } catch (error) {
            console.error('Error parsing saved coordinates:', error);
            return null;
        }
    });

    // Save coordinates to sessionStorage whenever they change
    useEffect(() => {
        if (userCoordinates) {
            sessionStorage.setItem('userCoordinates', JSON.stringify(userCoordinates));
        }
    }, [userCoordinates]);

    // Request location access
    const requestLocationAccess = useCallback(() => {
        // If we already have coordinates but status is granted, just return
        if (locationStatus === 'granted' && userCoordinates) {
            console.log("Already have location access and coordinates");
            return;
        }
        
        // If status is unavailable, don't try again
        if (locationStatus === 'unavailable') {
            return;
        }

        // Check if geolocation is available
        if (!navigator.geolocation) {
            setLocationStatus('unavailable');
            return;
        }

        // Request location
        setLocationStatus('requesting');
        console.log("Requesting geolocation...");

        navigator.geolocation.getCurrentPosition(
            (position) => {
                // Success
                const coords = {
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                };
                console.log("Got coordinates:", coords);
                setUserCoordinates(coords);
                setLocationStatus('granted');
                localStorage.setItem('locationPermission', 'granted');
                // Also save to sessionStorage
                sessionStorage.setItem('userCoordinates', JSON.stringify(coords));
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
    }, [locationStatus, userCoordinates]);

    // Automatically request location if permission is granted but no coordinates
    useEffect(() => {
        if (locationStatus === 'granted' && !userCoordinates) {
            console.log("Permission granted but no coordinates - requesting location");
            requestLocationAccess();
        }
    }, [locationStatus, userCoordinates, requestLocationAccess]);

    // Context value
    const value = {
        locationStatus,
        userCoordinates,
        requestLocationAccess
    };

    return (
        <LocationContext.Provider value={value}>
            {children}
        </LocationContext.Provider>
    );
};

export default LocationContext;