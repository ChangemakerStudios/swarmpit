import api from "./client";

export interface SwarmSecret {
  id: string;
  secretName: string;
  version: number;
  createdAt?: string;
  updatedAt?: string;
}

export async function getSecrets(): Promise<SwarmSecret[]> {
  const response = await api.get("/api/secrets");
  return response.data;
}

export async function getSecret(id: string): Promise<SwarmSecret> {
  const response = await api.get(`/api/secrets/${id}`);
  return response.data;
}

export async function getSecretServices(id: string): Promise<any[]> {
  const response = await api.get(`/api/secrets/${id}/services`);
  return response.data;
}

export async function createSecret(data: any): Promise<any> {
  const response = await api.post("/api/secrets", data);
  return response.data;
}

export async function deleteSecret(id: string): Promise<void> {
  await api.delete(`/api/secrets/${id}`);
}
