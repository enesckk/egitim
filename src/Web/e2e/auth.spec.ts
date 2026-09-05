import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Frontend Authentication Quality Gate Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Default: return 401 for refresh on initial page load unless overridden
    await page.route('**/api/v1/auth/refresh', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
      });
    });
  });

  test('1. Login screen has NO role selector and uses email input', async ({ page }) => {
    await page.goto('/login');

    // Verify there are no role selector elements (select dropdowns, role radio groups)
    const selectElements = page.locator('select');
    await expect(selectElements).toHaveCount(0);

    const roleRadios = page.locator('input[name="role"]');
    await expect(roleRadios).toHaveCount(0);

    // Verify email input
    const emailInput = page.locator('input#login-email');
    await expect(emailInput).toBeVisible();
    await expect(emailInput).toHaveAttribute('type', 'email');

    // Verify password input
    const passwordInput = page.locator('input#login-password');
    await expect(passwordInput).toBeVisible();
    await expect(passwordInput).toHaveAttribute('type', 'password');
  });

  test('2. Invalid login displays generic error message without leaking details', async ({ page }) => {
    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
      });
    });

    await page.goto('/login');
    await page.fill('input#login-email', 'wrong@example.com');
    await page.fill('input#login-password', 'WrongPassword123!');
    await page.click('button[type="submit"]');

    const alert = page.locator('[role="alert"]');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('E-posta adresi veya şifre hatalı');
  });

  test('3. Unknown or missing backend role fails closed and does NOT fallback to student', async ({ page }) => {
    const invalidJwt = createMockJwt({
      sub: 'usr-unknown-1',
      email: 'hacker@example.com',
      name: 'Unknown User',
      role: 'SuperHackerRole', // Unknown role
    });

    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ accessToken: invalidJwt }),
      });
    });

    await page.goto('/login');
    await page.fill('input#login-email', 'hacker@example.com');
    await page.fill('input#login-password', 'ValidPassword123!');
    await page.click('button[type="submit"]');

    // Should fail closed, display error and remain on login page, not redirect to /student
    const alert = page.locator('[role="alert"]');
    await expect(alert).toBeVisible();
    await expect(alert).toContainText('Geçersiz veya yetkisiz kimlik doğrulama belirteci');
    await expect(page).toHaveURL(/\/login/);
  });

  test('4. Protected route redirects unauthenticated user to /login', async ({ page }) => {
    await page.goto('/student');
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/coach');
    await expect(page).toHaveURL(/\/login/);

    await page.goto('/admin');
    await expect(page).toHaveURL(/\/login/);
  });

  test('5. Valid login navigates directly to correct role dashboard', async ({ page }) => {
    const studentJwt = createMockJwt({
      sub: 'usr-student-1',
      email: 'student@example.com',
      name: 'Öğrenci Ali',
      role: 'Student',
      institutionId: 'inst-1',
    });

    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ accessToken: studentJwt }),
      });
    });

    await page.goto('/login');
    await page.fill('input#login-email', 'student@example.com');
    await page.fill('input#login-password', 'ValidPassword123!');
    await page.click('button[type="submit"]');

    // Direct navigation based on authenticated result
    await expect(page).toHaveURL(/\/student/);
  });

  test('6. Startup initialize triggers single-flight refresh request', async ({ page }) => {
    let refreshCallCount = 0;
    await page.route('**/api/v1/auth/refresh', async (route) => {
      refreshCallCount++;
      // Return 401 after slight delay to simulate network latency
      await new Promise((resolve) => setTimeout(resolve, 50));
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
      });
    });

    await page.goto('/login');
    // Wait for network idle
    await page.waitForLoadState('networkidle');

    // Initial startup must make exactly 1 refresh request, not 2
    expect(refreshCallCount).toBe(1);
  });

  test('7. Mobile viewport (390x844) has no horizontal scroll overflow', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/login');

    const hasHorizontalScroll = await page.evaluate(() => {
      return document.documentElement.scrollWidth > window.innerWidth;
    });

    expect(hasHorizontalScroll).toBe(false);
  });

  test('8. Desktop Student view renders desktop sidebar and not mobile bottom nav', async ({ page }) => {
    const studentJwt = createMockJwt({
      sub: 'usr-student-2',
      email: 'student2@example.com',
      name: 'Öğrenci Ayşe',
      role: 'Student',
    });

    await page.route('**/api/v1/auth/login', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ accessToken: studentJwt }),
      });
    });

    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/login');
    await page.fill('input#login-email', 'student2@example.com');
    await page.fill('input#login-password', 'ValidPassword123!');
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL(/\/student/);

    // Verify desktop layout features
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeVisible();

    // Verify mobile navigation bar is hidden on desktop
    const mobileNav = page.locator('nav.md\\:hidden');
    if (await mobileNav.count() > 0) {
      await expect(mobileNav).not.toBeVisible();
    }
  });

  test('9. Password visibility toggle works correctly and accessibly', async ({ page }) => {
    await page.goto('/login');
    const passwordInput = page.locator('input#login-password');
    const toggleButton = page.locator('button[aria-label*="Şifreyi"]');

    await expect(passwordInput).toHaveAttribute('type', 'password');
    await expect(toggleButton).toHaveAttribute('aria-label', 'Şifreyi göster');

    // Click to show password
    await toggleButton.click();
    await expect(passwordInput).toHaveAttribute('type', 'text');
    await expect(toggleButton).toHaveAttribute('aria-label', 'Şifreyi gizle');

    // Click to hide password
    await toggleButton.click();
    await expect(passwordInput).toHaveAttribute('type', 'password');
    await expect(toggleButton).toHaveAttribute('aria-label', 'Şifreyi göster');
  });
});
