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
  MenuItem,
  Snackbar,
  Alert,
  TextField,
  Typography,
} from "@mui/material";
import SaveIcon from "@mui/icons-material/Save";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import DescriptionIcon from "@mui/icons-material/Description";
import DataTable from "../../components/DataTable";
import {
  getStack,
  getStackFile,
  getStackCompose,
  getStackServices,
  saveStackFile,
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
  const [composeVersion, setComposeVersion] = useState<"current" | "previous" | "generated">("current");
  const [generatedCompose, setGeneratedCompose] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [snackbar, setSnackbar] = useState<string | null>(null);

  useEffect(() => {
    if (!name) return;

    const loadData = async () => {
      try {
        const [s, svc] = await Promise.all([getStack(name), getStackServices(name)]);
        setStack(s);
        setServices(svc);

        // Try loading stackfile (may not exist)
        try {
          const sf = await getStackFile(name);
          setStackFile(sf);
        } catch {
          // No saved stackfile — will use generated
        }
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [name]);

  const handleOpenCompose = async () => {
    // If no saved stackfile, generate from live state
    if (!stackFile?.spec?.compose && name) {
      try {
        const yaml = await getStackCompose(name);
        setGeneratedCompose(yaml);
        setComposeVersion("generated");
      } catch {
        setGeneratedCompose("# Failed to generate compose from live state");
        setComposeVersion("generated");
      }
    } else {
      setComposeVersion("current");
    }
    setComposeOpen(true);
  };

  const handleSaveCompose = async () => {
    if (!name) return;
    const yaml = getComposeContent();
    if (!yaml) return;

    setSaving(true);
    try {
      await saveStackFile(name, yaml);
      const sf = await getStackFile(name);
      setStackFile(sf);
      setSnackbar("Stackfile saved");
    } catch {
      setSnackbar("Failed to save stackfile");
    } finally {
      setSaving(false);
    }
  };

  // Fetch generated compose on demand
  useEffect(() => {
    if (composeVersion === "generated" && !generatedCompose && name) {
      getStackCompose(name)
        .then(setGeneratedCompose)
        .catch(() => setGeneratedCompose("# Failed to generate compose"));
    }
  }, [composeVersion, generatedCompose, name]);

  const getComposeContent = (): string => {
    if (composeVersion === "previous") return stackFile?.previousSpec?.compose ?? "";
    if (composeVersion === "generated") return generatedCompose ?? "";
    return stackFile?.spec?.compose ?? "";
  };

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
        <Button
          variant="outlined"
          startIcon={<DescriptionIcon />}
          onClick={handleOpenCompose}
        >
          View Compose
        </Button>
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
        <DialogTitle sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          Compose File
          <TextField
            select
            size="small"
            value={composeVersion}
            onChange={(e) => setComposeVersion(e.target.value as any)}
            sx={{ ml: "auto", minWidth: 160 }}
          >
            {stackFile?.spec?.compose && (
              <MenuItem value="current">Current</MenuItem>
            )}
            {stackFile?.previousSpec?.compose && (
              <MenuItem value="previous">Previous</MenuItem>
            )}
            <MenuItem value="generated">Generated (live)</MenuItem>
          </TextField>
        </DialogTitle>
        <DialogContent>
          {composeVersion === "generated" && !generatedCompose ? (
            <Typography color="text.secondary" sx={{ py: 4, textAlign: "center" }}>
              Loading...
            </Typography>
          ) : (
            <YamlEditor
              value={getComposeContent()}
              onChange={() => {}}
              readOnly
              minHeight="300px"
            />
          )}
        </DialogContent>
        <DialogActions>
          {composeVersion === "generated" && generatedCompose && (
            <Button
              startIcon={<SaveIcon />}
              onClick={handleSaveCompose}
              disabled={saving}
            >
              Save as Stackfile
            </Button>
          )}
          <Button onClick={() => setComposeOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={!!snackbar}
        autoHideDuration={3000}
        onClose={() => setSnackbar(null)}
      >
        <Alert severity="success" onClose={() => setSnackbar(null)}>
          {snackbar}
        </Alert>
      </Snackbar>
    </Box>
  );
}
