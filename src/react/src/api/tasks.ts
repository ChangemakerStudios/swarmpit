import api from "./client";

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

export async function getTasks(): Promise<SwarmTask[]> {
  const response = await api.get("/api/tasks");
  return response.data;
}

export async function getTask(id: string): Promise<SwarmTask> {
  const response = await api.get(`/api/tasks/${id}`);
  return response.data;
}
