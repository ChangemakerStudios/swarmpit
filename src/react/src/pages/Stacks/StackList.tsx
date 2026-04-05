import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Button, Chip, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getStacks, type SwarmStack } from "../../api/stacks";

function stateColor(state: string): "success" | "default" {
  return state === "deployed" ? "success" : "default";
}

const columns: Column<SwarmStack>[] = [
  { id: "stackName", label: "Name", minWidth: 200 },
  {
    id: "state",
    label: "State",
    render: (row) => (
      <Chip label={row.state} size="small" color={stateColor(row.state)} />
    ),
  },
  {
    id: "stats.services",
    label: "Services",
    render: (row) => String(row.stats?.services ?? 0),
  },
  {
    id: "stats.networks",
    label: "Networks",
    render: (row) => String(row.stats?.networks ?? 0),
  },
  {
    id: "stats.volumes",
    label: "Volumes",
    render: (row) => String(row.stats?.volumes ?? 0),
  },
];

export default function StackList() {
  const [stacks, setStacks] = useState<SwarmStack[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getStacks()
      .then(setStacks)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5">Stacks</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/stacks/create")}
        >
          New Stack
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={stacks}
        loading={loading}
        onRowClick={(row) => navigate(`/stacks/${row.stackName}`)}
        searchFields={["stackName", "state"]}
        defaultSortField="stackName"
      />
    </Box>
  );
}
