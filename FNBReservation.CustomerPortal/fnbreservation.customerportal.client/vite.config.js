import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { env } from 'process';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        port: parseInt(env.DEV_SERVER_PORT || '56288'),
        proxy: {
            '/api/v1': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false,
                rewrite: (path) => path
            },
            '/api/CustomerQueue': {
                target: 'http://localhost:5000/api/v1/queue',
                changeOrigin: true,
                secure: false,
                rewrite: (path) => path.replace(/^\/api\/CustomerQueue/, '')
            },
            '/api/public': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false
            }
        }
    }
})
