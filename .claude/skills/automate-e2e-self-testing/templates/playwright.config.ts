import { defineConfig, devices } from '@playwright/test';

// Starter config for the self-testing spine. Clone and adapt:
//   - Vite SPA:  webServer command `npm run dev:mock` (or build+preview), baseURL :5173/:4173
//   - Next.js:   webServer command `npm run dev` (mock env) or build + `next start`, baseURL :3000
// Runner options stay top-level; context options go inside `use` (misplaced runner opts are ignored).
const PORT = 4173;
const baseURL = `http://127.0.0.1:${PORT}`;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0, // environmental flakes only; 0 locally
  reporter: [['list'], ['html', { open: 'never' }], ['json', { outputFile: 'playwright-report/report.json' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Log in once; dependent projects reuse the saved state (see templates/fixtures.ts).
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      testIgnore: /.*\.setup\.ts/,
      dependencies: ['setup'],
      use: { ...devices['Desktop Chrome'], storageState: 'playwright/.auth/user.json' },
    },
  ],
  // Only launches the process + checks readiness. Never seed/migrate here.
  webServer: {
    command: 'npm run build && npm run preview -- --port 4173',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
