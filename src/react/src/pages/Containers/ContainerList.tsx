import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Chip, FormControlLabel, Switch, Typography } from "@mui/material";
import DataTable, { type Column } from "../../components/DataTable";
import { getContainers, type Container } from "../../api/containers";

function stateColor(
  state: string
): "success" | "warning" | "error" | "default" | "info" {
  switch (state) {
    case "running":
      return "success";
    case "exited":
      return "default";
    case "paused":
      return "warning";
    case "created":
    case "restarting":
      return "info";
    case "dead":
    case "removing":
      return "error";
    default:
      return "default";
  }
}

function formatPorts(ports: Container["ports"]): string {
  if (!ports || ports.length === 0) return "-";
  return ports
    .map((p) =>
      p.publicPort
        ? `${p.publicPort}:${p.privatePort}/${p.type}`
        : `${p.privatePort}/${p.type}`
    )
    .join(", ");
}

const columns: Column<Container>[] = [
  { id: "name", label: "Name", minWidth: 200 },
  { id: "image", label: "Image", minWidth: 200 },
  {
    id: "state",
    label: "State",
    render: (row) => (
      <Chip label={row.state} size="small" color={stateColor(row.state)} />
    ),
  },
  { id: "status", label: "Status" },
  {
    id: "ports",
    label: "Ports",
    render: (row) => formatPorts(row.ports),
  },
];

export default function ContainerList() {
  const [containers, setContainers] = useState<Container[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAll, setShowAll] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    setLoading(true);
    getContainers(showAll)
      .then(setContainers)
      .finally(() => setLoading(false));
  }, [showAll]);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
        <Typography variant="h5">Containers</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <FormControlLabel
          control={
            <Switch
              checked={showAll}
              onChange={(e) => setShowAll(e.target.checked)}
            />
          }
          label="Show stopped"
        />
      </Box>
      <DataTable
        columns={columns}
        rows={containers}
        loading={loading}
        onRowClick={(row) => navigate(`/containers/${row.id}`)}
        searchFields={["name", "image", "state"]}
        defaultSortField="name"
      />
    </Box>
  );
}
