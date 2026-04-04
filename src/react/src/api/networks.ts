import api from "./client";

export interface SwarmNetwork {
  id: string;
  networkName: string;
  driver: string;
  scope: string;
  internal: boolean;
  attachable: boolean;
  ingress: boolean;
  enableIPv6: boolean;
  stack?: string;
  ipam?: {
    subnet?: string;
    gateway?: string;
  };
  created?: string;
}

export async function getNetworks(): Promise<SwarmNetwork[]> {
  const response = await api.get("/api/networks");
  return response.data;
}

export async function getNetwork(id: string): Promise<SwarmNetwork> {
  const response = await api.get(`/api/networks/${id}`);
  return response.data;
}

export async function getNetworkServices(id: string): Promise<any[]> {
  const response = await api.get(`/api/networks/${id}/services`);
  return response.data;
}

export async function createNetwork(data: any): Promise<any> {
  const response = await api.post("/api/networks", data);
  return response.data;
}

export async function deleteNetwork(id: string): Promise<void> {
  await api.delete(`/api/networks/${id}`);
}
