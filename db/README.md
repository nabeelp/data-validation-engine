# Database Migrations

This folder holds sequential SQL migration scripts for SQL Server.

Scripts are named with a numeric prefix to ensure execution order:

```
001_create_validation_rules.sql
002_create_validation_audit_log.sql
...
```

Apply migrations in order against the target database.
