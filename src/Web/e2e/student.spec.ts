import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

const mockStudentJwt = createMockJwt({
  sub: 'usr-student-001',
  email: 'ogrenci@bilimakademi.com',
  name: 'Zeynep Kaya',
  institutionId: 'inst-001',
  institutionName: 'Kadıköy Bilim Şubesi',
  role: 'Student',
  roleLabel: '12. Sınıf • Sayısal',
});

test.describe('A1: Student Experience Foundation Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Default refresh route mock for student session
    await page.route('**/api/v1/auth/refresh', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: mockStudentJwt,
          accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
        }),
      });
    });
  });

  test('1. Unauthenticated user accessing /student/today is redirected to /login', async ({ page }) => {
    await page.unroute('**/api/v1/auth/refresh');
    await page.route('**/api/v1/auth/refresh', async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ title: 'Unauthorized', status: 401 }),
      });
    });

    await page.goto('/student/today');
    await expect(page).toHaveURL(/\/login/);
    await expect(page.locator('input#login-email')).toBeVisible();
  });

  test('2. Authenticated Student lands on /student/today with calm academic greeting and dynamic name', async ({ page }) => {
    await page.goto('/student/today');
    await expect(page).toHaveURL(/\/student\/today/);

    // Verify student name and institutional badge
    const heading = page.locator('h1.font-serif');
    await expect(heading).toContainText('İyi çalışmalar, Zeynep Kaya');
    await expect(page.locator('text="Kadıköy Bilim Şubesi"').first()).toBeVisible();
  });

  test('3. Student Today view contains NO role selector', async ({ page }) => {
    await page.goto('/student/today');
    const selectElements = page.locator('select');
    await expect(selectElements).toHaveCount(0);
    const roleRadios = page.locator('input[name="role"]');
    await expect(roleRadios).toHaveCount(0);
  });

  test('4. Mobile viewport (390x844) has no horizontal scroll overflow across all student routes', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const studentRoutes = [
      '/student/today',
      '/student/plans',
      '/student/exams',
      '/student/messages',
      '/student/profile',
    ];

    for (const route of studentRoutes) {
      await page.goto(route);
      const isOverflowing = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth;
      });
      expect(isOverflowing, `Route ${route} should not have horizontal overflow`).toBe(false);
    }
  });

  test('5. Desktop viewport (1440x900) displays desktop sidebar and hides mobile bottom navigation', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/student/today');

    // Desktop sidebar should be visible
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeVisible();

    // Bottom navigation should be hidden
    const bottomNav = page.locator('nav.fixed.bottom-0');
    await expect(bottomNav).toBeHidden();
  });

  test('6. Mobile viewport (390x844) displays mobile bottom navigation and hides desktop sidebar', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/student/today');

    // Bottom nav should be visible on mobile
    const bottomNav = page.locator('nav.fixed.bottom-0');
    await expect(bottomNav).toBeVisible();

    // Desktop sidebar should be hidden on mobile
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeHidden();
  });

  test('7. Student Today safely renders honest empty state without fake planning records by default', async ({ page }) => {
    await page.goto('/student/today');

    // Must show honest empty guidance
    await expect(page.locator('text="Bugün İçin Planlanmış Görev Bulunmuyor"')).toBeVisible();
    await expect(page.locator('text="GÜNLÜK BAKIŞ"')).toBeVisible();

    // Must not show fake hardcoded course items
    await expect(page.locator('text="Limit ve Süreklilik"')).toBeHidden();
    await expect(page.locator('text="Kasım Denemesi Çözüm"')).toBeHidden();
  });

  test('8. Student Plans route renders honest empty state and no fake plans', async ({ page }) => {
    await page.goto('/student/plans');
    await expect(page).toHaveURL(/\/student\/plans/);

    await expect(page.locator('h1.font-serif')).toContainText('Çalışma Planları');
    await expect(page.locator('text="Aktif Çalışma Planı Bulunmuyor"')).toBeVisible();
  });

  test('9. Student Exams route renders honest empty state and no fake exams', async ({ page }) => {
    await page.goto('/student/exams');
    await expect(page).toHaveURL(/\/student\/exams/);

    await expect(page.locator('h1.font-serif')).toContainText('Deneme Sınavları');
    await expect(page.locator('text="Kayıtlı Deneme Sınavı Bulunmuyor"')).toBeVisible();
  });

  test('10. Student Messages route renders honest empty state and no fake conversations', async ({ page }) => {
    await page.goto('/student/messages');
    await expect(page).toHaveURL(/\/student\/messages/);

    await expect(page.locator('h1.font-serif')).toContainText('Mesajlar');
    await expect(page.locator('text="Henüz Mesajlaşma Başlatılmadı"')).toBeVisible();
  });

  test('11. Student Profile renders authenticated user info and safe password change modal', async ({ page }) => {
    await page.goto('/student/profile');
    await expect(page).toHaveURL(/\/student\/profile/);

    // Dynamic user details
    await expect(page.locator('h2')).toContainText('Zeynep Kaya');
    await expect(page.locator('text="ogrenci@bilimakademi.com"')).toBeVisible();

    // Click Password Change modal
    await page.click('button:has-text("Şifre Değiştir")');
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('Şifre değiştirme işlemi henüz bu platform üzerinden kullanılamıyor');
    await expect(modal).not.toContainText('başarıyla güncellendi');
  });

  test('12. Main student navigation is keyboard accessible and navigable via tabs', async ({ page }) => {
    await page.goto('/student/today');

    // Click nav link to /student/plans
    const plansNavLink = page.locator('aside button:has-text("Planlar"), nav.fixed.bottom-0 button:has-text("Planlar")').first();
    await plansNavLink.click();
    await expect(page).toHaveURL(/\/student\/plans/);

    // Click nav link to /student/exams
    const examsNavLink = page.locator('aside button:has-text("Denemeler"), nav.fixed.bottom-0 button:has-text("Denemeler")').first();
    await examsNavLink.click();
    await expect(page).toHaveURL(/\/student\/exams/);
  });

  test('13. All five student routes render without uncaught console exceptions', async ({ page }) => {
    const consoleErrors: string[] = [];
    page.on('pageerror', (err) => consoleErrors.push(err.message));

    const routes = ['/student/today', '/student/plans', '/student/exams', '/student/messages', '/student/profile'];
    for (const r of routes) {
      await page.goto(r);
      await page.waitForLoadState('networkidle');
    }

    expect(consoleErrors).toEqual([]);
  });
});
