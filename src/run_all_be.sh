#!/bin/bash

# Run all services in parallel
echo "Starting all API services..."
for dir in */*.Api/; do
    if [ -d "$dir" ]; then
        echo "Starting service in $dir"
        (cd "$dir" && dotnet run) &
    fi
done

# Wait for all background processes
wait 