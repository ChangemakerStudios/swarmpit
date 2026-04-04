import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Card,
  CardContent,
  TextField,
  Typography,
  Alert,
  CircularProgress,
} from "@mui/material";
import { createVolume } from "../../api/volumes";

export default function VolumeCreate() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [driver, setDriver] = useState("local");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await createVolume({
        volumeName: name || undefined,
        driver,
      });
      navigate("/volumes");
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to create volume");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Create Volume
      </Typography>
      <Card sx={{ maxWidth: 600 }}>
        <CardContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          <form onSubmit={handleSubmit}>
            <TextField
              fullWidth
              label="Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              margin="normal"
              helperText="Leave empty for auto-generated name"
            />
            <TextField
              fullWidth
              label="Driver"
              value={driver}
              onChange={(e) => setDriver(e.target.value)}
              margin="normal"
            />
            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button type="submit" variant="contained" disabled={loading}>
                {loading ? <CircularProgress size={24} /> : "Create"}
              </Button>
              <Button variant="outlined" onClick={() => navigate("/volumes")}>
                Cancel
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
