// App.jsx
import { BrowserRouter } from 'react-router-dom';
import { ReservationProvider } from './contexts/ReservationContext';
import { LocationProvider } from './contexts/LocationContext';
import { QueueProvider } from './contexts/QueueContext';
import AppRoutes from './routes';
import Layout from './components/Layout';

function App() {
    return (
        <BrowserRouter>
            <LocationProvider>
                <ReservationProvider>
                    <QueueProvider>
                        <Layout>
                            <AppRoutes />
                        </Layout>
                    </QueueProvider>
                </ReservationProvider>
            </LocationProvider>
        </BrowserRouter>
    );
}

export default App;