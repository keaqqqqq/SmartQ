
// App.jsx
import { BrowserRouter } from 'react-router-dom';
import { ReservationProvider } from './contexts/ReservationContext';
import { LocationProvider } from './contexts/LocationContext';
import AppRoutes from './routes';
import Layout from './components/Layout';

function App() {
    return (
        <BrowserRouter>
            <LocationProvider>
                <ReservationProvider>
                    <Layout>
                        <AppRoutes />
                    </Layout>
                </ReservationProvider>
            </LocationProvider>
        </BrowserRouter>
    );
}

export default App;
