import { createTheme } from "@mui/material/styles";

export function getTheme(mode: "light" | "dark") {
  return createTheme({
    palette: {
      mode,
      primary: { main: "#65519e" },
      secondary: { main: "#f50057" },
    },
    typography: {
      fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    },
    components: {
      MuiAppBar: {
        defaultProps: { elevation: 1 },
      },
    },
  });
}
