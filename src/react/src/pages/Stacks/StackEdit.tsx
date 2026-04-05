import { useEffect, useState, type FormEvent } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  LinearProgress,
  MenuItem,
  TextField,
  Typography,
} from "@mui/material";
import { getStackFile, deployStack, type StackFile } from "../../api/stacks";
import YamlEditor from "../../components/YamlEditor";

export default function StackEdit() {
  const { name } = useParams<{ name: string }>();
  const navigate = useNavigate();
  const [stackFile, setStackFile] = useState<StackFile | null>(null);
  const [compose, setCompose] = useState("");
  const [version, setVersion] = useState<"current" | "previous">("current");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!name) return;
    getStackFile(name)
      .then((sf) => {
        setStackFile(sf);
        setCompose(sf.spec?.compose ?? "");
      })
      .finally(() => setLoading(false));
  }, [name]);

  useEffect(() => {
    if (!stackFile) return;
    if (version === "current") {
      setCompose(stackFile.spec?.compose ?? "");
    } else {
      setCompose(stackFile.previousSpec?.compose ?? "");
    }
  }, [version, stackFile]);

  const handleDeploy = async (e: FormEvent) => {
    e.preventDefault();
    if (!name) return;
    setError("");
    setSaving(true);
    try {
      await deployStack(name, compose);
      navigate(`/stacks/${name}`);
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to deploy stack");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LinearProgress />;

  const hasPrevious = !!stackFile?.previousSpec?.compose;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Edit Stack: {name}
      </Typography>
      <Card sx={{ maxWidth: 800 }}>
        <CardContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          <form onSubmit={handleDeploy}>
            {hasPrevious && (
              <TextField
                select
                fullWidth
                label="Version"
                value={version}
                onChange={(e) =>
                  setVersion(e.target.value as "current" | "previous")
                }
                margin="normal"
              >
                <MenuItem value="current">Current</MenuItem>
                <MenuItem value="previous">Previous</MenuItem>
              </TextField>
            )}
            <Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
              Compose File
            </Typography>
            <YamlEditor
              value={compose}
              onChange={setCompose}
            />
            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={saving || !compose}
              >
                {saving ? <CircularProgress size={24} /> : "Deploy"}
              </Button>
              <Button
                variant="outlined"
                onClick={() => navigate(`/stacks/${name}`)}
              >
                Cancel
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
