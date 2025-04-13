// Authentication helper functions
window.authHelpers = {
    storeAuthData: function (authData) {
        try {
            console.log('Storing auth data in localStorage', authData);
            
            // Make sure the data is properly formatted
            if (!authData || typeof authData !== 'object') {
                console.error('Invalid auth data format', authData);
                return false;
            }
            
            // Make sure we have the required fields
            if (!authData.Username) {
                console.error('Auth data missing Username', authData);
                return false;
            }
            
            const serialized = JSON.stringify(authData);
            localStorage.setItem('authData', serialized);
            return true;
        } catch (error) {
            console.error('Error storing auth data:', error);
            return false;
        }
    },
    
    getAuthData: function () {
        try {
            const authData = localStorage.getItem('authData');
            if (authData) {
                try {
                    const parsed = JSON.parse(authData);
                    return parsed;
                } catch (parseError) {
                    console.error('Failed to parse auth data', parseError);
                    return null;
                }
            }
            return null;
        } catch (error) {
            console.error('Error getting auth data:', error);
            return null;
        }
    },
    
    clearAuthData: function () {
        try {
            console.log('Clearing auth data from localStorage');
            localStorage.removeItem('authData');
            return true;
        } catch (error) {
            console.error('Error clearing auth data:', error);
            return false;
        }
    },
    
    isAuthenticated: function () {
        try {
            return !!localStorage.getItem('authData');
        } catch (error) {
            console.error('Error checking authentication:', error);
            return false;
        }
    },
    
    // Utility method to help diagnose issues
    diagnose: function() {
        try {
            const authData = this.getAuthData();
            console.log('Auth data diagnosis:');
            console.log('- Is authenticated:', this.isAuthenticated());
            console.log('- Auth data:', authData);
            
            if (authData && authData.AccessToken) {
                const tokenParts = authData.AccessToken.split('.');
                if (tokenParts.length === 3) {
                    try {
                        const payload = JSON.parse(atob(tokenParts[1]));
                        console.log('- Token payload:', payload);
                        
                        // Check expiration
                        if (payload.exp) {
                            const expiry = new Date(payload.exp * 1000);
                            const now = new Date();
                            console.log('- Token expires:', expiry);
                            console.log('- Token is expired:', expiry < now);
                        }
                    } catch (e) {
                        console.error('Error parsing token:', e);
                    }
                }
            }
            
            return true;
        } catch (error) {
            console.error('Error running diagnosis:', error);
            return false;
        }
    }
}; 