import { test as base, expect, type Page } from '@playwright/test';

// Two common starting points. Pick one per project.
//
// A) Mock/dev mode where any credentials sign in (deterministic, no real auth):
//    use the `signedIn` fixture below to skip the login steps in every spec.
//
// B) Real auth: prefer a `*.setup.ts` project that logs in once and saves
//    storageState, then mount it via the project `use.storageState` (see
//    templates/playwright.config.ts). Do NOT log in per test.

const EMAIL = 'demo@example.com';
const PASSWORD = 'any-password';

export const test = base.extend<{ signedIn: Page }>({
  signedIn: async ({ page }, use) => {
    await page.goto('/');
    // Accessibility-only locators - survive CSS/markup refactors.
    await page.getByLabel('Email').fill(EMAIL);
    await page.getByLabel('Password').fill(PASSWORD);
    await page.getByRole('button', { name: 'Sign in' }).click();
    // Web-first assertion doubles as the readiness wait - never waitForTimeout.
    await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible();
    await use(page);
  },
});

export { expect };

// Reference: a real-auth setup file (e2e/auth.setup.ts) would instead do:
//   await page.goto('/login'); ...fill + click...;
//   await expect(page).toHaveURL('/dashboard');
//   await page.context().storageState({ path: 'playwright/.auth/user.json' });
// and gitignore the playwright/.auth directory.
