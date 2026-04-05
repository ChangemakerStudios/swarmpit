import api from "./client";

export interface ContainerPort {
  privatePort: number;
  publicPort?: number;
  type: string;
  ip?: string;
}

export interface Container {
  id: string;
  fullId: string;
  name: string;
  image: string;
  state: string;
  status: string;
  created: string;
  ports: ContainerPort[];
  labels: Record<string, string>;
  networks: string[];
  stack?: string;
  command: string;
}

export interface ContainerDetail extends Container {
  env: string[];
  restartPolicy: string;
  hostname?: string;
  workingDir?: string;
  user?: string;
  mounts: { type: string; source: string; destination: string; readOnly: boolean }[];
}

export const getContainers = (all = true) =>
  api.get<Container[]>(`/api/containers?all=${all}`).then((r) => r.data);

export const getContainer = (id: string) =>
  api.get<ContainerDetail>(`/api/containers/${id}`).then((r) => r.data);

export const startContainer = (id: string) =>
  api.post(`/api/containers/${id}/start`).then((r) => r.data);

export const stopContainer = (id: string) =>
  api.post(`/api/containers/${id}/stop`).then((r) => r.data);

export const restartContainer = (id: string) =>
  api.post(`/api/containers/${id}/restart`).then((r) => r.data);

export const removeContainer = (id: string, force = false) =>
  api.delete(`/api/containers/${id}?force=${force}`);

export const getContainerLogs = (id: string, since?: string) =>
  api
    .get(`/api/containers/${id}/logs${since ? `?since=${since}` : ""}`)
    .then((r) => r.data);
