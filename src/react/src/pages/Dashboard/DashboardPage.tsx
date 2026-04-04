import { Box, Typography, Card, CardContent, Grid } from "@mui/material";

export default function DashboardPage() {
  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>
        Dashboard
      </Typography>
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Services
              </Typography>
              <Typography variant="h4">-</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Nodes
              </Typography>
              <Typography variant="h4">-</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Networks
              </Typography>
              <Typography variant="h4">-</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Volumes
              </Typography>
              <Typography variant="h4">-</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
      <Box sx={{ mt: 4 }}>
        <Typography variant="body2" color="text.secondary">
          Dashboard charts and real-time stats will be added in Phase 5.
        </Typography>
      </Box>
    </Box>
  );
}
