import React from 'react';
import { Link } from 'react-router-dom';

const Home = () => {
  return (
    <div className="max-w-5xl mx-auto p-8">
      <div className="text-center mb-12">
        <h1 className="text-4xl font-bold text-gray-800 mb-4">FNB Reservation System</h1>
        <p className="text-xl text-gray-600">Manage your restaurant reservations with ease</p>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <Link to="/reservation/new" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
          <h2 className="text-2xl font-semibold text-gray-800 mb-2">Make a Reservation</h2>
          <p className="text-gray-600 mb-4">Book a table at one of our restaurants</p>
          <div className="text-green-600 font-medium">Get Started →</div>
        </Link>

        <Link to="/reservation/lookup" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
          <h2 className="text-2xl font-semibold text-gray-800 mb-2">Find a Reservation</h2>
          <p className="text-gray-600 mb-4">Check or modify your existing reservations</p>
          <div className="text-green-600 font-medium">Look Up →</div>
        </Link>

        <Link to="/reservations" className="block bg-white p-6 rounded-lg shadow-md hover:shadow-lg transition-shadow">
          <h2 className="text-2xl font-semibold text-gray-800 mb-2">My Reservations</h2>
          <p className="text-gray-600 mb-4">View all your current reservations</p>
          <div className="text-green-600 font-medium">View All →</div>
        </Link>

        <div className="block bg-white p-6 rounded-lg shadow-md">
          <h2 className="text-2xl font-semibold text-gray-800 mb-2">Need Help?</h2>
          <p className="text-gray-600 mb-4">Contact our support team for assistance</p>
          <div className="text-gray-700 mb-1">Phone: +60 12-345 6789</div>
          <div className="text-gray-700">Email: support@fnbreservation.com</div>
        </div>
      </div>
    </div>
  );
};

export default Home; 