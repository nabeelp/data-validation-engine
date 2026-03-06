CREATE TABLE validation_rules (
    id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    name            NVARCHAR(255)    NOT NULL,
    description     NVARCHAR(MAX)    NULL,
    rule_text       NVARCHAR(MAX)    NOT NULL,
    scope           NVARCHAR(20)     NOT NULL,
    file_type       NVARCHAR(10)     NOT NULL,
    is_active       BIT              NOT NULL DEFAULT 1,
    created_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_validation_rules PRIMARY KEY (id),
    CONSTRAINT CK_validation_rules_scope CHECK (scope IN ('FILE', 'HEADER', 'FOOTER', 'RECORD')),
    CONSTRAINT CK_validation_rules_file_type CHECK (file_type IN ('CSV', 'XLSX', 'ALL'))
);
