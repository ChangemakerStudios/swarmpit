import { createBrowserRouter } from "react-router-dom";
import AppLayout from "./layout/AppLayout";
import ProtectedRoute from "./components/ProtectedRoute";
import LoginPage from "./pages/Login/LoginPage";
import DashboardPage from "./pages/Dashboard/DashboardPage";
import NodeList from "./pages/Nodes/NodeList";
import NodeDetail from "./pages/Nodes/NodeDetail";
import ServiceList from "./pages/Services/ServiceList";
import ServiceCreate from "./pages/Services/ServiceCreate";
import ServiceDetail from "./pages/Services/ServiceDetail";
import ServiceEdit from "./pages/Services/ServiceEdit";
import ServiceLogs from "./pages/Services/ServiceLogs";
import TaskList from "./pages/Tasks/TaskList";
import TaskDetail from "./pages/Tasks/TaskDetail";
import NetworkList from "./pages/Networks/NetworkList";
import NetworkDetail from "./pages/Networks/NetworkDetail";
import NetworkCreate from "./pages/Networks/NetworkCreate";
import VolumeList from "./pages/Volumes/VolumeList";
import VolumeDetail from "./pages/Volumes/VolumeDetail";
import VolumeCreate from "./pages/Volumes/VolumeCreate";
import SecretList from "./pages/Secrets/SecretList";
import SecretDetail from "./pages/Secrets/SecretDetail";
import SecretCreate from "./pages/Secrets/SecretCreate";
import ConfigList from "./pages/Configs/ConfigList";
import ConfigDetail from "./pages/Configs/ConfigDetail";
import ConfigCreate from "./pages/Configs/ConfigCreate";
import ContainerList from "./pages/Containers/ContainerList";
import ContainerDetail from "./pages/Containers/ContainerDetail";
import ContainerLogs from "./pages/Containers/ContainerLogs";
import StackList from "./pages/Stacks/StackList";
import StackCreate from "./pages/Stacks/StackCreate";
import StackDetail from "./pages/Stacks/StackDetail";
import StackEdit from "./pages/Stacks/StackEdit";
import RegistryList from "./pages/Registries/RegistryList";
import RegistryCreate from "./pages/Registries/RegistryCreate";
import RegistryDetail from "./pages/Registries/RegistryDetail";
import RegistryEdit from "./pages/Registries/RegistryEdit";

const router = createBrowserRouter([
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    path: "/",
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "nodes", element: <NodeList /> },
      { path: "nodes/:id", element: <NodeDetail /> },
      { path: "containers", element: <ContainerList /> },
      { path: "containers/:id", element: <ContainerDetail /> },
      { path: "containers/:id/logs", element: <ContainerLogs /> },
      { path: "services", element: <ServiceList /> },
      { path: "services/create", element: <ServiceCreate /> },
      { path: "services/:id", element: <ServiceDetail /> },
      { path: "services/:id/edit", element: <ServiceEdit /> },
      { path: "services/:id/logs", element: <ServiceLogs /> },
      { path: "tasks", element: <TaskList /> },
      { path: "tasks/:id", element: <TaskDetail /> },
      { path: "networks", element: <NetworkList /> },
      { path: "networks/create", element: <NetworkCreate /> },
      { path: "networks/:id", element: <NetworkDetail /> },
      { path: "volumes", element: <VolumeList /> },
      { path: "volumes/create", element: <VolumeCreate /> },
      { path: "volumes/:name", element: <VolumeDetail /> },
      { path: "secrets", element: <SecretList /> },
      { path: "secrets/create", element: <SecretCreate /> },
      { path: "secrets/:id", element: <SecretDetail /> },
      { path: "configs", element: <ConfigList /> },
      { path: "configs/create", element: <ConfigCreate /> },
      { path: "configs/:id", element: <ConfigDetail /> },
      { path: "stacks", element: <StackList /> },
      { path: "stacks/create", element: <StackCreate /> },
      { path: "stacks/:name", element: <StackDetail /> },
      { path: "stacks/:name/edit", element: <StackEdit /> },
      { path: "registries", element: <RegistryList /> },
      { path: "registries/create", element: <RegistryCreate /> },
      { path: "registries/:id", element: <RegistryDetail /> },
      { path: "registries/:id/edit", element: <RegistryEdit /> },
      { path: "users", element: <PlaceholderPage title="Users" /> },
    ],
  },
]);

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div>
      <h2>{title}</h2>
      <p>Coming in Phase 3+</p>
    </div>
  );
}

export default router;
