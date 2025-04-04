import React from "react";
import { useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, isBefore } from "date-fns";

const ReservationList = () => {
    const navigate = useNavigate();
    const { userReservations, loading } = useReservation();

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
        if (reservation.status === "Cancelled") {
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
        : sampleReservations;

    if (loading) {
        return (
            <div className="flex justify-center items-center min-h-screen p-4">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto p-4">
            <h1 className="text-2xl font-bold mb-8 text-center">My Reservations</h1>

            <div className="bg-white rounded-lg shadow-md overflow-hidden">
                <div className="p-4 border-b">
                    <h2 className="text-lg font-medium">Reservations for {reservationsToDisplay[0]?.customerPhone}</h2>
                </div>

                {reservationsToDisplay.length > 0 ? (
                    <div className="divide-y divide-gray-200">
                        {reservationsToDisplay.map((reservation) => {
                            const status = getReservationStatus(reservation);

                            return (
                                <div
                                    key={reservation.id}
                                    className="p-4 hover:bg-gray-50 cursor-pointer"
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
                                        <span className="text-sm text-gray-500">{reservation.reservationCode}</span>
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