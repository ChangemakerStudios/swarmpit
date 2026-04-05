import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, Button } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getConfigs, type SwarmConfig } from "../../api/configs";
import { formatDate, timeAgo } from "../../utils/time";

const columns: Column<SwarmConfig>[] = [
  { id: "configName", label: "Name", minWidth: 200 },
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

export default function ConfigList() {
  const [configs, setConfigs] = useState<SwarmConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getConfigs()
      .then(setConfigs)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          Configs
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/configs/create")}
        >
          New Config
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={configs}
        loading={loading}
        onRowClick={(row) => navigate(`/configs/${row.id}`)}
        searchFields={["configName"]}
        defaultSortField="configName"
      />
    </Box>
  );
}
