import { Box, Typography } from '@mui/material';

export default function NotAuthenticatedPage() {
  return (
    <Box className="flex items-center justify-center min-h-screen">
      <Box className="text-center">
        <Typography variant="h4" color="error" gutterBottom>Not Authenticated</Typography>
        <Typography variant="body1">You must be authenticated to access this application.</Typography>
      </Box>
    </Box>
  );
}
