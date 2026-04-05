import api from "./client";

export interface Registry {
  id: string;
  name: string;
  registryType: string;
  url: string;
  username?: string;
  password?: string;
  public: boolean;
  owner: string;
  region?: string;
  accessKeyId?: string;
  accessKey?: string;
  servicePrincipalId?: string;
  token?: string;
  selfHosted?: boolean;
  gitlabUrl?: string;
  registryUrl?: string;
  customApi?: boolean;
  withAuth?: boolean;
}

export async function getRegistries(type?: string): Promise<Registry[]> {
  const response = await api.get(
    `/api/registries${type ? `?registryType=${type}` : ""}`
  );
  return response.data;
}

export async function getRegistry(id: string): Promise<Registry> {
  const response = await api.get(`/api/registries/${id}`);
  return response.data;
}

export async function createRegistry(data: any): Promise<any> {
  const response = await api.post("/api/registries", data);
  return response.data;
}

export async function updateRegistry(id: string, data: any): Promise<any> {
  const response = await api.post(`/api/registries/${id}`, data);
  return response.data;
}

export async function deleteRegistry(id: string): Promise<void> {
  await api.delete(`/api/registries/${id}`);
}
