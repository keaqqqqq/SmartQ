import React from "react";
import { Route, Routes } from "react-router-dom";

// Import components
import Home from "./components/Home";
import ReservationForm from "./components/reservation/ReservationForm";
import ReservationDetail from "./components/reservation/ReservationDetail";
import ReservationList from "./components/reservation/ReservationList";
import ReservationLookup from "./components/reservation/ReservationLookup";
import ModifyReservation from "./components/reservation/ModifyReservation";
import Outlet from "./components/Outlet/Outlets";

const AppRoutes = () => {
    return (
        <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/Outlets" element={<Outlet />} />
            <Route path="/reservation/new" element={<ReservationForm />} />
            <Route path="/reservation/:id" element={<ReservationDetail />} />
            <Route path="/reservation/code/:code" element={<ReservationDetail />} />
            <Route path="/reservations" element={<ReservationList />} />
            <Route path="/reservation/lookup" element={<ReservationLookup />} />
            <Route path="/update-reservation/:id" element={<ModifyReservation />} />
        </Routes>
    );
};

export default AppRoutes;