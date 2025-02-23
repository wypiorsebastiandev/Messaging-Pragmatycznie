#!/bin/bash

# Infra
echo "Starting infra..."
sh ./run_infra.sh
echo "Waiting for infra background processes for 10s..."
sleep 10

# Build backend first
(cd src && ./run_all_migrations.sh) || { echo "Backend build failed"; exit 1; }

# Start all services in parallel
echo "Starting all services..."

# Start frontend applications
(cd src_frontend && ./run_all_fe.sh) &

# Start mock API
(cd src_frontend && ./run_mock_api.sh) &

# Start backend services
(cd src && ./run_all_be.sh) &

# Clear the terminal after a brief pause to let services start
sleep 2
clear

echo "All services are running..."
echo "Press Ctrl+C to stop all services"