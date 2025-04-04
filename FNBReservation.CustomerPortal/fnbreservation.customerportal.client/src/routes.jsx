import React from "react";
import { Route, Routes } from "react-router-dom";

// Import components
import Home from "./components/Home";
import ReservationForm from "./components/reservation/ReservationForm";
import ReservationDetail from "./components/reservation/ReservationDetail";
import ReservationList from "./components/reservation/ReservationList";
import ReservationLookup from "./components/reservation/ReservationLookup";
import Outlets from "./components/outlet/Outlets";
import OutletDetail from "./components/outlet/OutletDetail";

const AppRoutes = () => {
    return (
        <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/reservation/new" element={<ReservationForm />} />
            <Route path="/reservation/:id" element={<ReservationDetail />} />
            <Route path="/reservation/code/:code" element={<ReservationDetail />} />
            <Route path="/reservations" element={<ReservationList />} />
            <Route path="/reservation/lookup" element={<ReservationLookup />} />
            <Route path="/outlets" element={<Outlets />} />
            <Route path="/outlet/:id" element={<OutletDetail />} />
        </Routes>
    );
};

export default AppRoutes;