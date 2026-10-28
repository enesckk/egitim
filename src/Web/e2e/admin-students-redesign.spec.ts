import { test, expect } from '@playwright/test';

test.describe('Admin Students Redesign - Real Backend & Zero Fabricated Metrics', () => {
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

  test('Admin Students - Real student directory, verified student info, no fake 85% adherence, and responsive viewports', async ({ page }) => {
    await page.goto('/admin/students');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-students-view')).toBeVisible({ timeout: 10000 });

    // Verify Header
    await expect(page.getByRole('heading', { name: 'Öğrenciler' })).toBeVisible();
    await expect(page.getByText('Yeni Öğrenci Ekle')).toBeVisible();

    // Verify search and filter controls
    await expect(page.locator('input[placeholder*="Öğrenci adı, okul veya numara ara"]')).toBeVisible();

    // Verify fabricated metrics are GONE
    await expect(page.getByText(/Plan Uyumu/i)).not.toBeVisible();
    await expect(page.getByText(/%85/i)).not.toBeVisible();

    // Verify real student directory rows exist
    const studentRows = page.locator('div.divide-y > div');
    const count = await studentRows.count();
    expect(count).toBeGreaterThan(0);

    // Open Student Detail Modal
    await studentRows.first().click();
    await expect(page.getByText('Öğrenci Durumu')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Danışman Koç Ata / Değiştir')).toBeVisible();

    // Close Modal
    await page.getByLabel('Kapat').click();

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

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/admin-students-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });
});
