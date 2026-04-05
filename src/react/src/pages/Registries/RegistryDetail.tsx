import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  LinearProgress,
  Typography,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import { getRegistry, deleteRegistry, type Registry } from "../../api/registries";

function InfoItem({
  label,
  value,
}: {
  label: string;
  value?: string | number | boolean | null;
}) {
  const display =
    typeof value === "boolean" ? (value ? "Yes" : "No") : (value ?? "-");
  return (
    <Box sx={{ mb: 1 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{display}</Typography>
    </Box>
  );
}

export default function RegistryDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [registry, setRegistry] = useState<Registry | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);

  useEffect(() => {
    if (!id) return;
    getRegistry(id)
      .then(setRegistry)
      .finally(() => setLoading(false));
  }, [id]);

  const handleDelete = async () => {
    if (!id) return;
    await deleteRegistry(id);
    navigate("/registries");
  };

  if (loading) return <LinearProgress />;
  if (!registry) return <Typography>Registry not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{registry.name}</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={() => navigate(`/registries/${id}/edit`)}
        >
          Edit
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
                Details
              </Typography>
              <InfoItem label="Name" value={registry.name} />
              <InfoItem
                label="Type"
                value={
                  registry.registryType.charAt(0).toUpperCase() +
                  registry.registryType.slice(1)
                }
              />
              <InfoItem label="URL" value={registry.url} />
              {registry.username && (
                <InfoItem label="Username" value={registry.username} />
              )}
              <InfoItem label="Public" value={registry.public} />
              <InfoItem label="Owner" value={registry.owner} />
              {registry.region && (
                <InfoItem label="Region" value={registry.region} />
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Registry</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete registry{" "}
            <strong>{registry.name}</strong>? This action cannot be undone.
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
