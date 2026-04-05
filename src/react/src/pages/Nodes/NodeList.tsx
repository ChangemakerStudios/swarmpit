import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, Chip } from "@mui/material";
import DataTable, { type Column } from "../../components/DataTable";
import { getNodes, type SwarmNode } from "../../api/nodes";

const columns: Column<SwarmNode>[] = [
  {
    id: "nodeName",
    label: "Name",
    render: (row) => (
      <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
        {row.nodeName}
        {row.leader && (
          <Chip label="Leader" size="small" color="primary" />
        )}
      </Box>
    ),
  },
  { id: "role", label: "Role" },
  {
    id: "state",
    label: "State",
    render: (row) => (
      <Chip
        label={row.state}
        size="small"
        color={row.state === "ready" ? "success" : "default"}
      />
    ),
  },
  { id: "availability", label: "Availability" },
  { id: "engine", label: "Engine" },
  {
    id: "resources.cpu",
    label: "CPU",
    render: (row) => row.resources.cpu.toFixed(1),
  },
  {
    id: "resources.memory",
    label: "Memory",
    render: (row) => `${Math.round(row.resources.memory)} MiB`,
  },
  { id: "address", label: "Address" },
];

export default function NodeList() {
  const [nodes, setNodes] = useState<SwarmNode[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getNodes()
      .then(setNodes)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Nodes
      </Typography>
      <DataTable
        columns={columns}
        rows={nodes}
        loading={loading}
        onRowClick={(row) => navigate(`/nodes/${row.id}`)}
        searchFields={["nodeName", "role", "state", "address"]}
        defaultSortField="nodeName"
      />
    </Box>
  );
}
