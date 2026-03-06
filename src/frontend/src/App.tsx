import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AppBar, Box, Button, CircularProgress, Toolbar, Typography } from '@mui/material';
import { AuthProvider, useAuth } from './context/AuthContext';
import AdminRulesPage from './pages/AdminRulesPage';
import UploadPage from './pages/UploadPage';
import NotAuthenticatedPage from './pages/NotAuthenticatedPage';

function AppRoutes() {
  const { user, loading, error } = useAuth();

  if (loading) {
    return (
      <Box className="flex items-center justify-center min-h-screen">
        <CircularProgress />
      </Box>
    );
  }

  if (error || !user) {
    return <NotAuthenticatedPage />;
  }

  return (
    <>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" className="flex-grow">CDS Validation Engine</Typography>
          <Typography variant="body2" className="mr-4">{user.email} ({user.role})</Typography>
          {user.role === 'Admin' && (
            <Button color="inherit" href="/admin/validation-rules">Rules</Button>
          )}
          <Button color="inherit" href="/upload">Upload</Button>
        </Toolbar>
      </AppBar>
      <Routes>
        {user.role === 'Admin' && (
          <Route path="/admin/validation-rules" element={<AdminRulesPage />} />
        )}
        <Route path="/upload" element={<UploadPage />} />
        <Route path="*" element={<Navigate to="/upload" replace />} />
      </Routes>
    </>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
