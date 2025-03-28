import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";
import { format, parseISO, isPast } from "date-fns";

const ReservationList = () => {
    const navigate = useNavigate();
    const {
        userReservations,
        loading,
        error,
        getReservationsByPhone,
        clearError
    } = useReservation();

    const [phone, setPhone] = useState('');
    const [submitted, setSubmitted] = useState(false);
    const [filter, setFilter] = useState('upcoming'); // 'upcoming', 'past', 'all'

    // Handle form submission
    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!phone) return;

        try {
            await getReservationsByPhone(phone);
            setSubmitted(true);
        } catch (err) {
            console.error('Failed to fetch reservations', err);
        }
    };

    // Cleanup on component unmount
    useEffect(() => {
        return () => {
            clearError();
        };
    }, [clearError]);

    // Filter reservations based on date
    const filteredReservations = () => {
        if (!userReservations.length) return [];

        return userReservations.filter(reservation => {
            try {
                const reservationDate = parseISO(reservation.reservationDate);
                const isPastReservation = isPast(reservationDate);

                if (filter === 'upcoming') return !isPastReservation;
                if (filter === 'past') return isPastReservation;
                return true; // 'all' filter
            } catch (error) {
                console.error('Date filtering error:', error);
                return true; // Include by default if date parsing fails
            }
        });
    };

    // Format date
    const formatReservationDate = (dateString) => {
        try {
            const date = parseISO(dateString);
            return format(date, 'MMMM d, yyyy h:mm a');
        } catch (error) {
            console.error('Date formatting error:', error);
            return dateString; // Return original if parsing fails
        }
    };

    return (
        <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-lg">
            <h1 className="text-2xl font-bold mb-6">My Reservations</h1>

            {/* Phone number form */}
            {!submitted && (
                <form onSubmit={handleSubmit} className="mb-6">
                    <div className="mb-4">
                        <label htmlFor="phone" className="block text-sm font-medium text-gray-700 mb-1">
                            Enter your phone number to view your reservations
                        </label>
                        <input
                            type="tel"
                            id="phone"
                            value={phone}
                            onChange={(e) => setPhone(e.target.value)}
                            placeholder="+60 12-345 6789"
                            className="w-full px-4 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                            required
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                    >
                        {loading ? "Searching..." : "Find My Reservations"}
                    </button>
                </form>
            )}

            {/* Results section */}
            {submitted && (
                <>
                    {/* Show phone number and search again button */}
                    <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6">
                        <div>
                            <p className="text-sm text-gray-500">Phone number</p>
                            <p className="font-medium">{phone}</p>
                        </div>
                        <button
                            onClick={() => {
                                setSubmitted(false);
                                setPhone('');
                            }}
                            className="text-blue-600 hover:text-blue-800 text-sm mt-2 md:mt-0"
                        >
                            Search different number
                        </button>
                    </div>

                    {/* Error message */}
                    {error && (
                        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4" role="alert">
                            <span className="block sm:inline">{error}</span>
                        </div>
                    )}

                    {/* Filter tabs */}
                    <div className="flex border-b mb-6">
                        <button
                            onClick={() => setFilter('upcoming')}
                            className={`px-4 py-2 font-medium ${filter === 'upcoming'
                                    ? 'border-b-2 border-green-500 text-green-600'
                                    : 'text-gray-500 hover:text-gray-700'
                                }`}
                        >
                            Upcoming
                        </button>
                        <button
                            onClick={() => setFilter('past')}
                            className={`px-4 py-2 font-medium ${filter === 'past'
                                    ? 'border-b-2 border-green-500 text-green-600'
                                    : 'text-gray-500 hover:text-gray-700'
                                }`}
                        >
                            Past
                        </button>
                        <button
                            onClick={() => setFilter('all')}
                            className={`px-4 py-2 font-medium ${filter === 'all'
                                    ? 'border-b-2 border-green-500 text-green-600'
                                    : 'text-gray-500 hover:text-gray-700'
                                }`}
                        >
                            All
                        </button>
                    </div>

                    {/* Reservations list */}
                    {loading ? (
                        <div className="flex justify-center items-center py-8">
                            <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-green-500"></div>
                        </div>
                    ) : userReservations.length === 0 ? (
                        <div className="text-center py-8">
                            <p className="mb-4">No reservations found for this phone number.</p>
                            <button
                                onClick={() => navigate('/reservation/new')}
                                className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                            >
                                Make a Reservation
                            </button>
                        </div>
                    ) : filteredReservations().length === 0 ? (
                        <div className="text-center py-8">
                            <p className="mb-4">No {filter} reservations found.</p>
                            {filter !== 'upcoming' && (
                                <button
                                    onClick={() => setFilter('upcoming')}
                                    className="bg-gray-200 hover:bg-gray-300 text-gray-800 font-medium py-2 px-6 rounded mr-3"
                                >
                                    View Upcoming
                                </button>
                            )}
                            <button
                                onClick={() => navigate('/reservation/new')}
                                className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                            >
                                Make a Reservation
                            </button>
                        </div>
                    ) : (
                        <>
                            <div className="space-y-4">
                                {filteredReservations().map((reservation) => (
                                    <Link
                                        key={reservation.id}
                                        to={`/reservation/${reservation.id}`}
                                        className="block border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                                    >
                                        <div className="flex justify-between items-start">
                                            <div>
                                                <p className="font-medium">{reservation.outletName}</p>
                                                <p className="text-gray-600">{formatReservationDate(reservation.reservationDate)}</p>
                                                <p className="text-sm text-gray-500">
                                                    {reservation.partySize} {reservation.partySize === 1 ? 'person' : 'people'} • Reservation #{reservation.reservationCode}
                                                </p>
                                            </div>
                                            <div className={`px-3 py-1 rounded text-sm ${reservation.status === 'Confirmed' ? 'bg-green-100 text-green-800' :
                                                    reservation.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                                                        'bg-yellow-100 text-yellow-800'
                                                }`}>
                                                {reservation.status}
                                            </div>
                                        </div>
                                    </Link>
                                ))}
                            </div>

                            <div className="mt-6 text-center">
                                <button
                                    onClick={() => navigate('/reservation/new')}
                                    className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                                >
                                    Make a New Reservation
                                </button>
                            </div>
                        </>
                    )}
                </>
            )}
        </div>
    );
};

export default ReservationList;