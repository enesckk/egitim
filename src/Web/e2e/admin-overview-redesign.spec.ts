import { test, expect } from '@playwright/test';

test.describe('Admin Institution Overview Redesign - Real Backend & Corrected Semantics', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Admin Demo
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    // Wait for redirect
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Admin Overview - Corrected Semantics, KPI cards and responsive viewports without horizontal overflow', async ({ page }) => {
    await page.goto('/admin/overview');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-dashboard-view')).toBeVisible({ timeout: 10000 });

    // Header inspection
    await expect(page.getByRole('heading', { name: 'Kurumsal Genel Bakış' })).toBeVisible();
    await expect(page.getByText('Demo Eğitim Kurumu')).toBeVisible();
    await expect(page.locator('span', { hasText: 'Aktif Sınıf' })).toBeVisible();

    // Verify misleading "şubede" is not displayed in the student KPI subtitle
    await expect(page.getByText(/\d+ şubede/i)).not.toBeVisible();
    await expect(page.locator('p', { hasText: 'aktif sınıfta' })).toBeVisible();

    // Verify Real KPIs
    await expect(page.getByText('Aktif Öğrenci')).toBeVisible();
    await expect(page.getByText('Aktif Koç')).toBeVisible();
    await expect(page.getByText('Ortalama Plan Uyumu')).toBeVisible();

    // Verify Coach portfolio card and Attention card
    await expect(page.getByRole('heading', { name: 'Koçluk Portföy Durumu' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dikkat Gerektiren Alanlar' })).toBeVisible();

    // Responsive viewports capture
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.waitForTimeout(500);

      const hasHorizontalScroll = await page.evaluate(() => {
        return document.documentElement.scrollWidth > window.innerWidth;
      });
      expect(hasHorizontalScroll).toBe(false);

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/admin-overview-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });
});
