import api from "./client";

export async function login(
  username: string,
  password: string
): Promise<string> {
  const response = await api.post("/login", { username, password });
  return response.data.token;
}

export async function initialize(
  username: string,
  password: string
): Promise<void> {
  await api.post("/initialize", { username, password });
}

export async function getMe(): Promise<{
  username: string;
  role: string;
  email?: string;
}> {
  const response = await api.get("/api/me");
  return response.data;
}
