import axios from 'axios';
import { getCurrentSessionId } from '@/store/sessionStore';

// No default Content-Type header here: axios auto-sets 'application/json' for
// plain object payloads and must be left untouched for FormData payloads so the
// browser can generate the correct 'multipart/form-data; boundary=...' header.
// A forced 'application/json' default breaks file-upload requests (axios detects
// the JSON content-type and stringifies the FormData instead of sending it raw).
const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
});

axiosInstance.interceptors.request.use((config) => {
  const sessionId = getCurrentSessionId();
  if (sessionId) {
    config.headers['X-Session-Id'] = sessionId;
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      window.location.href = '/login';
      return Promise.reject(error);
    }
    if (error.response?.status >= 500) {
      const message = error.response?.data?.message ?? 'Sunucu hatası oluştu.';
      console.error('[API Error]', message);
    }
    return Promise.reject(error);
  }
);

export default axiosInstance;
