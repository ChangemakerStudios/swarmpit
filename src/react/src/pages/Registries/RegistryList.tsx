import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Button, Chip, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getRegistries, type Registry } from "../../api/registries";

const columns: Column<Registry>[] = [
  { id: "name", label: "Name", minWidth: 200 },
  {
    id: "registryType",
    label: "Type",
    render: (row) => row.registryType.charAt(0).toUpperCase() + row.registryType.slice(1),
  },
  { id: "url", label: "URL", minWidth: 200 },
  {
    id: "public",
    label: "Public",
    render: (row) => (
      <Chip
        label={row.public ? "Yes" : "No"}
        size="small"
        color={row.public ? "success" : "default"}
      />
    ),
  },
];

export default function RegistryList() {
  const [registries, setRegistries] = useState<Registry[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getRegistries()
      .then(setRegistries)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5">Registries</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/registries/create")}
        >
          Add Registry
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={registries}
        loading={loading}
        onRowClick={(row) => navigate(`/registries/${row.id}`)}
        searchFields={["name", "registryType", "url"]}
        defaultSortField="name"
      />
    </Box>
  );
}
