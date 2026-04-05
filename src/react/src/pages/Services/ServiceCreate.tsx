import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Typography,
  Paper,
  Tabs,
  Tab,
  TextField,
  Button,
  Radio,
  RadioGroup,
  FormControlLabel,
  FormControl,
  FormLabel,
  Switch,
  Slider,
  Select,
  MenuItem,
  InputLabel,
  Autocomplete,
  Snackbar,
  Alert,
} from "@mui/material";
import NavigateNextIcon from "@mui/icons-material/NavigateNext";
import NavigateBeforeIcon from "@mui/icons-material/NavigateBefore";
import { createService } from "../../api/services";
import { getNetworks, type SwarmNetwork } from "../../api/networks";
import { getSecrets, type SwarmSecret } from "../../api/secrets";
import { getConfigs, type SwarmConfig } from "../../api/configs";
import DynamicList from "../../components/DynamicList";

interface ServiceFormData {
  serviceName: string;
  image: string;
  tag: string;
  mode: string;
  replicas: number;
  command: string;
  ports: { containerPort: number; protocol: string; mode: string; hostPort: number }[];
  networks: string[];
  mounts: { type: string; source: string; target: string; readOnly: boolean }[];
  variables: { name: string; value: string }[];
  labels: { name: string; value: string }[];
  secrets: { secretName: string; secretTarget: string }[];
  configs: { configName: string; configTarget: string }[];
  hosts: { name: string; value: string }[];
  resources: {
    reservation: { cpu: number; memory: number };
    limit: { cpu: number; memory: number };
  };
  deployment: {
    autoredeploy: boolean;
    restartPolicy: { condition: string; delay: number; window: number; attempts: number };
    update: { parallelism: number; delay: number; order: string; failureAction: string };
    rollback: { parallelism: number; delay: number; order: string; failureAction: string };
    placement: string[];
  };
  logdriver: { name: string; opts: { name: string; value: string }[] };
}

const defaultForm: ServiceFormData = {
  serviceName: "",
  image: "",
  tag: "latest",
  mode: "replicated",
  replicas: 1,
  command: "",
  ports: [],
  networks: [],
  mounts: [],
  variables: [],
  labels: [],
  secrets: [],
  configs: [],
  hosts: [],
  resources: {
    reservation: { cpu: 0, memory: 0 },
    limit: { cpu: 0, memory: 0 },
  },
  deployment: {
    autoredeploy: false,
    restartPolicy: { condition: "any", delay: 5, window: 0, attempts: 0 },
    update: { parallelism: 1, delay: 0, order: "stop-first", failureAction: "pause" },
    rollback: { parallelism: 1, delay: 0, order: "stop-first", failureAction: "pause" },
    placement: [],
  },
  logdriver: { name: "json-file", opts: [] },
};

const LOG_DRIVERS = [
  "json-file",
  "none",
  "syslog",
  "journald",
  "gelf",
  "fluentd",
  "awslogs",
  "splunk",
  "gcplogs",
  "loki",
  "local",
];

const TAB_LABELS = ["General", "Network", "Environment", "Resources", "Deployment", "Logs"];

export interface ServiceFormProps {
  initialData?: ServiceFormData;
  onSubmit: (data: any) => Promise<void>;
  editMode?: boolean;
  title: string;
  submitLabel: string;
}

export function ServiceForm({
  initialData,
  onSubmit,
  editMode = false,
  title,
  submitLabel,
}: ServiceFormProps) {
  const [tab, setTab] = useState(0);
  const [form, setForm] = useState<ServiceFormData>(initialData ?? { ...defaultForm });
  const [availableNetworks, setAvailableNetworks] = useState<SwarmNetwork[]>([]);
  const [availableSecrets, setAvailableSecrets] = useState<SwarmSecret[]>([]);
  const [availableConfigs, setAvailableConfigs] = useState<SwarmConfig[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([getNetworks(), getSecrets(), getConfigs()]).then(
      ([nets, secs, cfgs]) => {
        setAvailableNetworks(nets);
        setAvailableSecrets(secs);
        setAvailableConfigs(cfgs);
      }
    );
  }, []);

  useEffect(() => {
    if (initialData) setForm(initialData);
  }, [initialData]);

  const updateForm = <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      await onSubmit(buildPayload(form));
    } catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? "An error occurred");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        {title}
      </Typography>

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        {TAB_LABELS.map((label) => (
          <Tab key={label} label={label} />
        ))}
      </Tabs>

      <Paper sx={{ p: 3, mb: 2 }}>
        {tab === 0 && (
          <GeneralTab form={form} updateForm={updateForm} editMode={editMode} />
        )}
        {tab === 1 && (
          <NetworkTab
            form={form}
            updateForm={updateForm}
            availableNetworks={availableNetworks}
          />
        )}
        {tab === 2 && (
          <EnvironmentTab
            form={form}
            updateForm={updateForm}
            availableSecrets={availableSecrets}
            availableConfigs={availableConfigs}
          />
        )}
        {tab === 3 && <ResourcesTab form={form} updateForm={updateForm} />}
        {tab === 4 && <DeploymentTab form={form} updateForm={updateForm} />}
        {tab === 5 && <LogsTab form={form} updateForm={updateForm} />}
      </Paper>

      <Box sx={{ display: "flex", justifyContent: "space-between" }}>
        <Button
          startIcon={<NavigateBeforeIcon />}
          onClick={() => setTab((t) => Math.max(0, t - 1))}
          disabled={tab === 0}
        >
          Previous
        </Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={submitting || !form.image}
        >
          {submitLabel}
        </Button>
        <Button
          endIcon={<NavigateNextIcon />}
          onClick={() => setTab((t) => Math.min(TAB_LABELS.length - 1, t + 1))}
          disabled={tab === TAB_LABELS.length - 1}
        >
          Next
        </Button>
      </Box>

      <Snackbar open={!!error} autoHideDuration={6000} onClose={() => setError(null)}>
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      </Snackbar>
    </Box>
  );
}

function GeneralTab({
  form,
  updateForm,
  editMode,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
  editMode: boolean;
}) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
      <TextField
        label="Image"
        value={form.image}
        onChange={(e) => updateForm("image", e.target.value)}
        fullWidth
        required
        disabled={editMode}
      />
      <TextField
        label="Tag"
        value={form.tag}
        onChange={(e) => updateForm("tag", e.target.value)}
        fullWidth
      />
      <TextField
        label="Service Name"
        value={form.serviceName}
        onChange={(e) => updateForm("serviceName", e.target.value)}
        fullWidth
        disabled={editMode}
      />
      <FormControl>
        <FormLabel>Mode</FormLabel>
        <RadioGroup
          row
          value={form.mode}
          onChange={(e) => updateForm("mode", e.target.value)}
        >
          <FormControlLabel
            value="replicated"
            control={<Radio />}
            label="Replicated"
            disabled={editMode}
          />
          <FormControlLabel
            value="global"
            control={<Radio />}
            label="Global"
            disabled={editMode}
          />
        </RadioGroup>
      </FormControl>
      {form.mode === "replicated" && (
        <TextField
          label="Replicas"
          type="number"
          value={form.replicas}
          onChange={(e) => updateForm("replicas", Math.max(0, parseInt(e.target.value) || 0))}
          fullWidth
          inputProps={{ min: 0 }}
        />
      )}
      <TextField
        label="Command"
        value={form.command}
        onChange={(e) => updateForm("command", e.target.value)}
        fullWidth
        multiline
        minRows={2}
      />
    </Box>
  );
}

function NetworkTab({
  form,
  updateForm,
  availableNetworks,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
  availableNetworks: SwarmNetwork[];
}) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <Autocomplete
        multiple
        options={availableNetworks.map((n) => n.networkName)}
        value={form.networks}
        onChange={(_, value) => updateForm("networks", value)}
        renderInput={(params) => <TextField {...params} label="Networks" />}
      />

      <DynamicList
          title="Ports"
          items={form.ports}
          onChange={(items) => updateForm("ports", items)}
          defaultItem={{ containerPort: 0, protocol: "tcp", mode: "ingress", hostPort: 0 }}
          emptyMessage="No ports defined"
          addLabel="Add Port"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField
                label="Container Port"
                type="number"
                value={item.containerPort || ""}
                onChange={(e) => onChange("containerPort", parseInt(e.target.value) || 0)}
                size="small"
                sx={{ flex: 1 }}
              />
              <FormControl size="small" sx={{ minWidth: 100 }}>
                <InputLabel>Protocol</InputLabel>
                <Select
                  value={item.protocol}
                  label="Protocol"
                  onChange={(e) => onChange("protocol", e.target.value)}
                >
                  <MenuItem value="tcp">TCP</MenuItem>
                  <MenuItem value="udp">UDP</MenuItem>
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 100 }}>
                <InputLabel>Mode</InputLabel>
                <Select
                  value={item.mode}
                  label="Mode"
                  onChange={(e) => onChange("mode", e.target.value)}
                >
                  <MenuItem value="ingress">Ingress</MenuItem>
                  <MenuItem value="host">Host</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Host Port"
                type="number"
                value={item.hostPort || ""}
                onChange={(e) => onChange("hostPort", parseInt(e.target.value) || 0)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />

      <DynamicList
          title="Extra Hosts"
          items={form.hosts}
          onChange={(items) => updateForm("hosts", items)}
          defaultItem={{ name: "", value: "" }}
          emptyMessage="No hosts defined"
          addLabel="Add Host"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField
                label="Hostname"
                value={item.name}
                onChange={(e) => onChange("name", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <TextField
                label="IP Address"
                value={item.value}
                onChange={(e) => onChange("value", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />
    </Box>
  );
}

function EnvironmentTab({
  form,
  updateForm,
  availableSecrets,
  availableConfigs,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
  availableSecrets: SwarmSecret[];
  availableConfigs: SwarmConfig[];
}) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <DynamicList
          title="Environment Variables"
          items={form.variables}
          onChange={(items) => updateForm("variables", items)}
          defaultItem={{ name: "", value: "" }}
          emptyMessage="No variables defined"
          addLabel="Add Variable"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField
                label="Name"
                value={item.name}
                onChange={(e) => onChange("name", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <TextField
                label="Value"
                value={item.value}
                onChange={(e) => onChange("value", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />

      <DynamicList
          title="Mounts"
          items={form.mounts}
          onChange={(items) => updateForm("mounts", items)}
          defaultItem={{ type: "bind", source: "", target: "", readOnly: false }}
          emptyMessage="No mounts defined"
          addLabel="Add Mount"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
              <FormControl size="small" sx={{ minWidth: 100 }}>
                <InputLabel>Type</InputLabel>
                <Select
                  value={item.type}
                  label="Type"
                  onChange={(e) => onChange("type", e.target.value)}
                >
                  <MenuItem value="bind">Bind</MenuItem>
                  <MenuItem value="volume">Volume</MenuItem>
                </Select>
              </FormControl>
              <TextField
                label="Source"
                value={item.source}
                onChange={(e) => onChange("source", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <TextField
                label="Target"
                value={item.target}
                onChange={(e) => onChange("target", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={item.readOnly}
                    onChange={(e) => onChange("readOnly", e.target.checked)}
                    size="small"
                  />
                }
                label="RO"
              />
            </Box>
          )}
        />

      <DynamicList
          title="Secrets"
          items={form.secrets}
          onChange={(items) => updateForm("secrets", items)}
          defaultItem={{ secretName: "", secretTarget: "" }}
          emptyMessage="No secrets assigned"
          addLabel="Add Secret"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <FormControl size="small" sx={{ flex: 1 }}>
                <InputLabel>Secret</InputLabel>
                <Select
                  value={item.secretName}
                  label="Secret"
                  onChange={(e) => onChange("secretName", e.target.value)}
                >
                  {availableSecrets.map((s) => (
                    <MenuItem key={s.id} value={s.secretName}>
                      {s.secretName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="Target"
                value={item.secretTarget}
                onChange={(e) => onChange("secretTarget", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />

      <DynamicList
          title="Configs"
          items={form.configs}
          onChange={(items) => updateForm("configs", items)}
          defaultItem={{ configName: "", configTarget: "" }}
          emptyMessage="No configs assigned"
          addLabel="Add Config"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <FormControl size="small" sx={{ flex: 1 }}>
                <InputLabel>Config</InputLabel>
                <Select
                  value={item.configName}
                  label="Config"
                  onChange={(e) => onChange("configName", e.target.value)}
                >
                  {availableConfigs.map((c) => (
                    <MenuItem key={c.id} value={c.configName}>
                      {c.configName}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="Target"
                value={item.configTarget}
                onChange={(e) => onChange("configTarget", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />
    </Box>
  );
}

function ResourcesTab({
  form,
  updateForm,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
}) {
  const updateResources = (
    section: "reservation" | "limit",
    field: "cpu" | "memory",
    value: number
  ) => {
    updateForm("resources", {
      ...form.resources,
      [section]: { ...form.resources[section], [field]: value },
    });
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <Typography variant="subtitle1">Reservation</Typography>
      <Box>
        <Typography gutterBottom>CPU: {form.resources.reservation.cpu}</Typography>
        <Slider
          value={form.resources.reservation.cpu}
          onChange={(_, v) => updateResources("reservation", "cpu", v as number)}
          min={0}
          max={4}
          step={0.1}
          valueLabelDisplay="auto"
        />
      </Box>
      <TextField
        label="Memory (MiB)"
        type="number"
        value={form.resources.reservation.memory || ""}
        onChange={(e) =>
          updateResources("reservation", "memory", parseInt(e.target.value) || 0)
        }
        fullWidth
        inputProps={{ min: 0 }}
      />

      <Typography variant="subtitle1">Limit</Typography>
      <Box>
        <Typography gutterBottom>CPU: {form.resources.limit.cpu}</Typography>
        <Slider
          value={form.resources.limit.cpu}
          onChange={(_, v) => updateResources("limit", "cpu", v as number)}
          min={0}
          max={4}
          step={0.1}
          valueLabelDisplay="auto"
        />
      </Box>
      <TextField
        label="Memory (MiB)"
        type="number"
        value={form.resources.limit.memory || ""}
        onChange={(e) =>
          updateResources("limit", "memory", parseInt(e.target.value) || 0)
        }
        fullWidth
        inputProps={{ min: 0 }}
      />
    </Box>
  );
}

function DeploymentTab({
  form,
  updateForm,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
}) {
  const updateDeployment = (field: string, value: any) => {
    updateForm("deployment", { ...form.deployment, [field]: value });
  };

  const updateRestartPolicy = (field: string, value: any) => {
    updateDeployment("restartPolicy", { ...form.deployment.restartPolicy, [field]: value });
  };

  const updateUpdateConfig = (field: string, value: any) => {
    updateDeployment("update", { ...form.deployment.update, [field]: value });
  };

  const updateRollbackConfig = (field: string, value: any) => {
    updateDeployment("rollback", { ...form.deployment.rollback, [field]: value });
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <DynamicList
          title="Labels"
          items={form.labels}
          onChange={(items) => updateForm("labels", items)}
          defaultItem={{ name: "", value: "" }}
          emptyMessage="No labels defined"
          addLabel="Add Label"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField
                label="Name"
                value={item.name}
                onChange={(e) => onChange("name", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <TextField
                label="Value"
                value={item.value}
                onChange={(e) => onChange("value", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />

      <FormControlLabel
        control={
          <Switch
            checked={form.deployment.autoredeploy}
            onChange={(e) => updateDeployment("autoredeploy", e.target.checked)}
          />
        }
        label="Autoredeploy"
      />

      <DynamicList
          title="Placement Constraints"
          items={form.deployment.placement.map((p) => ({ value: p }))}
          onChange={(items) =>
            updateDeployment(
              "placement",
              items.map((i: { value: string }) => i.value)
            )
          }
          defaultItem={{ value: "" }}
          emptyMessage="No placement constraints"
          addLabel="Add Constraint"
          renderItem={(item, _index, onChange) => (
            <TextField
              label="Constraint"
              value={item.value}
              onChange={(e) => onChange("value", e.target.value)}
              size="small"
              fullWidth
              placeholder="e.g. node.role == manager"
            />
          )}
        />

      <Typography variant="subtitle1">Restart Policy</Typography>
      <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Condition</InputLabel>
          <Select
            value={form.deployment.restartPolicy.condition}
            label="Condition"
            onChange={(e) => updateRestartPolicy("condition", e.target.value)}
          >
            <MenuItem value="any">Any</MenuItem>
            <MenuItem value="on-failure">On Failure</MenuItem>
            <MenuItem value="none">None</MenuItem>
          </Select>
        </FormControl>
        <TextField
          label="Delay (s)"
          type="number"
          value={form.deployment.restartPolicy.delay}
          onChange={(e) => updateRestartPolicy("delay", parseInt(e.target.value) || 0)}
          size="small"
          sx={{ width: 120 }}
        />
        <TextField
          label="Window (s)"
          type="number"
          value={form.deployment.restartPolicy.window}
          onChange={(e) => updateRestartPolicy("window", parseInt(e.target.value) || 0)}
          size="small"
          sx={{ width: 120 }}
        />
        <TextField
          label="Max Attempts"
          type="number"
          value={form.deployment.restartPolicy.attempts}
          onChange={(e) => updateRestartPolicy("attempts", parseInt(e.target.value) || 0)}
          size="small"
          sx={{ width: 120 }}
        />
      </Box>

      <Typography variant="subtitle1">Update Config</Typography>
      <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
        <TextField
          label="Parallelism"
          type="number"
          value={form.deployment.update.parallelism}
          onChange={(e) => updateUpdateConfig("parallelism", parseInt(e.target.value) || 0)}
          size="small"
          sx={{ width: 120 }}
        />
        <TextField
          label="Delay (s)"
          type="number"
          value={form.deployment.update.delay}
          onChange={(e) => updateUpdateConfig("delay", parseInt(e.target.value) || 0)}
          size="small"
          sx={{ width: 120 }}
        />
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Order</InputLabel>
          <Select
            value={form.deployment.update.order}
            label="Order"
            onChange={(e) => updateUpdateConfig("order", e.target.value)}
          >
            <MenuItem value="stop-first">Stop First</MenuItem>
            <MenuItem value="start-first">Start First</MenuItem>
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 150 }}>
          <InputLabel>Failure Action</InputLabel>
          <Select
            value={form.deployment.update.failureAction}
            label="Failure Action"
            onChange={(e) => updateUpdateConfig("failureAction", e.target.value)}
          >
            <MenuItem value="pause">Pause</MenuItem>
            <MenuItem value="continue">Continue</MenuItem>
            <MenuItem value="rollback">Rollback</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {form.deployment.update.failureAction === "rollback" && (
        <>
          <Typography variant="subtitle1">Rollback Config</Typography>
          <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap" }}>
            <TextField
              label="Parallelism"
              type="number"
              value={form.deployment.rollback.parallelism}
              onChange={(e) =>
                updateRollbackConfig("parallelism", parseInt(e.target.value) || 0)
              }
              size="small"
              sx={{ width: 120 }}
            />
            <TextField
              label="Delay (s)"
              type="number"
              value={form.deployment.rollback.delay}
              onChange={(e) => updateRollbackConfig("delay", parseInt(e.target.value) || 0)}
              size="small"
              sx={{ width: 120 }}
            />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Order</InputLabel>
              <Select
                value={form.deployment.rollback.order}
                label="Order"
                onChange={(e) => updateRollbackConfig("order", e.target.value)}
              >
                <MenuItem value="stop-first">Stop First</MenuItem>
                <MenuItem value="start-first">Start First</MenuItem>
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Failure Action</InputLabel>
              <Select
                value={form.deployment.rollback.failureAction}
                label="Failure Action"
                onChange={(e) => updateRollbackConfig("failureAction", e.target.value)}
              >
                <MenuItem value="pause">Pause</MenuItem>
                <MenuItem value="continue">Continue</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </>
      )}
    </Box>
  );
}

function LogsTab({
  form,
  updateForm,
}: {
  form: ServiceFormData;
  updateForm: <K extends keyof ServiceFormData>(key: K, value: ServiceFormData[K]) => void;
}) {
  const updateLogdriver = (field: string, value: any) => {
    updateForm("logdriver", { ...form.logdriver, [field]: value });
  };

  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: 3 }}>
      <FormControl fullWidth>
        <InputLabel>Log Driver</InputLabel>
        <Select
          value={form.logdriver.name}
          label="Log Driver"
          onChange={(e) => updateLogdriver("name", e.target.value)}
        >
          {LOG_DRIVERS.map((d) => (
            <MenuItem key={d} value={d}>
              {d}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <DynamicList
          title="Driver Options"
          items={form.logdriver.opts}
          onChange={(items) => updateLogdriver("opts", items)}
          defaultItem={{ name: "", value: "" }}
          emptyMessage="No driver options"
          addLabel="Add Option"
          renderItem={(item, _index, onChange) => (
            <Box sx={{ display: "flex", gap: 1 }}>
              <TextField
                label="Option"
                value={item.name}
                onChange={(e) => onChange("name", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
              <TextField
                label="Value"
                value={item.value}
                onChange={(e) => onChange("value", e.target.value)}
                size="small"
                sx={{ flex: 1 }}
              />
            </Box>
          )}
        />
    </Box>
  );
}

function buildPayload(form: ServiceFormData) {
  return {
    serviceName: form.serviceName,
    repository: {
      name: form.image,
      tag: form.tag,
    },
    mode: form.mode,
    replicas: form.mode === "replicated" ? form.replicas : undefined,
    command: form.command || undefined,
    ports: form.ports,
    networks: form.networks.map((n) => ({ networkName: n })),
    mounts: form.mounts,
    environment: form.variables,
    labels: form.labels,
    secrets: form.secrets,
    configs: form.configs,
    hosts: form.hosts,
    resources: form.resources,
    deployment: form.deployment,
    logdriver: form.logdriver,
  };
}

export { defaultForm, type ServiceFormData as ServiceFormDataType };

export default function ServiceCreate() {
  const navigate = useNavigate();

  const handleSubmit = async (payload: any) => {
    await createService(payload);
    navigate("/services");
  };

  return (
    <ServiceForm
      onSubmit={handleSubmit}
      title="Create Service"
      submitLabel="Deploy"
    />
  );
}
