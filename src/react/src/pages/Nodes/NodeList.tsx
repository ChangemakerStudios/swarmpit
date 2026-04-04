import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Chip,
  LinearProgress,
} from "@mui/material";
import { getNodes, type SwarmNode } from "../../api/nodes";

export default function NodeList() {
  const [nodes, setNodes] = useState<SwarmNode[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getNodes()
      .then(setNodes)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Nodes
      </Typography>
      {loading && <LinearProgress />}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Name</TableCell>
              <TableCell>Role</TableCell>
              <TableCell>State</TableCell>
              <TableCell>Availability</TableCell>
              <TableCell>Engine</TableCell>
              <TableCell>CPU</TableCell>
              <TableCell>Memory</TableCell>
              <TableCell>Address</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {nodes.map((node) => (
              <TableRow
                key={node.id}
                hover
                sx={{ cursor: "pointer" }}
                onClick={() => navigate(`/nodes/${node.id}`)}
              >
                <TableCell>
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    {node.nodeName}
                    {node.leader && (
                      <Chip label="Leader" size="small" color="primary" />
                    )}
                  </Box>
                </TableCell>
                <TableCell>{node.role}</TableCell>
                <TableCell>
                  <Chip
                    label={node.state}
                    size="small"
                    color={node.state === "ready" ? "success" : "default"}
                  />
                </TableCell>
                <TableCell>{node.availability}</TableCell>
                <TableCell>{node.engine}</TableCell>
                <TableCell>{node.resources.cpu.toFixed(1)}</TableCell>
                <TableCell>{Math.round(node.resources.memory)} MiB</TableCell>
                <TableCell>{node.address}</TableCell>
              </TableRow>
            ))}
            {!loading && nodes.length === 0 && (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  No nodes found
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
