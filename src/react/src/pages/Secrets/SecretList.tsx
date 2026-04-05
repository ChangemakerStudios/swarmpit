import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, Button } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getSecrets, type SwarmSecret } from "../../api/secrets";
import { formatDate, timeAgo } from "../../utils/time";

const columns: Column<SwarmSecret>[] = [
  { id: "secretName", label: "Name", minWidth: 200 },
  {
    id: "updatedAt",
    label: "Last Update",
    render: (row) => timeAgo(row.updatedAt),
  },
  {
    id: "createdAt",
    label: "Created",
    render: (row) => formatDate(row.createdAt),
  },
];

export default function SecretList() {
  const [secrets, setSecrets] = useState<SwarmSecret[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getSecrets()
      .then(setSecrets)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          Secrets
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/secrets/create")}
        >
          New Secret
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={secrets}
        loading={loading}
        onRowClick={(row) => navigate(`/secrets/${row.id}`)}
        searchFields={["secretName"]}
        defaultSortField="secretName"
      />
    </Box>
  );
}
