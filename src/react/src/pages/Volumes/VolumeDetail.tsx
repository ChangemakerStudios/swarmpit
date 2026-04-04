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
  Link,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  getVolume,
  getVolumeServices,
  deleteVolume,
  type SwarmVolume,
} from "../../api/volumes";

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

export default function VolumeDetail() {
  const { name } = useParams<{ name: string }>();
  const navigate = useNavigate();
  const [volume, setVolume] = useState<SwarmVolume | null>(null);
  const [services, setServices] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);

  useEffect(() => {
    if (!name) return;
    Promise.all([getVolume(name), getVolumeServices(name)])
      .then(([vol, svcs]) => {
        setVolume(vol);
        setServices(svcs);
      })
      .finally(() => setLoading(false));
  }, [name]);

  const handleDelete = async () => {
    if (!name) return;
    await deleteVolume(name);
    navigate("/volumes");
  };

  if (loading) return <LinearProgress />;
  if (!volume) return <Typography>Volume not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{volume.volumeName}</Typography>
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
              <InfoItem label="Driver" value={volume.driver} />
              <InfoItem label="Scope" value={volume.scope} />
              <InfoItem label="Mountpoint" value={volume.mountpoint} />
              {volume.stack && (
                <Box sx={{ mb: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    Stack
                  </Typography>
                  <Typography variant="body1">
                    <Link component={RouterLink} to={`/stacks/${volume.stack}`}>
                      {volume.stack}
                    </Link>
                  </Typography>
                </Box>
              )}
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
      </Grid>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Volume</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete volume{" "}
            <strong>{volume.volumeName}</strong>? This action cannot be undone.
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
