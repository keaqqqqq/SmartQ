// App.jsx
import { BrowserRouter } from 'react-router-dom';
import { ReservationProvider } from './contexts/ReservationContext';
import AppRoutes from './routes';
import Layout from './components/Layout';

function App() {
    return (
        <BrowserRouter>
            <ReservationProvider>
                <Layout>
                    <AppRoutes />
                </Layout>
            </ReservationProvider>
        </BrowserRouter>
    );
}

export default App;