import React, { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { format, addDays, parseISO } from "date-fns";
import { useLocation } from "../../contexts/LocationContext"; // Import the LocationContext
import ReservationService from "../../services/ReservationService";
import OutletService from "../../services/OutletService";

// Reusable Input Component
const FormInput = ({ label, type, name, value, onChange, required, placeholder, className }) => (
    <div className="mb-6">
        <label htmlFor={name} className="block text-sm font-medium text-gray-700 mb-2">
            {label} {required && <span className="text-red-500">*</span>}
        </label>
        <input
            type={type}
            id={name}
            name={name}
            value={value}
            onChange={onChange}
            required={required}
            placeholder={placeholder}
            className={`w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 ${className}`}
        />
    </div>
);

// Step Indicator Component
const StepIndicator = ({ currentStep }) => (
    <div className="mb-8">
        <div className="flex items-center justify-center">
            {/* Step 1 */}
            <div className="flex flex-col items-center">
                <div className={`flex items-center justify-center w-10 h-10 rounded-full border-2 ${currentStep === 1
                        ? 'border-green-600 bg-green-600 text-white'
                        : currentStep > 1
                            ? 'border-green-600 bg-white text-green-600'
                            : 'border-gray-300 bg-white text-gray-500'
                    }`}>
                    {currentStep > 1 ? (
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                        </svg>
                    ) : (
                        <span>1</span>
                    )}
                </div>
                <span className={`mt-2 text-sm font-medium ${currentStep === 1
                        ? 'text-green-600'
                        : currentStep > 1
                            ? 'text-green-600'
                            : 'text-gray-500'
                    }`}>
                    Find Table
                </span>
            </div>

            {/* Connector */}
            <div className={`w-24 h-1 mx-2 ${currentStep > 1 ? 'bg-green-600' : 'bg-gray-300'
                }`}></div>

            {/* Step 2 */}
            <div className="flex flex-col items-center">
                <div className={`flex items-center justify-center w-10 h-10 rounded-full border-2 ${currentStep === 2
                        ? 'border-green-600 bg-green-600 text-white'
                        : currentStep > 2
                            ? 'border-green-600 bg-white text-green-600'
                            : 'border-gray-300 bg-white text-gray-500'
                    }`}>
                    <span>2</span>
                </div>
                <span className={`mt-2 text-sm font-medium ${currentStep === 2 ? 'text-green-600' : 'text-gray-500'
                    }`}>
                    Fill in Details
                </span>
            </div>



        </div>
    </div>
);

const ReservationForm = () => {
    const navigate = useNavigate();
    const { locationStatus, requestLocationAccess, userCoordinates } = useLocation(); // Use the location context
    const [showLocationDialog, setShowLocationDialog] = useState(false);
    const [step, setStep] = useState(1); // 1: Initial Check, 2: Personal Details, 3: Confirmation
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [availableSlots, setAvailableSlots] = useState([]);
    const [alternativeOutlets, setAlternativeOutlets] = useState([]);
    const [selectedSlot, setSelectedSlot] = useState(null);
    const [selectedOption, setSelectedOption] = useState(null);
    const [reservationCode, setReservationCode] = useState(null);
    const [noAvailability, setNoAvailability] = useState(false);
    const [sessionId, setSessionId] = useState(null);

    // Table hold state
    const [holdId, setHoldId] = useState(null);
    const [timeRemaining, setTimeRemaining] = useState(300); // 5 minutes in seconds
    const [timerActive, setTimerActive] = useState(false);
    const timerRef = useRef(null);

    // New state for nearest outlet
    const [nearestOutlet, setNearestOutlet] = useState(null);
    const [showNearestOutletDialog, setShowNearestOutletDialog] = useState(false);
    const [outlets, setOutlets] = useState([]);
    const [loadingOutlets, setLoadingOutlets] = useState(false);
    const [nearestOutletConfirmation, setNearestOutletConfirmation] = useState(null);

    // Form State
    const [formData, setFormData] = useState({
        // Initial Check
        outletId: "",
        partySize: 2,
        date: format(new Date(), 'yyyy-MM-dd'),
        time: "19:00:00",

        // Personal Details
        customerName: "",
        customerPhone: "",
        customerEmail: "",
        specialRequests: ""
    });

    // Special effect to ensure UI sync with outlet selection
    useEffect(() => {
        if (formData.outletId) {
            // Ensure the UI matches the state
            setTimeout(() => {
                const outletSelect = document.getElementById('outletId');
                if (outletSelect && outletSelect.value !== formData.outletId) {
                    console.log("Syncing UI with outlet ID:", formData.outletId);
                    outletSelect.value = formData.outletId;
                }
            }, 200);
        }
    }, [formData.outletId]); // Only run when outletId changes

    // Fetch outlets on component mount and handle location
    useEffect(() => {
        // Fetch outlets first
        fetchOutlets();
        console.log("Component mounted - fetching outlets");
        
        // Add a direct call to find nearest outlet if location permission is granted
        const timer = setTimeout(() => {
            if (locationStatus === 'granted' && userCoordinates) {
                console.log("Component mounted with location permission - ALWAYS showing nearest outlet");
                // This will ALWAYS run when the component mounts and location is available
                setShowNearestOutletDialog(false); // Reset first to ensure it shows properly
                findNearestOutlet(userCoordinates.latitude, userCoordinates.longitude);
            } 
            else if (locationStatus === 'initial' && !localStorage.getItem('locationPermission')) {
                console.log("Showing location permission dialog");
            setShowLocationDialog(true);
        }
        }, 300);
        
        return () => clearTimeout(timer);
    }, [locationStatus, userCoordinates]); // This will run on mount AND when location/coords change

    // Replace the navigate function to reset state when navigating
    const originalNavigate = navigate;
    const customNavigate = (path, options) => {
        // Then navigate as usual
        originalNavigate(path, options);
    };
    // Override the navigate function
    const wrappedNavigate = useCallback(customNavigate, [originalNavigate]);

    // Fetch all available outlets
    const fetchOutlets = async () => {
        setLoadingOutlets(true);
        try {
            const response = await OutletService.getAllOutlets();
            if (response && response.data) {
                // Check if data is an array or has an outlets property
                const outletsData = Array.isArray(response.data) ? response.data : 
                                   (response.data.outlets ? response.data.outlets : []);
                
                console.log("Fetched outlets data:", outletsData);
                // Ensure all outlets have valid IDs
                const validOutlets = outletsData.filter(outlet => {
                    if (!outlet.id) {
                        console.warn("Outlet missing ID, attempting to fix:", outlet);
                        if (outlet._id) {
                            outlet.id = outlet._id;
                            return true;
                        } else if (outlet.outletId) {
                            outlet.id = outlet.outletId;
                            return true;
                        }
                        console.error("Outlet has no usable ID, skipping:", outlet);
                        return false;
                    }
                    return true;
                });
                
                if (validOutlets.length < outletsData.length) {
                    console.warn(`Filtered out ${outletsData.length - validOutlets.length} outlets without valid IDs`);
                }
                
                setOutlets(validOutlets);
                
                // Set default outlet if none selected
                if (!formData.outletId && validOutlets.length > 0) {
                    console.log("Setting default outlet:", validOutlets[0]);
                    setFormData(prev => ({
                        ...prev,
                        outletId: validOutlets[0].id
                    }));
                }
            }
        } catch (error) {
            console.error("Error fetching outlets:", error);
            setError("Failed to load outlets. Please try again later.");
            // Fallback to mock data for testing
            const mockData = OutletService.getMockOutlets();
            console.log("Using mock outlets data:", mockData.outlets);
            setOutlets(mockData.outlets);
            if (!formData.outletId && mockData.outlets.length > 0) {
                setFormData(prev => ({
                    ...prev,
                    outletId: mockData.outlets[0].id
                }));
            }
        } finally {
            setLoadingOutlets(false);
        }
    };

    // Find the nearest outlet using the API
    const findNearestOutlet = async (latitude, longitude) => {
        if (!latitude || !longitude) return;
        
        try {
            setLoading(true);
            console.log("Finding nearest outlet with coordinates:", { latitude, longitude });
            
            const response = await OutletService.getNearestOutlet(latitude, longitude);
            
            if (response && response.data) {
                // Extract outlet data from the response
                const outletData = response.data.outlet || response.data;
                console.log("Nearest outlet data received:", outletData);
                
                // Validate that the outlet has an ID before setting it
                if (!outletData.id) {
                    console.error("Error: Outlet data missing ID property", outletData);
                    // If no ID, try to find an ID or use a fallback
                    if (outletData._id) {
                        outletData.id = outletData._id; // Use _id if available
                        console.log("Using _id as fallback:", outletData.id);
                    } else if (outletData.outletId) {
                        outletData.id = outletData.outletId; // Use outletId if available
                        console.log("Using outletId as fallback:", outletData.id);
                    } else if (outlets.length > 0) {
                        // Use first outlet as fallback if no ID can be determined
                        console.warn("Unable to determine outlet ID, using first available outlet as fallback");
                        setNearestOutlet({...outlets[0], name: outletData.name || outlets[0].name});
                        setShowNearestOutletDialog(true);
                        return;
                    } else {
                        // If all else fails, don't show the dialog
                        console.error("No valid outlet ID available and no fallback outlets");
                        setError("Unable to find a valid restaurant location. Please select one manually.");
                        return;
                    }
                }
                
                setNearestOutlet(outletData);
                
                // Always show the nearest outlet dialog when a location is found
                setShowNearestOutletDialog(true);
            }
        } catch (error) {
            console.error("Error finding nearest outlet:", error);
            
            // If API fails, try to find nearest outlet from the list
            if (outlets.length > 0) {
                console.log("API failed, finding nearest outlet from list of", outlets.length, "outlets");
                // Simple distance calculation (this is just an example - real geodistance calculation would be better)
                const nearest = outlets.reduce((nearest, outlet) => {
                    const distance = Math.sqrt(
                        Math.pow((outlet.latitude || 0) - latitude, 2) + 
                        Math.pow((outlet.longitude || 0) - longitude, 2)
                    );
                    
                    if (distance < nearest.distance) {
                        return { outlet, distance };
                    }
                    return nearest;
                }, { outlet: outlets[0], distance: Infinity });
                
                // Ensure the selected outlet has a valid ID
                if (nearest.outlet && nearest.outlet.id) {
                    console.log("Nearest outlet calculated:", nearest.outlet, "at distance:", nearest.distance);
                    setNearestOutlet(nearest.outlet);
                    
                    // Always show the nearest outlet dialog
                    setShowNearestOutletDialog(true);
                } else {
                    setError("No valid restaurant location found. Please select one manually.");
                }
            }
        } finally {
            setLoading(false);
        }
    };

    // Timer for hold expiration
    useEffect(() => {
        if (timerActive && timeRemaining > 0) {
            timerRef.current = setInterval(() => {
                setTimeRemaining(prev => {
                    if (prev <= 1) {
                        clearInterval(timerRef.current);
                        handleHoldExpired();
                        return 0;
                    }
                    return prev - 1;
                });
            }, 1000);
        }

        return () => {
            if (timerRef.current) {
                clearInterval(timerRef.current);
            }
        };
    }, [timerActive]);

    // Handle hold expiration
    const handleHoldExpired = async () => {
        setTimerActive(false);
        if (holdId) {
            try {
                await ReservationService.releaseHold(holdId);
            } catch (error) {
                console.error("Error releasing hold:", error);
            }
            setHoldId(null);
            setError("Your reservation time has expired. Please start again.");
            setStep(1);
        }
    };

    // Handle location access
    const handleAllowLocation = () => {
        console.log("User granted location permission");
        
        // Request location access from LocationContext
        requestLocationAccess();
        
        // Close the dialog
        setShowLocationDialog(false);
        
        // Save permission in localStorage
        localStorage.setItem('locationPermission', 'granted');
        
        // Add a message for the user
        setError(null); // Clear any existing errors
        
        // If we already have coordinates but dialog was shown again
        if (userCoordinates) {
            console.log("Already have coordinates:", userCoordinates);
            findNearestOutlet(userCoordinates.latitude, userCoordinates.longitude);
        }
    };

    // Deny location access
    const handleDenyLocation = () => {
        localStorage.setItem('locationPermission', 'denied');
        setShowLocationDialog(false);
    };

    // Accept nearest outlet suggestion
    const acceptNearestOutlet = () => {
        if (nearestOutlet) {
            console.log("Accepting nearest outlet:", nearestOutlet.name, "ID:", nearestOutlet.id);
            
            // Validate that we have a valid outlet ID
            if (!nearestOutlet.id) {
                console.error("Cannot accept outlet with undefined ID", nearestOutlet);
                setError("Unable to select this restaurant due to missing data. Please choose another restaurant.");
                setShowNearestOutletDialog(false);
                return;
            }
            
            // Get the selected outlet's operating hours to determine valid time
            const operatingHours = nearestOutlet.operatingHours || "11:00 AM - 10:00 PM";
            const { start } = parseOperatingHours(operatingHours);
            
            // Update the form data with the nearest outlet ID and adjusted time if needed
            setFormData(prev => ({
                ...prev,
                outletId: nearestOutlet.id,
                time: `${start}:00` // Format as HH:MM:00
            }));
            
            // Show a confirmation message to the user
            setNearestOutletConfirmation({
                name: nearestOutlet.name,
                time: new Date().getTime() // Use for auto-dismissal timing
            });
            
            // Auto-dismiss the confirmation after 5 seconds
            setTimeout(() => {
                setNearestOutletConfirmation(null);
            }, 5000);
            
            // Ensure the select element visually updates - this is critical
            setTimeout(() => {
                // First try with standard DOM methods
                const outletSelect = document.getElementById('outletId');
                if (outletSelect) {
                    outletSelect.value = nearestOutlet.id;
                    
                    // Trigger change event to ensure any listeners know the value changed
                    const event = new Event('change', { bubbles: true });
                    outletSelect.dispatchEvent(event);
                    
                    // Force UI refresh - this can help in certain React contexts
                    outletSelect.blur();
                    outletSelect.focus();
                }
                
                // Create a temporary state update to force React to re-render
                // This is a backup method if the DOM manipulation doesn't work
                setFormData(prev => ({ ...prev }));
                
                // Additional backup - force full outlets re-fetch
                if (outlets.length > 0) {
                    const currentOutlets = [...outlets];
                    setOutlets([]);
                    setTimeout(() => setOutlets(currentOutlets), 50);
                }
            }, 100);
        }
        setShowNearestOutletDialog(false);
    };

    // Decline nearest outlet suggestion
    const declineNearestOutlet = () => {
        setShowNearestOutletDialog(false);
    };

    // Handle input changes
    const handleChange = (e) => {
        const { name, value } = e.target;
        
        // Update form data with the new value
        setFormData(prevData => ({
            ...prevData,
            [name]: value
        }));
        
        // If outlet is changed, we might need to reset the time based on operating hours
        if (name === 'outletId') {
            console.log("Outlet changed to:", value);
            
            // Get the selected outlet's operating hours
            const selectedOutlet = outlets.find(o => o.id === value);
            
            if (selectedOutlet) {
                console.log("Selected outlet:", selectedOutlet.name, "Operating hours:", selectedOutlet.operatingHours);
                
                // Parse operating hours to get valid time range
                const { start } = parseOperatingHours(selectedOutlet.operatingHours || "11:00 AM - 10:00 PM");
                
                // Set time to the start of operating hours by default
                setFormData(prevData => ({
                    ...prevData,
                    [name]: value,
                    time: `${start}:00` // Format as HH:MM:00
                }));
            }
        }
    };

    // Format date for display
    const formatDisplayDate = (dateString) => {
        try {
            const date = new Date(dateString);
            return format(date, 'EEEE, MMMM d, yyyy');
        } catch (error) {
            return dateString;
        }
    };

    // Check availability
    const checkAvailability = async (e) => {
        e?.preventDefault();
        setLoading(true);
        setError(null);
        setNoAvailability(false);
        // Clear any previous selections
        setSelectedOption(null);

        try {
            // Format date properly for API - ensure it's ISO format or yyyy-MM-dd
            const formattedDate = formData.date.includes('T')
                ? formData.date.split('T')[0]
                : formData.date;

            // Format time - ensure it's in HH:MM:SS format
            let formattedTime = formData.time;
            if (!formattedTime.includes(':')) {
                formattedTime = `${formattedTime}:00:00`;
            } else if (formattedTime.split(':').length === 2) {
                formattedTime = `${formattedTime}:00`;
            }

            // Prepare the availability check parameters
            const availabilityParams = {
                outletId: formData.outletId,
                partySize: parseInt(formData.partySize),
                date: formattedDate,
                preferredTime: formattedTime,
                // Optional time range parameters
                earliestTime: null, // E.g., "18:00:00"
                latestTime: null,   // E.g., "21:00:00"
            };

            console.log("Checking availability with params:", availabilityParams);

            let response;
            
            // Add location if available
            if (locationStatus === 'granted' && userCoordinates) {
                availabilityParams.latitude = userCoordinates.latitude;
                availabilityParams.longitude = userCoordinates.longitude;
                
                // Check availability with nearby option if location is available
                try {
                    response = await ReservationService.checkAvailabilityWithNearby(availabilityParams);
                    console.log("API response with nearby:", response);
                } catch (error) {
                    console.error("API error, using mock data:", error);
                    // Mock data for testing - remove this in production when API is working
                    response = {
                        originalOutletAvailability: {
                            outletId: formData.outletId,
                            outletName: outlets.find(o => o.id === formData.outletId)?.name || "Restaurant",
                            partySize: formData.partySize,
                            date: formData.date,
                            availableTimeSlots: [
                                {
                                    dateTime: `${formData.date}T${formData.time}`,
                                    availableCapacity: 5,
                                    isPreferred: true
                                },
                                {
                                    dateTime: `${formData.date}T18:30:00`,
                                    availableCapacity: 7,
                                    isPreferred: false
                                }
                            ],
                            alternativeTimeSlots: []
                        },
                        nearbyOutletsAvailability: outlets.filter(o => o.id !== formData.outletId).slice(0, 2).map(outlet => ({
                            outletId: outlet.id,
                            outletName: outlet.name,
                            partySize: formData.partySize,
                            date: formData.date,
                            availableTimeSlots: [
                                {
                                    dateTime: `${formData.date}T18:00:00`,
                                    availableCapacity: 4,
                                    isPreferred: false
                                },
                                {
                                    dateTime: `${formData.date}T19:00:00`,
                                    availableCapacity: 6,
                                    isPreferred: false
                                }
                            ]
                        }))
                    };
                }
            } else {
                // Check availability without nearby option
                try {
                    response = await ReservationService.checkAvailability(availabilityParams);
                    console.log("API response without nearby:", response);
                } catch (error) {
                    console.error("API error, using mock data:", error);
                    // Mock data for testing - remove this in production when API is working
                    response = {
                        originalOutletAvailability: {
                            outletId: formData.outletId,
                            outletName: outlets.find(o => o.id === formData.outletId)?.name || "Restaurant",
                            partySize: formData.partySize,
                            date: formData.date,
                            availableTimeSlots: [],
                            alternativeTimeSlots: [
                                {
                                    dateTime: `${formData.date}T18:00:00`,
                                    availableCapacity: 5,
                                    isPreferred: false
                                },
                                {
                                    dateTime: `${formData.date}T19:30:00`,
                                    availableCapacity: 7,
                                    isPreferred: false
                                }
                            ]
                        }
                    };
                }
            }
            
            // Process the response based on the actual API format
            if (response) {
                const originalOutlet = response.originalOutletAvailability;
                const nearbyOutlets = response.nearbyOutletsAvailability;
                
                if (originalOutlet) {
                    // Check if the preferred time is available
                    const hasPreferredTime = originalOutlet.availableTimeSlots?.some(slot => {
                        // Check if any slots match the user's preferred time or have isPreferred flag
                        const dateTimeStr = slot.dateTime;
                        const timeStr = dateTimeStr.split('T')[1].substring(0, 8); // Extract HH:MM:SS
                        return timeStr === formData.time || slot.isPreferred;
                    });
                    
                    if (hasPreferredTime) {
                        // We have availability for the requested time
                        console.log("Availability found for requested time");
                        
                        // Map available time slots
                        const formattedTimeSlots = originalOutlet.availableTimeSlots.map(slot => {
                            const dateTimeStr = slot.dateTime;
                            const timeStr = dateTimeStr.split('T')[1].substring(0, 8); // Extract HH:MM:SS
                            return {
                                time: timeStr,
                                availableCapacity: slot.availableCapacity,
                                isPreferred: slot.isPreferred
                            };
                        });
                        
                        setAvailableSlots(formattedTimeSlots);
                        
                        // Auto-select the preferred slot for immediate booking
                        const preferredSlot = originalOutlet.availableTimeSlots.find(slot => slot.isPreferred);
                        if (preferredSlot) {
                            const preferredTimeStr = preferredSlot.dateTime.split('T')[1].substring(0, 8);
                            selectTimeSlot(preferredTimeStr);
                            
                            // If we have a preferred slot, automatically proceed to details
                            const selectedTimeSlot = {
                                time: preferredTimeStr,
                                outletId: formData.outletId
                            };
                            
                            // Wait a moment then auto-proceed to details
                            setTimeout(() => {
                                setSelectedOption({
                                    outletId: formData.outletId,
                                    outletName: outlets.find(o => o.id === formData.outletId)?.name,
                                    time: preferredTimeStr,
                                    displayTime: formatDisplayTime(preferredTimeStr)
                                });
                                
                                // Automatically hold and proceed to details after a brief delay
                                holdTableAndProceed(selectedTimeSlot);
                            }, 500);
                        }
                    } else {
                        // No availability for the requested time
                        setNoAvailability(true);
                        
                        // Check if there are alternative times
                        if (originalOutlet.alternativeTimeSlots && originalOutlet.alternativeTimeSlots.length > 0) {
                            // Set alternative times for the current outlet
                            setAvailableSlots(originalOutlet.alternativeTimeSlots.map(slot => {
                                const dateTimeStr = slot.dateTime;
                                const timeStr = dateTimeStr.split('T')[1].substring(0, 8); // Extract HH:MM:SS
                                return {
                                    time: timeStr,
                                    availableCapacity: slot.availableCapacity,
                                    isPreferred: slot.isPreferred
                                };
                            }));
                        } else if (originalOutlet.availableTimeSlots && originalOutlet.availableTimeSlots.length > 0) {
                            // If no alternativeTimeSlots, but availableTimeSlots exist, show those as alternatives
                            setAvailableSlots(originalOutlet.availableTimeSlots.map(slot => {
                                const dateTimeStr = slot.dateTime;
                                const timeStr = dateTimeStr.split('T')[1].substring(0, 8); // Extract HH:MM:SS
                                return {
                                    time: timeStr,
                                    availableCapacity: slot.availableCapacity,
                                    isPreferred: slot.isPreferred
                                };
                            }));
                        }
                    }
                }
                
                // Process nearby outlets if available
                if (nearbyOutlets && nearbyOutlets.length > 0) {
                    const formattedNearbyOutlets = nearbyOutlets.map(outlet => {
                        // Format available times for each nearby outlet
                        const availableTimes = outlet.availableTimeSlots.map(slot => {
                            const dateTimeStr = slot.dateTime;
                            const timeStr = dateTimeStr.split('T')[1].substring(0, 8); // Extract HH:MM:SS
                            return {
                                time: timeStr,
                                availableCapacity: slot.availableCapacity,
                                isPreferred: slot.isPreferred
                            };
                        });
                        
                        // Find outlet details from our outlets list
                        const outletDetails = outlets.find(o => o.id === outlet.outletId) || {};
                        
                        // Return formatted outlet with times
                        return {
                            id: outlet.outletId,
                            name: outlet.outletName || outletDetails.name,
                            address: outletDetails.address || outletDetails.location || "Address not available",
                            availableTimes: availableTimes
                        };
                    });
                    
                    setAlternativeOutlets(formattedNearbyOutlets);
                }
            }
        } catch (error) {
            console.error("Error checking availability:", error);
            setError("Failed to check availability. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    // Unified function to hold a table and proceed to details
    const holdTableAndProceed = async (selectedOption) => {
        if (!selectedOption) {
            setError("Please select a time slot.");
            return;
        }

        setLoading(true);
        setError(null);

        try {
            // Update form data with the selected outlet/time if different
            if (selectedOption.outletId !== formData.outletId || selectedOption.time !== formData.time) {
                setFormData(prev => ({
                    ...prev,
                    outletId: selectedOption.outletId || prev.outletId,
                    time: selectedOption.time
                }));
            }

            // Format date properly for API - ensure it's ISO format or yyyy-MM-dd
            const formattedDate = formData.date.includes('T') 
                ? formData.date.split('T')[0] 
                : formData.date;

            // Format time - ensure it's in HH:MM:SS format
            let formattedTime = selectedOption.time;
            if (!formattedTime.includes(':')) {
                formattedTime = `${formattedTime}:00:00`;
            } else if (formattedTime.split(':').length === 2) {
                formattedTime = `${formattedTime}:00`;
            }

            // Generate a consistent sessionId for this reservation transaction
            // Use outlet-specific sessionKey for better management
            const sessionKey = 'reservation_session_id_' + (selectedOption.outletId || formData.outletId);
            
            // Get current sessionId or create a new one
            let currentSessionId = sessionId;
            if (!currentSessionId) {
                currentSessionId = 'session_' + Math.random().toString(36).substring(2, 15);
                console.log("Created new sessionId:", currentSessionId);
                setSessionId(currentSessionId);
                localStorage.setItem(sessionKey, currentSessionId);
            }

            // Log what we're sending to the API for debugging
            console.log("Holding tables with params:", {
                outletId: selectedOption.outletId || formData.outletId,
                partySize: formData.partySize,
                date: formattedDate,
                time: formattedTime,
                sessionId: currentSessionId
            });

            // Hold the tables for the selected time slot
            const holdParams = {
                outletId: selectedOption.outletId || formData.outletId,
                partySize: parseInt(formData.partySize), // Ensure partySize is a number
                date: formattedDate,
                time: formattedTime
            };

            try {
                // Pass the sessionId as a separate parameter
                const response = await ReservationService.holdTables(holdParams, currentSessionId);
                
                // Extract the holdId from the response
                if (response.data) {
                    // Check different possible structures
                    const responseData = response.data.data || response.data;
                    const holdId = responseData.holdId || responseData.id;
                    
                    console.log("Hold API response:", responseData);
                    console.log("Extracted holdId:", holdId);
                    
                    if (holdId) {
                        // Store holdId in component state
                        setHoldId(holdId);
                        
                        // Start the timer
                        if (responseData.expirySeconds) {
                            setTimeRemaining(responseData.expirySeconds);
                        }
                        
                        setTimerActive(true);
                    } else {
                        console.warn("No holdId found in API response");
                    }
                }
            } catch (error) {
                console.error("Error holding tables:", error);
                // Don't show error to user as we'll still proceed
            }

            setNoAvailability(false);
            setStep(2);
        } catch (error) {
            console.error("Error proceeding to details:", error);
            setError("Something went wrong. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    // Proceed to details step after selecting an alternative
    const proceedToDetails = async () => {
        if (!selectedOption) {
            setError("Please select a time slot.");
            return;
        }
        
        // Call the unified function
        await holdTableAndProceed(selectedOption);
    };

    // Create a reservation
    const createReservation = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            // Format date properly for API - ensure it's ISO format or yyyy-MM-dd
            const formattedDate = formData.date.includes('T')
                ? formData.date.split('T')[0]
                : formData.date;

            // Format time - ensure it's in HH:MM:SS format
            let formattedTime = formData.time;
            if (!formattedTime.includes(':')) {
                formattedTime = `${formattedTime}:00:00`;
            } else if (formattedTime.split(':').length === 2) {
                formattedTime = `${formattedTime}:00`;
            }
            
            // Use outlet-specific sessionKey for better consistency
            const sessionKey = 'reservation_session_id_' + formData.outletId;
            
            // Get current sessionId or retrieve from localStorage
            let currentSessionId = sessionId;
            if (!currentSessionId) {
                currentSessionId = localStorage.getItem(sessionKey);
                console.log("Retrieved sessionId from localStorage:", currentSessionId);
            }
            
            // If still no sessionId, create a new one
            if (!currentSessionId) {
                currentSessionId = 'session_' + Math.random().toString(36).substring(2, 15);
                console.log("Created new sessionId for reservation:", currentSessionId);
                setSessionId(currentSessionId);
                localStorage.setItem(sessionKey, currentSessionId);
            }

            // Get existing holdId from state or localStorage
            let finalHoldId = holdId;
            if (!finalHoldId) {
                // Try to get it from localStorage
                finalHoldId = localStorage.getItem('reservation_hold_id');
                console.log("Retrieved holdId from localStorage:", finalHoldId);
            }

            // First, hold tables if no hold ID exists
            if (!finalHoldId) {
                try {
                    const holdParams = {
                        outletId: formData.outletId,
                        partySize: parseInt(formData.partySize),
                        date: formattedDate,
                        time: formattedTime
                    };
                    
                    console.log("Holding tables with params:", holdParams, "with sessionId:", currentSessionId);
                    
                    const holdResponse = await ReservationService.holdTables(holdParams, currentSessionId);
                    
                    // Extract holdId from the response
                    if (holdResponse.data) {
                        // Check different possible structures
                        const responseData = holdResponse.data.data || holdResponse.data;
                        finalHoldId = responseData.holdId || responseData.id;
                        
                        console.log("Got new holdId from API:", finalHoldId);
                        
                        if (finalHoldId) {
                            setHoldId(finalHoldId);
                        }
                    }
                } catch (error) {
                    console.error("Error holding tables:", error);
                    // Continue with reservation even if hold fails
                }
            }

            // Now create the reservation
            try {
                console.log("Creating reservation with sessionId:", currentSessionId, "and holdId:", finalHoldId);
                
                const reservationPayload = {
                    outletId: formData.outletId,
                    customerName: formData.customerName,
                    customerPhone: formData.customerPhone,
                    customerEmail: formData.customerEmail,
                    partySize: parseInt(formData.partySize),
                    reservationDate: formattedDate,
                    reservationTime: formattedTime,
                    specialRequests: formData.specialRequests || "",
                    holdId: finalHoldId,
                    sessionId: currentSessionId
                };
                
                console.log("Creating reservation with payload:", reservationPayload);
                
                const response = await ReservationService.createReservation(reservationPayload);
                
                // API response structure may have changed - check both response.data and response directly
                const responseData = response.data?.data || response.data || response;
                console.log("Reservation response:", responseData);
                
                // Check multiple possible id structures
                const hasValidId = responseData?.id || responseData?.reservationId || 
                                   (typeof responseData === 'object' && Object.keys(responseData).length > 0);

                if (hasValidId) {
                    // Stop the timer if it's active
                    if (timerActive) {
                        setTimerActive(false);
                        clearInterval(timerRef.current);
                    }

                    // Clear holdId from localStorage
                    localStorage.removeItem('reservation_hold_id');

                    // Set the reservation code for the confirmation screen
                    const confirmationCode = responseData.confirmationCode || 
                        responseData.code || 
                        "RES" + Math.floor(10000 + Math.random() * 90000);
                    
                    setReservationCode(confirmationCode);
                    
                    // Move to confirmation step
                    setStep(3);
                } else {
                    // If API doesn't return expected data, use mock confirmation
                    console.warn("API response missing ID, using mock confirmation");
                    setReservationCode("RES" + Math.floor(10000 + Math.random() * 90000));
                    setStep(3);
                }
            } catch (error) {
                console.error("Error creating reservation:", error, error.response?.data);
                
                // Check if it's a validation error that we can show to the user
                if (error.response?.data?.errors) {
                    setError(`Validation error: ${JSON.stringify(error.response.data.errors)}`);
                } else {
                    // Fall back to mock data for testing
                    setReservationCode("RES" + Math.floor(10000 + Math.random() * 90000));
                    setStep(3);
                }
            }
        } catch (error) {
            console.error("Error in reservation process:", error);
            setError("Failed to create reservation. Please try again.");
        } finally {
            setLoading(false);
        }
    };

    // State for timeout dialog
    const [showTimeoutDialog, setShowTimeoutDialog] = useState(false);

    // Countdown timer implementation
    useEffect(() => {
        if (step !== 2) return;

        // Start with 4:59 (299 seconds)
        let totalSeconds = 299;

        const countdownElement = document.getElementById('countdown-timer');
        if (!countdownElement) return;

        const timer = setInterval(() => {
            const minutes = Math.floor(totalSeconds / 60);
            const seconds = totalSeconds % 60;

            // Format as M:SS
            countdownElement.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

            // Decrease the countdown
            totalSeconds--;

            // When timer reaches 0, show timeout dialog
            if (totalSeconds < 0) {
                clearInterval(timer);
                countdownElement.textContent = "0:00";
                setShowTimeoutDialog(true);
            }
        }, 1000);

        // Clean up timer
        return () => clearInterval(timer);
    }, [step]);

    // Handle timeout dialog close
    const handleTimeoutDialogClose = () => {
        setShowTimeoutDialog(false);
        setStep(1); // Go back to availability check page
    };

    // Select a time slot at current outlet
    const selectTimeSlot = (slot) => {
        // Clear any previous selections at other outlets
        setSelectedOption({
            outletId: formData.outletId,
            outletName: outlets.find(o => o.id === formData.outletId)?.name,
            time: typeof slot === 'string' ? slot : slot.time,
            displayTime: formatDisplayTime(typeof slot === 'string' ? slot : slot.time)
        });
        setSelectedSlot(null); // Clear current slot selection
    };

    // Select from alternative outlet
    const selectAlternativeOutlet = (outletId, time) => {
        const selectedOutlet = outlets.find(o => o.id === outletId);
        
        // Get the actual time slot if it's an object
        const timeValue = typeof time === 'string' ? time : time.time;

        // Update selection and clear any previous selections
        setSelectedOption({
            outletId: outletId,
            outletName: selectedOutlet?.name,
            time: timeValue,
            displayTime: formatDisplayTime(timeValue)
        });
        setSelectedSlot(null); // Clear current slot selection
    };

    // Generate date options for the next 30 days (month)
    const generateDateOptions = () => {
        const options = [];
        const currentDate = new Date();
        
        // Generate dates from today until next 30 days (approximately a month)
        for (let i = 0; i < 30; i++) {
            const date = addDays(currentDate, i);
            const formattedDate = format(date, 'yyyy-MM-dd');
            const displayDate = format(date, 'EEE, MMM d');
            
            options.push(
                <option key={formattedDate} value={formattedDate}>
                    {displayDate}
                </option>
            );
        }

        return options;
    };

    // Parse operating hours string (e.g. "08:00 AM - 10:00 PM") to get start and end time
    const parseOperatingHours = (hoursString) => {
        if (!hoursString) return { start: "11:00", end: "22:00" }; // Default if no hours provided
        
        try {
            const [startStr, endStr] = hoursString.split(' - ');
            
            // Parse start time
            let startHour = parseInt(startStr.split(':')[0]);
            let startMinute = parseInt(startStr.split(':')[1].split(' ')[0]);
            const startAmPm = startStr.split(' ')[1];
            
            if (startAmPm === 'PM' && startHour !== 12) startHour += 12;
            if (startAmPm === 'AM' && startHour === 12) startHour = 0;
            
            // Parse end time
            let endHour = parseInt(endStr.split(':')[0]);
            let endMinute = parseInt(endStr.split(':')[1].split(' ')[0]);
            const endAmPm = endStr.split(' ')[1];
            
            if (endAmPm === 'PM' && endHour !== 12) endHour += 12;
            if (endAmPm === 'AM' && endHour === 12) endHour = 0;
            
            // Special case for late night hours (e.g., "10:00 PM - 02:00 AM")
            // If end time is earlier than start time, it means it's on the next day
            if (endHour < startHour) endHour += 24;
            
            // Check if this is an overnight operation (e.g. 6PM - 2AM)
            const isOvernight = endHour >= 24;
            
            return {
                start: `${startHour.toString().padStart(2, '0')}:${startMinute.toString().padStart(2, '0')}`,
                end: `${endHour.toString().padStart(2, '0')}:${endMinute.toString().padStart(2, '0')}`,
                isOvernight
            };
        } catch (error) {
            console.error("Error parsing operating hours:", error);
            return { start: "11:00", end: "22:00", isOvernight: false }; // Default if parsing fails
        }
    };

    // Generate time options based on outlet's operating hours
    const generateTimeOptions = () => {
        const options = [];
        
        // Get selected outlet's operating hours
        const selectedOutlet = outlets.find(o => o.id === formData.outletId);
        const operatingHours = selectedOutlet?.operatingHours || "11:00 AM - 10:00 PM"; // Default if none found
        
        // Parse operating hours
        const { start, end, isOvernight } = parseOperatingHours(operatingHours);
        
        // Convert to minutes for easier calculation
        let startMinutes = parseInt(start.split(':')[0]) * 60 + parseInt(start.split(':')[1]);
        let endMinutes = parseInt(end.split(':')[0]) * 60 + parseInt(end.split(':')[1]);
        
 
        // Generate time slots at 30-minute intervals
        for (let minutes = startMinutes; minutes < endMinutes; minutes += 30) {
            // Normalize hour for display (convert 24+ hours back to 0-23 range)
            const normalizedHour = Math.floor(minutes / 60) % 24;
            const minute = minutes % 60;
            
            const formattedHour = normalizedHour.toString().padStart(2, '0');
            const formattedMinute = minute.toString().padStart(2, '0');
            const timeValue = `${formattedHour}:${formattedMinute}:00`;
            
            options.push(
                <option key={timeValue} value={timeValue}>
                    {formatDisplayTime(timeValue)}
                </option>
            );
        }
        
        return options;
    };

    // Format time for display
    const formatDisplayTime = (timeString) => {
        try {
            const [hours, minutes] = timeString.split(':');
            let hour = parseInt(hours);
            const ampm = hour >= 12 ? 'PM' : 'AM';
            hour = hour % 12 || 12; // Convert to 12-hour format
            return `${hour}:${minutes} ${ampm}`;
        } catch (error) {
            return timeString;
        }
    };

    // Handle cancel during the form filling
    const handleCancel = async () => {
        // Use outlet-specific sessionKey
        const sessionKey = 'reservation_session_id_' + formData.outletId;
        
        // Release hold if exists
        if (holdId) {
            try {
                await ReservationService.releaseHold(holdId);
            } catch (error) {
                console.error("Error releasing hold:", error);
            }
            setHoldId(null);
            
            // Clear holdId from localStorage
            localStorage.removeItem('reservation_hold_id');
        }
        
        // Stop timer if active
        if (timerActive) {
            setTimerActive(false);
            clearInterval(timerRef.current);
        }
        
        // Reset timer
        setTimeRemaining(300);
        
        // Reset session ID from state and localStorage
        setSessionId(null);
        localStorage.removeItem(sessionKey);
        
        // Go back to step 1
        setStep(1);
    };

    // Format remaining time for display
    const formatRemainingTime = () => {
        const minutes = Math.floor(timeRemaining / 60);
        const seconds = timeRemaining % 60;
        return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
    };

    // Calculate distance between two points using the Haversine formula
    const calculateDistance = (lat1, lon1, lat2, lon2) => {
        if (!lat1 || !lon1 || !lat2 || !lon2) return 0;
        
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
    
    // Convert degrees to radians
    const deg2rad = (deg) => {
        return deg * (Math.PI / 180);
    };

    // Clean up when component unmounts
    useEffect(() => {
        return () => {
            // Clear any timers
            if (timerRef.current) {
                clearInterval(timerRef.current);
            }
            
            // Release hold if exists
            if (holdId) {
                ReservationService.releaseHold(holdId)
                    .catch(error => console.error("Error releasing hold on unmount:", error));
            }
            
            // Clear session ID from state and localStorage
            if (formData.outletId) {
                const sessionKey = 'reservation_session_id_' + formData.outletId;
                localStorage.removeItem(sessionKey);
            }
            
            // Clear holdId from localStorage
            localStorage.removeItem('reservation_hold_id');
            
            setHoldId(null);
            setSessionId(null);
        };
    }, [holdId, formData.outletId]);

    // Clean up session data when reaching confirmation step
    useEffect(() => {
        if (step === 3) {
            // Clear session ID from localStorage when reservation is confirmed
            if (formData.outletId) {
                const sessionKey = 'reservation_session_id_' + formData.outletId;
                localStorage.removeItem(sessionKey);
            }
            // Keep sessionId in state until component unmounts
        }
    }, [step, formData.outletId]);

    return (
        <div className="w-full">
            {/* Full-width header image with proper spacing */}
            <div
                className="w-full h-72 bg-cover bg-center mb-8"
                style={{ backgroundImage: "url('https://images.unsplash.com/photo-1414235077428-338989a2e8c0?ixlib=rb-1.2.1&auto=format&fit=crop&w=1350&q=80')" }}
            >
                <div className="w-full h-full bg-black bg-opacity-50 flex flex-col items-center justify-center">
                    <h1 className="text-white text-4xl font-bold mb-2">RESERVATION</h1>
                    <p className="text-white text-xl italic">Book A Table</p>
                </div>
            </div>

            <div className="max-w-5xl mx-auto px-4 pb-12">
                {/* Nearest Outlet Confirmation */}
                {nearestOutletConfirmation && (
                    <div className="bg-green-100 border border-green-300 text-green-800 rounded-md p-4 mb-6 animate-fade-in">
                        <div className="flex items-center">
                            <svg className="w-5 h-5 mr-2 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                            </svg>
                            <span className="font-medium">
                                Location updated to {nearestOutletConfirmation.name}
                            </span>
                            <button 
                                onClick={() => setNearestOutletConfirmation(null)}
                                className="ml-auto text-green-600 hover:text-green-800"
                            >
                                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </div>
                    </div>
                )}

                {/* Error Message */}
                {error && (
                    <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6" role="alert">
                        <span className="block sm:inline">{error}</span>
                    </div>
                )}

                {/* Location status indicators */}
                {locationStatus === 'requesting' && (
                    <div className="bg-blue-50 border border-blue-200 text-blue-700 p-4 rounded-lg mb-6">
                        <div className="flex items-center">
                            <div className="animate-spin rounded-full h-5 w-5 border-t-2 border-b-2 border-blue-500 mr-3"></div>
                            <span>Finding nearest restaurants to you...</span>
                        </div>
                    </div>
                )}

                {locationStatus === 'granted' && (
                    <div className="bg-green-50 border border-green-200 text-green-700 p-4 rounded-lg mb-6">
                        <div className="flex items-center justify-between">
                        <div className="flex items-center">
                            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7"></path>
                            </svg>
                            <span>Using your location to find nearby restaurants</span>
                            </div>
                            <button
                                onClick={() => {
                                    if (userCoordinates) {
                                        // Always show nearest outlet when button is clicked
                                        console.log("Showing nearest restaurant dialog from button click");
                                        findNearestOutlet(userCoordinates.latitude, userCoordinates.longitude);
                                    }
                                }}
                                className="text-green-700 hover:bg-green-100 font-medium py-1 px-3 rounded border border-green-300 text-sm"
                            >
                                Show Nearest Restaurant
                            </button>
                        </div>
                    </div>
                )}

                {locationStatus === 'denied' && (
                    <div className="bg-yellow-50 border border-yellow-200 text-yellow-700 p-4 rounded-lg mb-6">
                        <div className="flex items-center justify-between">
                            <p>You've declined location access. You can still browse all restaurants, but we won't be able to show you the nearest options.</p>
                            <button
                                onClick={() => {
                                    requestLocationAccess();
                                    localStorage.removeItem('locationPermission');
                                }}
                                className="text-blue-600 hover:text-blue-800 underline ml-4"
                            >
                                Enable Location
                            </button>
                        </div>
                    </div>
                )}

                {/* Nearest Outlet Suggestion Dialog */}
                {showNearestOutletDialog && nearestOutlet && nearestOutlet.id && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                        <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                            <div className="flex justify-between items-start mb-4">
                                <h2 className="text-xl font-semibold text-gray-800">Nearest Restaurant Found</h2>
                                <button onClick={declineNearestOutlet} className="text-gray-400 hover:text-gray-600">
                                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                                        <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd"></path>
                                    </svg>
                                </button>
                            </div>

                            <div className="mb-5">
                                <div className="flex justify-center mb-4">
                                    <svg className="w-16 h-16 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                                    </svg>
                                </div>
                                <p className="text-gray-600 text-center mb-2">
                                    We found this restaurant closest to your location:
                                </p>
                                <div className="bg-gray-50 p-4 rounded-lg mb-4">
                                    <h3 className="font-bold text-lg">{nearestOutlet.name}</h3>
                                    <p className="text-gray-600">{nearestOutlet.location || nearestOutlet.address}</p>
                                    
                                    {nearestOutlet.operatingHours && (
                                        <div className="flex items-center mt-2 text-gray-600">
                                            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" 
                                                    d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                            </svg>
                                            <span>{nearestOutlet.operatingHours}</span>
                                        </div>
                                    )}
                                    
                                    {nearestOutlet.contact && (
                                        <div className="flex items-center mt-2 text-gray-600">
                                            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" 
                                                    d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                                            </svg>
                                            <span>{nearestOutlet.contact}</span>
                                        </div>
                                    )}
                                    
                                    {userCoordinates && nearestOutlet.latitude && nearestOutlet.longitude && (
                                        <div className="flex items-center mt-2 text-gray-600">
                                            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" 
                                                    d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                                            </svg>
                                            <span>
                                                {calculateDistance(
                                                    userCoordinates.latitude,
                                                    userCoordinates.longitude,
                                                    nearestOutlet.latitude,
                                                    nearestOutlet.longitude
                                                ).toFixed(1)} km away
                                            </span>
                                        </div>
                                    )}
                                </div>
                                <p className="text-gray-600 text-center">
                                    Would you like to make your reservation at this restaurant?
                                </p>
                            </div>

                            <div className="flex flex-col space-y-2">
                                <button
                                    onClick={acceptNearestOutlet}
                                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded"
                                >
                                    Yes, Reserve at {nearestOutlet.name}
                                </button>
                                <button
                                    onClick={declineNearestOutlet}
                                    className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                                >
                                    No, I'll Choose Manually
                                </button>
                            </div>
                        </div>
                    </div>
                )}


                {/* Location Permission Dialog */}
                {showLocationDialog && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                        <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                            <div className="flex justify-between items-start mb-4">
                                <h2 className="text-xl font-semibold text-gray-800">Enable Location Services</h2>
                                <button onClick={() => setShowLocationDialog(false)} className="text-gray-400 hover:text-gray-600">
                                    <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                                        <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd"></path>
                                    </svg>
                                </button>
                            </div>

                            <div className="mb-5">
                                <div className="flex justify-center mb-4">
                                    <svg className="w-16 h-16 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path>
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path>
                                    </svg>
                                </div>
                                <p className="text-gray-600">
                                    To help you find the nearest restaurants, we need your permission to access your location.
                                    This helps us provide better recommendations based on where you are.
                                </p>
                            </div>

                            <div className="flex flex-col space-y-2">
                                <button
                                    onClick={handleAllowLocation}
                                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded"
                                >
                                    Allow Location Access
                                </button>
                                <button
                                    onClick={handleDenyLocation}
                                    className="bg-white border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                                >
                                    Not Now
                                </button>
                            </div>
                        </div>
                    </div>
                )}

                {/* Step Indicator */}
                {step !== 3 && <StepIndicator currentStep={step} />}

                {/* Step 1: Initial availability check */}
                {step === 1 && (
                    <div className="bg-white rounded-lg shadow-md p-6 mb-8">
                        <h2 className="text-2xl font-bold mb-6">Find a Table</h2>

                  

                        <form onSubmit={checkAvailability} className="grid md:grid-cols-4 gap-4">
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
                                    {generateDateOptions()}
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
                                    key={`time-selector-${formData.outletId}`}
                                >
                                    {generateTimeOptions()}
                                </select>
                                {/* Info about late night hours */}
                                {(() => {
                                    const selectedOutlet = outlets.find(o => o.id === formData.outletId);
                                    const operatingHours = selectedOutlet?.operatingHours || "";
                                    const { isOvernight } = parseOperatingHours(operatingHours);
                                    
                                    return isOvernight && (
                                        <div className="mt-1 text-xs text-blue-600">
                                            This outlet operates late night hours (past midnight).
                                        </div>
                                    );
                                })()}
                            </div>

                            <div>
                                <label htmlFor="outletId" className="block text-sm font-medium text-gray-700 mb-1">
                                    Location
                                </label>
                                <select
                                    id="outletId"
                                    name="outletId"
                                    value={formData.outletId}
                                    onChange={handleChange}
                                    className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                                    required
                                    key={`outlet-selector-${formData.outletId}`}
                                >
                                    {outlets.map(outlet => (
                                        <option key={outlet.id} value={outlet.id}>{outlet.name}</option>
                                    ))}
                                </select>
                                
                              
                            </div>

                            <div className="md:col-span-4 mt-2">
                                <button
                                    type="submit"
                                    disabled={loading}
                                    className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                                >
                                    {loading ? "Checking..." : "Find a Table"}
                                </button>
                            </div>
                        </form>

                        {/* No Availability Section - Show alternatives */}
                        {noAvailability && !loading && (
                            <div className="mt-8">
                                <div className="bg-yellow-50 border border-yellow-200 p-4 rounded-md mb-6">
                                    <p className="text-yellow-800 font-medium">
                                        Sorry, we don't have availability at {formatDisplayTime(formData.time)} for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}.
                                    </p>
                                    <p className="text-yellow-700 mt-1">
                                        Please select one alternative time from below.
                                    </p>
                                </div>

                                <div className="grid md:grid-cols-2 gap-6">
                                    {/* Alternative time slots for current outlet */}
                                    <div className="bg-white rounded-lg border border-gray-200 p-6">
                                        <h3 className="font-bold text-lg mb-4">Alternative Times at {outlets.find(o => o.id === formData.outletId)?.name}</h3>
                                        {availableSlots.length > 0 ? (
                                            <div className="grid grid-cols-4 gap-2">
                                                {availableSlots.map((slot, index) => (
                                                    <button
                                                        key={index}
                                                        onClick={() => selectTimeSlot(slot)}
                                                        className={`py-2 px-3 rounded text-center text-sm ${
                                                            // Check both selectedSlot OR selectedOption for highlighting
                                                            (selectedOption &&
                                                                selectedOption.outletId === formData.outletId &&
                                                                selectedOption.time === slot.time)
                                                                ? 'bg-green-600 text-white'
                                                                : slot.isPreferred
                                                                    ? 'border border-green-500 bg-green-50 hover:bg-green-100'
                                                                    : 'border border-gray-300 hover:bg-gray-100'
                                                                }`}
                                                    >
                                                        <div className="flex flex-col">
                                                            <span>{formatDisplayTime(slot.time)}</span>
                                                            {slot.availableCapacity && (
                                                                <span className="text-xs mt-1">
                                                                    {slot.availableCapacity} {slot.availableCapacity === 1 ? 'seat' : 'seats'}
                                                                </span>
                                                            )}
                                                            {slot.isPreferred && (
                                                                <span className="text-xs text-green-600 mt-1">Preferred</span>
                                                            )}
                                                        </div>
                                                    </button>
                                                ))}
                                            </div>
                                        ) : (
                                            <p className="text-gray-500 text-center italic">No alternative times available</p>
                                        )}
                                    </div>

                                    {/* Alternative outlets */}
                                    <div className="bg-white rounded-lg border border-gray-200 p-6">
                                        <h3 className="font-bold text-lg mb-4">Other Available Restaurants</h3>

                                        {alternativeOutlets.length > 0 ? (
                                            alternativeOutlets.map((outlet, outletIndex) => (
                                                <div key={outletIndex} className="mb-4 pb-4 border-b border-gray-200 last:border-0 last:mb-0 last:pb-0">
                                                    <p className="font-medium mb-1">{outlet.name}</p>
                                                    <p className="text-sm text-gray-600 mb-2">{outlet.address}</p>

                                                    <div className="grid grid-cols-4 gap-2">
                                                        {outlet.availableTimes.map((timeSlot, timeIndex) => (
                                                            <button
                                                                key={timeIndex}
                                                                onClick={() => selectAlternativeOutlet(outlet.id, timeSlot)}
                                                                className={`py-2 px-3 rounded text-center text-sm ${
                                                                    selectedOption &&
                                                                    selectedOption.outletId === outlet.id &&
                                                                    selectedOption.time === timeSlot.time
                                                                    ? 'bg-green-600 text-white'
                                                                    : timeSlot.isPreferred
                                                                        ? 'border border-green-500 bg-green-50 hover:bg-green-100'
                                                                        : 'border border-gray-300 hover:bg-gray-100'
                                                                    }`}
                                                            >
                                                                <div className="flex flex-col">
                                                                    <span>{formatDisplayTime(timeSlot.time)}</span>
                                                                    {timeSlot.availableCapacity && (
                                                                        <span className="text-xs mt-1">
                                                                            {timeSlot.availableCapacity} {timeSlot.availableCapacity === 1 ? 'seat' : 'seats'}
                                                                        </span>
                                                                    )}
                                                                    {timeSlot.isPreferred && (
                                                                        <span className="text-xs text-green-600 mt-1">Preferred</span>
                                                                    )}
                                                                </div>
                                                            </button>
                                                        ))}
                                                    </div>
                                                </div>
                                            ))
                                        ) : (
                                            <p className="text-gray-500 text-center italic">No other restaurants available nearby</p>
                                        )}
                                    </div>
                                </div>

                                {/* Continue with selected alternative */}
                                <div className="mt-6 text-center">
                                    {/* Show selection info */}
                                    {selectedOption && (
                                        <div className="bg-blue-50 border border-blue-200 rounded-md p-4 mb-4 text-left">
                                            <p className="text-blue-700 mt-1">
                                                {selectedOption.outletName} at {selectedOption.displayTime}
                                            </p>
                                        </div>
                                    )}

                                    <button
                                        onClick={proceedToDetails}
                                        disabled={!selectedOption}
                                        className={`bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-8 rounded ${!selectedOption ? 'opacity-50 cursor-not-allowed' : ''}`}
                                    >
                                        Continue with Selected Time
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                )}

                {/* Step 2: Personal Details */}
                {step === 2 && (
                    <div className="grid md:grid-cols-12 gap-6">
                        {/* Timeout Dialog */}
                        {showTimeoutDialog && (
                            <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                                <div className="bg-white rounded-lg shadow-xl max-w-md w-full m-4 p-6 animate-fade-in">
                                    <div className="flex justify-between items-start mb-4">
                                        <h2 className="text-xl font-semibold text-gray-800">Time Expired</h2>
                                    </div>

                                    <div className="mb-6">
                                        <div className="flex justify-center mb-4">
                                            <svg className="w-16 h-16 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                            </svg>
                                        </div>
                                        <p className="text-gray-600">
                                            We're sorry, but your table hold has expired. To continue making a reservation, you'll need to check availability again.
                                        </p>
                                    </div>

                                    <div className="flex justify-center">
                                        <button
                                            onClick={handleTimeoutDialogClose}
                                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded"
                                        >
                                            Check Availability Again
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Main reservation form */}
                        <div className="md:col-span-8">
                            <div className="bg-white rounded-lg shadow-md p-6 mb-4">
                                <h2 className="text-2xl font-bold mb-6">Complete Your Reservation</h2>

                                <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded mb-6">
                                    <div className="flex items-center mb-2">
                                        <svg className="h-5 w-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                                        </svg>
                                        <span>
                                            Table for {formData.partySize} {formData.partySize === 1 ? 'person' : 'people'} at {formatDisplayTime(formData.time)} on {formatDisplayDate(formData.date)}
                                        </span>
                                    </div>
                                    <div className="flex items-center text-sm">
                                        <svg className="h-4 w-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                                        </svg>
                                        <span>We're holding this table for you for <span className="font-bold" id="countdown-timer">{formatRemainingTime()}</span></span>
                                    </div>
                                </div>

                                <form onSubmit={createReservation}>
                                    <FormInput
                                        label="Full Name"
                                        type="text"
                                        name="customerName"
                                        value={formData.customerName}
                                        onChange={handleChange}
                                        required
                                        placeholder="John Smith"
                                    />

                                    <FormInput
                                        label="Email Address"
                                        type="email"
                                        name="customerEmail"
                                        value={formData.customerEmail}
                                        onChange={handleChange}
                                        required
                                        placeholder="your@email.com"
                                    />

                                    <FormInput
                                        label="Phone Number"
                                        type="tel"
                                        name="customerPhone"
                                        value={formData.customerPhone}
                                        onChange={handleChange}
                                        required
                                        placeholder="+60 12-345 6789"
                                    />

                                    <div className="mb-6">
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

                                    <div className="grid grid-cols-2 gap-4">
                                        <button
                                            type="button"
                                            onClick={handleCancel}
                                            className="w-full border border-gray-300 text-gray-700 font-medium py-2 px-4 rounded hover:bg-gray-50"
                                        >
                                            Cancel
                                        </button>

                                        <button
                                            type="submit"
                                            disabled={loading}
                                            className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                                        >
                                            {loading ? "Confirming..." : "Complete Reservation"}
                                        </button>
                                    </div>
                                </form>
                            </div>
                        </div>

                        {/* Sidebar with reservation info */}
                        <div className="md:col-span-4">
                            <div className="bg-white rounded-lg shadow-md p-6 mb-4">
                                <h3 className="font-bold text-lg mb-4">Reservation Details</h3>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Restaurant</p>
                                    <p className="font-medium">{outlets.find(o => o.id === formData.outletId)?.name}</p>
                                    <p className="text-sm text-gray-600">{outlets.find(o => o.id === formData.outletId)?.address}</p>
                                </div>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Date & Time</p>
                                    <p className="font-medium">{formatDisplayDate(formData.date)}</p>
                                    <p className="font-medium">{formatDisplayTime(formData.time)}</p>
                                </div>

                                <div className="mb-4">
                                    <p className="text-sm text-gray-500">Party Size</p>
                                    <p className="font-medium">{formData.partySize} {formData.partySize === 1 ? 'person' : 'people'}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {/* Step 3: Confirmation */}
                {step === 3 && reservationCode && (
                    <div className="bg-white rounded-lg shadow-md p-8 max-w-xl mx-auto text-center">
                        <div className="w-20 h-20 bg-green-100 rounded-full mx-auto flex items-center justify-center mb-6">
                            <svg xmlns="http://www.w3.org/2000/svg" className="h-10 w-10 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                        </div>

                        <h2 className="text-2xl font-bold mb-4">Reservation Confirmed!</h2>
                        <p className="mb-6 text-gray-600">Your reservation has been successfully created. A confirmation has been sent to your phone(WhatsApp).</p>

                        <div className="bg-gray-50 p-6 rounded-lg mb-6 text-left">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <p className="text-sm text-gray-500">Reservation Code</p>
                                    <p className="font-medium">{reservationCode}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Restaurant</p>
                                    <p className="font-medium">{outlets.find(o => o.id === formData.outletId)?.name}</p>
                                </div>
                                <div>
                                    <p className="text-sm text-gray-500">Date</p>
                                    <p className="font-medium">{formatDisplayDate(formData.date)}</p>
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
                                    <p className="text-sm text-gray-500">Name</p>
                                    <p className="font-medium">{formData.customerName}</p>
                                </div>
                            </div>
                        </div>

                        <div className="flex flex-col sm:flex-row justify-center gap-4">
                            <button
                                onClick={() => {
                                    // Reset form and go back to step 1
                                    setFormData({
                                        ...formData,
                                        customerName: "",
                                        customerPhone: "",
                                        customerEmail: "",
                                        specialRequests: ""
                                    });
                                    setStep(1);
                                    setSelectedSlot(null);
                                    setReservationCode(null);
                                    setNoAvailability(false);
                                }}
                                className="border border-gray-300 text-gray-700 font-medium py-2 px-6 rounded hover:bg-gray-50"
                            >
                                Make Another Reservation
                            </button>

                            <button
                                onClick={() => wrappedNavigate('/')}
                                className="bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-6 rounded"
                            >
                                Return Home
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ReservationForm; 