import { Breadcrumbs, Link, Typography } from "@mui/material";
import { Link as RouterLink, useLocation } from "react-router-dom";
import NavigateNextIcon from "@mui/icons-material/NavigateNext";

const labelMap: Record<string, string> = {
  containers: "Containers",
  services: "Services",
  tasks: "Tasks",
  stacks: "Stacks",
  networks: "Networks",
  nodes: "Nodes",
  volumes: "Volumes",
  secrets: "Secrets",
  configs: "Configs",
  users: "Users",
  registries: "Registries",
  create: "Create",
  edit: "Edit",
  logs: "Logs",
};

export default function Breadcrumb() {
  const location = useLocation();
  const segments = location.pathname.split("/").filter(Boolean);

  if (segments.length === 0) return null;

  const crumbs: { label: string; path: string }[] = [];
  let currentPath = "";

  for (let i = 0; i < segments.length; i++) {
    const segment = segments[i];
    currentPath += `/${segment}`;
    const label = labelMap[segment] ?? decodeURIComponent(segment);
    crumbs.push({ label, path: currentPath });
  }

  return (
    <Breadcrumbs
      separator={<NavigateNextIcon fontSize="small" />}
      sx={{ mb: 2 }}
    >
      <Link component={RouterLink} to="/" underline="hover" color="inherit">
        Home
      </Link>
      {crumbs.map((crumb, i) =>
        i === crumbs.length - 1 ? (
          <Typography key={crumb.path} color="text.primary">
            {crumb.label}
          </Typography>
        ) : (
          <Link
            key={crumb.path}
            component={RouterLink}
            to={crumb.path}
            underline="hover"
            color="inherit"
          >
            {crumb.label}
          </Link>
        )
      )}
    </Breadcrumbs>
  );
}
