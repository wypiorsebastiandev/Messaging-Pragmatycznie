import axios from 'axios';

// Create axios instance with default config
const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5002',
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api; 