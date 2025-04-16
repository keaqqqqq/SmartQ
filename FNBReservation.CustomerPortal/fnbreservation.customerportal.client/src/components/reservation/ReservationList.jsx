import React, { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, isBefore } from "date-fns";

const ReservationList = () => {
    const navigate = useNavigate();
    const { phone } = useParams(); // Get phone from URL
    const [isLoading, setIsLoading] = useState(true);
    const { userReservations, loading, getReservationsByPhone } = useReservation();
    const [error, setError] = useState(null);
    const [phoneNumber, setPhoneNumber] = useState(phone || "");

    // Fetch reservations when component mounts or phone changes
    useEffect(() => {
        const fetchReservations = async () => {
            setIsLoading(true);
            try {
                if (phone) {
                    // Decode phone number from URL
                    const decodedPhone = decodeURIComponent(phone);
                    setPhoneNumber(decodedPhone);
                    console.log(`Fetching reservations for phone: ${decodedPhone}`);
                    
                    const response = await getReservationsByPhone(decodedPhone);
                    
                    // Log the data for debugging
                    console.log("Fetch reservations result:", response);
                    console.log("Current user reservations:", userReservations);
                }
            } catch (err) {
                setError("Failed to load reservations. Please try again.");
                console.error("Error loading reservations:", err);
            } finally {
                setIsLoading(false);
            }
        };

        fetchReservations();
    }, [phone, getReservationsByPhone]);

    // Handle phone search form submission
    const handlePhoneSearch = async (e) => {
        e.preventDefault();
        if (!phoneNumber.trim()) return;

        setIsLoading(true);
        setError(null);
        
        try {
            console.log(`Searching reservations for phone: ${phoneNumber}`);
            const response = await getReservationsByPhone(phoneNumber);
            
            // Log the response for debugging
            console.log("Phone search result:", response);
            console.log("User reservations after search:", userReservations);
            
            // Update URL with the phone number
            navigate(`/reservations/${encodeURIComponent(phoneNumber)}`);
        } catch (err) {
            setError("Failed to find reservations. Please try again.");
            console.error("Error searching reservations:", err);
        } finally {
            setIsLoading(false);
        }
    };

    // Format date for display
    const formatReservationDate = (dateString) => {
        try {
            const date = parseISO(dateString);
            return format(date, 'MMM d, yyyy h:mm a');
        } catch (error) {
            console.error('Date formatting error:', error);
            return dateString;
        }
    };

    // Determine if a reservation is upcoming, past, or canceled
    const getReservationStatus = (reservation) => {
        // Check for both spellings: "Cancelled" and "Canceled"
        if (reservation.status === "Cancelled" || reservation.status === "Canceled") {
            return "cancelled";
        }

        try {
            const reservationDate = parseISO(reservation.reservationDate);
            if (isBefore(reservationDate, new Date())) {
                return "past";
            }
            return "upcoming";
        } catch (error) {
            console.error('Date comparison error:', error);
            return "upcoming";
        }
    };

    // Navigate to reservation details
    const viewReservation = (reservationCode) => {
        navigate(`/reservation/code/${reservationCode}`);
    };

    // Sample data for demonstration - use this when no data is available from API
    const sampleReservations = [
        {
            id: "1234",
            reservationCode: "RES9299",
            outletId: "7add93fc-8aba-4e37-ac87-24328e00372f",
            outletName: "Main Branch",
            customerName: "Chu Kea Qiu",
            customerPhone: "+6011-59655960",
            customerEmail: "keaqiuynwa@gmail.com",
            partySize: 2,
            reservationDate: "2025-04-11T15:00:00Z",
            status: "Confirmed",
            specialRequests: ""
        },
        {
            id: "2345",
            reservationCode: "RES8765",
            outletId: "7add93fc-8aba-4e37-ac87-24328e00372f",
            outletName: "Main Branch",
            customerName: "Chu Kea Qiu",
            customerPhone: "+6011-59655960",
            customerEmail: "keaqiuynwa@gmail.com",
            partySize: 4,
            reservationDate: "2025-03-20T18:30:00Z",
            status: "Completed",
            specialRequests: "Window seat please"
        },
        {
            id: "3456",
            reservationCode: "RES7654",
            outletId: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5",
            outletName: "Downtown Location",
            customerName: "Chu Kea Qiu",
            customerPhone: "+6011-59655960",
            customerEmail: "keaqiuynwa@gmail.com",
            partySize: 2,
            reservationDate: "2025-04-15T19:00:00Z",
            status: "Confirmed",
            specialRequests: "Anniversary celebration"
        }
    ];

    // Get reservations to display (use sample data if no userReservations)
    const reservationsToDisplay = userReservations && userReservations.length > 0
        ? userReservations
        : phone ? [] : sampleReservations; // Only show sample data if not searching
    
    // Sort reservations: upcoming first, then canceled, then past
    const sortedReservations = [...reservationsToDisplay].sort((a, b) => {
        const statusA = getReservationStatus(a);
        const statusB = getReservationStatus(b);
        
        // First sort by status priority: upcoming > canceled > past
        if (statusA !== statusB) {
            if (statusA === "upcoming") return -1;
            if (statusB === "upcoming") return 1;
            if (statusA === "cancelled") return -1;
            if (statusB === "cancelled") return 1;
        }
        
        // For same status, sort by date (newest first)
        try {
            const dateA = parseISO(a.reservationDate);
            const dateB = parseISO(b.reservationDate);
            return dateA > dateB ? -1 : 1;
        } catch (error) {
            return 0;
        }
    });

    if (loading || isLoading) {
        return (
            <div className="flex justify-center items-center min-h-screen p-4">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto p-4">
            <h1 className="text-2xl font-bold mb-8 text-center">My Reservations</h1>

            {/* Phone search form */}
            <div className="mb-8 bg-white rounded-lg shadow-md p-4">
                <form onSubmit={handlePhoneSearch} className="flex flex-col md:flex-row gap-3">
                    <div className="flex-grow">
                        <label htmlFor="phoneSearch" className="sr-only">Phone Number</label>
                        <div className="relative">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                                </svg>
                            </div>
                            <input
                                type="tel"
                                id="phoneSearch"
                                value={phoneNumber}
                                onChange={(e) => setPhoneNumber(e.target.value)}
                                placeholder="Enter phone number"
                                className="pl-10 py-3 block w-full rounded-md border border-gray-300 focus:ring-green-500 focus:border-green-500"
                            />
                        </div>
                    </div>
                    <button
                        type="submit"
                        className="bg-green-600 hover:bg-green-700 text-white font-medium py-3 px-6 rounded"
                    >
                        Search
                    </button>
                </form>
                {error && (
                    <div className="mt-4 bg-red-50 border-l-4 border-red-500 p-4 text-red-700">
                        {error}
                    </div>
                )}
            </div>

            <div className="bg-white rounded-lg shadow-md overflow-hidden">
                {phone && (
                    <div className="p-4 border-b">
                        <h2 className="text-lg font-medium">
                            {sortedReservations.length > 0 
                                ? `Reservations for ${phoneNumber} (${sortedReservations.length} found)`
                                : `No reservations found for ${phoneNumber}`
                            }
                        </h2>
                    </div>
                )}

             

                {sortedReservations.length > 0 ? (
                    <div className="divide-y divide-gray-200">
                        {sortedReservations.map((reservation) => {
                            const status = getReservationStatus(reservation);

                            return (
                                <div
                                    key={reservation.id}
                                    className={`p-4 hover:bg-gray-50 cursor-pointer ${
                                        reservation.status === "Canceled" || reservation.status === "Cancelled" 
                                            ? "border-l-4 border-red-300 bg-red-50 hover:bg-red-100" 
                                            : ""
                                    }`}
                                    onClick={() => viewReservation(reservation.reservationCode)}
                                >
                                    <div className="flex items-center justify-between mb-2">
                                        <span className={`px-2 py-1 text-xs font-semibold rounded-full ${status === "upcoming"
                                                ? "bg-green-100 text-green-800"
                                                : status === "past"
                                                    ? "bg-gray-100 text-gray-800"
                                                    : "bg-red-100 text-red-800"
                                            }`}>
                                            {status === "upcoming"
                                                ? "Upcoming"
                                                : status === "past"
                                                    ? "Completed"
                                                    : "Cancelled"}
                                        </span>
                                        <span className="text-sm text-gray-500">
                                            {reservation.reservationCode}
                                            {reservation.status === "Canceled" && 
                                                <span className="ml-2 text-xs text-red-500">(Canceled)</span>
                                            }
                                        </span>
                                    </div>

                                    <div className="flex flex-col sm:flex-row sm:justify-between">
                                        <div>
                                            <h3 className="text-base font-medium text-gray-800">
                                                {formatReservationDate(reservation.reservationDate)}
                                            </h3>
                                            <p className="text-sm text-gray-600">{reservation.outletName}</p>
                                        </div>

                                        <div className="flex items-center mt-2 sm:mt-0">
                                            <div className="text-sm text-gray-700">
                                                {reservation.partySize} {reservation.partySize === 1 ? 'person' : 'people'}
                                            </div>
                                            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 ml-2 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                            </svg>
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                ) : (
                    <div className="py-8 px-4 text-center">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-12 w-12 text-gray-400 mx-auto mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                        </svg>
                        <h3 className="text-lg font-medium text-gray-900 mb-1">No Reservations Found</h3>
                        <p className="text-gray-500 mb-6">We couldn't find any reservations associated with your phone number.</p>
                        <button
                            onClick={() => navigate('/reservation/new')}
                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Make a Reservation
                        </button>
                    </div>
                )}
            </div>

            <div className="mt-6 text-center">
                <button
                    onClick={() => navigate('/reservation/new')}
                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                >
                    Make New Reservation
                </button>
            </div>
        </div>
    );
};

export default ReservationList;