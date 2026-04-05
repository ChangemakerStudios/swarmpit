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
import { getUser, updateUser, type User } from "../../api/users";

export default function UserEdit() {
  const { username } = useParams<{ username: string }>();
  const navigate = useNavigate();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [role, setRole] = useState("user");
  const [email, setEmail] = useState("");

  useEffect(() => {
    if (!username) return;
    getUser(username)
      .then((u) => {
        setUser(u);
        setRole(u.role ?? "user");
        setEmail(u.email ?? "");
      })
      .finally(() => setLoading(false));
  }, [username]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!username) return;
    setError("");

    if (password && password !== confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    setSaving(true);
    try {
      await updateUser(username, {
        role,
        email: email || undefined,
        ...(password ? { password } : {}),
      });
      navigate(`/users/${username}`);
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to update user");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LinearProgress />;
  if (!user) return <Typography>User not found</Typography>;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Edit User: {user.username}
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
              label="Username"
              value={user.username}
              margin="normal"
              disabled
            />
            <TextField
              fullWidth
              label="New Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
            <TextField
              fullWidth
              label="Confirm Password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
              error={!!password && !!confirmPassword && password !== confirmPassword}
              helperText={
                !!password && !!confirmPassword && password !== confirmPassword
                  ? "Passwords do not match"
                  : ""
              }
            />
            <TextField
              select
              fullWidth
              label="Role"
              value={role}
              onChange={(e) => setRole(e.target.value)}
              margin="normal"
            >
              <MenuItem value="admin">Admin</MenuItem>
              <MenuItem value="user">User</MenuItem>
            </TextField>
            <TextField
              fullWidth
              label="Email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              margin="normal"
            />

            <Box sx={{ mt: 2, display: "flex", gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                disabled={saving}
              >
                {saving ? <CircularProgress size={24} /> : "Save"}
              </Button>
              <Button
                variant="outlined"
                onClick={() => navigate(`/users/${username}`)}
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
