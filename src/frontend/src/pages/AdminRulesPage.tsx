import { useCallback, useEffect, useState } from 'react';
import {
  Box, Button, Chip, Dialog, DialogContent, DialogTitle, IconButton, Paper,
  Switch, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import RuleForm from '../components/RuleForm';
import { createRule, deleteRule, getRules, updateRule, type RuleCreateRequest, type ValidationRule } from '../services/api';

export default function AdminRulesPage() {
  const [rules, setRules] = useState<ValidationRule[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<ValidationRule | null>(null);

  const loadRules = useCallback(async () => {
    setRules(await getRules());
  }, []);

  useEffect(() => { loadRules(); }, [loadRules]);

  async function handleCreate(data: RuleCreateRequest) {
    await createRule(data);
    setDialogOpen(false);
    await loadRules();
  }

  async function handleUpdate(data: RuleCreateRequest) {
    if (!editingRule) return;
    await updateRule(editingRule.id, data);
    setEditingRule(null);
    setDialogOpen(false);
    await loadRules();
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this rule?')) return;
    await deleteRule(id);
    await loadRules();
  }

  async function handleToggleActive(rule: ValidationRule) {
    await updateRule(rule.id, {
      name: rule.name, description: rule.description, ruleText: rule.ruleText,
      scope: rule.scope, fileType: rule.fileType, isActive: !rule.isActive,
    });
    await loadRules();
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
      <Box className="flex justify-between items-center mb-4">
        <Typography variant="h5">Validation Rules</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>Add Rule</Button>
      </Box>

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
