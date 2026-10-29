# Agent Instructions

## Core Rules
- Work on one task at a time.
- Read relevant files and project structure before editing.
- Make the smallest complete change that solves the task.
- Prefer real tool use over text-only answers.
- Preserve existing work unless the task requires changing it.
- Ask before deleting files, resetting git state, or changing system configuration.

## Tool Use (Critical – Read Carefully)
- Tool calling is fully enabled and working.
- You MUST use tools to read files, list directories, run commands, and inspect the project.
- NEVER say any of the following:
  - “tool calls appear to be disabled”
  - “file reading tools are not working”
  - “tools seem to be disabled in this session”
  - “I cannot use tools”
  - “Let me try a different approach because tools are broken”
- If a tool call fails, report the actual error. Do not invent a general “tools are disabled” explanation.
- Do not narrate multiple failed attempts. Make a proper tool call and use the result.
- Prefer using the correct file-reading tool (or shell) once, instead of repeatedly saying you will try to read the file.

## Progress Tracking
- For any non-trivial task, maintain `PROGRESS.md` in the project root.
- Update it after significant steps and before context gets full.
- Include:
  - Goal and acceptance criteria
  - Current status (completed / in progress / blocked)
  - Files changed and what changed
  - Commands run and their actual results
  - Exact error messages and fixes attempted
  - Important decisions
  - Numbered next steps
- Before resuming after a break or context compaction, read `PROGRESS.md` and check the actual files. Do not rely only on memory.

## Coding & Editing
- Follow the existing project style, naming, and architecture.
- Prefer complete working changes over stubs or TODOs.
- Do not invent APIs, config keys, file paths, or tool options. Check the project first.
- When a full file is needed, write the complete file.
- Keep unrelated refactors out of the task.
- When reading project files (especially `.csproj`, `.props`, `Environment.props`, etc.), actually read their content. Do not just print the path.

## Commands & Verification
- Use the project’s existing scripts and tooling when possible.
- After making changes, verify them with the appropriate command when practical.
- Report the exact command and its output when something fails.
- Do not claim a build or test passed unless it was actually run and succeeded.

## Git
- Check `git status` before making changes if the project uses Git.
- Do not commit, push, reset, or rebase unless explicitly asked.
- Preserve uncommitted work.

## Valheim / Thunderstore Packaging
- When building a new DLL, always print the **full system file path** of the resulting `.dll`.
- When creating a release ZIP for Thunderstore:
  - Always place the final `.zip` inside the project’s `/dist` directory.
  - Always print the **full system file path** of the created `.zip`.
  - Structure the zip correctly for Thunderstore (plugins folder, manifest, icon, README, etc. as required by the mod).
  - Do not leave the zip in the project root or other random locations.

## Windows Shell Rules (Critical)
- The outer shell is bash (WSL / Git Bash style).
- When using PowerShell, the **entire** PowerShell command must be inside the `-Command "..."` string.
- Never pipe PowerShell output to bash cmdlets (`| Format-List`, `| Select-Object`, etc. outside the quotes).
- Correct pattern example:

  powershell -NoProfile -Command "Get-ChildItem 'D:\AI-Agent-Projects\cjaycraft\Cjayride.BossLootChests\dist\*.zip' | Format-List Name, Length, FullName"

- Wrong pattern (will fail with exit 127):

  powershell ... | Format-List ...

- Prefer PowerShell for Windows file and .NET operations. Avoid mixing bash syntax (`||`, `2>/dev/null`, `grep`, `dir /b`) with PowerShell in the same command.

## Working with ZIP files
- To list the contents of a zip file, always use this exact pattern:

  powershell -NoProfile -Command "[System.IO.Compression.ZipFile]::OpenRead('FULL_PATH_TO_ZIP').Entries | ForEach-Object { $_.FullName }"

- Do not use `New-Object IO.Compression.ZipFile` or similar incorrect syntax.
- Always keep the entire PowerShell expression inside the `-Command "..."` quotes.

## .NET DLL Version Checking
- When checking a .NET DLL version, use one of these exact patterns:

  powershell -NoProfile -Command "(Get-Item 'FULL_PATH_TO_DLL').VersionInfo | Format-List *"

  or

  powershell -NoProfile -Command "[System.Diagnostics.FileVersionInfo]::GetVersionInfo('FULL_PATH_TO_DLL') | Format-List FileVersion, ProductVersion, FileName"

- Always use the **full absolute path** to the DLL.
- Do not use `ildasm`, `Get-FileVersion`, or invent cmdlets that do not exist.
- If a previous command failed with a syntax error, do not keep retrying slight variations of the same broken command. Switch to the exact patterns above.

## Communication
- Be concise and specific.
- State what changed, which files were touched, and how it was verified.
- If blocked, clearly state the blocker and the next action needed.
- Only ask questions when a missing detail prevents correct progress.

## Tool Call JSON Rules (Important)
- Keep tool call arguments reasonably short.
- Prefer multiple smaller tool calls over one extremely long command.
- When writing PowerShell, keep the command concise. Do not put huge multi-line scripts inside a single tool call if possible.
- Always properly close strings and escape quotes correctly.
- If you need to write a long file, prefer writing it in parts or using a here-string carefully.
- Never leave an open quote in a tool call argument.

## File Access Rules
- If a tool returns “No permission to read” or similar, you must call `request_readonly_access` (or the equivalent access request tool) on that path before trying to read or list it again.
- Always request access to the project root early if you need to work in it.
- Example:
  request_readonly_access(path="D:/AI-Agent-Projects/cjaycraft/Cjayride.BossLootChests")