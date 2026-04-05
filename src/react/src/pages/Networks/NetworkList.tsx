import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, Button } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getNetworks, type SwarmNetwork } from "../../api/networks";

const columns: Column<SwarmNetwork>[] = [
  { id: "networkName", label: "Name", minWidth: 200 },
  { id: "driver", label: "Driver" },
  {
    id: "ipam.subnet",
    label: "Subnet",
    render: (row) => row.ipam?.subnet ?? "-",
  },
  {
    id: "ipam.gateway",
    label: "Gateway",
    render: (row) => row.ipam?.gateway ?? "-",
  },
];

export default function NetworkList() {
  const [networks, setNetworks] = useState<SwarmNetwork[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getNetworks()
      .then(setNetworks)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          Networks
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/networks/create")}
        >
          New Network
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={networks}
        loading={loading}
        onRowClick={(row) => navigate(`/networks/${row.id}`)}
        searchFields={["networkName", "driver"]}
        defaultSortField="networkName"
      />
    </Box>
  );
}
