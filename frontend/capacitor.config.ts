import type { CapacitorConfig } from '@capacitor/cli';

// Insecure options (plain HTTP API called from the https://localhost WebView origin) are only
// enabled for local development: `$env:CAP_ENV='dev'; npx cap sync android`.
const isDev = process.env['CAP_ENV'] === 'dev';

const config: CapacitorConfig = {
  appId: 'io.ionic.starter',
  appName: 'frontend',
  webDir: 'www',
  server: {
    androidScheme: 'https',
    ...(isDev && { cleartext: true }),
  },
  ...(isDev && { android: { allowMixedContent: true } }),
};

export default config;
