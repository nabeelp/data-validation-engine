import { Alert, Box, Chip, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorIcon from '@mui/icons-material/Error';
import WarningIcon from '@mui/icons-material/Warning';
import type { ValidationResponse } from '../services/api';

interface Props {
  result: ValidationResponse;
}

export default function ValidationResult({ result }: Props) {
  if (result.status === 'PASS') {
    return (
      <Alert severity="success" icon={<CheckCircleIcon />} className="mt-4">
        <Typography variant="body1" fontWeight="bold">Validation Passed</Typography>
        <Typography variant="body2">
          {result.rulesEvaluated} rule(s) evaluated. File accepted for ingestion.
        </Typography>
      </Alert>
    );
  }

  if (result.status === 'ERROR') {
    return (
      <Alert severity="warning" icon={<WarningIcon />} className="mt-4">
        <Typography variant="body1" fontWeight="bold">Validation could not be completed. Please retry.</Typography>
        {result.failures.length > 0 && (
          <Typography variant="body2">{result.failures[0].reason}</Typography>
        )}
      </Alert>
    );
  }

  return (
    <Box className="mt-4">
      <Alert severity="error" icon={<ErrorIcon />}>
        <Typography variant="body1" fontWeight="bold">Validation Failed</Typography>
        <Typography variant="body2">
          {result.rulesEvaluated} rule(s) evaluated, {result.rulesFailed} failed.
          {result.scopeShortCircuitedAt && (
            <> Short-circuited at <Chip label={result.scopeShortCircuitedAt} size="small" color="error" className="ml-1" />.</>
          )}
        </Typography>
      </Alert>
      <TableContainer component={Paper} className="mt-2">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell><strong>Rule Name</strong></TableCell>
              <TableCell><strong>Scope</strong></TableCell>
              <TableCell><strong>Reason</strong></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {result.failures.map((f, i) => (
              <TableRow key={i}>
                <TableCell>{f.ruleName}</TableCell>
                <TableCell><Chip label={f.scope} size="small" /></TableCell>
                <TableCell>{f.reason}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
