import api from "./client";

export interface User {
  username: string;
  role: string;
  email?: string;
  hasApiToken?: boolean;
}

export const getUsers = () => api.get<User[]>('/api/users').then(r => r.data);
export const getUser = (username: string) => api.get<User>(`/api/users/${username}`).then(r => r.data);
export const createUser = (data: { username: string; password: string; role?: string; email?: string }) => api.post('/api/users', data).then(r => r.data);
export const updateUser = (username: string, data: { password?: string; role?: string; email?: string }) => api.put(`/api/users/${username}`, data).then(r => r.data);
export const deleteUser = (username: string) => api.delete(`/api/users/${username}`);
export const generateApiToken = (username: string) => api.post(`/api/users/${username}/token`).then(r => r.data);
export const revokeApiToken = (username: string) => api.delete(`/api/users/${username}/token`);
