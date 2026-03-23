import { useCallback, useEffect, useState } from 'react';
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle,
  IconButton, Paper, Stack, Switch, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Typography,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import RuleForm from '../components/RuleForm';
import {
  createRule,
  deleteRule,
  getRules,
  initializeDatabase,
  updateRule,
  type RuleCreateRequest,
  type ValidationRule,
} from '../services/api';

type StatusMessage = {
  severity: 'success' | 'error' | 'info';
  text: string;
};

export default function AdminRulesPage() {
  const [rules, setRules] = useState<ValidationRule[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<ValidationRule | null>(null);
  const [statusMessage, setStatusMessage] = useState<StatusMessage | null>(null);
  const [isInitializing, setIsInitializing] = useState(false);

  const loadRules = useCallback(async () => {
    try {
      setRules(await getRules());
    } catch (error) {
      setRules([]);
      const message = error instanceof Error ? error.message : 'Unable to load validation rules.';
      setStatusMessage({ severity: 'error', text: message });
    }
  }, []);

  useEffect(() => { loadRules(); }, [loadRules]);

  async function handleCreate(data: RuleCreateRequest) {
    await createRule(data);
    setDialogOpen(false);
    setStatusMessage(null);
    await loadRules();
  }

  async function handleUpdate(data: RuleCreateRequest) {
    if (!editingRule) return;
    await updateRule(editingRule.id, data);
    setEditingRule(null);
    setDialogOpen(false);
    setStatusMessage(null);
    await loadRules();
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this rule?')) return;
    await deleteRule(id);
    setStatusMessage(null);
    await loadRules();
  }

  async function handleToggleActive(rule: ValidationRule) {
    await updateRule(rule.id, {
      name: rule.name, description: rule.description, ruleText: rule.ruleText,
      scope: rule.scope, fileType: rule.fileType, isActive: !rule.isActive,
    });
    setStatusMessage(null);
    await loadRules();
  }

  async function handleInitializeDatabase() {
    setIsInitializing(true);
    setStatusMessage(null);

    try {
      const result = await initializeDatabase();
      setStatusMessage({
        severity: 'success',
        text: `${result.databaseCreated ? 'Database created.' : 'Database already existed.'} ${result.tablesCreated} table(s) created. ${result.sampleRulesInserted} sample rule(s) inserted, ${result.sampleRulesSkipped} already present.`,
      });
      await loadRules();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Database initialization failed.';
      setStatusMessage({ severity: 'error', text: message });
    } finally {
      setIsInitializing(false);
    }
  }

  function openCreate() {
    setEditingRule(null);
    setDialogOpen(true);
  }

  function openEdit(rule: ValidationRule) {
    setEditingRule(rule);
    setDialogOpen(true);
  }

  return (
    <Box className="p-6">
      <Box className="flex justify-between items-center mb-4 gap-4 flex-wrap">
        <Box>
          <Typography variant="h5">Validation Rules</Typography>
          <Typography variant="body2" color="text.secondary">
            Initialize the configured SQL Server database, create the required tables, and seed sample validation rules.
          </Typography>
        </Box>
        <Stack direction="row" spacing={2}>
          <Button
            variant="outlined"
            onClick={handleInitializeDatabase}
            disabled={isInitializing}
            startIcon={isInitializing ? <CircularProgress size={16} color="inherit" /> : undefined}
          >
            {isInitializing ? 'Initializing...' : 'Initialize Database'}
          </Button>
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>Add Rule</Button>
        </Stack>
      </Box>

      {statusMessage && (
        <Alert severity={statusMessage.severity} className="mb-4">
          {statusMessage.text}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell><strong>Name</strong></TableCell>
              <TableCell><strong>Scope</strong></TableCell>
              <TableCell><strong>File Type</strong></TableCell>
              <TableCell><strong>Active</strong></TableCell>
              <TableCell><strong>Actions</strong></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rules.map((rule) => (
              <TableRow key={rule.id}>
                <TableCell>
                  <Typography variant="body2" fontWeight="bold">{rule.name}</Typography>
                  {rule.description && <Typography variant="caption" color="text.secondary">{rule.description}</Typography>}
                </TableCell>
                <TableCell><Chip label={rule.scope} size="small" /></TableCell>
                <TableCell><Chip label={rule.fileType} size="small" variant="outlined" /></TableCell>
                <TableCell>
                  <Switch checked={rule.isActive} onChange={() => handleToggleActive(rule)} size="small" />
                </TableCell>
                <TableCell>
                  <IconButton size="small" onClick={() => openEdit(rule)}><EditIcon fontSize="small" /></IconButton>
                  <IconButton size="small" onClick={() => handleDelete(rule.id)}><DeleteIcon fontSize="small" /></IconButton>
                </TableCell>
              </TableRow>
            ))}
            {rules.length === 0 && (
              <TableRow><TableCell colSpan={5} align="center">No rules configured</TableCell></TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={dialogOpen} onClose={() => { setDialogOpen(false); setEditingRule(null); }} maxWidth="sm" fullWidth>
        <DialogTitle>{editingRule ? 'Edit Rule' : 'Create Rule'}</DialogTitle>
        <DialogContent>
          <RuleForm
            initial={editingRule ? {
              name: editingRule.name, description: editingRule.description,
              ruleText: editingRule.ruleText, scope: editingRule.scope,
              fileType: editingRule.fileType, isActive: editingRule.isActive,
            } : undefined}
            onSubmit={editingRule ? handleUpdate : handleCreate}
            onCancel={() => { setDialogOpen(false); setEditingRule(null); }}
          />
        </DialogContent>
      </Dialog>
    </Box>
  );
}
