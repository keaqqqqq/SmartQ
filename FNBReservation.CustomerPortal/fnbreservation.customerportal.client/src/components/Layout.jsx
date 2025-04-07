
import React from 'react';
import { useLocation as useRouterLocation } from 'react-router-dom';
import Header from './Header';

const Layout = ({ children }) => {
    const location = useRouterLocation();
    const isHomePage = location.pathname === '/';

    return (
        <div className={isHomePage ? '' : 'min-h-screen flex flex-col'}>
            <Header />
            <main className={isHomePage ? '' : 'flex-grow '}>
                {children}
            </main>
        </div>
    );
};

export default Layout;