import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, LinearProgress, Button } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getVolumes, type SwarmVolume } from "../../api/volumes";

const columns: Column<SwarmVolume>[] = [
  { id: "volumeName", label: "Name", minWidth: 200 },
  { id: "driver", label: "Driver" },
  { id: "mountpoint", label: "Mountpoint", minWidth: 300 },
];

export default function VolumeList() {
  const [volumes, setVolumes] = useState<SwarmVolume[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getVolumes()
      .then(setVolumes)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          Volumes
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/volumes/create")}
        >
          New Volume
        </Button>
      </Box>
      {loading && <LinearProgress />}
      <DataTable
        columns={columns}
        rows={volumes}
        onRowClick={(row) => navigate(`/volumes/${row.volumeName}`)}
        searchFields={["volumeName", "driver"]}
        defaultSortField="volumeName"
      />
    </Box>
  );
}
