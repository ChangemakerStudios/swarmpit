import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import {
  Box,
  Card,
  CardContent,
  Chip,
  Grid,
  LinearProgress,
  Typography,
} from "@mui/material";
import { getNode, type SwarmNode } from "../../api/nodes";

function InfoItem({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <Box sx={{ mb: 1 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1">{value ?? "-"}</Typography>
    </Box>
  );
}

export default function NodeDetail() {
  const { id } = useParams<{ id: string }>();
  const [node, setNode] = useState<SwarmNode | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    getNode(id)
      .then(setNode)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <LinearProgress />;
  if (!node) return <Typography>Node not found</Typography>;

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 3 }}>
        <Typography variant="h5">{node.nodeName}</Typography>
        <Chip
          label={node.state}
          color={node.state === "ready" ? "success" : "default"}
        />
        {node.leader && <Chip label="Leader" color="primary" />}
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                General
              </Typography>
              <InfoItem label="ID" value={node.id} />
              <InfoItem label="Role" value={node.role} />
              <InfoItem label="Availability" value={node.availability} />
              <InfoItem label="Address" value={node.address} />
              <InfoItem label="Engine" value={node.engine} />
              <InfoItem label="OS" value={node.os} />
              <InfoItem label="Architecture" value={node.arch} />
            </CardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, md: 6 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Resources
              </Typography>
              <InfoItem label="CPU" value={`${node.resources.cpu.toFixed(1)} cores`} />
              <InfoItem label="Memory" value={`${Math.round(node.resources.memory)} MiB`} />
            </CardContent>
          </Card>
        </Grid>

        {node.labels.length > 0 && (
          <Grid size={{ xs: 12 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Labels
                </Typography>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {node.labels.map((l) => (
                    <Chip key={l.name} label={`${l.name}=${l.value}`} size="small" />
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        )}

        <Grid size={{ xs: 12 }}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Plugins
              </Typography>
              <Typography variant="subtitle2" gutterBottom>
                Networks
              </Typography>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1, mb: 2 }}>
                {node.plugins.networks.map((n) => (
                  <Chip key={n} label={n} size="small" variant="outlined" />
                ))}
              </Box>
              <Typography variant="subtitle2" gutterBottom>
                Volumes
              </Typography>
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                {node.plugins.volumes.map((v) => (
                  <Chip key={v} label={v} size="small" variant="outlined" />
                ))}
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
