CREATE TABLE validation_audit_log (
    id                UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    file_name         NVARCHAR(255)    NOT NULL,
    user_id           NVARCHAR(255)    NOT NULL,
    user_email        NVARCHAR(255)    NOT NULL,
    validated_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    overall_result    NVARCHAR(10)     NOT NULL,
    rules_evaluated   NVARCHAR(MAX)    NULL,
    ai_response       NVARCHAR(MAX)    NULL,
    scopes_evaluated  NVARCHAR(100)    NULL,
    CONSTRAINT PK_validation_audit_log PRIMARY KEY (id),
    CONSTRAINT CK_validation_audit_log_overall_result CHECK (overall_result IN ('PASS', 'FAIL', 'ERROR'))
);
