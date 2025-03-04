FROM nginx:alpine
COPY ./portal-published/wwwroot /usr/share/nginx/html
# Configure nginx for Blazor
RUN echo 'server { \
    listen 80; \
    location / { \
        root /usr/share/nginx/html; \
        try_files $uri $uri/ /index.html; \
    } \
    # Set MIME type for .wasm files \
    types { \
        application/wasm wasm; \
    } \
}' > /etc/nginx/conf.d/default.conf
EXPOSE 80