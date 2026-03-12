# fuel-app

Your command output confirms the issue:
- `160000 ... angular-client` means Git is tracking `angular-client` as a gitlink (submodule pointer), not regular files.
- So GitHub receives only the pointer, which is why only `dotnet-server` files appear and the Angular app is blank in deployments.

## Quick fix (on the machine that has your Angular files)
Run:

```bash
./fix-angular-client-gitlink.sh
git status
git commit -m "Track angular-client as regular files instead of gitlink"
git push
```

## Manual verification
- `git ls-tree HEAD | sed -n '1,20p'`
- After fix, `angular-client` should no longer be `160000`; it should be normal file/tree entries.
