import { useEffect, useState } from "react";
import { useParams, Link as RouterLink } from "react-router-dom";
import {
  Box,
  Card,
  CardContent,
  Chip,
  Grid,
  LinearProgress,
  Typography,
  Link,
  Alert,
} from "@mui/material";
import { getTask, type SwarmTask } from "../../api/tasks";
import { formatDate } from "../../utils/time";

function InfoItem({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <Box sx={{ mb: 1 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value ?? "-"}</Typography>
    </Box>
  );
}

function stateColor(state: string): "success" | "error" | "warning" | "default" {
  if (state === "running") return "success";
  if (state === "failed" || state === "rejected") return "error";
  if (state === "pending" || state === "preparing" || state === "starting") return "warning";
  return "default";
}

export default function TaskDetail() {
  const { id } = useParams<{ id: string }>();
  const [task, setTask] = useState<SwarmTask | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    getTask(id)
      .then(setTask)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <LinearProgress />;
  if (!task) return <Typography>Task not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{task.taskName}</Typography>
        <Chip label={task.state} color={stateColor(task.state)} />
      </Box>

      {task.status?.error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {task.status.error}
        </Alert>
      )}

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                General
              </Typography>
              <InfoItem label="ID" value={task.id} />
              <InfoItem label="Image" value={task.repository.image} />
              <InfoItem label="Image Digest" value={task.repository.imageDigest} />
              <InfoItem label="Desired State" value={task.desiredState} />
              <InfoItem label="Created" value={formatDate(task.createdAt)} />
              <InfoItem label="Last Update" value={formatDate(task.updatedAt)} />
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Links
              </Typography>
              <Box sx={{ mb: 1 }}>
                <Typography variant="caption" color="text.secondary">
                  Service
                </Typography>
                <Typography variant="body1">
                  <Link component={RouterLink} to={`/services/${task.serviceName}`}>
                    {task.serviceName}
                  </Link>
                </Typography>
              </Box>
              <Box sx={{ mb: 1 }}>
                <Typography variant="caption" color="text.secondary">
                  Node
                </Typography>
                <Typography variant="body1">
                  <Link component={RouterLink} to={`/nodes/${task.nodeId}`}>
                    {task.nodeName}
                  </Link>
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
