import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormControlLabel,
  TextField,
  Typography,
  Alert,
  CircularProgress,
} from "@mui/material";
import { createNetwork } from "../../api/networks";

export default function NetworkCreate() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [driver, setDriver] = useState("overlay");
  const [internal, setInternal] = useState(false);
  const [attachable, setAttachable] = useState(false);
  const [ingress, setIngress] = useState(false);
  const [enableIPv6, setEnableIPv6] = useState(false);
  const [subnet, setSubnet] = useState("");
  const [gateway, setGateway] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await createNetwork({
        networkName: name,
        driver,
        internal,
        attachable,
        ingress,
        enableIPv6,
        ipam: subnet || gateway ? { subnet, gateway } : undefined,
      });
      navigate("/networks");
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to create network");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Create Network
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
              label="Driver"
              value={driver}
              onChange={(e) => setDriver(e.target.value)}
              margin="normal"
            />
            <Box sx={{ mt: 2 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={internal}
                    onChange={(e) => setInternal(e.target.checked)}
                  />
                }
                label="Internal"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={attachable}
                    onChange={(e) => setAttachable(e.target.checked)}
                  />
                }
                label="Attachable"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={ingress}
                    onChange={(e) => setIngress(e.target.checked)}
                  />
                }
                label="Ingress"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={enableIPv6}
                    onChange={(e) => setEnableIPv6(e.target.checked)}
                  />
                }
                label="Enable IPv6"
              />
            </Box>
            <TextField
              fullWidth
              label="IPAM Subnet"
              value={subnet}
              onChange={(e) => setSubnet(e.target.value)}
              margin="normal"
              placeholder="e.g. 10.0.0.0/24"
            />
            <TextField
              fullWidth
              label="IPAM Gateway"
              value={gateway}
              onChange={(e) => setGateway(e.target.value)}
              margin="normal"
              placeholder="e.g. 10.0.0.1"
            />
            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={loading || !name}
              >
                {loading ? <CircularProgress size={24} /> : "Create"}
              </Button>
              <Button variant="outlined" onClick={() => navigate("/networks")}>
                Cancel
              </Button>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
