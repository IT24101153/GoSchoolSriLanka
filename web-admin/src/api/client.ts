import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5133/api",
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

apiClient.interceptors.response.use(undefined, (error) => {
  if (error.response?.status === 401 && window.location.pathname !== "/login") {
    localStorage.removeItem("token");
    window.location.assign("/login");
  }
  return Promise.reject(error);
});

export function errorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; title?: string } | undefined;
    return data?.message ?? data?.title ?? error.message;
  }
  return String(error);
}
