
import React, { createContext, useState, useContext, useEffect } from 'react';

// Create context
const LocationContext = createContext();

export const useLocation = () => useContext(LocationContext);

export const LocationProvider = ({ children }) => {
    const [location, setLocation] = useState(null);
    const [locationStatus, setLocationStatus] = useState('initial'); // 'initial', 'granted', 'denied', 'requesting', 'unavailable'

    useEffect(() => {
        // Check stored permission on component mount
        const storedPermission = localStorage.getItem('locationPermission');

        if (storedPermission === 'granted') {
            requestLocationAccess();
        } else if (storedPermission === 'denied') {
            setLocationStatus('denied');
        }
    }, []);

    const requestLocationAccess = () => {
        if (!navigator.geolocation) {
            setLocationStatus('unavailable');
            return;
        }

        setLocationStatus('requesting');

        navigator.geolocation.getCurrentPosition(
            (position) => {
                setLocation({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
                setLocationStatus('granted');
                localStorage.setItem('locationPermission', 'granted');
            },
            (error) => {
                console.error('Error getting location:', error);
                setLocationStatus('denied');
                localStorage.setItem('locationPermission', 'denied');
            },
            {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0
            }
        );
    };

    const resetLocation = () => {
        setLocation(null);
        setLocationStatus('initial');
        localStorage.removeItem('locationPermission');
    };

    // Context value
    const value = {
        location,
        locationStatus,
        requestLocationAccess,
        resetLocation
    };

    return (
        <LocationContext.Provider value={value}>
            {children}
        </LocationContext.Provider>
    );
};

export default LocationContext;
