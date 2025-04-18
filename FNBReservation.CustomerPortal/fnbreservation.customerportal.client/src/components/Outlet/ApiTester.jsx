import React, { useState } from 'react';
import OutletService from '../../services/OutletService';

const ApiTester = () => {
    const [result, setResult] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [rawResponse, setRawResponse] = useState(null);

    const testGetAllOutlets = async () => {
        setLoading(true);
        setError(null);
        setResult(null);
        setRawResponse(null);

        try {
            // Use our service to make the API call
            const response = await OutletService.getAllOutlets();
            
            // Save the raw response for debugging
            setRawResponse(JSON.stringify(response, null, 2));
            
            // Process the response based on its format
            if (Array.isArray(response)) {
                setResult({ 
                    format: 'Array', 
                    count: response.length, 
                    data: response 
                });
            } else if (response && Array.isArray(response.outlets)) {
                setResult({ 
                    format: 'Object with outlets array', 
                    count: response.outlets.length, 
                    data: response.outlets 
                });
            } else if (response && response.success && Array.isArray(response.data)) {
                setResult({ 
                    format: 'Success object with data array', 
                    count: response.data.length, 
                    data: response.data 
                });
            } else {
                setResult({ 
                    format: 'Unknown', 
                    data: response 
                });
            }
        } catch (err) {
            console.error('API test error:', err);
            setError(err.message || 'Failed to fetch outlets');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="bg-white p-6 rounded-lg shadow-lg max-w-4xl mx-auto mt-8">
            <h2 className="text-2xl font-bold mb-4">API Integration Tester</h2>
            
            <div className="flex gap-2 mb-6">
                <button 
                    onClick={testGetAllOutlets}
                    className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 transition-colors"
                    disabled={loading}
                >
                    {loading ? 'Testing...' : 'Test Get All Outlets'}
                </button>
            </div>
            
            {loading && (
                <div className="flex items-center gap-2 text-blue-600 mb-4">
                    <div className="animate-spin h-5 w-5 border-2 border-blue-600 border-t-transparent rounded-full"></div>
                    <span>Loading...</span>
                </div>
            )}
            
            {error && (
                <div className="bg-red-50 border border-red-200 p-4 rounded-md mb-4">
                    <p className="text-red-700 font-medium">Error:</p>
                    <p className="text-red-600">{error}</p>
                </div>
            )}
            
            {result && (
                <div className="bg-green-50 border border-green-200 p-4 rounded-md mb-4">
                    <p className="text-green-700 font-medium">Result:</p>
                    <p className="text-green-800">Format: {result.format}</p>
                    {result.count !== undefined && <p className="text-green-800">Count: {result.count}</p>}
                    
                    <div className="mt-2">
                        <p className="text-green-700 font-medium">Data Preview:</p>
                        <pre className="bg-gray-50 p-3 rounded text-xs overflow-auto max-h-60">
                            {JSON.stringify(result.data ? result.data.slice(0, 1) : result.data, null, 2)}
                        </pre>
                    </div>
                </div>
            )}
            
            {rawResponse && (
                <div className="mt-4">
                    <p className="text-gray-700 font-medium mb-2">Raw Response:</p>
                    <pre className="bg-gray-50 p-3 rounded text-xs overflow-auto max-h-96">
                        {rawResponse}
                    </pre>
                </div>
            )}
        </div>
    );
};

export default ApiTester; 