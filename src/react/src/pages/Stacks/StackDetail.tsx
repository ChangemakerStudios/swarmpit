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
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import DescriptionIcon from "@mui/icons-material/Description";
import DataTable from "../../components/DataTable";
import {
  getStack,
  getStackFile,
  getStackServices,
  deleteStack,
  type SwarmStack,
  type StackFile,
} from "../../api/stacks";
import YamlEditor from "../../components/YamlEditor";

function InfoItem({
  label,
  value,
}: {
  label: string;
  value?: string | number | null;
}) {
  return (
    <Box sx={{ mb: 1 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value ?? "-"}</Typography>
    </Box>
  );
}

function StatCard({ label, count }: { label: string; count: number }) {
  return (
    <Card>
      <CardContent sx={{ textAlign: "center" }}>
        <Typography variant="h4">{count}</Typography>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
      </CardContent>
    </Card>
  );
}

function stateColor(state: string): "success" | "default" {
  return state === "deployed" ? "success" : "default";
}

export default function StackDetail() {
  const { name } = useParams<{ name: string }>();
  const navigate = useNavigate();
  const [stack, setStack] = useState<SwarmStack | null>(null);
  const [services, setServices] = useState<any[]>([]);
  const [stackFile, setStackFile] = useState<StackFile | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [composeOpen, setComposeOpen] = useState(false);

  useEffect(() => {
    if (!name) return;
    Promise.all([getStack(name), getStackServices(name), getStackFile(name)])
      .then(([s, svc, sf]) => {
        setStack(s);
        setServices(svc);
        setStackFile(sf);
      })
      .catch(() => {
        // stackFile might not exist, still load the rest
        Promise.all([getStack(name), getStackServices(name)])
          .then(([s, svc]) => {
            setStack(s);
            setServices(svc);
          });
      })
      .finally(() => setLoading(false));
  }, [name]);

  const handleDelete = async () => {
    if (!name) return;
    await deleteStack(name);
    navigate("/stacks");
  };

  if (loading) return <LinearProgress />;
  if (!stack) return <Typography>Stack not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{stack.stackName}</Typography>
        <Chip
          label={stack.state}
          color={stateColor(stack.state)}
        />
        <Box sx={{ flexGrow: 1 }} />
        {stackFile?.spec?.compose && (
          <Button
            variant="outlined"
            startIcon={<DescriptionIcon />}
            onClick={() => setComposeOpen(true)}
          >
            View Compose
          </Button>
        )}
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={() => navigate(`/stacks/${name}/edit`)}
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

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 6, sm: 4, md: 2.4 }}>
          <StatCard label="Services" count={stack.stats?.services ?? 0} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2.4 }}>
          <StatCard label="Networks" count={stack.stats?.networks ?? 0} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2.4 }}>
          <StatCard label="Volumes" count={stack.stats?.volumes ?? 0} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2.4 }}>
          <StatCard label="Configs" count={stack.stats?.configs ?? 0} />
        </Grid>
        <Grid size={{ xs: 6, sm: 4, md: 2.4 }}>
          <StatCard label="Secrets" count={stack.stats?.secrets ?? 0} />
        </Grid>
      </Grid>

      <Box>
          <Typography variant="h6" gutterBottom>
            Services
          </Typography>
          <DataTable
            columns={[
              { id: "serviceName", label: "Name" },
              { id: "image", label: "Image", render: (row: any) => row.repository?.image ?? "-" },
              { id: "mode", label: "Mode" },
              { id: "replicas", label: "Replicas", render: (row: any) => row.status?.tasks ? `${row.status.tasks.running}/${row.status.tasks.total}` : "-" },
            ]}
            rows={services}
            onRowClick={(row: any) => navigate(`/services/${row.id}`)}
            searchFields={["serviceName"]}
          />
      </Box>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete Stack</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete stack{" "}
            <strong>{stack.stackName}</strong>? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {/* Compose File Dialog */}
      <Dialog
        open={composeOpen}
        onClose={() => setComposeOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Compose File</DialogTitle>
        <DialogContent>
          <YamlEditor
            value={stackFile?.spec?.compose ?? ""}
            onChange={() => {}}
            readOnly
            minHeight="300px"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setComposeOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
