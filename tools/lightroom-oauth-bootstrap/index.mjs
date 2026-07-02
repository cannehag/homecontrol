// One-time script to obtain an Adobe Lightroom API refresh token.
//
// Usage (PowerShell):
//   $env:LR_CLIENT_ID="..."; $env:LR_CLIENT_SECRET="..."; node index.mjs
//
// Usage (bash):
//   LR_CLIENT_ID=... LR_CLIENT_SECRET=... node index.mjs
//
// The redirect URI below does not need to resolve to anything real - Adobe requires
// it to be HTTPS, and requires it to be registered on the Developer Console project,
// but nothing needs to be listening on it. After login/consent, the browser will fail
// to load the redirect and show an address bar like:
//   https://localhost/callback?code=XXXX&state=YYYY
// Copy that whole URL and paste it back into this script when prompted.

import { randomBytes } from 'node:crypto';
import { createInterface } from 'node:readline/promises';
import { stdin, stdout } from 'node:process';

const AUTHORIZE_URL = 'https://ims-na1.adobelogin.com/ims/authorize/v2';
const TOKEN_URL = 'https://ims-na1.adobelogin.com/ims/token/v3';
const REDIRECT_URI = 'https://localhost/callback';
const SCOPE = 'openid,lr_partner_apis,offline_access';

const clientId = process.env.LR_CLIENT_ID;
const clientSecret = process.env.LR_CLIENT_SECRET;

if (!clientId || !clientSecret) {
  console.error('Set LR_CLIENT_ID and LR_CLIENT_SECRET environment variables first.');
  process.exit(1);
}

const state = randomBytes(16).toString('hex');

const authorizeUrl = new URL(AUTHORIZE_URL);
authorizeUrl.searchParams.set('client_id', clientId);
authorizeUrl.searchParams.set('redirect_uri', REDIRECT_URI);
authorizeUrl.searchParams.set('response_type', 'code');
authorizeUrl.searchParams.set('scope', SCOPE);
authorizeUrl.searchParams.set('state', state);

console.log('1. Open this URL, log in with your Adobe account, and approve access:\n');
console.log(authorizeUrl.toString());
console.log(
  '\n2. The browser will redirect to a page that fails to load (that is expected) -',
  '\n   copy the full URL from the address bar and paste it below.\n'
);

const rl = createInterface({ input: stdin, output: stdout });
const pastedUrl = (await rl.question('Paste the redirected URL: ')).trim();
rl.close();

let code;
try {
  const parsed = new URL(pastedUrl);
  code = parsed.searchParams.get('code');
  const returnedState = parsed.searchParams.get('state');
  if (returnedState !== state) {
    console.error('State mismatch - the pasted URL does not match this session. Aborting.');
    process.exit(1);
  }
} catch {
  console.error('Could not parse that as a URL. Aborting.');
  process.exit(1);
}

if (!code) {
  console.error('No "code" query param found in the pasted URL. Aborting.');
  process.exit(1);
}

const basicAuth = Buffer.from(`${clientId}:${clientSecret}`).toString('base64');
const body = new URLSearchParams({
  grant_type: 'authorization_code',
  code,
  redirect_uri: REDIRECT_URI,
});

const response = await fetch(TOKEN_URL, {
  method: 'POST',
  headers: {
    Authorization: `Basic ${basicAuth}`,
    'Content-Type': 'application/x-www-form-urlencoded',
  },
  body: body.toString(),
});

const text = await response.text();

if (!response.ok) {
  console.error(`Token exchange failed (${response.status}):\n${text}`);
  process.exit(1);
}

const tokens = JSON.parse(text);

if (!tokens.refresh_token) {
  console.error(
    'Response did not include a refresh_token - double check the "offline_access" scope was granted.\n',
    JSON.stringify(tokens, null, 2)
  );
  process.exit(1);
}

console.log('\nSuccess. Paste these into Site/config/secrets.json (gitignored):\n');
console.log(`  "lightroom-client-secret": "${clientSecret}"`);
console.log(`  "lightroom-refresh-token": "${tokens.refresh_token}"`);
console.log(`\nAccess token (short-lived, just for the probe call below):\n`);
console.log(tokens.access_token);
console.log(
  '\nProbe call to inspect real album/folder JSON shape before writing C# DTOs:\n',
  `\n  curl -H "Authorization: Bearer ${tokens.access_token}" -H "X-API-Key: ${clientId}" \\` +
    `\n    https://lr.adobe.io/v2/catalog\n`,
  '\n(Note: Lightroom API JSON responses are prefixed with `while (1) {}` - strip that line before parsing.)'
);
