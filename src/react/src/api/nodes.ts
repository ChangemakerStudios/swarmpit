import api from "./client";

export interface SwarmNode {
  id: string;
  version: number;
  nodeName: string;
  role: string;
  availability: string;
  labels: { name: string; value: string }[];
  state: string;
  address?: string;
  engine?: string;
  arch?: string;
  os?: string;
  resources: { cpu: number; memory: number };
  plugins: { networks: string[]; volumes: string[] };
  leader?: boolean;
}

export async function getNodes(): Promise<SwarmNode[]> {
  const response = await api.get("/api/nodes");
  return response.data;
}

export async function getNode(id: string): Promise<SwarmNode> {
  const response = await api.get(`/api/nodes/${id}`);
  return response.data;
}
