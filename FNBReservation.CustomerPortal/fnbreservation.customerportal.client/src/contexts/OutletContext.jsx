import React, { createContext, useState, useContext, useCallback, useEffect } from 'react';
import OutletService from '../services/OutletService';
import { useLocation as useLocationContext } from './LocationContext';

// Create context
const OutletContext = createContext();

export const useOutlet = () => useContext(OutletContext);

export const OutletProvider = ({ children }) => {
    // Get location from LocationContext
    const { locationStatus, userLocation } = useLocationContext();

    // Outlet state
    const [outlets, setOutlets] = useState([]);
    const [currentOutlet, setCurrentOutlet] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    // Calculate distance between user location and outlet
    const calculateDistance = (lat1, lon1, lat2, lon2) => {
        if (!lat1 || !lon1 || !lat2 || !lon2) return null;

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

    // Fetch all outlets
    const getAllOutlets = useCallback(async () => {
        setLoading(true);
        setError(null);

        try {
            // For development, use mock data
            // In production, uncomment the API call below
            // const response = await OutletService.getAllOutlets();
            const response = OutletService.getMockOutlets();

            // If user location is available, sort by distance
            if (locationStatus === 'granted' && userLocation) {
                const outletsWithDistance = response.outlets.map(outlet => ({
                    ...outlet,
                    distance: calculateDistance(
                        userLocation.latitude,
                        userLocation.longitude,
                        outlet.latitude,
                        outlet.longitude
                    )
                }));

                // Sort by distance
                outletsWithDistance.sort((a, b) => {
                    if (a.distance === null) return 1;
                    if (b.distance === null) return -1;
                    return a.distance - b.distance;
                });

                setOutlets(outletsWithDistance);
            } else {
                setOutlets(response.outlets || []);
            }

            return response.outlets || [];
        } catch (err) {
            setError('Failed to fetch outlets. Please try again.');
            console.error('Error fetching outlets:', err);
            return [];
        } finally {
            setLoading(false);
        }
    }, [locationStatus, userLocation]);

    // Get outlet by ID
    const getOutletById = useCallback(async (id) => {
        setLoading(true);
        setError(null);

        try {
            // For development, use mock data
            // In production, uncomment the API call below
            // const response = await OutletService.getOutletById(id);
            const response = OutletService.getMockOutlets();
            const outlet = response.outlets.find(o => o.id === id);

            if (outlet) {
                setCurrentOutlet(outlet);
                return outlet;
            } else {
                setError('Outlet not found');
                return null;
            }
        } catch (err) {
            setError('Failed to fetch outlet details. Please try again.');
            console.error('Error fetching outlet:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get nearby outlets
    const getNearbyOutlets = useCallback(async (radius = 10) => {
        if (locationStatus !== 'granted' || !userLocation) {
            return getAllOutlets();
        }

        setLoading(true);
        setError(null);

        try {
            // For development, use mock data
            // In production, uncomment the API call below
            /*
            const response = await OutletService.getNearbyOutlets(
                userLocation.latitude,
                userLocation.longitude,
                radius
            );
            */
            const response = OutletService.getMockOutlets();

            // Calculate distance for each outlet
            const outletsWithDistance = response.outlets.map(outlet => ({
                ...outlet,
                distance: calculateDistance(
                    userLocation.latitude,
                    userLocation.longitude,
                    outlet.latitude,
                    outlet.longitude
                )
            }));

            // Sort by distance
            outletsWithDistance.sort((a, b) => {
                if (a.distance === null) return 1;
                if (b.distance === null) return -1;
                return a.distance - b.distance;
            });

            // Filter outlets within the radius
            const nearbyOutlets = outletsWithDistance.filter(outlet => {
                return outlet.distance !== null && outlet.distance <= radius;
            });

            setOutlets(nearbyOutlets);
            return nearbyOutlets;
        } catch (err) {
            setError('Failed to fetch nearby outlets. Please try again.');
            console.error('Error fetching nearby outlets:', err);
            return [];
        } finally {
            setLoading(false);
        }
    }, [locationStatus, userLocation, getAllOutlets]);

    // Fetch outlets on location change
    useEffect(() => {
        if (locationStatus === 'granted' && userLocation) {
            getNearbyOutlets();
        } else if (outlets.length === 0) {
            getAllOutlets();
        }
    }, [locationStatus, userLocation, getAllOutlets, getNearbyOutlets, outlets.length]);

    // Clear current outlet
    const clearCurrentOutlet = useCallback(() => {
        setCurrentOutlet(null);
    }, []);

    // Clear error
    const clearError = useCallback(() => {
        setError(null);
    }, []);

    // Context value
    const value = {
        outlets,
        currentOutlet,
        loading,
        error,
        getAllOutlets,
        getOutletById,
        getNearbyOutlets,
        clearCurrentOutlet,
        clearError
    };

    return (
        <OutletContext.Provider value={value}>
            {children}
        </OutletContext.Provider>
    );
};

export default OutletContext;