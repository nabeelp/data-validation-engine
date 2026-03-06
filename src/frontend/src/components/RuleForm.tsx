import { useState } from 'react';
import {
  TextField, Select, MenuItem, FormControl, InputLabel, Switch, FormControlLabel, Button, Box, type SelectChangeEvent,
} from '@mui/material';
import type { RuleCreateRequest } from '../services/api';

interface RuleFormProps {
  initial?: RuleCreateRequest;
  onSubmit: (rule: RuleCreateRequest) => void;
  onCancel: () => void;
}

const SCOPES = ['FILE', 'HEADER', 'FOOTER', 'RECORD'];
const FILE_TYPES = ['CSV', 'XLSX', 'ALL'];

export default function RuleForm({ initial, onSubmit, onCancel }: RuleFormProps) {
  const [form, setForm] = useState<RuleCreateRequest>(
    initial ?? { name: '', description: '', ruleText: '', scope: '', fileType: '', isActive: true }
  );
  const [errors, setErrors] = useState<Record<string, string>>({});

  function validate(): boolean {
    const e: Record<string, string> = {};
    if (!form.name.trim()) e.name = 'Name is required';
    if (!form.ruleText.trim()) e.ruleText = 'Rule text is required';
    if (!form.scope) e.scope = 'Scope is required';
    if (!form.fileType) e.fileType = 'File type is required';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  function handleSubmit() {
    if (validate()) onSubmit(form);
  }

  function handleSelectChange(field: keyof RuleCreateRequest) {
    return (e: SelectChangeEvent) => setForm({ ...form, [field]: e.target.value });
  }

  return (
    <Box className="flex flex-col gap-4 p-4 max-w-xl">
      <TextField label="Name" value={form.name} required error={!!errors.name} helperText={errors.name}
        onChange={(e) => setForm({ ...form, name: e.target.value })} />
      <TextField label="Description" value={form.description ?? ''} multiline rows={2}
        onChange={(e) => setForm({ ...form, description: e.target.value })} />
      <TextField label="Rule Text" value={form.ruleText} required multiline rows={3}
        error={!!errors.ruleText} helperText={errors.ruleText}
        onChange={(e) => setForm({ ...form, ruleText: e.target.value })} />
      <FormControl error={!!errors.scope}>
        <InputLabel>Scope</InputLabel>
        <Select value={form.scope} label="Scope" onChange={handleSelectChange('scope')}>
          {SCOPES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
        </Select>
      </FormControl>
      <FormControl error={!!errors.fileType}>
        <InputLabel>File Type</InputLabel>
        <Select value={form.fileType} label="File Type" onChange={handleSelectChange('fileType')}>
          {FILE_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </Select>
      </FormControl>
      <FormControlLabel label="Active"
        control={<Switch checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />} />
      <Box className="flex gap-2">
        <Button variant="contained" onClick={handleSubmit}>Save</Button>
        <Button variant="outlined" onClick={onCancel}>Cancel</Button>
      </Box>
    </Box>
  );
}
