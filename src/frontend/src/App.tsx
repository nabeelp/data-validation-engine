import { useEffect, useRef, useState } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { Alert, AppBar, Box, Button, CircularProgress, Toolbar, Typography } from '@mui/material';
import { AuthProvider, useAuth } from './context/AuthContext';
import AdminRulesPage from './pages/AdminRulesPage';
import UploadPage from './pages/UploadPage';
import NotAuthenticatedPage from './pages/NotAuthenticatedPage';
import { getDatabaseStatus, initializeDatabase } from './services/api';

function AppRoutes() {
  const { user, loading, error } = useAuth();
  const [databaseLoading, setDatabaseLoading] = useState(true);
  const [databaseError, setDatabaseError] = useState<string | null>(null);
  const initStarted = useRef(false);

  useEffect(() => {
    if (loading) {
      return;
    }

    if (error || !user) {
      setDatabaseLoading(false);
      return;
    }

    if (initStarted.current) {
      return;
    }

    initStarted.current = true;

    const bootstrapDatabase = async () => {
      try {
        const status = await getDatabaseStatus();
        if (!status.exists) {
          await initializeDatabase();
        }
        setDatabaseError(null);
      } catch (bootstrapError) {
        const message = bootstrapError instanceof Error
          ? bootstrapError.message
          : 'Database initialization failed.';
        setDatabaseError(message);
      } finally {
        setDatabaseLoading(false);
      }
    };

    bootstrapDatabase();
  }, [error, loading, user]);

  if (loading || databaseLoading) {
    return (
      <Box className="flex items-center justify-center min-h-screen">
        <CircularProgress />
      </Box>
    );
  }

  if (error || !user) {
    return <NotAuthenticatedPage />;
  }

  if (databaseError) {
    return (
      <Box className="min-h-screen p-6 flex items-center justify-center">
        <Box className="max-w-xl w-full">
          <Alert severity="error">{databaseError}</Alert>
        </Box>
      </Box>
    );
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
