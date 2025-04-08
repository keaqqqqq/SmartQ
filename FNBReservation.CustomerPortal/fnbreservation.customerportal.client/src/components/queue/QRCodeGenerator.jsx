import React, { useState, useRef } from 'react';

// Note: In a real implementation, you would need to install the QRCode library
// npm install qrcode.react
// This is a simulated version to show the UI structure
const QRCode = ({ value, size }) => {
    // This is a placeholder - in real implementation, use the actual QRCode component
    return (
        <div
            className="border-4 border-black rounded-lg bg-white flex items-center justify-center"
            style={{ width: size, height: size }}
        >
            <div className="text-center">
                <div className="font-mono text-xs break-all px-3">{value}</div>
                <div className="bg-black text-white mt-2 text-xs py-1">QR Code Placeholder</div>
            </div>
        </div>
    );
};

const QRCodeGenerator = () => {
    const [outlets, setOutlets] = useState([
        { id: "3f1417c7-ac1f-4cd2-9c42-2a858271c2f5", name: "Main Branch", address: "123 Main Street" },
        { id: "8a2417c7-bc1f-4cd2-9c42-2a858271c2f5", name: "Downtown Location", address: "456 Center Ave" },
        { id: "9c3417c7-cc1f-4cd2-9c42-2a858271c2f5", name: "Riverside Branch", address: "789 River Road" }
    ]);

    const [selectedOutlet, setSelectedOutlet] = useState(outlets[0]);
    const [domainName, setDomainName] = useState("https://smartq.example.com");
    const [qrSize, setQrSize] = useState(200);
    const qrRef = useRef(null);

    // Generate the QR code URL
    const generateQRUrl = () => {
        return `${domainName}/queue/join?outletId=${selectedOutlet.id}`;
    };

    // Handle outlet change
    const handleOutletChange = (e) => {
        const outletId = e.target.value;
        const outlet = outlets.find(o => o.id === outletId);
        if (outlet) {
            setSelectedOutlet(outlet);
        }
    };

    // Handle domain name change
    const handleDomainChange = (e) => {
        setDomainName(e.target.value);
    };

    // Handle QR size change
    const handleSizeChange = (e) => {
        setQrSize(parseInt(e.target.value));
    };

    // Print QR code
    const handlePrint = () => {
        const printWindow = window.open('', '_blank');

        printWindow.document.write(`
            <html>
                <head>
                    <title>SmartQ - QR Code for ${selectedOutlet.name}</title>
                    <style>
                        body {
                            font-family: Arial, sans-serif;
                            text-align: center;
                            padding: 20px;
                        }
                        .qr-container {
                            margin: 20px auto;
                        }
                        .outlet-name {
                            font-size: 24px;
                            font-weight: bold;
                            margin-bottom: 10px;
                        }
                        .instructions {
                            margin-top: 20px;
                            font-size: 18px;
                        }
                    </style>
                </head>
                <body>
                    <div class="outlet-name">${selectedOutlet.name}</div>
                    <div>${selectedOutlet.address}</div>
                    <div class="qr-container">
                        ${qrRef.current?.innerHTML || ''}
                    </div>
                    <div class="instructions">Scan this QR code to join our queue</div>
                </body>
            </html>
        `);

        printWindow.document.close();
        printWindow.focus();

        // Print after a short delay to ensure content is loaded
        setTimeout(() => {
            printWindow.print();
            printWindow.close();
        }, 500);
    };

    // Download QR code as an image
    const handleDownload = () => {
        // In a real implementation, you would use html2canvas or another library
        // to capture the QR code as an image and trigger a download
        alert("In a real implementation, this would download the QR code as an image.");
    };

    return (
        <div className="max-w-4xl mx-auto px-4 py-8">
            <h1 className="text-2xl font-bold mb-6">QR Code Generator for Queue System</h1>

            <div className="grid md:grid-cols-2 gap-8">
                <div className="bg-white rounded-lg shadow-md p-6">
                    <h2 className="text-xl font-bold mb-4">Settings</h2>

                    <div className="mb-4">
                        <label htmlFor="outlet" className="block text-sm font-medium text-gray-700 mb-1">
                            Select Outlet
                        </label>
                        <select
                            id="outlet"
                            value={selectedOutlet.id}
                            onChange={handleOutletChange}
                            className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                        >
                            {outlets.map(outlet => (
                                <option key={outlet.id} value={outlet.id}>{outlet.name}</option>
                            ))}
                        </select>
                    </div>

                    <div className="mb-4">
                        <label htmlFor="domain" className="block text-sm font-medium text-gray-700 mb-1">
                            Domain Name
                        </label>
                        <input
                            type="text"
                            id="domain"
                            value={domainName}
                            onChange={handleDomainChange}
                            className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                        />
                    </div>

                    <div className="mb-4">
                        <label htmlFor="size" className="block text-sm font-medium text-gray-700 mb-1">
                            QR Code Size
                        </label>
                        <div className="flex items-center">
                            <input
                                type="range"
                                id="size"
                                min="100"
                                max="400"
                                step="50"
                                value={qrSize}
                                onChange={handleSizeChange}
                                className="w-full mr-4"
                            />
                            <span>{qrSize}px</span>
                        </div>
                    </div>

                    <div className="bg-gray-100 rounded p-3 mb-4">
                        <p className="text-sm font-medium text-gray-700 mb-1">QR Code URL:</p>
                        <p className="text-sm break-all bg-white p-2 rounded border">{generateQRUrl()}</p>
                    </div>
                </div>

                <div className="bg-white rounded-lg shadow-md p-6 flex flex-col items-center">
                    <h2 className="text-xl font-bold mb-4">Generated QR Code</h2>

                    <div ref={qrRef} className="mb-6">
                        <QRCode
                            value={generateQRUrl()}
                            size={qrSize}
                        />
                    </div>

                    <div className="text-center mb-6">
                        <h3 className="font-bold">{selectedOutlet.name}</h3>
                        <p>{selectedOutlet.address}</p>
                    </div>

                    <div className="flex gap-4">
                        <button
                            onClick={handlePrint}
                            className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Print QR Code
                        </button>

                        <button
                            onClick={handleDownload}
                            className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-6 rounded"
                        >
                            Download
                        </button>
                    </div>
                </div>
            </div>

            <div className="mt-8 bg-blue-50 border border-blue-200 p-4 rounded">
                <h3 className="font-bold text-blue-800 mb-2">How to use:</h3>
                <ol className="list-decimal ml-5 text-blue-700">
                    <li className="mb-1">Select the outlet for which you want to generate a QR code.</li>
                    <li className="mb-1">Customize the domain name if needed (must match your actual website domain).</li>
                    <li className="mb-1">Adjust the QR code size as needed.</li>
                    <li className="mb-1">Print the QR code and display it at your restaurant's entrance.</li>
                    <li className="mb-1">Customers scan this QR code to join your virtual queue.</li>
                </ol>
            </div>
        </div>
    );
};

export default QRCodeGenerator;