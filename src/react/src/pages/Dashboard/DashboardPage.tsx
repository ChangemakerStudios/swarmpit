import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Typography,
  Card,
  CardContent,
  CardActionArea,
  Grid,
  LinearProgress,
} from "@mui/material";
import { getContainers } from "../../api/containers";
import { getServices } from "../../api/services";
import { getNodes } from "../../api/nodes";
import { getNetworks } from "../../api/networks";
import { getVolumes } from "../../api/volumes";
import { getSecrets } from "../../api/secrets";
import { getConfigs } from "../../api/configs";
import { getTasks } from "../../api/tasks";

interface CountCard {
  label: string;
  path: string;
  count: number | null;
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [counts, setCounts] = useState<CountCard[]>([
    { label: "Containers", path: "/containers", count: null },
    { label: "Services", path: "/services", count: null },
    { label: "Tasks", path: "/tasks", count: null },
    { label: "Nodes", path: "/nodes", count: null },
    { label: "Networks", path: "/networks", count: null },
    { label: "Volumes", path: "/volumes", count: null },
    { label: "Secrets", path: "/secrets", count: null },
    { label: "Configs", path: "/configs", count: null },
  ]);

  useEffect(() => {
    Promise.all([
      getContainers(false).then((r) => r.length),
      getServices().then((r) => r.length),
      getTasks().then((r) => r.length),
      getNodes().then((r) => r.length),
      getNetworks().then((r) => r.length),
      getVolumes().then((r) => r.length),
      getSecrets().then((r) => r.length),
      getConfigs().then((r) => r.length),
    ])
      .then(([containers, services, tasks, nodes, networks, volumes, secrets, configs]) => {
        setCounts([
          { label: "Containers", path: "/containers", count: containers },
          { label: "Services", path: "/services", count: services },
          { label: "Tasks", path: "/tasks", count: tasks },
          { label: "Nodes", path: "/nodes", count: nodes },
          { label: "Networks", path: "/networks", count: networks },
          { label: "Volumes", path: "/volumes", count: volumes },
          { label: "Secrets", path: "/secrets", count: secrets },
          { label: "Configs", path: "/configs", count: configs },
        ]);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Dashboard
      </Typography>
      {loading && <LinearProgress sx={{ mb: 2 }} />}
      <Grid container spacing={3}>
        {counts.map((item) => (
          <Grid key={item.label} size={{ xs: 12, sm: 6, md: 3 }}>
            <Card>
              <CardActionArea onClick={() => navigate(item.path)}>
                <CardContent>
                  <Typography color="text.secondary" gutterBottom>
                    {item.label}
                  </Typography>
                  <Typography variant="h4">
                    {item.count !== null ? item.count : "-"}
                  </Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
      <Box sx={{ mt: 4 }}>
        <Typography variant="body2" color="text.secondary">
          Dashboard charts and real-time stats will be added in Phase 5.
        </Typography>
      </Box>
    </Box>
  );
}
