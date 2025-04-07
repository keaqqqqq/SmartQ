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


// Import queue components
import QueueForm from "./components/queue/QueueForm";
import QueueStatus from "./components/queue/QueueStatus";
import QueueConfirmation from "./components/queue/QueueConfirmation";
import QRCodeGenerator from "./components/queue/QRCodeGenerator";

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

            {/* Queue routes */}
            <Route path="/queue/join" element={<QueueForm />} />
            <Route path="/queue/status/:id" element={<QueueStatus />} />
            <Route path="/queue/confirm/:id" element={<QueueConfirmation />} />

            {/* Admin/Staff tools */}
            <Route path="/admin/qrcode-generator" element={<QRCodeGenerator />} />
               </Routes>
    );
};

export default AppRoutes;