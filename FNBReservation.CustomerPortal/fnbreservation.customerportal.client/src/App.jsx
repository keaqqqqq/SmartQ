import { BrowserRouter } from 'react-router-dom';
import { ReservationProvider } from './contexts/ReservationContext';
import { LocationProvider } from './contexts/LocationContext';
import { OutletProvider } from './contexts/OutletContext';
import AppRoutes from './routes';
import Layout from './components/Layout';

function App() {
    return (
        <BrowserRouter>
            <LocationProvider>
                <OutletProvider>
                    <ReservationProvider>
                        <Layout>
                            <AppRoutes />
                        </Layout>
                    </ReservationProvider>
                </OutletProvider>
            </LocationProvider>
        </BrowserRouter>
    );
}

export default App;