import api from "./client";

export interface SwarmConfig {
  id: string;
  configName: string;
  version: number;
  data?: string;
  createdAt?: string;
  updatedAt?: string;
}

export async function getConfigs(): Promise<SwarmConfig[]> {
  const response = await api.get("/api/configs");
  return response.data;
}

export async function getConfig(id: string): Promise<SwarmConfig> {
  const response = await api.get(`/api/configs/${id}`);
  return response.data;
}

export async function getConfigServices(id: string): Promise<any[]> {
  const response = await api.get(`/api/configs/${id}/services`);
  return response.data;
}

export async function createConfig(data: any): Promise<any> {
  const response = await api.post("/api/configs", data);
  return response.data;
}

export async function deleteConfig(id: string): Promise<void> {
  await api.delete(`/api/configs/${id}`);
}
