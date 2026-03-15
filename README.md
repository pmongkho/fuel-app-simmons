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

## Vercel deploy notes (Angular SPA)

If routes like `/admin/users` or `/reports/mine` 404 on refresh in Vercel, the app needs an SPA fallback rewrite.

This repo now includes `vercel.json` configured to:
- install dependencies in `angular-client`
- build with `npm run build --prefix angular-client`
- publish `angular-client/dist/angular-client`
- rewrite all requests to `/index.html` for SPA routing

If you use the Vercel dashboard, make sure:
- Framework preset: **Other** (or keep custom `vercel.json` handling)
- Root directory: repository root (since `vercel.json` is at root)
- Build command is not overriding the config above
