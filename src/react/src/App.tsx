import { useMemo } from "react";
import { RouterProvider } from "react-router-dom";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { getTheme } from "./theme/theme";
import { useLayoutStore } from "./store/layoutStore";
import router from "./router";

export default function App() {
  const themeMode = useLayoutStore((s) => s.themeMode);
  const theme = useMemo(() => getTheme(themeMode), [themeMode]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <RouterProvider router={router} />
    </ThemeProvider>
  );
}
