# GitHub Copilot Workspace Instructions

## Always Load Task Instructions

For every request in this workspace, load additional instructions from `.github/task-instructions.md` before producing an answer.

Treat `task-instructions.md` as an extension of this file and augment behavior with those rules.

If both files define guidance on the same topic, follow both when possible and prefer the more specific rule.

If `task-instructions.md` is missing, continue with the rest of these instructions and state that the task instructions file was not found when it is relevant.

## General Instructions

1. Keep changes small, incremental, and easy to review.
2. Prioritize correctness first, then readability, then optimization.
3. Match existing project conventions and naming before introducing new patterns.
4. Avoid broad refactors unless they are required by the active task.
5. Prefer clear, maintainable code over clever or dense code.
6. Be concise. Keep responses short and low-verbosity — avoid unnecessary explanation, preamble, or summaries. Let the code speak.

## Coding Standards

1. Write code that is modular, testable, and single-purpose.
2. Use descriptive names for functions, variables, types, and files.
3. Keep function bodies focused; split overly long logic into helpers.
4. Validate inputs and handle error paths explicitly.
5. Add concise comments only where intent is not obvious from code.
6. Log decisions, state changes, and errors in code you touch; skip routine control flow.

## Testing Guidelines

1. Add or update tests for every behavior change.
2. Cover happy path, key edge cases, and failure paths relevant to the change.
3. Keep tests deterministic and isolated.
4. Run the smallest relevant test scope first, then broader validation as needed.
5. Do not mark work complete until required checks pass or blockers are documented.

## Task Management Model

`task-instructions.md` is the single source of truth for the current active task.

1. Treat this file as high-level policy.
2. Treat `.github/task-instructions.md` as execution-level instructions for the next small unit of work.
3. If these files conflict, prefer the more specific instruction in `task-instructions.md`.
4. Keep the active task intentionally small to limit risk and keep focus.

## Task Completion Protocol

Before a task can be marked complete:

1. Perform a critical code review of all changes made during the task.
2. Evaluate for correctness, edge cases, convention adherence, test coverage, and security.
3. Fix any issues identified by the review.
4. Only after the review passes with no outstanding issues, proceed to close the task.

## Task Rollover Protocol

Task rollover happens **only** when the user explicitly says "next task". Do not auto-identify or auto-write the next task after completing work.

When the user says "next task":

1. Append a one-line entry to `.github/completed-tasks.md`: `- <date> | <task summary>`.
2. After performing step 1, commit all changes from the completed task with a concise commit message.
3. After performing step 2, review the specification in the `docs` folder.
4. Identify the next smallest meaningful task that can be completed independently.
5. Replace the entire contents of `.github/task-instructions.md` with that next task.
6. Include clear scope boundaries, acceptance criteria, and required validation steps.
7. If `docs` is missing or does not define clear next steps, state that limitation and request the minimum clarifying input needed.

## Task File Format

When writing a new task into `task-instructions.md`, replace the entire file with exactly this structure:

```
# Task Instructions

## Task
<one-sentence description of what to build or change>

## Scope
- <specific file, module, or area to touch>
- <another item if needed>

## Out of Scope
- <what must NOT be changed in this task>

## Acceptance Criteria
- [ ] <concrete, verifiable condition>
- [ ] <another condition>

## Validation
- [ ] <command to run or check to perform>
- [ ] <another validation step>
```

Do not include meta-documentation, usage instructions, or rollover rules in the task file. The task file must contain only the active task.

## Idle State

When `task-instructions.md` contains the text `No active task is currently defined`:

1. Wait for the user to say "next task".
2. Then review the specification in the `docs` folder.
3. If a next task can be identified, write it into `task-instructions.md` using the format above.
4. If `docs` is missing or has no remaining work, inform the user and request direction.
