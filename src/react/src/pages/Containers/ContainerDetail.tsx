import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  LinearProgress,
  Typography,
} from "@mui/material";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import StopIcon from "@mui/icons-material/Stop";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import SubjectIcon from "@mui/icons-material/Subject";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  getContainer,
  startContainer,
  stopContainer,
  restartContainer,
  removeContainer,
  type ContainerDetail as ContainerDetailType,
} from "../../api/containers";
import { formatDate } from "../../utils/time";
import DataTable from "../../components/DataTable";

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

function stateColor(
  state: string
): "success" | "warning" | "error" | "default" | "info" {
  switch (state) {
    case "running":
      return "success";
    case "exited":
      return "default";
    case "paused":
      return "warning";
    case "created":
    case "restarting":
      return "info";
    case "dead":
    case "removing":
      return "error";
    default:
      return "default";
  }
}

export default function ContainerDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [container, setContainer] = useState<ContainerDetailType | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const fetchContainer = async () => {
    if (!id) return;
    try {
      const data = await getContainer(id);
      setContainer(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchContainer();
  }, [id]);

  const handleStart = async () => {
    if (!id) return;
    await startContainer(id);
    await fetchContainer();
  };

  const handleStop = async () => {
    if (!id) return;
    await stopContainer(id);
    await fetchContainer();
  };

  const handleRestart = async () => {
    if (!id) return;
    await restartContainer(id);
    await fetchContainer();
  };

  const handleDelete = async () => {
    if (!id) return;
    await removeContainer(id, true);
    navigate("/containers");
  };

  if (loading) return <LinearProgress />;
  if (!container) return <Typography>Container not found</Typography>;

  const isRunning = container.state === "running";

  const envRows = (container.env ?? []).map((e) => {
    const idx = e.indexOf("=");
    return idx >= 0
      ? { name: e.substring(0, idx), value: e.substring(idx + 1) }
      : { name: e, value: "" };
  });

  const labelEntries = Object.entries(container.labels ?? {});

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{container.name}</Typography>
        <Chip
          label={container.state}
          color={stateColor(container.state)}
        />
        <Box sx={{ flexGrow: 1 }} />
        {!isRunning && (
          <Button
            variant="outlined"
            startIcon={<PlayArrowIcon />}
            onClick={handleStart}
          >
            Start
          </Button>
        )}
        {isRunning && (
          <Button
            variant="outlined"
            startIcon={<StopIcon />}
            onClick={handleStop}
          >
            Stop
          </Button>
        )}
        {isRunning && (
          <Button
            variant="outlined"
            startIcon={<RestartAltIcon />}
            onClick={handleRestart}
          >
            Restart
          </Button>
        )}
        <Button
          variant="outlined"
          startIcon={<SubjectIcon />}
          onClick={() => navigate(`/containers/${id}/logs`)}
        >
          Logs
        </Button>
        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteIcon />}
          onClick={() => setDeleteOpen(true)}
        >
          Delete
        </Button>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                General
              </Typography>
              <InfoItem label="ID" value={container.id} />
              <InfoItem label="Image" value={container.image} />
              <InfoItem label="Status" value={container.status} />
              <InfoItem label="Created" value={formatDate(container.created)} />
              <InfoItem label="Command" value={container.command} />
              <InfoItem label="Restart Policy" value={container.restartPolicy} />
              {container.hostname && (
                <InfoItem label="Hostname" value={container.hostname} />
              )}
              {container.workingDir && (
                <InfoItem label="Working Dir" value={container.workingDir} />
              )}
              {container.user && (
                <InfoItem label="User" value={container.user} />
              )}
            </CardContent>
          </Card>
        </Grid>

        {container.ports && container.ports.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Typography variant="h6" gutterBottom>
              Ports
            </Typography>
            <DataTable
              columns={[
                { id: "privatePort", label: "Container Port" },
                {
                  id: "publicPort",
                  label: "Host Port",
                  render: (row: any) => row.publicPort ?? "-",
                },
                { id: "type", label: "Protocol" },
                {
                  id: "ip",
                  label: "IP",
                  render: (row: any) => row.ip ?? "-",
                },
              ]}
              rows={container.ports}
            />
          </Grid>
        )}

        {envRows.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Typography variant="h6" gutterBottom>
              Environment Variables
            </Typography>
            <DataTable
              columns={[
                { id: "name", label: "Name" },
                { id: "value", label: "Value" },
              ]}
              rows={envRows}
            />
          </Grid>
        )}

        {container.mounts && container.mounts.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Typography variant="h6" gutterBottom>
              Mounts
            </Typography>
            <DataTable
              columns={[
                { id: "type", label: "Type" },
                { id: "source", label: "Source" },
                { id: "destination", label: "Destination" },
                {
                  id: "readOnly",
                  label: "Read Only",
                  render: (row: any) => (row.readOnly ? "Yes" : "No"),
                },
              ]}
              rows={container.mounts}
            />
          </Grid>
        )}

        {container.networks && container.networks.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Networks
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {container.networks.map((n) => (
                    <Chip key={n} label={n} size="small" variant="outlined" />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {labelEntries.length > 0 && (
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Labels
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {labelEntries.map(([key, value]) => (
                    <Chip key={key} label={`${key}=${value}`} size="small" />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Container</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete container{" "}
            <strong>{container.name}</strong>? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
