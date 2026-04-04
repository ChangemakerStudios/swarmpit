import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, LinearProgress, Chip } from "@mui/material";
import DataTable, { type Column } from "../../components/DataTable";
import { getServices, type SwarmService } from "../../api/services";

function statusColor(service: SwarmService): "success" | "warning" | "error" | "default" {
  const running = service.status?.tasks?.running ?? 0;
  const total = service.status?.tasks?.total ?? 0;
  if (total === 0) return "default";
  if (running === total) return "success";
  if (running > 0) return "warning";
  return "error";
}

function statusLabel(service: SwarmService): string {
  const running = service.status?.tasks?.running ?? 0;
  const total = service.status?.tasks?.total ?? 0;
  if (running === total && total > 0) return "running";
  if (running > 0) return "partly running";
  return "not running";
}

const columns: Column<SwarmService>[] = [
  { id: "serviceName", label: "Name", minWidth: 200 },
  {
    id: "repository.image",
    label: "Image",
    render: (row) => `${row.repository.name}:${row.repository.tag}`,
  },
  { id: "mode", label: "Mode" },
  {
    id: "replicas",
    label: "Replicas",
    render: (row) => {
      const running = row.status?.tasks?.running ?? 0;
      const total = row.status?.tasks?.total ?? 0;
      return `${running}/${total}`;
    },
  },
  {
    id: "state",
    label: "Status",
    render: (row) => (
      <Chip label={statusLabel(row)} size="small" color={statusColor(row)} />
    ),
  },
];

export default function ServiceList() {
  const [services, setServices] = useState<SwarmService[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getServices()
      .then(setServices)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Services
      </Typography>
      {loading && <LinearProgress />}
      <DataTable
        columns={columns}
        rows={services}
        onRowClick={(row) => navigate(`/services/${row.id}`)}
        searchFields={["serviceName", "repository.name", "repository.tag"]}
        defaultSortField="serviceName"
      />
    </Box>
  );
}
