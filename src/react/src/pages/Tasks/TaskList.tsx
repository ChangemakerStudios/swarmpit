import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Typography, LinearProgress, Chip } from "@mui/material";
import DataTable, { type Column } from "../../components/DataTable";
import { getTasks, type SwarmTask } from "../../api/tasks";
import { timeAgo } from "../../utils/time";

function stateColor(state: string): "success" | "error" | "warning" | "default" {
  if (state === "running") return "success";
  if (state === "failed" || state === "rejected") return "error";
  if (state === "pending" || state === "preparing" || state === "starting") return "warning";
  return "default";
}

const columns: Column<SwarmTask>[] = [
  {
    id: "taskName",
    label: "Task",
    minWidth: 200,
    render: (row) => (
      <Box>
        <Typography variant="body2">{row.taskName}</Typography>
        <Typography variant="caption" color="text.secondary">
          {row.repository.image}
        </Typography>
      </Box>
    ),
  },
  { id: "serviceName", label: "Service" },
  { id: "nodeName", label: "Node" },
  {
    id: "updatedAt",
    label: "Last Update",
    render: (row) => timeAgo(row.updatedAt),
  },
  {
    id: "state",
    label: "Status",
    render: (row) => (
      <Chip label={row.state} size="small" color={stateColor(row.state)} />
    ),
  },
];

export default function TaskList() {
  const [tasks, setTasks] = useState<SwarmTask[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getTasks()
      .then(setTasks)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Tasks
      </Typography>
      {loading && <LinearProgress />}
      <DataTable
        columns={columns}
        rows={tasks}
        onRowClick={(row) => navigate(`/tasks/${row.id}`)}
        searchFields={["taskName", "serviceName", "nodeName", "state"]}
        defaultSortField="updatedAt"
        defaultSortDirection="desc"
      />
    </Box>
  );
}
