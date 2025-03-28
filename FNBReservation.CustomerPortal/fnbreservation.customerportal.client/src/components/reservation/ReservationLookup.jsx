import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useReservation } from "../../contexts/ReservationContext";

const ReservationLookup = () => {
    const navigate = useNavigate();
    const { getReservationByCode, loading } = useReservation();

    const [reservationCode, setReservationCode] = useState('');
    const [error, setError] = useState(null);

    // Handle form submission
    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!reservationCode) return;

        setError(null);

        try {
            const response = await getReservationByCode(reservationCode);
            if (response && response.id) {
                navigate(`/reservation/code/${reservationCode}`);
            } else {
                setError('Reservation not found. Please check your code and try again.');
            }
        } catch (err) {
            setError('Failed to find reservation. Please check your code and try again.');
            console.error('Reservation lookup error:', err);
        }
    };

    return (
        <div className="max-w-lg mx-auto p-6 bg-white rounded-lg shadow-lg">
            <h1 className="text-2xl font-bold mb-6 text-center">Find Your Reservation</h1>

            {error && (
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4" role="alert">
                    <span className="block sm:inline">{error}</span>
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className="mb-6">
                    <label htmlFor="reservationCode" className="block text-sm font-medium text-gray-700 mb-1">
                        Reservation Code <span className="text-red-500">*</span>
                    </label>
                    <input
                        type="text"
                        id="reservationCode"
                        value={reservationCode}
                        onChange={(e) => setReservationCode(e.target.value)}
                        placeholder="Enter your reservation code"
                        className="w-full px-4 py-3 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                        required
                    />
                    <p className="text-gray-500 text-sm mt-1">
                        The code was sent to you in the confirmation message.
                    </p>
                </div>

                <div className="mb-6">
                    <button
                        type="submit"
                        disabled={loading || !reservationCode}
                        className="w-full bg-green-600 hover:bg-green-700 text-white font-bold py-3 px-4 rounded focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50"
                    >
                        {loading ? "Searching..." : "Find Reservation"}
                    </button>
                </div>
            </form>

            <div className="text-center border-t pt-6">
                <p className="mb-4 text-gray-600">Don't have your reservation code?</p>
                <button
                    onClick={() => navigate('/reservations')}
                    className="text-green-600 hover:text-green-800 font-medium"
                >
                    Look up by phone number instead
                </button>
            </div>
        </div>
    );
};

export default ReservationLookup;