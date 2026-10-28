import { test, expect } from '@playwright/test';

test.describe('Coach Reports Redesign - Real Backend & Responsive Layout', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Demo Coach
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('coach.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    // Wait for redirect
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Coach Reports - Real API data load, KPIs, grade breakdown, and responsive viewports with zero horizontal overflow', async ({ page }) => {
    await page.goto('/coach/reports');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('coach-reports-view')).toBeVisible({ timeout: 10000 });

    // 1. Verify header title
    const heading = page.locator('h1', { hasText: 'Portföy Gelişim Raporu' });
    await expect(heading).toBeVisible();

    // 2. Verify 4 KPI cards exist
    await expect(page.getByText('Atanmış Öğrenci', { exact: true })).toBeVisible();
    await expect(page.getByText('Aktif Planlar', { exact: true })).toBeVisible();
    await expect(page.getByText('Ortalama Plan Uyumu', { exact: true })).toBeVisible();
    await expect(page.getByText('Koçluk Görevleri', { exact: true })).toBeVisible();

    // 3. Verify Grade Breakdown section
    await expect(page.getByText('Sınıf Bazında Portföy Dağılımı ve Plan Uyumu')).toBeVisible();

    // 4. Verify Student list / cards
    await expect(page.getByText('Öğrenci Gelişim ve Çalışma Durumları')).toBeVisible();

    // 5. Capture Responsive Screenshots & verify zero horizontal scroll at 390px, 768px, 1440px
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 }
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.waitForTimeout(500);

      const hasHorizontalScroll = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth;
      });
      expect(hasHorizontalScroll).toBe(false);

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-reports-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });

  test('Coach Reports - Student cards navigate to authorized student detail', async ({ page }) => {
    await page.goto('/coach/reports');
    await page.waitForLoadState('networkidle');

    const firstStudentCard = page.locator('div[class*="cursor-pointer"]:has(h4)').first();
    const count = await firstStudentCard.count();

    if (count > 0) {
      await firstStudentCard.click();
      await page.waitForURL(/\/coach\/students\/[a-f0-9-]+/, { timeout: 10000 });
      expect(page.url()).toContain('/coach/students/');
    }
  });
});
