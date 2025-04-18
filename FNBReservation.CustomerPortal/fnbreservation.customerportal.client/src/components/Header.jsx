import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';

const Header = () => {
    const location = useLocation();
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

    // Determine if we're on the homepage
    const isHomePage = location.pathname === '/';

    // Conditionally apply styles based on page
    const headerClasses = "bg-black text-white";

    const linkClasses = "px-4 py-2 hover:text-green-400 transition duration-300";

    // Check if we're in development mode
    const isDev = import.meta.env.MODE === 'development';

    return (
        <header className={`${headerClasses} ${isHomePage ? "absolute top-0 left-0 w-full z-40" : ""}`}>
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                <div className="flex justify-between h-16 items-center">
                    {/* Logo */}
                    <Link to="/" className="flex-shrink-0 flex items-center">
                        <span className={`font-bold text-4xl italic ${isHomePage ? 'text-green-400' : 'text-green-600'}`} style={{ fontFamily: 'cursive' }}>
                            SmartQ
                        </span>
                    </Link>

                    {/* Desktop Navigation */}
                    <nav className="hidden md:flex items-center space-x-1">
                        <Link to="/" className={linkClasses}>
                            Home
                        </Link>
                        <Link to="/reservation/new" className={linkClasses}>
                            Reservation
                        </Link>
                        <Link to="/outlets" className={linkClasses}>
                            Our Locations
                        </Link>
                        <Link to="/reservation/lookup" className={linkClasses}>
                            Find a reservation
                        </Link>
                       
                    </nav>

                    {/* Mobile menu button */}
                    <div className="md:hidden flex items-center">
                        <button
                            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
                            className="inline-flex items-center justify-center p-2 rounded-md text-gray-400 hover:text-white hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-white"
                        >
                            <span className="sr-only">Open main menu</span>
                            <svg
                                className={`${mobileMenuOpen ? 'hidden' : 'block'} h-6 w-6`}
                                xmlns="http://www.w3.org/2000/svg"
                                fill="none"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                                aria-hidden="true"
                            >
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                            </svg>
                            <svg
                                className={`${mobileMenuOpen ? 'block' : 'hidden'} h-6 w-6`}
                                xmlns="http://www.w3.org/2000/svg"
                                fill="none"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                                aria-hidden="true"
                            >
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                </div>
            </div>

            {/* Mobile menu */}
            <div className={`${mobileMenuOpen ? 'block' : 'hidden'} md:hidden`}>
                <div className="px-2 pt-2 pb-3 space-y-1 sm:px-3">
                    <Link
                        to="/"
                        className="block px-3 py-2 rounded-md text-base font-medium text-white hover:bg-gray-700"
                        onClick={() => setMobileMenuOpen(false)}
                    >
                        Home
                    </Link>
                    <Link
                        to="/reservation/new"
                        className="block px-3 py-2 rounded-md text-base font-medium text-white hover:bg-gray-700"
                        onClick={() => setMobileMenuOpen(false)}
                    >
                        Reservation
                    </Link>
                    <Link
                        to="/outlets"
                        className="block px-3 py-2 rounded-md text-base font-medium text-white hover:bg-gray-700"
                        onClick={() => setMobileMenuOpen(false)}
                    >
                        Our Locations
                    </Link>
                    <Link
                        to="/reservation/lookup"
                        className="block px-3 py-2 rounded-md text-base font-medium text-white hover:bg-gray-700"
                        onClick={() => setMobileMenuOpen(false)}
                    >
                        Find a reservation
                    </Link>
             
                </div>
            </div>
        </header>
    );
};

export default Header;