import { useEffect, useState, type FormEvent } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  LinearProgress,
  MenuItem,
  Switch,
  TextField,
  Typography,
} from "@mui/material";
import {
  getRegistry,
  updateRegistry,
  type Registry,
} from "../../api/registries";

const AWS_REGIONS = [
  "us-east-1",
  "us-east-2",
  "us-west-1",
  "us-west-2",
  "eu-west-1",
  "eu-west-2",
  "eu-west-3",
  "eu-central-1",
  "eu-north-1",
  "ap-southeast-1",
  "ap-southeast-2",
  "ap-northeast-1",
  "ap-northeast-2",
  "ap-south-1",
  "sa-east-1",
  "ca-central-1",
];

const TYPE_LABELS: Record<string, string> = {
  dockerhub: "Docker Hub",
  v2: "Registry V2",
  ecr: "Amazon ECR",
  acr: "Azure ACR",
  gitlab: "GitLab",
  ghcr: "GitHub GHCR",
};

export default function RegistryEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [registry, setRegistry] = useState<Registry | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // Form fields
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [region, setRegion] = useState("us-east-1");
  const [accessKeyId, setAccessKeyId] = useState("");
  const [accessKey, setAccessKey] = useState("");
  const [servicePrincipalId, setServicePrincipalId] = useState("");
  const [token, setToken] = useState("");
  const [selfHosted, setSelfHosted] = useState(false);
  const [gitlabUrl, setGitlabUrl] = useState("");
  const [registryUrl, setRegistryUrl] = useState("");
  const [customApi, setCustomApi] = useState(false);
  const [withAuth, setWithAuth] = useState(false);
  const [isPublic, setIsPublic] = useState(false);

  useEffect(() => {
    if (!id) return;
    getRegistry(id)
      .then((r) => {
        setRegistry(r);
        setName(r.name ?? "");
        setUrl(r.url ?? "");
        setUsername(r.username ?? "");
        setRegion(r.region ?? "us-east-1");
        setAccessKeyId(r.accessKeyId ?? "");
        setServicePrincipalId(r.servicePrincipalId ?? "");
        setSelfHosted(r.selfHosted ?? false);
        setGitlabUrl(r.gitlabUrl ?? "");
        setRegistryUrl(r.registryUrl ?? "");
        setCustomApi(r.customApi ?? false);
        setWithAuth(r.withAuth ?? false);
        setIsPublic(r.public ?? false);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!id || !registry) return;
    setError("");
    setSaving(true);

    try {
      const payload: any = {
        registryType: registry.registryType,
        public: isPublic,
      };

      switch (registry.registryType) {
        case "dockerhub":
          payload.username = username;
          if (password) payload.password = password;
          break;
        case "v2":
          payload.name = name;
          payload.url = url;
          payload.customApi = customApi;
          payload.withAuth = withAuth;
          if (withAuth) {
            payload.username = username;
            if (password) payload.password = password;
          }
          break;
        case "ecr":
          payload.region = region;
          payload.accessKeyId = accessKeyId;
          if (accessKey) payload.accessKey = accessKey;
          break;
        case "acr":
          payload.name = name;
          payload.servicePrincipalId = servicePrincipalId;
          if (password) payload.password = password;
          break;
        case "gitlab":
          payload.username = username;
          if (token) payload.token = token;
          payload.selfHosted = selfHosted;
          if (selfHosted) {
            payload.gitlabUrl = gitlabUrl;
            payload.registryUrl = registryUrl;
          }
          break;
        case "ghcr":
          payload.username = username;
          if (token) payload.token = token;
          break;
      }

      await updateRegistry(id, payload);
      navigate(`/registries/${id}`);
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to update registry");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LinearProgress />;
  if (!registry) return <Typography>Registry not found</Typography>;

  const typeLabel = TYPE_LABELS[registry.registryType] ?? registry.registryType;

  const renderFields = () => {
    switch (registry.registryType) {
      case "dockerhub":
        return (
          <>
            <TextField
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
          </>
        );
      case "v2":
        return (
          <>
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
              label="URL"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              margin="normal"
              required
            />
            <Box sx={{ mt: 1 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={customApi}
                    onChange={(e) => setCustomApi(e.target.checked)}
                  />
                }
                label="Custom API"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={withAuth}
                    onChange={(e) => setWithAuth(e.target.checked)}
                  />
                }
                label="Requires Authentication"
              />
            </Box>
            {withAuth && (
              <>
                <TextField
                  fullWidth
                  label="Username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  margin="normal"
                  required
                />
                <TextField
                  fullWidth
                  label="Password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  margin="normal"
                  placeholder="Leave blank to keep current"
                />
              </>
            )}
          </>
        );
      case "ecr":
        return (
          <>
            <TextField
              select
              fullWidth
              label="Region"
              value={region}
              onChange={(e) => setRegion(e.target.value)}
              margin="normal"
              required
            >
              {AWS_REGIONS.map((r) => (
                <MenuItem key={r} value={r}>
                  {r}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              fullWidth
              label="Access Key ID"
              value={accessKeyId}
              onChange={(e) => setAccessKeyId(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Secret Access Key"
              type="password"
              value={accessKey}
              onChange={(e) => setAccessKey(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
          </>
        );
      case "acr":
        return (
          <>
            <TextField
              fullWidth
              label="Registry Name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Service Principal ID"
              value={servicePrincipalId}
              onChange={(e) => setServicePrincipalId(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Service Principal Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
          </>
        );
      case "gitlab":
        return (
          <>
            <TextField
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Token"
              type="password"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
            <Box sx={{ mt: 1 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={selfHosted}
                    onChange={(e) => setSelfHosted(e.target.checked)}
                  />
                }
                label="Self-hosted"
              />
            </Box>
            {selfHosted && (
              <>
                <TextField
                  fullWidth
                  label="GitLab URL"
                  value={gitlabUrl}
                  onChange={(e) => setGitlabUrl(e.target.value)}
                  margin="normal"
                />
                <TextField
                  fullWidth
                  label="Registry URL"
                  value={registryUrl}
                  onChange={(e) => setRegistryUrl(e.target.value)}
                  margin="normal"
                />
              </>
            )}
          </>
        );
      case "ghcr":
        return (
          <>
            <TextField
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Token"
              type="password"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              margin="normal"
              placeholder="Leave blank to keep current"
            />
          </>
        );
      default:
        return null;
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Edit Registry: {registry.name}
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
              label="Type"
              value={typeLabel}
              margin="normal"
              InputProps={{ readOnly: true }}
            />

            {renderFields()}

            <Box sx={{ mt: 2 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={isPublic}
                    onChange={(e) => setIsPublic(e.target.checked)}
                  />
                }
                label="Share with team (public registry)"
              />
            </Box>

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
                onClick={() => navigate(`/registries/${id}`)}
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
