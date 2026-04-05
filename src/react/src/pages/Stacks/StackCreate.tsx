import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  TextField,
  Typography,
} from "@mui/material";
import { deployStack } from "../../api/stacks";
import YamlEditor from "../../components/YamlEditor";

export default function StackCreate() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [compose, setCompose] = useState("");
  const [error, setError] = useState("");
  const [details, setDetails] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const handleDeploy = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setDetails([]);
    setLoading(true);
    try {
      const result = await deployStack(name, compose);
      setDetails(result.details ?? []);
      // Check if any failures
      const failures = (result.details ?? []).filter((d: string) => d.startsWith("Failed"));
      if (failures.length === 0) {
        navigate("/stacks");
      }
      // If there were failures, stay on page and show details
    } catch (err: any) {
      setError(err.response?.data?.error ?? err.response?.data?.message ?? "Failed to deploy stack");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Create Stack
      </Typography>
      <Card sx={{ maxWidth: 800 }}>
        <CardContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          {details.length > 0 && (
            <Alert severity={details.some(d => d.startsWith("Failed")) ? "warning" : "success"} sx={{ mb: 2 }}>
              {details.map((d, i) => <div key={i}>{d}</div>)}
            </Alert>
          )}
          <form onSubmit={handleDeploy}>
            <TextField
              fullWidth
              label="Stack Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              margin="normal"
              required
            />
            <Typography variant="subtitle2" sx={{ mt: 2, mb: 1 }}>
              Compose File
            </Typography>
            <YamlEditor
              value={compose}
              onChange={setCompose}
              placeholder="version: '3.7'&#10;services:&#10;  app:&#10;    image: my-app:latest"
            />
            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={loading || !name || !compose}
              >
                {loading ? <CircularProgress size={24} /> : "Deploy"}
              </Button>
              <Button variant="outlined" onClick={() => navigate("/stacks")}>
                Cancel
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
