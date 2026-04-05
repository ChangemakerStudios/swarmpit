import { useEffect, useRef, useState, useCallback } from "react";
import { useParams } from "react-router-dom";
import {
  Box,
  FormControl,
  IconButton,
  InputAdornment,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import VerticalAlignBottomIcon from "@mui/icons-material/VerticalAlignBottom";
import { getServiceLogs } from "../../api/services";

interface LogLine {
  taskId: string;
  line: string;
}

const HISTORY_OPTIONS = [
  { label: "15 min", value: "15m" },
  { label: "30 min", value: "30m" },
  { label: "1 hour", value: "1h" },
  { label: "4 hours", value: "4h" },
  { label: "8 hours", value: "8h" },
  { label: "12 hours", value: "12h" },
  { label: "24 hours", value: "24h" },
  { label: "All", value: "" },
];

function parseLogs(data: any): LogLine[] {
  if (!data) return [];
  if (Array.isArray(data)) {
    return data.map((entry: any) => ({
      taskId: (entry.taskId ?? entry.task ?? "").slice(0, 5),
      line: entry.line ?? entry.message ?? String(entry),
    }));
  }
  if (typeof data === "string") {
    return data
      .split("\n")
      .filter((l) => l.trim())
      .map((l) => ({ taskId: "", line: l }));
  }
  return [];
}

export default function ServiceLogs() {
  const { id } = useParams<{ id: string }>();
  const [logs, setLogs] = useState<LogLine[]>([]);
  const [loading, setLoading] = useState(true);
  const [since, setSince] = useState("15m");
  const [search, setSearch] = useState("");
  const [autoScroll, setAutoScroll] = useState(true);
  const containerRef = useRef<HTMLPreElement>(null);
  const intervalRef = useRef<number | null>(null);

  const fetchLogs = useCallback(async () => {
    if (!id) return;
    try {
      const data = await getServiceLogs(id, since || undefined);
      setLogs(parseLogs(data));
    } catch {
      // silently fail on poll
    } finally {
      setLoading(false);
    }
  }, [id, since]);

  useEffect(() => {
    setLoading(true);
    fetchLogs();
  }, [fetchLogs]);

  useEffect(() => {
    intervalRef.current = window.setInterval(fetchLogs, 5000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchLogs]);

  useEffect(() => {
    if (autoScroll && containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [logs, autoScroll]);

  const filtered = search
    ? logs.filter(
        (l) =>
          l.line.toLowerCase().includes(search.toLowerCase()) ||
          l.taskId.toLowerCase().includes(search.toLowerCase())
      )
    : logs;

  if (loading) return <LinearProgress />;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Service Logs
      </Typography>

      <Toolbar disableGutters sx={{ gap: 2, mb: 1, flexWrap: "wrap" }}>
        <FormControl size="small" sx={{ minWidth: 130 }}>
          <InputLabel>History</InputLabel>
          <Select
            value={since}
            label="History"
            onChange={(e) => {
              setSince(e.target.value);
              setLoading(true);
            }}
          >
            {HISTORY_OPTIONS.map((opt) => (
              <MenuItem key={opt.value} value={opt.value}>
                {opt.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <TextField
          size="small"
          placeholder="Filter logs..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
          }}
          sx={{ flexGrow: 1, maxWidth: 400 }}
        />

        <Tooltip title={autoScroll ? "Auto-scroll on" : "Auto-scroll off"}>
          <IconButton
            onClick={() => setAutoScroll((v) => !v)}
            color={autoScroll ? "primary" : "default"}
          >
            <VerticalAlignBottomIcon />
          </IconButton>
        </Tooltip>
      </Toolbar>

      <Box
        ref={containerRef}
        component="pre"
        sx={{
          bgcolor: "#1e1e1e",
          color: "#d4d4d4",
          fontFamily: '"Roboto Mono", "Courier New", monospace',
          fontSize: "0.8125rem",
          lineHeight: 1.6,
          p: 2,
          borderRadius: 1,
          overflow: "auto",
          maxHeight: "calc(100vh - 280px)",
          minHeight: 300,
          whiteSpace: "pre-wrap",
          wordBreak: "break-all",
          m: 0,
        }}
      >
        {filtered.length === 0 ? (
          <Box component="span" sx={{ color: "#888" }}>
            {search ? "No matching log lines" : "No logs available"}
          </Box>
        ) : (
          filtered.map((l, i) => (
            <Box component="span" key={i}>
              {l.taskId && (
                <Box component="span" sx={{ color: "#569cd6" }}>
                  [{l.taskId}]{" "}
                </Box>
              )}
              {l.line}
              {"\n"}
            </Box>
          ))
        )}
      </Box>
    </Box>
  );
}
