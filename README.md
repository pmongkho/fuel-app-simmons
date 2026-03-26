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


## Render deploy/env vars (current production)

Backend service URL:
- `https://fuel-app-simmons.onrender.com`

Database URL:
- `postgresql://fuel_app_simmons_user:xcrZ4WpBTKWHefScIDBAgmJhvbt2HpKy@dpg-d6rmub7gi27c73daq9p0-a/fuel_app_simmons`

Set these in Render for the .NET API service:
- `ConnectionStrings__DefaultConnection` = `Host=dpg-d6rmub7gi27c73daq9p0-a;Port=5432;Database=fuel_app_simmons;Username=fuel_app_simmons_user;Password=xcrZ4WpBTKWHefScIDBAgmJhvbt2HpKy;SSL Mode=Require;Trust Server Certificate=true`
- (optional alternative) `DATABASE_URL` = `postgresql://fuel_app_simmons_user:xcrZ4WpBTKWHefScIDBAgmJhvbt2HpKy@dpg-d6rmub7gi27c73daq9p0-a/fuel_app_simmons`
- `Resend__ApiKey` = your Resend API key
- `Resend__FromEmail` = verified sender email/domain in Resend
- `Resend__FromName` = optional sender display name, for example `Fuel App`

These values are read from the app settings configuration (`Resend` section).

### Resend without your own domain (quick test mode)

If you do not have a domain yet, in **Development** the backend defaults the sender to:
- `onboarding@resend.dev`

This allows quick testing with just an API key. In this mode, Resend typically only delivers to your own Resend account email until you verify a domain/sender.

In **non-development environments** (production/staging), the API now requires `FromEmail` to be configured. If missing, sends are skipped and logged so you do not silently route through onboarding mode.

Angular production API URL is hardcoded in `angular-client/src/environments/environment.production.ts` to:
- `https://fuel-app-simmons.onrender.com/api`

## Backend production cleanup

For production database cleanup / admin SQL commands, see:
- `dotnet-server/docs/production-sql.md`


## Azure Blob + OCR env vars

Set these on the API service (recommended via environment variables, not committed secrets):
- `BlobStorage__AccountName` = `fuelappsimmons`
- `BlobStorage__AccessKey` = your Azure storage access key
- `BlobStorage__ContainerName` = `fuel-photos`
- `GaugeOcr__Endpoint` = Azure OCR endpoint
- `GaugeOcr__ApiKey` = Azure OCR API key

If you prefer, you can still set `BlobStorage__ConnectionString` directly instead of account name/access key.
