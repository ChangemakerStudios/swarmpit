import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Box, Button, Chip, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DataTable, { type Column } from "../../components/DataTable";
import { getUsers, type User } from "../../api/users";

const columns: Column<User>[] = [
  { id: "username", label: "Username", minWidth: 200 },
  {
    id: "role",
    label: "Role",
    render: (row) => (
      <Chip
        label={row.role}
        size="small"
        color={row.role === "admin" ? "primary" : "default"}
      />
    ),
  },
  { id: "email", label: "Email", minWidth: 200 },
  {
    id: "hasApiToken",
    label: "API Token",
    render: (row) => (
      <Chip
        label={row.hasApiToken ? "Active" : "None"}
        size="small"
        color={row.hasApiToken ? "success" : "default"}
        variant={row.hasApiToken ? "filled" : "outlined"}
      />
    ),
  },
];

export default function UserList() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    getUsers()
      .then(setUsers)
      .finally(() => setLoading(false));
  }, []);

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", mb: 2 }}>
        <Typography variant="h5">Users</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/users/create")}
        >
          Add User
        </Button>
      </Box>
      <DataTable
        columns={columns}
        rows={users}
        loading={loading}
        onRowClick={(row) => navigate(`/users/${row.username}`)}
        searchFields={["username", "role", "email"]}
        defaultSortField="username"
      />
    </Box>
  );
}
