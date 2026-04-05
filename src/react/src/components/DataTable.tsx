import { useState, useMemo, type ReactNode } from "react";
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  TextField,
  LinearProgress,
  List,
  ListItem,
  ListItemText,
  Paper,
  Typography,
  InputAdornment,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";

export interface Column<T> {
  id: string;
  label: string;
  render?: (row: T) => ReactNode;
  align?: "left" | "right";
  minWidth?: number;
  width?: string | number;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  onRowClick?: (row: T) => void;
  searchFields?: string[];
  defaultSortField?: string;
  defaultSortDirection?: "asc" | "desc";
  loading?: boolean;
}

function getNestedValue(obj: any, path: string): any {
  return path.split(".").reduce((acc, part) => acc?.[part], obj);
}

export default function DataTable<T extends Record<string, any>>({
  columns,
  rows,
  onRowClick,
  searchFields = [],
  defaultSortField,
  defaultSortDirection = "asc",
  loading = false,
}: DataTableProps<T>) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(0);
  const [rowsPerPage] = useState(30);
  const [sortField, setSortField] = useState(defaultSortField ?? "");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">(
    defaultSortDirection
  );

  const filtered = useMemo(() => {
    if (!search.trim()) return rows;
    const lower = search.toLowerCase();
    return rows.filter((row) =>
      searchFields.some((field) => {
        const val = getNestedValue(row, field);
        return val != null && String(val).toLowerCase().includes(lower);
      })
    );
  }, [rows, search, searchFields]);

  const sorted = useMemo(() => {
    if (!sortField) return filtered;
    return [...filtered].sort((a, b) => {
      const aVal = getNestedValue(a, sortField) ?? "";
      const bVal = getNestedValue(b, sortField) ?? "";
      const cmp = String(aVal).localeCompare(String(bVal), undefined, {
        numeric: true,
      });
      return sortDirection === "asc" ? cmp : -cmp;
    });
  }, [filtered, sortField, sortDirection]);

  const paged = sorted.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);

  const handleSort = (field: string) => {
    if (sortField === field) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortField(field);
      setSortDirection("asc");
    }
  };

  return (
    <Box>
      <Box sx={{ height: 4, mb: 1 }}>
        {loading && <LinearProgress />}
      </Box>
      <TextField
        fullWidth
        size="small"
        placeholder="Search..."
        value={search}
        onChange={(e) => {
          setSearch(e.target.value);
          setPage(0);
        }}
        sx={{ mb: 2 }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon />
            </InputAdornment>
          ),
        }}
      />

      {isMobile ? (
        <Paper>
          <List>
            {paged.map((row, idx) => (
              <ListItem
                key={idx}
                divider
                onClick={() => onRowClick?.(row)}
                sx={onRowClick ? { cursor: "pointer" } : undefined}
              >
                <ListItemText
                  primary={
                    columns[0].render
                      ? columns[0].render(row)
                      : String(getNestedValue(row, columns[0].id) ?? "")
                  }
                  secondary={columns
                    .slice(1)
                    .map((col) => {
                      const val = col.render
                        ? col.render(row)
                        : getNestedValue(row, col.id);
                      if (typeof val === "object" && val !== null) {
                        return `${col.label}: ${(val as any)?.props?.label ?? "-"}`;
                      }
                      return `${col.label}: ${val ?? "-"}`;
                    })
                    .join(" | ")}
                />
              </ListItem>
            ))}
            {paged.length === 0 && (
              <ListItem>
                <ListItemText
                  primary={
                    <Typography align="center" color="text.secondary">
                      No items found
                    </Typography>
                  }
                />
              </ListItem>
            )}
          </List>
        </Paper>
      ) : (
        <TableContainer component={Paper}>
          <Table sx={{ tableLayout: "fixed" }}>
            <TableHead>
              <TableRow>
                {columns.map((col) => (
                  <TableCell
                    key={col.id}
                    align={col.align ?? "left"}
                    sx={{
                      ...(col.minWidth ? { minWidth: col.minWidth } : {}),
                      ...(col.width ? { width: col.width } : {}),
                    }}
                  >
                    <TableSortLabel
                      active={sortField === col.id}
                      direction={sortField === col.id ? sortDirection : "asc"}
                      onClick={() => handleSort(col.id)}
                    >
                      {col.label}
                    </TableSortLabel>
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {paged.map((row, idx) => (
                <TableRow
                  key={idx}
                  hover
                  sx={onRowClick ? { cursor: "pointer" } : undefined}
                  onClick={() => onRowClick?.(row)}
                >
                  {columns.map((col) => (
                    <TableCell
                      key={col.id}
                      align={col.align ?? "left"}
                      sx={{
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {col.render
                        ? col.render(row)
                        : String(getNestedValue(row, col.id) ?? "-")}
                    </TableCell>
                  ))}
                </TableRow>
              ))}
              {paged.length === 0 && (
                <TableRow>
                  <TableCell colSpan={columns.length} align="center">
                    No items found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {sorted.length > rowsPerPage && (
        <TablePagination
          component="div"
          count={sorted.length}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          rowsPerPageOptions={[30]}
        />
      )}
    </Box>
  );
}
