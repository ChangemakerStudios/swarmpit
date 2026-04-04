import { createBrowserRouter } from "react-router-dom";
import AppLayout from "./layout/AppLayout";
import ProtectedRoute from "./components/ProtectedRoute";
import LoginPage from "./pages/Login/LoginPage";
import DashboardPage from "./pages/Dashboard/DashboardPage";
import NodeList from "./pages/Nodes/NodeList";
import NodeDetail from "./pages/Nodes/NodeDetail";

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
      // Placeholder routes — pages to be built in Phase 2+
      { path: "services", element: <PlaceholderPage title="Services" /> },
      { path: "stacks", element: <PlaceholderPage title="Stacks" /> },
      { path: "networks", element: <PlaceholderPage title="Networks" /> },
      { path: "volumes", element: <PlaceholderPage title="Volumes" /> },
      { path: "secrets", element: <PlaceholderPage title="Secrets" /> },
      { path: "configs", element: <PlaceholderPage title="Configs" /> },
      { path: "users", element: <PlaceholderPage title="Users" /> },
      { path: "registries", element: <PlaceholderPage title="Registries" /> },
    ],
  },
]);

function PlaceholderPage({ title }: { title: string }) {
  return (
    <div>
      <h2>{title}</h2>
      <p>Coming in Phase 2+</p>
    </div>
  );
}

export default router;
