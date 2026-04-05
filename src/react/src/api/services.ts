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
  serviceId: string;
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

export async function createService(data: any): Promise<any> {
  const response = await api.post("/api/services", data);
  return response.data;
}

export async function updateService(id: string, data: any): Promise<any> {
  const response = await api.post(`/api/services/${id}`, data);
  return response.data;
}

export async function redeployService(id: string, tag?: string): Promise<any> {
  const response = await api.post(
    `/api/services/${id}/redeploy${tag ? `?tag=${tag}` : ""}`
  );
  return response.data;
}

export async function rollbackService(id: string): Promise<any> {
  const response = await api.post(`/api/services/${id}/rollback`);
  return response.data;
}

export async function stopService(id: string): Promise<any> {
  const response = await api.post(`/api/services/${id}/stop`);
  return response.data;
}

export async function getServiceLogs(
  id: string,
  since?: string
): Promise<any> {
  const response = await api.get(
    `/api/services/${id}/logs${since ? `?since=${since}` : ""}`
  );
  return response.data;
}

export async function getServiceCompose(id: string): Promise<any> {
  const response = await api.get(`/api/services/${id}/compose`);
  return response.data;
}
