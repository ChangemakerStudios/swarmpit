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
  Paper,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  getConfig,
  getConfigServices,
  deleteConfig,
  type SwarmConfig,
} from "../../api/configs";
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

function decodeData(data?: string): string {
  if (!data) return "";
  try {
    return atob(data);
  } catch {
    return data;
  }
}

export default function ConfigDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [config, setConfig] = useState<SwarmConfig | null>(null);
  const [services, setServices] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);

  useEffect(() => {
    if (!id) return;
    Promise.all([getConfig(id), getConfigServices(id)])
      .then(([cfg, svcs]) => {
        setConfig(cfg);
        setServices(svcs);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleDelete = async () => {
    if (!id) return;
    await deleteConfig(id);
    navigate("/configs");
  };

  if (loading) return <LinearProgress />;
  if (!config) return <Typography>Config not found</Typography>;

  const decoded = decodeData(config.data);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{config.configName}</Typography>
        <Box sx={{ flexGrow: 1 }} />
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
              <InfoItem label="ID" value={config.id} />
              <InfoItem label="Created" value={formatDate(config.createdAt)} />
              <InfoItem label="Last Update" value={formatDate(config.updatedAt)} />
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Linked Services
              </Typography>
              {services.length === 0 ? (
                <Typography color="text.secondary">No linked services</Typography>
              ) : (
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {services.map((svc: any) => (
                    <Chip
                      key={svc.id}
                      label={svc.serviceName}
                      size="small"
                      variant="outlined"
                      component={RouterLink}
                      to={`/services/${svc.id}`}
                      clickable
                    />
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        {decoded && (
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Data
                </Typography>
                <Paper
                  variant="outlined"
                  sx={{
                    p: 2,
                    fontFamily: "monospace",
                    fontSize: "0.875rem",
                    whiteSpace: "pre-wrap",
                    wordBreak: "break-all",
                    maxHeight: 400,
                    overflow: "auto",
                    bgcolor: "action.hover",
                  }}
                >
                  {decoded}
                </Paper>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Config</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete config{" "}
            <strong>{config.configName}</strong>? This action cannot be undone.
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
