import { test, expect } from '@playwright/test';
import { API_BASE } from '../helpers/e2e.config';

test.describe('Authentication', () => {
  test('unauthenticated user is redirected to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText('Welcome back')).toBeVisible();
  });

  test('register + auto-redirect to board', async ({ page }) => {
    const unique = Date.now();
    await page.goto('/register');

    await page.getByLabel('Display name').fill(`Test User ${unique}`);
    await page.getByLabel('Email').fill(`test-${unique}@lemondo.dev`);
    await page.getByLabel('Password').fill('TestPass123!');
    await page.getByRole('button', { name: 'Create account' }).click();

    // Should redirect to the board
    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible({ timeout: 10000 });
    await expect(page).toHaveURL('/');
  });

  test('login with valid credentials', async ({ page }) => {
    const unique = Date.now();
    const email = `login-test-${unique}@lemondo.dev`;

    // Register via API first
    await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password: 'TestPass123!', displayName: 'Login Test' }),
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill('TestPass123!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('heading', { name: 'To Do' })).toBeVisible({ timeout: 10000 });
  });

  test('login with wrong password shows error', async ({ page }) => {
    const unique = Date.now();
    const email = `wrong-pw-${unique}@lemondo.dev`;

    // Register via API first
    await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password: 'TestPass123!', displayName: 'Wrong PW' }),
    });

    await page.goto('/login');
    await page.getByLabel('Email').fill(email);
    await page.getByLabel('Password').fill('WrongPassword999!');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByText('Invalid email or password')).toBeVisible();
  });

  test('navigate between login and register', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('link', { name: 'Create one' }).click();
    await expect(page).toHaveURL(/\/register/);
    await expect(page.getByText('Create your account')).toBeVisible();

    await page.getByRole('link', { name: 'Sign in' }).click();
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText('Welcome back')).toBeVisible();
  });
});
