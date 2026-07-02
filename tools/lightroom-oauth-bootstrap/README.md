# Lightroom OAuth bootstrap

One-time script to get a long-lived Adobe Lightroom API refresh token. Not part of the
deployed `Site`/`App` - run this locally, once, then throw the terminal output away
(after copying the printed values into `Site/config/secrets.json`).

## 1. Register an Adobe app (one-time, in the browser)

1. Go to https://console.adobe.io, sign in with your Adobe ID, create a new project.
2. "Add API" -> Creative Cloud -> "Lightroom Services" -> OAuth Web App credential.
3. Set the redirect URI to `https://localhost/callback` (it does not need to resolve to
   anything - see below).
4. Note the generated Client ID and Client Secret.

This can stay in "Development" status - no Adobe approval process is needed since only
your own Adobe account is authenticating (see the project notes for why).

## 2. Run the script

```
$env:LR_CLIENT_ID="<client id>"
$env:LR_CLIENT_SECRET="<client secret>"
node index.mjs
```

(bash: `LR_CLIENT_ID=... LR_CLIENT_SECRET=... node index.mjs`)

It prints an Adobe login URL. Open it, log in, approve. The browser will then try to
load `https://localhost/callback?code=...&state=...` and fail (nothing is listening
there) - that's expected. Copy the full URL from the address bar and paste it back into
the terminal when prompted.

The script exchanges the code for tokens and prints the refresh token plus the exact
lines to paste into `Site/config/secrets.json`.

## 3. Probe the real API shape

The script also prints a ready-to-run `curl` command using the short-lived access
token. Use it (and the album-listing endpoint) to inspect the real JSON shape - field
names, `subtype` values for folders vs. albums, the `parent` field - before writing any
C# models. Don't assume the docs match exactly what a real personal catalog returns.
