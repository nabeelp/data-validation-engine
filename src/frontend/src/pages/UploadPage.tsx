import { useRef, useState } from 'react';
import { Box, Button, CircularProgress, Paper, Typography } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import ValidationResult from '../components/ValidationResult';
import { uploadAndValidate, type ValidationResponse } from '../services/api';

export default function UploadPage() {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ValidationResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0] ?? null;
    setSelectedFile(file);
    setResult(null);
    setError(null);
  }

  async function handleUpload() {
    if (!selectedFile) return;
    setLoading(true);
    setResult(null);
    setError(null);

    try {
      const response = await uploadAndValidate(selectedFile);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box className="p-6 max-w-2xl mx-auto">
      <Typography variant="h5" className="mb-4">Upload CDS File</Typography>
      <Paper className="p-6">
        <Box className="flex flex-col gap-4 items-start">
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv,.xlsx"
            onChange={handleFileChange}
            className="hidden"
          />
          <Button variant="outlined" startIcon={<CloudUploadIcon />}
            onClick={() => fileInputRef.current?.click()}>
            {selectedFile ? selectedFile.name : 'Choose File (.csv or .xlsx)'}
          </Button>

          <Button variant="contained" onClick={handleUpload}
            disabled={!selectedFile || loading}>
            {loading ? <CircularProgress size={24} /> : 'Validate & Upload'}
          </Button>
        </Box>

        {error && (
          <Typography color="error" className="mt-4">{error}</Typography>
        )}

        {result && <ValidationResult result={result} />}
      </Paper>
    </Box>
  );
}
