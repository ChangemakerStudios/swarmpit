import api from "./client";

export interface SwarmStack {
  stackName: string;
  state: string;
  stackFile: boolean;
  stats: {
    services: number;
    networks: number;
    volumes: number;
    configs: number;
    secrets: number;
  };
}

export interface StackFile {
  spec?: { compose: string };
  previousSpec?: { compose: string };
}

export async function getStacks(): Promise<SwarmStack[]> {
  const response = await api.get("/api/stacks");
  return response.data;
}

export async function getStack(name: string): Promise<SwarmStack> {
  const response = await api.get(`/api/stacks/${name}`);
  return response.data;
}

export async function getStackFile(name: string): Promise<StackFile> {
  const response = await api.get(`/api/stacks/${name}/file`);
  return response.data;
}

export async function saveStackFile(
  name: string,
  compose: string
): Promise<any> {
  const response = await api.post(`/api/stacks/${name}/file`, { compose });
  return response.data;
}

export async function deleteStack(name: string): Promise<void> {
  await api.delete(`/api/stacks/${name}`);
}

export async function deployStack(name: string, compose: string): Promise<any> {
  const response = await api.post(`/api/stacks/${name}/deploy`, { compose });
  return response.data;
}

export async function redeployStack(name: string): Promise<any> {
  const response = await api.post(`/api/stacks/${name}/redeploy`);
  return response.data;
}

export async function getStackServices(name: string): Promise<any[]> {
  const response = await api.get(`/api/stacks/${name}/services`);
  return response.data;
}
