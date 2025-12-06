### What is this?

Very naive code for pinning Python deps.

### How it works

<img width="1195" height="683" alt="image" src="https://github.com/user-attachments/assets/54965077-1c1a-4e1f-9cd7-25f8053ed372" />

### How to use

1. Go to directory of Python project
2. Run the following:

```bash
(
  TMP="./PinPythonDeps.cs"
  trap 'rm -f "$TMP"' EXIT

  { 
    echo '#!/usr/bin/env dotnet run'
    curl -sL "https://raw.githubusercontent.com/budgetdevv/PinPythonDeps/refs/heads/main/PinPythonDeps/PinPythonDeps.cs"
  } > "$TMP"

  chmod +x "$TMP"
  "$TMP"
)
```
