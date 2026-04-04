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
import { createSecret } from "../../api/secrets";

export default function SecretCreate() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [data, setData] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await createSecret({
        secretName: name,
        data: btoa(data),
      });
      navigate("/secrets");
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to create secret");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Create Secret
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
              required
            />
            <TextField
              fullWidth
              label="Data"
              value={data}
              onChange={(e) => setData(e.target.value)}
              margin="normal"
              multiline
              minRows={6}
              required
              helperText="Data will be base64 encoded before submission"
              sx={{
                "& .MuiInputBase-input": {
                  fontFamily: "monospace",
                  fontSize: "0.875rem",
                },
              }}
            />
            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={loading || !name || !data}
              >
                {loading ? <CircularProgress size={24} /> : "Create"}
              </Button>
              <Button variant="outlined" onClick={() => navigate("/secrets")}>
                Cancel
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
