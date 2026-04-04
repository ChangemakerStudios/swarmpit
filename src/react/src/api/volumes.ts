import api from "./client";

export interface SwarmVolume {
  id: string;
  volumeName: string;
  driver: string;
  scope: string;
  stack?: string;
  mountpoint?: string;
}

export async function getVolumes(): Promise<SwarmVolume[]> {
  const response = await api.get("/api/volumes");
  return response.data;
}

export async function getVolume(name: string): Promise<SwarmVolume> {
  const response = await api.get(`/api/volumes/${name}`);
  return response.data;
}

export async function getVolumeServices(name: string): Promise<any[]> {
  const response = await api.get(`/api/volumes/${name}/services`);
  return response.data;
}

export async function createVolume(data: any): Promise<any> {
  const response = await api.post("/api/volumes", data);
  return response.data;
}

export async function deleteVolume(name: string): Promise<void> {
  await api.delete(`/api/volumes/${name}`);
}
