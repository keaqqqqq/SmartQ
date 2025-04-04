import React, { createContext, useState, useContext, useCallback } from 'react';
import ReservationService from '../services/ReservationService';
import { useLocation } from './LocationContext';
import { format } from 'date-fns';

// Create context
const ReservationContext = createContext();

export const useReservation = () => useContext(ReservationContext);

export const ReservationProvider = ({ children }) => {
    // Get location from LocationContext
    const { location } = useLocation();

    // Reservation state
    const [outlets, setOutlets] = useState([]);
    const [availableSlots, setAvailableSlots] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [reservationDetails, setReservationDetails] = useState(null);
    const [userReservations, setUserReservations] = useState([]);

    // Format date for API
    const formatDateForApi = (date) => {
        return format(new Date(date), 'yyyy-MM-dd');
    };

    // Format time with timezone for API
    const formatDateTimeWithTimezone = (date, time) => {
        const reservationDateTime = new Date(`${date}T${time}`);
        const timezone = '+08:00'; // Malaysia timezone
        return format(reservationDateTime, "yyyy-MM-dd'T'HH:mm:ss") + timezone;
    };

    // Fetch nearby outlets
    const getNearbyOutlets = useCallback(async () => {
        setLoading(true);
        setError(null);

        try {
            // Include user location if available
            const locationParams = location ? {
                latitude: location.latitude,
                longitude: location.longitude
            } : null;

            const response = await ReservationService.getNearbyOutlets(locationParams);
            setOutlets(response.outlets || []);
            return response.outlets || [];
        } catch (err) {
            setError('Failed to fetch nearby outlets. Please try again.');
            console.error('Error fetching outlets:', err);
            return [];
        } finally {
            setLoading(false);
        }
    }, [location]);

    // Check availability
    const checkAvailability = useCallback(async (params) => {
        setLoading(true);
        setError(null);

        try {
            const apiParams = {
                outletId: params.outletId,
                partySize: params.partySize,
                date: formatDateForApi(params.date),
                preferredTime: params.preferredTime,
                earliestTime: params.earliestTime || params.preferredTime,
                latestTime: params.latestTime || params.preferredTime
            };

            const response = await ReservationService.checkAvailability(apiParams);
            setAvailableSlots(response.availableSlots || []);
            return response.availableSlots || [];
        } catch (err) {
            setError('Failed to check availability. Please try again.');
            console.error('Error checking availability:', err);
            return [];
        } finally {
            setLoading(false);
        }
    }, []);

    // Create reservation
    const createReservation = useCallback(async (data) => {
        setLoading(true);
        setError(null);

        try {
            const apiData = {
                outletId: data.outletId,
                customerName: data.customerName,
                customerPhone: data.customerPhone,
                customerEmail: data.customerEmail,
                partySize: data.partySize,
                reservationDate: formatDateTimeWithTimezone(data.date, data.time),
                specialRequests: data.specialRequests
            };

            const response = await ReservationService.createReservation(apiData);
            setReservationDetails(response);
            return response;
        } catch (err) {
            setError('Failed to create reservation. Please try again.');
            console.error('Error creating reservation:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get reservation by ID
    const getReservationById = useCallback(async (id) => {
        setLoading(true);
        setError(null);

        try {
            const response = await ReservationService.getReservationById(id);
            setReservationDetails(response);
            return response;
        } catch (err) {
            setError('Failed to fetch reservation details. Please try again.');
            console.error('Error fetching reservation by ID:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get reservation by code - MODIFIED TO INCLUDE DUMMY DATA
    const getReservationByCode = useCallback(async (code) => {
        setLoading(true);
        setError(null);

        try {
            // Check if the code is our special test code "RES9299"
            if (code === "RES9299") {
                // Return dummy data for this special code
                const dummyReservation = {
                    id: "RES9299",
                    reservationCode: "RES9299",
                    outletId: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
                    outletName: "Main Branch",
                    customerName: "John Doe",
                    customerPhone: "+60 12-345-6789",
                    customerEmail: "john.doe@example.com",
                    partySize: 4,
                    reservationDate: "2025-04-10T19:30:00+08:00",
                    status: "Confirmed",
                    specialRequests: "Window seat if possible, celebrating anniversary",
                    createdAt: "2025-04-04T14:30:00+08:00"
                };



                // Simulate network delay
                await new Promise(resolve => setTimeout(resolve, 1000));

                setReservationDetails(dummyReservation);
                return dummyReservation;
            }

            // For any other code, proceed with the regular API call
            const response = await ReservationService.getReservationByCode(code);
            setReservationDetails(response);
            return response;
        } catch (err) {
            setError('Failed to fetch reservation details. Please try again.');
            console.error('Error fetching reservation by code:', err);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    // Get reservations by phone - MODIFIED TO INCLUDE DUMMY DATA
    const getReservationsByPhone = useCallback(async (phone) => {
        setLoading(true);
        setError(null);

        try {
            // Check if this is our test phone number
            if (phone === "+60 12-345-6789") {
                // Return dummy reservations for this phone
                const dummyReservations = [
                    {
                        id: "12345",
                        reservationCode: "RES9299",
                        outletId: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5",
                        outletName: "Main Branch",
                        customerName: "John Doe",
                        customerPhone: "+60 12-345-6789",
                        partySize: 4,
                        reservationDate: "2025-04-10T19:30:00+08:00",
                        status: "Confirmed"
                    },
                    {
                        id: "12346",
                        reservationCode: "RES8192",
                        outletId: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5",
                        outletName: "Downtown Location",
                        customerName: "John Doe",
                        customerPhone: "+60 12-345-6789",
                        partySize: 2,
                        reservationDate: "2025-03-28T18:00:00+08:00",
                        status: "Completed"
                    }
                ];

                // Simulate network delay
                await new Promise(resolve => setTimeout(resolve, 1000));

                setUserReservations(dummyReservations);
                return { reservations: dummyReservations };
            }

            // For any other phone, proceed with the regular API call
            const response = await ReservationService.getReservationsByPhone(phone);
            setUserReservations(response.reservations || []);
            return response;
        } catch (err) {
            setError('Failed to fetch your reservations. Please try again.');
            console.error('Error fetching reservations by phone:', err);
            return [];
        } finally {
            setLoading(false);
        }
    }, []);

    // Update reservation
    const updateReservation = useCallback(async (data) => {
        setLoading(true);
        setError(null);

        try {
            const response = await ReservationService.updateReservation(data);
            setReservationDetails(response);
            return response;
        } catch (err) {
            setError('Failed to update reservation. Please try again.');
            console.error('Error updating reservation:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, []);

    // Cancel reservation with dummy support
    const cancelReservation = useCallback(async (id) => {
        setLoading(true);
        setError(null);

        try {
            // For our dummy reservation
            if (id === "12345") {
                // Simulate network delay
                await new Promise(resolve => setTimeout(resolve, 1000));

                // Update the user reservations if they exist
                if (userReservations.length > 0) {
                    const updatedReservations = userReservations.map(res => {
                        if (res.id === id) {
                            return { ...res, status: "Cancelled" };
                        }
                        return res;
                    });
                    setUserReservations(updatedReservations);
                }

                // Also update reservation details if currently viewing this reservation
                if (reservationDetails && reservationDetails.id === id) {
                    setReservationDetails({
                        ...reservationDetails,
                        status: "Cancelled"
                    });
                }

                return { success: true, message: "Reservation cancelled successfully" };
            }

            // For any other ID, proceed with the regular API call
            const response = await ReservationService.cancelReservation(id);

            // Update user reservations if they exist
            if (userReservations.length > 0) {
                setUserReservations(userReservations.filter(res => res.id !== id));
            }

            return response;
        } catch (err) {
            setError('Failed to cancel reservation. Please try again.');
            console.error('Error canceling reservation:', err);
            throw err;
        } finally {
            setLoading(false);
        }
    }, [userReservations, reservationDetails]);

    // Clear reservation details
    const clearReservationDetails = useCallback(() => {
        setReservationDetails(null);
    }, []);

    // Clear error
    const clearError = useCallback(() => {
        setError(null);
    }, []);

    // Context value
    const value = {
        outlets,
        availableSlots,
        loading,
        error,
        reservationDetails,
        userReservations,
        getNearbyOutlets,
        checkAvailability,
        createReservation,
        getReservationById,
        getReservationByCode,
        getReservationsByPhone,
        updateReservation,
        cancelReservation,
        clearReservationDetails,
        clearError
    };

    return (
        <ReservationContext.Provider value={value}>
            {children}
        </ReservationContext.Provider>
    );
};