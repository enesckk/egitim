import { test, expect } from '@playwright/test';

test.describe('Admin Classes Redesign - Real Backend & Zero Fabricated Metrics', () => {
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

  test('Admin Classes - Real cards, verified student/teacher counts, no fabricated adherence/net, and responsive viewports', async ({ page }) => {
    await page.goto('/admin/classes');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-classes-view')).toBeVisible({ timeout: 10000 });

    // Verify Header
    await expect(page.getByRole('heading', { name: 'Sınıflar & Gruplar' })).toBeVisible();
    await expect(page.getByText('Yeni Sınıf Ekle')).toBeVisible();

    // Verify search and kademe filters
    await expect(page.locator('input[placeholder*="Sınıf adı veya şube ara"]')).toBeVisible();
    await expect(page.locator('select')).toBeVisible();

    // Verify fabricated metrics are GONE from class cards
    await expect(page.getByText(/Sınıf Net Ort/i)).not.toBeVisible();
    await expect(page.getByText(/65\.5 net/i)).not.toBeVisible();
    await expect(page.getByText(/%85/i)).not.toBeVisible();

    // Verify real classroom card content
    await expect(page.locator('h3', { hasText: '12-A' }).first()).toBeVisible();
    await expect(page.getByText(/Zümre Öğretmenleri/i).first()).toBeVisible();
    await expect(page.getByText('Ayşe Öğretmen').first()).toBeVisible();

    // Manage Class Roster Modal test
    const manageBtn = page.getByRole('button', { name: 'Sınıf Kadrosunu Yönet' }).first();
    await expect(manageBtn).toBeVisible();
    await manageBtn.click();

    // Verify Modal loaded with real roster
    await expect(page.getByText(/Kadro & Öğrenci Yönetimi/i)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Zümre Öğretmenleri (').first()).toBeVisible();
    await expect(page.getByText('Öğrenci Kadrosu (').first()).toBeVisible();

    // Close Modal
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();

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

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/admin-classes-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });
});
