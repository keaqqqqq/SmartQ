/**
 * Outlet Management JavaScript helpers
 */
export function debugToken() {
    const authData = localStorage.getItem('authData');
    console.log('Auth data exists:', authData !== null);
    
    if (authData) {
        try {
            const parsed = JSON.parse(authData);
            console.log('Username:', parsed.Username);
            console.log('Role:', parsed.Role);
            console.log('Access token exists:', !!parsed.AccessToken);
            console.log('Access token length:', parsed.AccessToken ? parsed.AccessToken.length : 0);
            console.log('Refresh token exists:', !!parsed.RefreshToken);
            return true;
        } catch (e) {
            console.error('Error parsing auth data:', e);
            return false;
        }
    }
    return false;
}

export function fixBrokenToken(username, role) {
    const authData = localStorage.getItem('authData');
    if (!authData) {
        console.log('No auth data to fix');
        return false;
    }
    
    try {
        const parsed = JSON.parse(authData);
        let modified = false;
        
        if (!parsed.Username && username) {
            parsed.Username = username;
            modified = true;
        }
        
        if (!parsed.Role && role) {
            parsed.Role = role;
            modified = true;
        }
        
        if (modified) {
            localStorage.setItem('authData', JSON.stringify(parsed));
            console.log('Auth data fixed');
            return true;
        }
        
        return false;
    } catch (e) {
        console.error('Error fixing auth data:', e);
        return false;
    }
}

export function clearAuth() {
    localStorage.removeItem('authData');
    console.log('Auth data cleared');
    return true;
}

export function setMockToken() {
    const mockAuth = {
        Username: 'admin',
        Role: 'Admin',
        AccessToken: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE5MTYyMzkwMjJ9.tbDepxpstvGdW8TC3G8zK4C8q8RbTImGNp6rEcDMJVQ',
        RefreshToken: 'mockRefreshToken123'
    };
    
    localStorage.setItem('authData', JSON.stringify(mockAuth));
    console.log('Mock auth token set');
    return true;
}

export function isAuthenticated() {
    const authData = localStorage.getItem('authData');
    return authData !== null && authData !== undefined;
} 