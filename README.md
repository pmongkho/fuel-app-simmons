# fuel-app

Your command output confirms the issue is a **git submodule pointer (gitlink)**, not Rider or VS Code:
- `160000 ... angular-client` means Git is tracking `angular-client` as a pointer to another repo commit.
- So GitHub gets only that pointer instead of your Angular files.

## Quick fix (on the machine that has your Angular files)
Run:

```bash
./fix-angular-client-gitlink.sh
git status
git commit -m "Track angular-client as regular files instead of gitlink"
git push
```

## Manual verification
Run:

```bash
git ls-files --stage | rg '^160000'
```

After fix, `angular-client` should **not** appear in that output.
