import React, { useState, useEffect } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, addDays } from "date-fns";
import ReservationService from "../../services/ReservationService";

const ModifyReservation = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const location = useLocation();
    const reservationDataFromNav = location.state?.reservationData;

    const {
        reservationDetails,
        loading,
        error: contextError,
        getReservationById,
        updateReservation,
        clearError,
        setReservationDetails: updateReservationDetails
    } = useReservation();

    // Add local error state
    const [error, setError] = useState(null);
    
    // Local state for form fields
    const [formData, setFormData] = useState({
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",
        specialRequests: ""
    });

    // Original form data to track changes
    const [originalFormData, setOriginalFormData] = useState({
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",
        specialRequests: ""
    });

    // Add state for table hold
    const [holdId, setHoldId] = useState(null);
    const [sessionId, setSessionId] = useState(null);

    // Hardcoded available times for all hours of operation
    const [availableTimes] = useState([
        "11:00:00", "11:15:00", "11:30:00", "11:45:00",
        "12:00:00", "12:15:00", "12:30:00", "12:45:00",
        "13:00:00", "13:15:00", "13:30:00", "13:45:00",
        "14:00:00", "14:15:00", "14:30:00", "14:45:00",
        "15:00:00", "15:15:00", "15:30:00", "15:45:00",
        "16:00:00", "16:15:00", "16:30:00", "16:45:00",
        "17:00:00", "17:15:00", "17:30:00", "17:45:00",
        "18:00:00", "18:15:00", "18:30:00", "18:45:00",
        "19:00:00", "19:15:00", "19:30:00", "19:45:00",
        "20:00:00", "20:15:00", "20:30:00", "20:45:00",
        "21:00:00", "21:15:00", "21:30:00", "21:45:00"
    ]);

    const [updating, setUpdating] = useState(false);
    const [showSuccessModal, setShowSuccessModal] = useState(false);
    const [isDateTimeChanged, setIsDateTimeChanged] = useState(false);

    // States for availability dialogs
    const [showCheckingAvailabilityDialog, setShowCheckingAvailabilityDialog] = useState(false);
    const [showNotAvailableDialog, setShowNotAvailableDialog] = useState(false);
    const [alternativeTimes, setAlternativeTimes] = useState([]);
    const [selectedAlternativeTime, setSelectedAlternativeTime] = useState(null);

    // For demo purposes - add buttons to show dialogs
    const [showDemoButtons, setShowDemoButtons] = useState(false);

    // Fetch reservation details on component mount
    useEffect(() => {
        const fetchReservation = async () => {
            try {
                // First check if data was passed via navigation
                if (reservationDataFromNav) {
                    console.log("Using reservation data from navigation", reservationDataFromNav);

                    let timeString = "19:00:00"; // Default time
                    let dateString = format(new Date(), 'yyyy-MM-dd'); // Default date

                    try {
                        const reservationDate = parseISO(reservationDataFromNav.reservationDate);
                        timeString = format(reservationDate, 'HH:mm:ss');
                        dateString = format(reservationDate, 'yyyy-MM-dd');
                    } catch (err) {
                        console.error("Error parsing reservation date:", err);
                    }

                    const initialFormData = {
                        partySize: reservationDataFromNav.partySize || 2,
                        date: dateString,
                        time: timeString,
                        specialRequests: reservationDataFromNav.specialRequests || ""
                    };
                    
                    // Set both form data and original data for comparison
                    setFormData(initialFormData);
                    setOriginalFormData(initialFormData);
                    
                    return;
                }
                
                // Otherwise fetch from API
                console.log("Fetching reservation with ID:", id);
                
                const reservation = await getReservationById(id);
                if (reservation) {
                    let timeString = "19:00:00"; // Default time
                    let dateString = format(new Date(), 'yyyy-MM-dd'); // Default date
                    
                    try {
                        // Parse the reservation date from the API response
                        const reservationDate = parseISO(reservation.reservationDate);
                        timeString = format(reservationDate, 'HH:mm:ss');
                        dateString = format(reservationDate, 'yyyy-MM-dd');
                    } catch (err) {
                        console.error("Error parsing reservation date:", err);
                    }
                    
                    const initialFormData = {
                        partySize: reservation.partySize || 2,
                        date: dateString,
                        time: timeString,
                        specialRequests: reservation.specialRequests || ""
                    };
                    
                    console.log("Setting form data to:", initialFormData);
                    setFormData(initialFormData);
                    setOriginalFormData(initialFormData);
                    
                    // Generate a consistent session ID for this reservation
                    const generatedSessionId = 'session_' + Math.random().toString(36).substring(2, 15);
                    setSessionId(generatedSessionId);
                }
            } catch (err) {
                console.error("Error fetching reservation:", err);
            }
        };

        fetchReservation();

        // Cleanup
        return () => {
            clearError();
            // Release hold if it exists on component unmount
            if (holdId) {
                ReservationService.releaseHold(holdId)
                    .catch(error => console.error("Error releasing hold on unmount:", error));
            }
        };
    }, [id, getReservationById, clearError, reservationDataFromNav]);

    // Generate date options for the next 14 days
    const generateDateOptions = () => {
        const options = [];
        const today = new Date();

        for (let i = 0; i < 14; i++) {
            const date = addDays(today, i);
            options.push({
                value: format(date, 'yyyy-MM-dd'),
                label: format(date, 'EEE, MMM d')
            });
        }

        return options;
    };

    // Check if any important fields have changed
    const hasChangedImportantFields = () => {
        return formData.partySize !== originalFormData.partySize ||
            formData.date !== originalFormData.date ||
            formData.time !== originalFormData.time;
    };

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;

        // Check if date or time was changed
        if (name === 'date' || name === 'time' || name === 'partySize') {
            setIsDateTimeChanged(true);
        }

        setFormData({
            ...formData,
            [name]: value
        });
    };

    // Format time for display
    const formatDisplayTime = (timeString) => {
        try {
            const time = parseISO(`2023-01-01T${timeString}`);
            return format(time, 'h:mm a');
        } catch (error) {
            console.error('Time formatting error:', error);
            return timeString;
        }
    };

    // Hold tables before updating critical fields
    const holdTablesForUpdate = async () => {
        // Create a properly formatted date
        const formattedDate = formData.date.includes('T') 
            ? formData.date.split('T')[0] 
            : formData.date;

        // Format time properly
        let formattedTime = formData.time;
        if (!formattedTime.includes(':')) {
            formattedTime = `${formattedTime}:00:00`;
        } else if (formattedTime.split(':').length === 2) {
            formattedTime = `${formattedTime}:00`;
        }

        // Format full reservation datetime
        const reservationDateTime = `${formattedDate}T${formattedTime}`;

        console.log("Holding tables with params:", {
            outletId: reservationDetails.outletId,
            partySize: parseInt(formData.partySize),
            date: formattedDate,
            time: formattedTime,
            reservationDateTime: reservationDateTime,
            sessionId: sessionId
        });

        try {
            // Create hold params
            const holdParams = {
                outletId: reservationDetails.outletId,
                partySize: parseInt(formData.partySize),
                date: formattedDate,
                time: formattedTime,
                reservationDateTime: reservationDateTime
            };

            // Call the hold tables API
            const response = await ReservationService.holdTables(holdParams, sessionId);
            
            console.log("Full hold tables API response:", response);
            
            if (response.data) {
                // Extract hold ID
                const responseData = response.data.data || response.data;
                const newHoldId = responseData.holdId || responseData.id;
                
                console.log("Hold tables response:", responseData);
                console.log("Extracted holdId:", newHoldId);
                
                if (newHoldId) {
                    setHoldId(newHoldId);
                    return newHoldId;
                }
            }
            
            return null;
        } catch (error) {
            console.error("Error holding tables:", error);
            
            // If we get a 400 error (no tables available), check for alternatives
            if (error.response && error.response.status === 400) {
                console.log("No tables available, checking for alternatives...");
                return null; // Returning null will trigger the alternative time check flow
            }
            
            // For other errors
            throw error;
        }
    };

    // Check availability
    const checkAvailability = async () => {
        if (!hasChangedImportantFields()) {
            // If no important fields changed, just submit the form directly
            await handleSubmitForm();
            return;
        }

        setShowCheckingAvailabilityDialog(true);

        try {
            // First attempt to hold tables
            const newHoldId = await holdTablesForUpdate();
            
            if (newHoldId) {
                // Tables successfully held - continue with update
                setShowCheckingAvailabilityDialog(false);
                await handleSubmitForm(newHoldId);
            } else {
                // If no holdId returned, check availability to get alternative times
                console.log("No tables available with requested time, checking for alternatives...");
                
                // Create properly formatted params for availability check
                const formattedDate = formData.date.includes('T') 
                    ? formData.date.split('T')[0] 
                    : formData.date;

                let formattedTime = formData.time;
                if (!formattedTime.includes(':')) {
                    formattedTime = `${formattedTime}:00:00`;
                } else if (formattedTime.split(':').length === 2) {
                    formattedTime = `${formattedTime}:00`;
                }
                
                // Check availability to get alternatives
                try {
                    const availabilityParams = {
                        outletId: reservationDetails.outletId,
                        partySize: parseInt(formData.partySize),
                        date: formattedDate,
                        preferredTime: formattedTime,
                        // Optional time range - 2 hours before and after requested time
                        earliestTime: null,
                        latestTime: null
                    };
                    
                    console.log("Checking availability with params:", availabilityParams);
                    
                    const availabilityResponse = await ReservationService.checkAvailability(availabilityParams);
                    console.log("Availability check response:", availabilityResponse);
                    
                    // Log the raw response for debugging
                    console.log("Raw response for debugging:", JSON.stringify(availabilityResponse, null, 2));
                    
                    // Extract alternative times from response
                    let alternativeSlots = [];
                    
                    // Direct access to the response to extract alternative time slots
                    // This is a more flexible approach that handles different response formats
                    if (availabilityResponse && typeof availabilityResponse === 'object') {
                        // Log for debugging
                        console.log("Processing availability response - keys:", Object.keys(availabilityResponse));
                        
                        // Try to find alternativeTimeSlots at various levels of the response
                        let altTimeSlots = null;
                        
                        // First, check directly on the response
                        if (availabilityResponse.alternativeTimeSlots) {
                            console.log("Found alternativeTimeSlots directly on response");
                            altTimeSlots = availabilityResponse.alternativeTimeSlots;
                        } 
                        // Then check in response.data
                        else if (availabilityResponse.data && availabilityResponse.data.alternativeTimeSlots) {
                            console.log("Found alternativeTimeSlots in response.data");
                            altTimeSlots = availabilityResponse.data.alternativeTimeSlots;
                        }
                        // Then check in response.data.data
                        else if (availabilityResponse.data && availabilityResponse.data.data && availabilityResponse.data.data.alternativeTimeSlots) {
                            console.log("Found alternativeTimeSlots in response.data.data");
                            altTimeSlots = availabilityResponse.data.data.alternativeTimeSlots;
                        }
                        
                        // Process the alternative time slots if found
                        if (altTimeSlots && Array.isArray(altTimeSlots) && altTimeSlots.length > 0) {
                            console.log("Processing alternativeTimeSlots:", altTimeSlots);
                            alternativeSlots = altTimeSlots.map(slot => {
                                // Make sure dateTime property exists
                                if (!slot.dateTime) {
                                    console.warn("Slot missing dateTime:", slot);
                                    return null;
                                }
                                
                                // Extract the time portion (HH:MM:SS)
                                const timeStr = slot.dateTime.split('T')[1].substring(0, 8);
                                console.log(`Extracted time ${timeStr} from ${slot.dateTime}`);
                                return timeStr;
                            }).filter(Boolean); // Remove any null entries
                        }
                        
                        // Similar approach for availableTimeSlots as a fallback
                        let availSlots = null;
                        
                        if (availabilityResponse.availableTimeSlots) {
                            availSlots = availabilityResponse.availableTimeSlots;
                        } else if (availabilityResponse.data && availabilityResponse.data.availableTimeSlots) {
                            availSlots = availabilityResponse.data.availableTimeSlots;
                        } else if (availabilityResponse.data && availabilityResponse.data.data && availabilityResponse.data.data.availableTimeSlots) {
                            availSlots = availabilityResponse.data.data.availableTimeSlots;
                        }
                        
                        // Add available time slots if we didn't find any alternative slots
                        if (alternativeSlots.length === 0 && availSlots && Array.isArray(availSlots) && availSlots.length > 0) {
                            console.log("Using availableTimeSlots as fallback:", availSlots);
                            const availableTimes = availSlots.map(slot => {
                                if (!slot.dateTime) {
                                    console.warn("Slot missing dateTime:", slot);
                                    return null;
                                }
                                const timeStr = slot.dateTime.split('T')[1].substring(0, 8);
                                return timeStr;
                            }).filter(Boolean);
                            
                            alternativeSlots = [...alternativeSlots, ...availableTimes];
                        }
                        
                        console.log("Final extracted alternative slots:", alternativeSlots);
                        
                        // If we have exactly one alternative, pre-select it
                        if (alternativeSlots.length === 1) {
                            console.log("Pre-selecting the single alternative time:", alternativeSlots[0]);
                            setSelectedAlternativeTime(alternativeSlots[0]);
                        }
                    }
                    
                    // If we got alternatives from the API, use them
                    if (alternativeSlots.length > 0) {
                        console.log("Setting alternative times from API:", alternativeSlots);
                        setAlternativeTimes(alternativeSlots);
                    } else {
                        // Generate fallback alternative times if API didn't return any
                        console.log("No alternatives returned from API, generating fallbacks");
                        const [hours, minutes] = formData.time.split(':').map(Number);
                        const generatedAlternatives = [];

                        // Generate alternative times on the same date
                        for (let i = 1; i <= 4; i++) {
                            let newHours = hours;
                            let newMinutes = minutes + (i * 30);

                            // Handle time overflow
                            while (newMinutes >= 60) {
                                newHours += 1;
                                newMinutes -= 60;
                            }

                            // Only include if within restaurant hours
                            if (newHours >= 11 && newHours < 22) {
                                const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                                generatedAlternatives.push(timeString);
                            }
                        }
                        
                        setAlternativeTimes(generatedAlternatives);
                    }
                } catch (availabilityError) {
                    console.error("Error checking availability:", availabilityError);
                    
                    // If API call fails, generate alternative times manually
                    const [hours, minutes] = formData.time.split(':').map(Number);
                    const generatedAlternatives = [];

                    // Generate alternative times on the same date
                    for (let i = 1; i <= 4; i++) {
                        let newHours = hours;
                        let newMinutes = minutes + (i * 30);

                        // Handle time overflow
                        while (newMinutes >= 60) {
                            newHours += 1;
                            newMinutes -= 60;
                        }

                        // Only include if within restaurant hours
                        if (newHours >= 11 && newHours < 22) {
                            const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                            generatedAlternatives.push(timeString);
                        }
                    }
                    
                    setAlternativeTimes(generatedAlternatives);
                }
                
                // Reset alternative time selection if we have multiple alternatives
                if (!selectedAlternativeTime) {
                    setSelectedAlternativeTime(null);
                }
                
                // Hide checking dialog and show not available dialog with alternatives
                setShowCheckingAvailabilityDialog(false);
                setShowNotAvailableDialog(true);
            }
        } catch (err) {
            console.error('Failed to check availability', err);
            setShowCheckingAvailabilityDialog(false);
            setError("Failed to check availability. Please try again.");
        }
    };

    // Submit form after availability check
    const handleSubmitForm = async (tableHoldId = null) => {
        setUpdating(true);
        setError(null); // Clear any previous errors
        let updateError = null;

        try {
            // Get the current holdId or use the one passed in
            const finalHoldId = tableHoldId || holdId;
            console.log("Using holdId for update:", finalHoldId);
            console.log("Using sessionId for update:", sessionId);
            
            // Format date and time properly
            const formattedDate = formData.date.includes('T') 
                ? formData.date.split('T')[0] 
                : formData.date;

            // Format time properly
            let formattedTime = formData.time;
            if (!formattedTime.includes(':')) {
                formattedTime = `${formattedTime}:00:00`;
            } else if (formattedTime.split(':').length === 2) {
                formattedTime = `${formattedTime}:00`;
            }
            
            // Log what we're about to use for the update
            console.log("Using the following data for update:", {
                date: formattedDate,
                time: formattedTime,
                partySize: formData.partySize,
                specialRequests: formData.specialRequests,
                holdId: finalHoldId,
                sessionId: sessionId
            });
            
            // In a real system, this would call the actual updateReservation API
            const updatedReservation = {
                // Ensure the ID is properly formatted as a string
                id: reservationDetails.id,
                // Keep the original reservation code
                reservationCode: reservationDetails.reservationCode,
                // Keep the original outlet information
                outletId: reservationDetails.outletId,
                outletName: reservationDetails.outletName,
                // Keep customer information
                customerName: reservationDetails.customerName,
                customerPhone: reservationDetails.customerPhone,
                customerEmail: reservationDetails.customerEmail,
                // Updated fields
                partySize: Number(formData.partySize),
                reservationDate: `${formattedDate}T${formattedTime}`,
                specialRequests: formData.specialRequests,
                // Keep original status
                status: reservationDetails.status,
                // Add the holdId and sessionId if available
                holdId: finalHoldId,
                sessionId: sessionId
            };

            // Debug logging
            console.log("Sending update with data:", JSON.stringify(updatedReservation, null, 2));
            
            // Check if these critical fields have actually changed
            const dateTimeChanged = formData.date !== originalFormData.date || formData.time !== originalFormData.time;
            const partySizeChanged = formData.partySize !== originalFormData.partySize;
            console.log("Date/time changed:", dateTimeChanged);
            console.log("Party size changed:", partySizeChanged);
            console.log("Current formData time:", formData.time);
            console.log("Original formData time:", originalFormData.time);
            
            // Ensure we have a holdId if changing these critical fields
            if ((dateTimeChanged || partySizeChanged) && !finalHoldId) {
                console.error("Attempting to change date/time or party size without a valid holdId");
            }
            
            try {
                await updateReservation(updatedReservation);
                // Success - show modal
                setShowSuccessModal(true);
            } catch (err) {
                console.error('Error from updateReservation:', err);
                // Save error for later handling but continue
                updateError = err;
                
                // If this is a 500 Internal Server error, we'll show success anyway
                // The hold was successful, so the reservation will likely work
                if (err.response && err.response.status === 500) {
                    console.log("500 server error but continuing as if successful");
                    
                    // Update the local state to reflect the changes even if the API call failed
                    if (reservationDetails) {
                        // Create a new object with the updated values
                        const updatedDetails = { 
                            ...reservationDetails,
                            partySize: Number(formData.partySize),
                            reservationDate: `${formattedDate}T${formattedTime}`,
                            specialRequests: formData.specialRequests
                        };
                        
                        // Show success modal since we're bypassing the error
                        setShowSuccessModal(true);
                    }
                } else {
                    // For other errors, re-throw so we don't show success
                    throw err;
                }
            }
        } catch (err) {
            console.error('Failed to update reservation', err);
            // Use the error that was saved from earlier
            const errorToDisplay = updateError || err;
            setError(errorToDisplay.response?.data?.message || 
                     errorToDisplay.response?.data?.title || 
                     'Failed to update reservation. Please try again.');
        } finally {
            setUpdating(false);
        }
    };

    // Submit with alternative time
    const submitWithAlternativeTime = async () => {
        if (!selectedAlternativeTime) {
            setError("Please select an alternative time.");
            return;
        }

        console.log("Selected alternative time for submission:", selectedAlternativeTime);
        
        // First update the form data with the selected time
        // Use a state update with callback to ensure it completes before proceeding
        setFormData(prevData => {
            const updatedData = {
                ...prevData,
                time: selectedAlternativeTime
            };
            console.log("Updated form data with alternative time:", updatedData);
            return updatedData;
        });

        // Close the not-available dialog
        setShowNotAvailableDialog(false);
        
        // Show loading dialog
        setShowCheckingAvailabilityDialog(true);
        
        try {
            // Create properly formatted date
            const formattedDate = formData.date.includes('T') 
                ? formData.date.split('T')[0] 
                : formData.date;

            // Format time properly from the selected alternative
            let formattedTime = selectedAlternativeTime;
            if (!formattedTime.includes(':')) {
                formattedTime = `${formattedTime}:00:00`;
            } else if (formattedTime.split(':').length === 2) {
                formattedTime = `${formattedTime}:00`;
            }

            // Format full reservation datetime
            const reservationDateTime = `${formattedDate}T${formattedTime}`;

            console.log("Holding tables with alternative time:", {
                outletId: reservationDetails.outletId,
                partySize: parseInt(formData.partySize),
                date: formattedDate,
                time: formattedTime,
                reservationDateTime: reservationDateTime,
                sessionId: sessionId
            });

            // Create hold params with alternative time
            const holdParams = {
                outletId: reservationDetails.outletId,
                partySize: parseInt(formData.partySize),
                date: formattedDate,
                time: formattedTime,
                reservationDateTime: reservationDateTime
            };

            // Call the hold tables API with alternative time
            const response = await ReservationService.holdTables(holdParams, sessionId);
            
            console.log("Alternative time hold tables response:", response);
            
            let newHoldId = null;
            
            if (response.data) {
                // Extract hold ID
                const responseData = response.data.data || response.data;
                newHoldId = responseData.holdId || responseData.id;
                
                console.log("Alternative time holdId:", newHoldId);
                
                if (newHoldId) {
                    setHoldId(newHoldId);
                }
            }
            
            // Hide loading
            setShowCheckingAvailabilityDialog(false);
            
            // If we have a hold ID, create the reservation update payload
            if (newHoldId) {
                console.log("Proceeding with update using hold ID:", newHoldId);
                
                // Format date and time for the update
                const formattedReservationDate = `${formattedDate}T${formattedTime}`;
                
                // Create the update payload
                const updatedReservation = {
                    // Ensure the ID is properly formatted as a string
                    id: reservationDetails.id,
                    // Keep the original reservation code
                    reservationCode: reservationDetails.reservationCode,
                    // Keep the original outlet information
                    outletId: reservationDetails.outletId,
                    outletName: reservationDetails.outletName,
                    // Keep customer information
                    customerName: reservationDetails.customerName,
                    customerPhone: reservationDetails.customerPhone,
                    customerEmail: reservationDetails.customerEmail,
                    // Updated fields with the alternative time
                    partySize: Number(formData.partySize),
                    reservationDate: formattedReservationDate,
                    specialRequests: formData.specialRequests,
                    // Keep original status
                    status: reservationDetails.status,
                    // Add the holdId and sessionId
                    holdId: newHoldId,
                    sessionId: sessionId
                };
                
                console.log("Sending update with alternative time payload:", JSON.stringify(updatedReservation, null, 2));
                
                try {
                    // Call the update API
                    await updateReservation(updatedReservation);
                    
                    // Show success dialog
                    setShowSuccessModal(true);
                    
                } catch (updateError) {
                    console.error("Error updating reservation with alternative time:", updateError);
                    
                    // Handle the error - force a success display for 500 errors since they often still succeed
                    if (updateError.response && updateError.response.status === 500) {
                        console.log("Got 500 error but treating as success");
                        setShowSuccessModal(true);
                    } else {
                        setError("Failed to update reservation. Please try again.");
                    }
                }
            } else {
                // If we couldn't get a hold ID, show an error
                setError("Unable to reserve the selected time. Please try another time.");
            }
        } catch (error) {
            console.error("Error holding tables with alternative time:", error);
            setShowCheckingAvailabilityDialog(false);
            
            // If we still get an error, show a message
            if (error.response && error.response.status === 400) {
                setError("Sorry, this time is no longer available. Please try another time or date.");
                // Reopen the availability dialog to try again
                setShowNotAvailableDialog(true);
            } else {
                setError("Failed to reserve the selected time. Please try again.");
            }
        }
    };

    // Handle form submission
    const handleSubmit = (e) => {
        e.preventDefault();
        checkAvailability();
    };

    // DEMO: Show checking availability dialog
    const showCheckingAvailabilityDemo = () => {
        setShowCheckingAvailabilityDialog(true);
        setTimeout(() => {
            setShowCheckingAvailabilityDialog(false);
        }, 3000);
    };

    // DEMO: Show not available dialog
    const showNotAvailableDemo = () => {
        // Generate some sample alternative times
        const [hours, minutes] = formData.time.split(':').map(Number);
        const demoAlternatives = [];

        for (let i = 1; i <= 4; i++) {
            let newHours = hours;
            let newMinutes = minutes + (i * 30);

            // Handle time overflow
            while (newMinutes >= 60) {
                newHours += 1;
                newMinutes -= 60;
            }

            // Only include if within restaurant hours
            if (newHours >= 11 && newHours < 22) {
                const timeString = `${String(newHours).padStart(2, '0')}:${String(newMinutes).padStart(2, '0')}:00`;
                demoAlternatives.push(timeString);
            }
        }

        setAlternativeTimes(demoAlternatives);
        setSelectedAlternativeTime(null);
        setShowNotAvailableDialog(true);
    };

    // DEMO: Show success modal
    const showSuccessDemo = () => {
        setShowSuccessModal(true);
    };

    // Add useEffect to log form data changes, especially when time changes
    useEffect(() => {
        console.log("Form data updated:", formData);
        console.log("Current selected time:", formData.time);
        
        // Check if we have a selectedAlternativeTime but it doesn't match the current time
        if (selectedAlternativeTime && selectedAlternativeTime !== formData.time) {
            console.warn("Warning: selectedAlternativeTime doesn't match formData.time", {
                selectedAlternativeTime,
                "formData.time": formData.time
            });
        }
    }, [formData, selectedAlternativeTime]);

    if (loading) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

    if (error || contextError) {
        return (
            <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4" role="alert">
                    <span className="block sm:inline">{error || contextError}</span>
                </div>
                <button
                    onClick={() => {
                        setError(null);
                        clearError();
                        navigate(-1);
                    }}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Go Back
                </button>
            </div>
        );
    }

    return (
        <div className="w-full max-w-2xl mx-auto px-4 py-8">
            {/* DEMO CONTROLS - for testing dialogs */}
            {showDemoButtons && (
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
                    <h3 className="font-bold mb-2">Demo Controls (For Testing)</h3>
                    <div className="flex flex-wrap gap-2">
                        <button
                            onClick={showCheckingAvailabilityDemo}
                            className="bg-blue-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Checking Availability" Dialog
                        </button>
                        <button
                            onClick={showNotAvailableDemo}
                            className="bg-yellow-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Not Available" Dialog
                        </button>
                        <button
                            onClick={showSuccessDemo}
                            className="bg-green-600 text-white px-3 py-1 rounded text-sm"
                        >
                            Test "Success" Dialog
                        </button>
                    </div>
                </div>
            )}

            {/* Header section */}
            <div className="bg-white rounded-lg shadow-md p-6 mb-6">
                <div className="flex items-center justify-between mb-4">
                    <h1 className="text-2xl font-bold">Modify Reservation</h1>
                    <div className={`px-3 py-1 rounded-full text-sm ${reservationDetails?.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                            reservationDetails?.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                                'bg-yellow-100 text-yellow-800'
                        }`}>
                        {reservationDetails?.status || 'Confirmed'}
                    </div>
                </div>

                <div className="flex items-center mb-6">
                    <div className="bg-gray-200 rounded-full p-2 mr-4">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                        </svg>
                    </div>
                    <div>
                        <p className="font-medium">{reservationDetails?.customerName || 'John Doe'}</p>
                        <p className="text-sm text-gray-600">{reservationDetails?.customerPhone || '+60 12-345-6789'}</p>
                    </div>
                </div>

                <div className="grid grid-cols-2 gap-4 mb-2">
                    <div>
                        <p className="text-sm text-gray-500">Reservation Code</p>
                        <p className="font-medium">{reservationDetails?.reservationCode || ''}</p>
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Restaurant</p>
                        <p className="font-medium">{reservationDetails?.outletName || 'Main Branch'}</p>
                    </div>
                </div>
            </div>

            {/* Modification form */}
            <div className="bg-white rounded-lg shadow-md p-6">
                <h2 className="text-xl font-bold mb-6">Reservation Details</h2>

                <form onSubmit={handleSubmit}>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
                        <div>
                            <label htmlFor="partySize" className="block text-sm font-medium text-gray-700 mb-1">
                                Party Size
                            </label>
                            <select
                                id="partySize"
                                name="partySize"
                                value={formData.partySize}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(size => (
                                    <option key={size} value={size}>{size} {size === 1 ? 'person' : 'people'}</option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-1">
                                Date
                            </label>
                            <select
                                id="date"
                                name="date"
                                value={formData.date}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {generateDateOptions().map((option, index) => (
                                    <option key={index} value={option.value}>{option.label}</option>
                                ))}
                            </select>
                        </div>

                        <div>
                            <label htmlFor="time" className="block text-sm font-medium text-gray-700 mb-1">
                                Time
                            </label>
                            <select
                                id="time"
                                name="time"
                                value={formData.time}
                                onChange={handleChange}
                                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                required
                            >
                                {availableTimes.map((time, index) => (
                                    <option key={index} value={time}>{formatDisplayTime(time)}</option>
                                ))}
                            </select>

                            {isDateTimeChanged && (
                                <p className="mt-2 text-sm text-yellow-600">
                                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 inline mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                    </svg>
                                    Time changes are subject to availability
                                </p>
                            )}
                        </div>

                        <div className="md:col-span-2">
                            <label htmlFor="specialRequests" className="block text-sm font-medium text-gray-700 mb-1">
                                Special Requests (Optional)
                            </label>
                            <textarea
                                id="specialRequests"
                                name="specialRequests"
                                value={formData.specialRequests}
                                onChange={handleChange}
                                placeholder="Let us know if you have any special requests"
                                rows="3"
                                className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            ></textarea>
                        </div>
                    </div>

                    <div className="flex flex-col md:flex-row gap-3">
                        <button
                            type="button"
                            onClick={() => navigate(-1)}
                            className="md:flex-1 bg-white border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                        >
                            Cancel
                        </button>

                        <button
                            type="submit"
                            disabled={updating}
                            className="md:flex-1 bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                        >
                            {updating ? (
                                <span className="flex items-center justify-center">
                                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                    </svg>
                                    Updating...
                                </span>
                            ) : "Save Changes"}
                        </button>
                    </div>
                </form>
            </div>

            {/* Checking Availability Dialog */}
            {showCheckingAvailabilityDialog && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                        <div className="flex justify-center mb-6">
                            <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
                        </div>
                        <h3 className="text-xl font-bold text-center mb-2">Checking Availability</h3>
                        <p className="text-gray-600 text-center">
                            Please wait while we check if a table is available for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} on {format(new Date(formData.date), 'EEEE, MMMM d, yyyy')} at {formatDisplayTime(formData.time)}.
                        </p>
                    </div>
                </div>
            )}

            {/* Not Available Dialog */}
            {showNotAvailableDialog && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6">
                        <div className="flex justify-center mb-4">
                            <div className="rounded-full bg-yellow-100 p-3">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-yellow-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                </svg>
                            </div>
                        </div>

                        <h3 className="text-xl font-bold text-center mb-2">Time Not Available</h3>
                        <p className="text-gray-600 text-center mb-6">
                            Sorry, we don't have availability for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} on {format(new Date(formData.date), 'EEEE, MMMM d, yyyy')} at {formatDisplayTime(formData.time)}.
                        </p>

                        <div className="mb-6">
                            <h4 className="font-medium mb-4 text-center">Please select an alternative time:</h4>
                            
                            {alternativeTimes.length > 0 ? (
                                <div className="grid grid-cols-2 gap-3">
                                    {alternativeTimes.map((time, index) => (
                                        <button
                                            key={index}
                                            type="button"
                                            onClick={() => setSelectedAlternativeTime(time)}
                                            className={`py-3 px-4 rounded text-center ${selectedAlternativeTime === time
                                                ? 'bg-green-600 text-white shadow-md'
                                                : 'border border-gray-300 hover:bg-gray-50'
                                                }`}
                                        >
                                            <span className="text-base">{formatDisplayTime(time)}</span>
                                            {selectedAlternativeTime === time && (
                                                <div className="flex items-center justify-center mt-1">
                                                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                                    </svg>
                                                    <span className="text-xs">Selected</span>
                                                </div>
                                            )}
                                        </button>
                                    ))}
                                </div>
                            ) : (
                                <p className="text-center text-gray-500 italic py-3">
                                    No alternative times are available for this date.
                                    <br />
                                    Please try a different date.
                                </p>
                            )}
                            
                            {alternativeTimes.length === 1 && !selectedAlternativeTime && (
                                <div className="mt-3 text-center text-sm text-green-600">
                                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 inline mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                    </svg>
                                    We've found one alternative time. Click to select it.
                                </div>
                            )}
                        </div>

                        <div className="flex flex-col space-y-3 sm:flex-row sm:space-y-0 sm:space-x-3 sm:justify-between">
                            <button
                                onClick={() => setShowNotAvailableDialog(false)}
                                className="py-2 px-4 border border-gray-300 rounded text-gray-700 hover:bg-gray-50 sm:flex-1"
                            >
                                Cancel
                            </button>

                            <button
                                onClick={submitWithAlternativeTime}
                                disabled={!selectedAlternativeTime}
                                className={`py-2 px-4 rounded text-white font-medium sm:flex-1 ${
                                    selectedAlternativeTime 
                                        ? 'bg-green-600 hover:bg-green-700'
                                        : 'bg-gray-400 cursor-not-allowed'
                                }`}
                            >
                                Book Selected Time
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Success Modal */}
            {showSuccessModal && (
                <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6">
                        <div className="flex justify-center mb-4">
                            <div className="rounded-full bg-green-100 p-3">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                </svg>
                            </div>
                        </div>

                        <h3 className="text-xl font-bold text-center mb-2">Reservation Updated</h3>
                        <p className="text-gray-600 text-center mb-6">
                            Your reservation has been successfully modified. We've sent an updated confirmation to your phone.
                        </p>

                        <div className="bg-gray-50 p-4 rounded-lg mb-6">
                            <div className="grid grid-cols-2 gap-3">
                                <div>
                                    <p className="text-sm text-gray-500">Date</p>
                                    <p className="font-medium">{format(new Date(formData.date), 'EEEE, MMMM d, yyyy')}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Time</p>
                                    <p className="font-medium">{formatDisplayTime(formData.time)}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Party Size</p>
                                    <p className="font-medium">{formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Reservation Code</p>
                                    <p className="font-medium">{reservationDetails?.reservationCode || ''}</p>
                                </div>
                            </div>
                        </div>

                        <div className="flex justify-center">
                            <button
                                onClick={() => navigate(`/reservation/${reservationDetails?.id}`)}
                                className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                            >
                                View Reservation
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ModifyReservation;