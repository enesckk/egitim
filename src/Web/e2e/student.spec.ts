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
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/student\/today/);

    // Verify student name and institutional badge
    const heading = page.locator('h1.font-serif');
    await expect(heading).toContainText('İyi çalışmalar, Zeynep Kaya');
    await expect(page.locator('text="Kadıköy Bilim Şubesi"').first()).toBeVisible();
  });

  test('3. Student Today view contains NO role selector', async ({ page }) => {
    await page.goto('/student/today');
    await page.waitForLoadState('networkidle');
    const selectElements = page.locator('select');
    await expect(selectElements).toHaveCount(0);
    const roleRadios = page.locator('input[name="role"]');
    await expect(roleRadios).toHaveCount(0);
  });

  test('4. Responsive: Mobile viewport (390x844) has no horizontal scroll overflow across all student routes', async ({ page }) => {
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
      await page.waitForLoadState('networkidle');
      const isOverflowing = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth;
      });
      expect(isOverflowing, `Route ${route} at 390x844 should not have horizontal overflow`).toBe(false);
    }
  });

  test('5. Responsive: Tablet viewport (768x1024) has no horizontal scroll overflow across all student routes', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    const studentRoutes = [
      '/student/today',
      '/student/plans',
      '/student/exams',
      '/student/messages',
      '/student/profile',
    ];

    for (const route of studentRoutes) {
      await page.goto(route);
      await page.waitForLoadState('networkidle');
      const isOverflowing = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth;
      });
      expect(isOverflowing, `Route ${route} at 768x1024 should not have horizontal overflow`).toBe(false);
    }
  });

  test('6. Responsive: Desktop viewport (1440x900) displays desktop sidebar and hides mobile bottom navigation', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/student/today');
    await page.waitForLoadState('networkidle');

    // Desktop sidebar should be visible
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeVisible();

    // Bottom navigation should be hidden
    const bottomNav = page.locator('nav.fixed.bottom-0');
    await expect(bottomNav).toBeHidden();

    // No overflow at 1440x900
    const isOverflowing = await page.evaluate(() => {
      return document.documentElement.scrollWidth > window.innerWidth;
    });
    expect(isOverflowing).toBe(false);
  });

  test('7. Responsive: Mobile viewport (390x844) displays mobile bottom navigation and hides desktop sidebar', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/student/today');
    await page.waitForLoadState('networkidle');

    // Bottom nav should be visible on mobile
    const bottomNav = page.locator('nav.fixed.bottom-0');
    await expect(bottomNav).toBeVisible();

    // Desktop sidebar should be hidden on mobile
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeHidden();
  });

  test('8. Honest Data: Student Today renders honest empty state without fake planning records or unsupported "Aktif Takipte"', async ({ page }) => {
    await page.goto('/student/today');
    await page.waitForLoadState('networkidle');

    // Must show honest empty guidance
    await expect(page.locator('text="Bugün İçin Planlanmış Görev Bulunmuyor"')).toBeVisible();
    await expect(page.locator('text="GÜNLÜK BAKIŞ"')).toBeVisible();

    // Must not show fake hardcoded course items
    await expect(page.locator('text="Limit ve Süreklilik"')).toBeHidden();
    await expect(page.locator('text="Kasım Denemesi Çözüm"')).toBeHidden();

    // Must not claim unbacked "Aktif Takipte"
    await expect(page.locator('text="Aktif Takipte"')).toHaveCount(0);
  });

  test('9. Honest Data: Student Plans route renders honest empty state and no fake plans', async ({ page }) => {
    await page.goto('/student/plans');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/student\/plans/);

    await expect(page.locator('h1.font-serif')).toContainText('Çalışma Planları');
    await expect(page.locator('text="Aktif Çalışma Planı Bulunmuyor"')).toBeVisible();
  });

  test('10. Honest Data: Student Exams route renders honest empty state and no fake exams', async ({ page }) => {
    await page.goto('/student/exams');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/student\/exams/);

    await expect(page.locator('h1.font-serif')).toContainText('Deneme Sınavları');
    await expect(page.locator('text="Kayıtlı Deneme Sınavı Bulunmuyor"')).toBeVisible();
  });

  test('11. Honest Data: Student Messages route renders honest empty state and no fake conversations', async ({ page }) => {
    await page.goto('/student/messages');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/student\/messages/);

    await expect(page.locator('h1.font-serif')).toContainText('Mesajlar');
    await expect(page.locator('text="Henüz Mesajlaşma Başlatılmadı"')).toBeVisible();
  });

  test('12. Honest Data: Student Profile renders authenticated user info without hardcoded 2026 participation year', async ({ page }) => {
    await page.goto('/student/profile');
    await page.waitForLoadState('networkidle');
    await expect(page).toHaveURL(/\/student\/profile/);

    // Dynamic user details
    await expect(page.locator('h2')).toContainText('Zeynep Kaya');
    await expect(page.locator('text="ogrenci@bilimakademi.com"')).toBeVisible();

    // Ensure no hardcoded 2026 participation date
    await expect(page.locator('text="Platforma Katılım"')).toHaveCount(0);
    await expect(page.locator('text="2026"')).toHaveCount(0);
  });

  test('13. Modal Accessibility & Focus Trap: Password change modal traps focus, has accessible title, and closes on Escape', async ({ page }) => {
    await page.goto('/student/profile');
    await page.waitForLoadState('networkidle');

    const pwBtn = page.locator('button:has-text("Şifre Değiştir")');
    await pwBtn.focus();
    await page.keyboard.press('Enter');

    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();
    await expect(modal).toHaveAttribute('aria-modal', 'true');
    const titleId = await modal.getAttribute('aria-labelledby');
    expect(titleId).toBeTruthy();
    if (titleId) {
      await expect(page.locator(`#${titleId}`)).toContainText('Şifre Değiştir');
    }

    // Verify focus is inside modal
    const isInside = await page.evaluate(() => {
      const dialog = document.querySelector('[role="dialog"]');
      return dialog?.contains(document.activeElement);
    });
    expect(isInside).toBe(true);

    // Focus trap: tab through elements
    await page.keyboard.press('Tab');
    const isStillInside1 = await page.evaluate(() => {
      const dialog = document.querySelector('[role="dialog"]');
      return dialog?.contains(document.activeElement);
    });
    expect(isStillInside1).toBe(true);

    // Press Escape to close modal
    await page.keyboard.press('Escape');
    await expect(modal).toBeHidden();
  });

  test('14. Modal Accessibility & Truthful State: Notification settings modal shows unavailable notice with no fake save success', async ({ page }) => {
    await page.goto('/student/profile');
    await page.waitForLoadState('networkidle');

    await page.click('button:has-text("Bildirim Tercihleri")');
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible();
    await expect(modal).toContainText('Bildirim tercihleri henüz bu platform üzerinden değiştirilemiyor');

    // Ensure no fake "Kaydet" button
    await expect(modal.locator('button:has-text("Kaydet")')).toHaveCount(0);

    // Close modal with Kapat button
    await modal.locator('button:has-text("Kapat")').click();
    await expect(modal).toBeHidden();

    // Ensure no fake success message
    await expect(page.locator('text="başarıyla kaydedildi"')).toHaveCount(0);
  });

  test('15. Keyboard Navigation: Main navigation is navigable via Tab and Enter key presses', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/student/today');
    await page.waitForLoadState('networkidle');

    // Focus the first navigation button in aside
    const firstNavBtn = page.locator('aside nav button').first();
    await firstNavBtn.focus();
    await expect(firstNavBtn).toBeFocused();

    // Tab to the next nav item (Planlar)
    await page.keyboard.press('Tab');
    const activeElText = await page.evaluate(() => document.activeElement?.textContent);
    expect(activeElText).toContain('Planlar');

    // Press Enter to navigate
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/student\/plans/);
    await expect(page.locator('h1.font-serif')).toContainText('Çalışma Planları');
  });

  test('16. Console Safety: All five student routes render without uncaught console exceptions', async ({ page }) => {
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
