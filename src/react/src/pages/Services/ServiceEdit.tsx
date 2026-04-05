import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { LinearProgress, Typography } from "@mui/material";
import { getService, updateService, type SwarmService } from "../../api/services";
import { ServiceForm, defaultForm, type ServiceFormDataType } from "./ServiceCreate";

function serviceToFormData(svc: SwarmService): ServiceFormDataType {
  return {
    serviceName: svc.serviceName,
    image: svc.repository.name,
    tag: svc.repository.tag,
    mode: svc.mode,
    replicas: svc.replicas ?? 1,
    command: "",
    ports: svc.ports.map((p) => ({
      containerPort: p.containerPort,
      protocol: p.protocol,
      mode: p.mode,
      hostPort: p.hostPort,
    })),
    networks: svc.networks.map((n) => n.networkName),
    mounts: (svc.mounts ?? []).map((m) => ({
      type: m.type,
      source: m.source,
      target: m.target,
      readOnly: m.readOnly,
    })),
    variables: (svc.environment ?? []).map((e) => ({ name: e.name, value: e.value })),
    labels: (svc.labels ?? []).map((l) => ({ name: l.name, value: l.value })),
    secrets: (svc.secrets ?? []).map((s) => ({
      secretName: s.secretName,
      secretTarget: "",
    })),
    configs: (svc.configs ?? []).map((c) => ({
      configName: c.configName,
      configTarget: "",
    })),
    hosts: [],
    resources: defaultForm.resources,
    deployment: defaultForm.deployment,
    logdriver: defaultForm.logdriver,
  };
}

export default function ServiceEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [formData, setFormData] = useState<ServiceFormDataType | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    getService(id)
      .then((svc) => setFormData(serviceToFormData(svc)))
      .finally(() => setLoading(false));
  }, [id]);

  const handleSubmit = async (payload: any) => {
    if (!id) return;
    await updateService(id, payload);
    navigate(`/services/${id}`);
  };

  if (loading) return <LinearProgress />;
  if (!formData) return <Typography>Service not found</Typography>;

  return (
    <ServiceForm
      initialData={formData}
      onSubmit={handleSubmit}
      editMode
      title="Edit Service"
      submitLabel="Update"
    />
  );
}
