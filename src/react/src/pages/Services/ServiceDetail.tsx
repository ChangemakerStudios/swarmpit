import { useEffect, useState } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box,
  Card,
  CardContent,
  Chip,
  Grid,
  LinearProgress,
  Typography,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Link,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import ReplayIcon from "@mui/icons-material/Replay";
import UndoIcon from "@mui/icons-material/Undo";
import StopIcon from "@mui/icons-material/Stop";
import SubjectIcon from "@mui/icons-material/Subject";
import {
  getService,
  getServiceTasks,
  deleteService,
  redeployService,
  rollbackService,
  stopService,
  type SwarmService,
  type SwarmTask,
} from "../../api/services";
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

function taskStateColor(state: string): "success" | "error" | "warning" | "default" {
  if (state === "running") return "success";
  if (state === "failed" || state === "rejected") return "error";
  if (state === "pending" || state === "preparing" || state === "starting") return "warning";
  return "default";
}

export default function ServiceDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [service, setService] = useState<SwarmService | null>(null);
  const [tasks, setTasks] = useState<SwarmTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [redeployOpen, setRedeployOpen] = useState(false);
  const [rollbackOpen, setRollbackOpen] = useState(false);
  const [stopOpen, setStopOpen] = useState(false);
  const [redeployTag, setRedeployTag] = useState("");

  useEffect(() => {
    if (!id) return;
    Promise.all([getService(id), getServiceTasks(id)])
      .then(([svc, t]) => {
        setService(svc);
        setTasks(t);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleDelete = async () => {
    if (!id) return;
    await deleteService(id);
    navigate("/services");
  };

  const handleRedeploy = async () => {
    if (!id) return;
    await redeployService(id, redeployTag || undefined);
    setRedeployOpen(false);
    setRedeployTag("");
    // refresh
    const [svc, t] = await Promise.all([getService(id), getServiceTasks(id)]);
    setService(svc);
    setTasks(t);
  };

  const handleRollback = async () => {
    if (!id) return;
    await rollbackService(id);
    setRollbackOpen(false);
    const [svc, t] = await Promise.all([getService(id), getServiceTasks(id)]);
    setService(svc);
    setTasks(t);
  };

  const handleStop = async () => {
    if (!id) return;
    await stopService(id);
    setStopOpen(false);
    const [svc, t] = await Promise.all([getService(id), getServiceTasks(id)]);
    setService(svc);
    setTasks(t);
  };

  if (loading) return <LinearProgress />;
  if (!service) return <Typography>Service not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{service.serviceName}</Typography>
        <Chip
          label={service.state ?? "unknown"}
          color={service.state === "running" ? "success" : "default"}
        />
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={() => navigate(`/services/${id}/edit`)}
        >
          Edit
        </Button>
        <Button
          variant="outlined"
          startIcon={<ReplayIcon />}
          onClick={() => setRedeployOpen(true)}
        >
          Redeploy
        </Button>
        <Button
          variant="outlined"
          startIcon={<UndoIcon />}
          onClick={() => setRollbackOpen(true)}
        >
          Rollback
        </Button>
        {service.mode === "replicated" && (
          <Button
            variant="outlined"
            startIcon={<StopIcon />}
            onClick={() => setStopOpen(true)}
          >
            Stop
          </Button>
        )}
        <Button
          variant="outlined"
          startIcon={<SubjectIcon />}
          onClick={() => navigate(`/services/${id}/logs`)}
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
              <InfoItem label="ID" value={service.id} />
              <InfoItem label="Image" value={service.repository.image} />
              <InfoItem label="Image Digest" value={service.repository.imageDigest} />
              <InfoItem label="Mode" value={service.mode} />
              <InfoItem
                label="Replicas"
                value={`${service.status?.tasks?.running ?? 0}/${service.status?.tasks?.total ?? 0}`}
              />
              {service.stack && <InfoItem label="Stack" value={service.stack} />}
              <InfoItem label="Created" value={formatDate(service.createdAt)} />
              <InfoItem label="Last Update" value={formatDate(service.updatedAt)} />
            </CardContent>
          </Card>
        </Grid>

        {service.ports.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Ports
                </Typography>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Container Port</TableCell>
                        <TableCell>Host Port</TableCell>
                        <TableCell>Protocol</TableCell>
                        <TableCell>Mode</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {service.ports.map((p, i) => (
                        <TableRow key={i}>
                          <TableCell>{p.containerPort}</TableCell>
                          <TableCell>{p.hostPort}</TableCell>
                          <TableCell>{p.protocol}</TableCell>
                          <TableCell>{p.mode}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.networks.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Networks
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {service.networks.map((n) => (
                    <Chip
                      key={n.id}
                      label={n.networkName}
                      size="small"
                      variant="outlined"
                      component={RouterLink}
                      to={`/networks/${n.id}`}
                      clickable
                    />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.mounts && service.mounts.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Mounts
                </Typography>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Type</TableCell>
                        <TableCell>Source</TableCell>
                        <TableCell>Target</TableCell>
                        <TableCell>Read Only</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {service.mounts.map((m, i) => (
                        <TableRow key={i}>
                          <TableCell>{m.type}</TableCell>
                          <TableCell>{m.source}</TableCell>
                          <TableCell>{m.target}</TableCell>
                          <TableCell>{m.readOnly ? "Yes" : "No"}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.environment && service.environment.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Environment Variables
                </Typography>
                <TableContainer>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Name</TableCell>
                        <TableCell>Value</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {service.environment.map((e, i) => (
                        <TableRow key={i}>
                          <TableCell>{e.name}</TableCell>
                          <TableCell>{e.value}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.labels && service.labels.length > 0 && (
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Labels
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {service.labels.map((l) => (
                    <Chip key={l.name} label={`${l.name}=${l.value}`} size="small" />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.secrets && service.secrets.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Secrets
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {service.secrets.map((s) => (
                    <Chip
                      key={s.id}
                      label={s.secretName}
                      size="small"
                      variant="outlined"
                      component={RouterLink}
                      to={`/secrets/${s.id}`}
                      clickable
                    />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        {service.configs && service.configs.length > 0 && (
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Configs
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {service.configs.map((c) => (
                    <Chip
                      key={c.id}
                      label={c.configName}
                      size="small"
                      variant="outlined"
                      component={RouterLink}
                      to={`/configs/${c.id}`}
                      clickable
                    />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Tasks
              </Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Task</TableCell>
                      <TableCell>Node</TableCell>
                      <TableCell>State</TableCell>
                      <TableCell>Desired State</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tasks.map((t) => (
                      <TableRow
                        key={t.id}
                        hover
                        sx={{ cursor: "pointer" }}
                        onClick={() => navigate(`/tasks/${t.id}`)}
                      >
                        <TableCell>{t.taskName}</TableCell>
                        <TableCell>
                          <Link
                            component={RouterLink}
                            to={`/nodes/${t.nodeId}`}
                            onClick={(e) => e.stopPropagation()}
                          >
                            {t.nodeName}
                          </Link>
                        </TableCell>
                        <TableCell>
                          <Chip
                            label={t.state}
                            size="small"
                            color={taskStateColor(t.state)}
                          />
                        </TableCell>
                        <TableCell>{t.desiredState}</TableCell>
                      </TableRow>
                    ))}
                    {tasks.length === 0 && (
                      <TableRow>
                        <TableCell colSpan={4} align="center">
                          No tasks
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Service</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete service{" "}
            <strong>{service.serviceName}</strong>? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={redeployOpen} onClose={() => setRedeployOpen(false)}>
        <DialogTitle>Redeploy Service</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Redeploy <strong>{service.serviceName}</strong>? Optionally specify a new image tag.
          </DialogContentText>
          <TextField
            label="Tag (optional)"
            value={redeployTag}
            onChange={(e) => setRedeployTag(e.target.value)}
            fullWidth
            placeholder={service.repository.tag}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRedeployOpen(false)}>Cancel</Button>
          <Button onClick={handleRedeploy} variant="contained">
            Redeploy
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={rollbackOpen} onClose={() => setRollbackOpen(false)}>
        <DialogTitle>Rollback Service</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to rollback <strong>{service.serviceName}</strong> to its previous version?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRollbackOpen(false)}>Cancel</Button>
          <Button onClick={handleRollback} variant="contained">
            Rollback
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={stopOpen} onClose={() => setStopOpen(false)}>
        <DialogTitle>Stop Service</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to stop <strong>{service.serviceName}</strong>? This will scale replicas to 0.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setStopOpen(false)}>Cancel</Button>
          <Button onClick={handleStop} color="warning" variant="contained">
            Stop
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
