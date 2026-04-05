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
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import BlockIcon from "@mui/icons-material/Block";
import { getUser, deleteUser, generateApiToken, revokeApiToken, type User } from "../../api/users";
import { useAuthStore } from "../../store/authStore";

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

export default function UserDetail() {
  const { username } = useParams<{ username: string }>();
  const navigate = useNavigate();
  const currentUsername = useAuthStore((s) => s.username);
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [tokenDialogOpen, setTokenDialogOpen] = useState(false);
  const [generatedToken, setGeneratedToken] = useState("");

  const isSelf = currentUsername === username;

  useEffect(() => {
    if (!username) return;
    getUser(username)
      .then(setUser)
      .finally(() => setLoading(false));
  }, [username]);

  const handleDelete = async () => {
    if (!username) return;
    await deleteUser(username);
    navigate("/users");
  };

  const handleGenerateToken = async () => {
    if (!username) return;
    try {
      const data = await generateApiToken(username);
      setGeneratedToken(data.token ?? JSON.stringify(data));
      setTokenDialogOpen(true);
    } catch {
      // error handled silently
    }
  };

  const handleRevokeToken = async () => {
    if (!username) return;
    await revokeApiToken(username);
  };

  if (loading) return <LinearProgress />;
  if (!user) return <Typography>User not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{user.username}</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={() => navigate(`/users/${username}/edit`)}
        >
          Edit
        </Button>
        <Button
          variant="outlined"
          startIcon={<VpnKeyIcon />}
          onClick={handleGenerateToken}
        >
          Generate API Token
        </Button>
        <Button
          variant="outlined"
          startIcon={<BlockIcon />}
          onClick={handleRevokeToken}
        >
          Revoke API Token
        </Button>
        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteIcon />}
          onClick={() => setDeleteOpen(true)}
          disabled={isSelf}
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
              <InfoItem label="Username" value={user.username} />
              <InfoItem label="Role" value={user.role} />
              <InfoItem label="Email" value={user.email} />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Delete confirmation dialog */}
      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)}>
        <DialogTitle>Delete User</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete user{" "}
            <strong>{user.username}</strong>? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {/* Generated token dialog */}
      <Dialog open={tokenDialogOpen} onClose={() => setTokenDialogOpen(false)}>
        <DialogTitle>API Token Generated</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ mb: 2 }}>
            Copy this token now. It will not be shown again.
          </DialogContentText>
          <Typography
            variant="body2"
            sx={{
              fontFamily: "monospace",
              wordBreak: "break-all",
              p: 2,
              bgcolor: "grey.100",
              borderRadius: 1,
            }}
          >
            {generatedToken}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setTokenDialogOpen(false)}>Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
