import api from "./client";

export interface ServicePort {
  containerPort: number;
  protocol: string;
  mode: string;
  hostPort: number;
}

export interface ServiceNetwork {
  id: string;
  networkName: string;
}

export interface SwarmService {
  id: string;
  version: number;
  serviceName: string;
  mode: string;
  replicas?: number;
  state?: string;
  stack?: string;
  repository: {
    name: string;
    tag: string;
    image: string;
    imageDigest?: string;
  };
  ports: ServicePort[];
  networks: ServiceNetwork[];
  mounts?: { type: string; source: string; target: string; readOnly: boolean }[];
  environment?: { name: string; value: string }[];
  labels?: { name: string; value: string }[];
  secrets?: { id: string; secretName: string }[];
  configs?: { id: string; configName: string }[];
  status?: {
    tasks: {
      running: number;
      total: number;
    };
  };
  createdAt?: string;
  updatedAt?: string;
}

export interface SwarmTask {
  id: string;
  taskName: string;
  state: string;
  desiredState: string;
  serviceName: string;
  nodeId: string;
  nodeName: string;
  repository: {
    image: string;
    imageDigest?: string;
  };
  status?: {
    error?: string;
  };
  createdAt?: string;
  updatedAt?: string;
}

export async function getServices(): Promise<SwarmService[]> {
  const response = await api.get("/api/services");
  return response.data;
}

export async function getService(id: string): Promise<SwarmService> {
  const response = await api.get(`/api/services/${id}`);
  return response.data;
}

export async function getServiceTasks(id: string): Promise<SwarmTask[]> {
  const response = await api.get(`/api/services/${id}/tasks`);
  return response.data;
}

export async function deleteService(id: string): Promise<void> {
  await api.delete(`/api/services/${id}`);
}
