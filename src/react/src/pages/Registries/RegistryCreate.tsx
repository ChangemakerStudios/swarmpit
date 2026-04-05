import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  MenuItem,
  Step,
  StepLabel,
  Stepper,
  Switch,
  TextField,
  Typography,
} from "@mui/material";
import { createRegistry } from "../../api/registries";

const REGISTRY_TYPES = [
  { value: "dockerhub", label: "Docker Hub" },
  { value: "v2", label: "Registry V2" },
  { value: "ecr", label: "Amazon ECR" },
  { value: "acr", label: "Azure ACR" },
  { value: "gitlab", label: "GitLab" },
  { value: "ghcr", label: "GitHub GHCR" },
];

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

const STEPS = ["Select Type", "Configure", "Sharing"];

interface FormState {
  registryType: string;
  name: string;
  url: string;
  username: string;
  password: string;
  region: string;
  accessKeyId: string;
  accessKey: string;
  servicePrincipalId: string;
  token: string;
  selfHosted: boolean;
  gitlabUrl: string;
  registryUrl: string;
  customApi: boolean;
  withAuth: boolean;
  isPublic: boolean;
}

const initialForm: FormState = {
  registryType: "",
  name: "",
  url: "",
  username: "",
  password: "",
  region: "us-east-1",
  accessKeyId: "",
  accessKey: "",
  servicePrincipalId: "",
  token: "",
  selfHosted: false,
  gitlabUrl: "",
  registryUrl: "",
  customApi: false,
  withAuth: false,
  isPublic: false,
};

export default function RegistryCreate() {
  const navigate = useNavigate();
  const [activeStep, setActiveStep] = useState(0);
  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const update = (field: keyof FormState, value: any) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const canAdvance = (): boolean => {
    if (activeStep === 0) return !!form.registryType;
    if (activeStep === 1) return isStep2Valid();
    return true;
  };

  const isStep2Valid = (): boolean => {
    switch (form.registryType) {
      case "dockerhub":
        return !!form.username && !!form.password;
      case "v2":
        return !!form.name && !!form.url;
      case "ecr":
        return !!form.region && !!form.accessKeyId && !!form.accessKey;
      case "acr":
        return !!form.name && !!form.servicePrincipalId && !!form.password;
      case "gitlab":
        return !!form.username && !!form.token;
      case "ghcr":
        return !!form.username && !!form.token;
      default:
        return false;
    }
  };

  const buildPayload = () => {
    const base: any = {
      registryType: form.registryType,
      public: form.isPublic,
    };

    switch (form.registryType) {
      case "dockerhub":
        return { ...base, username: form.username, password: form.password };
      case "v2":
        return {
          ...base,
          name: form.name,
          url: form.url,
          customApi: form.customApi,
          withAuth: form.withAuth,
          ...(form.withAuth && {
            username: form.username,
            password: form.password,
          }),
        };
      case "ecr":
        return {
          ...base,
          region: form.region,
          accessKeyId: form.accessKeyId,
          accessKey: form.accessKey,
        };
      case "acr":
        return {
          ...base,
          name: form.name,
          servicePrincipalId: form.servicePrincipalId,
          password: form.password,
        };
      case "gitlab":
        return {
          ...base,
          username: form.username,
          token: form.token,
          selfHosted: form.selfHosted,
          ...(form.selfHosted && {
            gitlabUrl: form.gitlabUrl,
            registryUrl: form.registryUrl,
          }),
        };
      case "ghcr":
        return { ...base, username: form.username, token: form.token };
      default:
        return base;
    }
  };

  const handleSubmit = async () => {
    setError("");
    setLoading(true);
    try {
      await createRegistry(buildPayload());
      navigate("/registries");
    } catch (err: any) {
      setError(err.response?.data?.message ?? "Failed to create registry");
    } finally {
      setLoading(false);
    }
  };

  const renderStep1 = () => (
    <TextField
      select
      fullWidth
      label="Registry Type"
      value={form.registryType}
      onChange={(e) => update("registryType", e.target.value)}
      margin="normal"
    >
      {REGISTRY_TYPES.map((t) => (
        <MenuItem key={t.value} value={t.value}>
          {t.label}
        </MenuItem>
      ))}
    </TextField>
  );

  const renderStep2 = () => {
    switch (form.registryType) {
      case "dockerhub":
        return (
          <>
            <TextField
              fullWidth
              label="Username"
              value={form.username}
              onChange={(e) => update("username", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Password"
              type="password"
              value={form.password}
              onChange={(e) => update("password", e.target.value)}
              margin="normal"
              required
            />
          </>
        );
      case "v2":
        return (
          <>
            <TextField
              fullWidth
              label="Name"
              value={form.name}
              onChange={(e) => update("name", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="URL"
              value={form.url}
              onChange={(e) => update("url", e.target.value)}
              margin="normal"
              required
              placeholder="https://registry.example.com"
            />
            <Box sx={{ mt: 1 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={form.customApi}
                    onChange={(e) => update("customApi", e.target.checked)}
                  />
                }
                label="Custom API"
              />
              <FormControlLabel
                control={
                  <Checkbox
                    checked={form.withAuth}
                    onChange={(e) => update("withAuth", e.target.checked)}
                  />
                }
                label="Requires Authentication"
              />
            </Box>
            {form.withAuth && (
              <>
                <TextField
                  fullWidth
                  label="Username"
                  value={form.username}
                  onChange={(e) => update("username", e.target.value)}
                  margin="normal"
                  required
                />
                <TextField
                  fullWidth
                  label="Password"
                  type="password"
                  value={form.password}
                  onChange={(e) => update("password", e.target.value)}
                  margin="normal"
                  required
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
              value={form.region}
              onChange={(e) => update("region", e.target.value)}
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
              value={form.accessKeyId}
              onChange={(e) => update("accessKeyId", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Secret Access Key"
              type="password"
              value={form.accessKey}
              onChange={(e) => update("accessKey", e.target.value)}
              margin="normal"
              required
            />
          </>
        );
      case "acr":
        return (
          <>
            <TextField
              fullWidth
              label="Registry Name"
              value={form.name}
              onChange={(e) => update("name", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Service Principal ID"
              value={form.servicePrincipalId}
              onChange={(e) => update("servicePrincipalId", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Service Principal Password"
              type="password"
              value={form.password}
              onChange={(e) => update("password", e.target.value)}
              margin="normal"
              required
            />
          </>
        );
      case "gitlab":
        return (
          <>
            <TextField
              fullWidth
              label="Username"
              value={form.username}
              onChange={(e) => update("username", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Token"
              type="password"
              value={form.token}
              onChange={(e) => update("token", e.target.value)}
              margin="normal"
              required
            />
            <Box sx={{ mt: 1 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={form.selfHosted}
                    onChange={(e) => update("selfHosted", e.target.checked)}
                  />
                }
                label="Self-hosted"
              />
            </Box>
            {form.selfHosted && (
              <>
                <TextField
                  fullWidth
                  label="GitLab URL"
                  value={form.gitlabUrl}
                  onChange={(e) => update("gitlabUrl", e.target.value)}
                  margin="normal"
                  placeholder="https://gitlab.example.com"
                />
                <TextField
                  fullWidth
                  label="Registry URL"
                  value={form.registryUrl}
                  onChange={(e) => update("registryUrl", e.target.value)}
                  margin="normal"
                  placeholder="https://registry.example.com"
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
              value={form.username}
              onChange={(e) => update("username", e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              label="Token"
              type="password"
              value={form.token}
              onChange={(e) => update("token", e.target.value)}
              margin="normal"
              required
            />
          </>
        );
      default:
        return (
          <Typography color="text.secondary">
            Please select a registry type first.
          </Typography>
        );
    }
  };

  const renderStep3 = () => (
    <Box sx={{ mt: 2 }}>
      <FormControlLabel
        control={
          <Switch
            checked={form.isPublic}
            onChange={(e) => update("isPublic", e.target.checked)}
          />
        }
        label="Share with team (public registry)"
      />
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        When enabled, all team members will be able to use this registry.
      </Typography>
    </Box>
  );

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Add Registry
      </Typography>
      <Card sx={{ maxWidth: 600 }}>
        <CardContent>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}
          <Stepper activeStep={activeStep} sx={{ mb: 3 }}>
            {STEPS.map((label) => (
              <Step key={label}>
                <StepLabel>{label}</StepLabel>
              </Step>
            ))}
          </Stepper>

          {activeStep === 0 && renderStep1()}
          {activeStep === 1 && renderStep2()}
          {activeStep === 2 && renderStep3()}

          <Box sx={{ mt: 3, display: "flex", gap: 2 }}>
            {activeStep > 0 && (
              <Button
                variant="outlined"
                onClick={() => setActiveStep((s) => s - 1)}
              >
                Back
              </Button>
            )}
            <Box sx={{ flexGrow: 1 }} />
            {activeStep < STEPS.length - 1 ? (
              <Button
                variant="contained"
                onClick={() => setActiveStep((s) => s + 1)}
                disabled={!canAdvance()}
              >
                Next
              </Button>
            ) : (
              <Button
                variant="contained"
                onClick={handleSubmit}
                disabled={loading}
              >
                {loading ? <CircularProgress size={24} /> : "Create"}
              </Button>
            )}
            <Button variant="outlined" onClick={() => navigate("/registries")}>
              Cancel
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
