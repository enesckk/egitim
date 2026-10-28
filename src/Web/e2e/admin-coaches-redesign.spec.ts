import { test, expect } from '@playwright/test';

test.describe('Admin Coaches Redesign - Real Backend & Zero Fabricated Metrics', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Institution Admin
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    await page.locator('input[type="email"]').fill('admin.demo@egitim.com');
    await page.locator('input[type="password"]').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Admin Coaches - Real coach roster, verified student assignments, no fake metrics and responsive viewports', async ({ page }) => {
    await page.goto('/admin/coaches');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-coaches-view')).toBeVisible({ timeout: 10000 });

    // Verify Header & Real Identity
    await expect(page.getByRole('heading', { name: 'Öğrenci Koçları' })).toBeVisible();
    await expect(page.getByText('Yeni Koç Ekle')).toBeVisible();

    // Verify fabricated metrics are GONE
    await expect(page.getByText(/Plan Uyumu/i)).not.toBeVisible();
    await expect(page.getByText(/%88/i)).not.toBeVisible();
    await expect(page.getByText(/Haftalık Ort/i)).not.toBeVisible();
    await expect(page.getByText(/24 sa/i)).not.toBeVisible();
    await expect(page.getByText(/\/ 25/i)).not.toBeVisible();

    // Verify real coach cards exist
    const coachCards = page.locator('[data-testid="admin-coaches-view"] .grid > div');
    const count = await coachCards.count();
    expect(count).toBeGreaterThan(0);

    // Verify Search filter
    const searchInput = page.locator('input[placeholder*="Koç adı ile filtrele"]');
    await expect(searchInput).toBeVisible();

    // Open Coach Detail Modal
    const detailBtn = page.getByRole('button', { name: 'Detay İncele' }).first();
    await detailBtn.click();
    await expect(page.getByRole('heading', { name: 'Koç Portföy Detayı' })).toBeVisible({ timeout: 5000 });
    await expect(page.getByText('Atanmış Öğrenci Portföyü')).toBeVisible();

    // Close Modal
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Koç Portföy Detayı' })).not.toBeVisible();

    // Open Student Assignment Modal & perform live assignment verification
    const assignBtn = page.getByRole('button', { name: 'Öğrenci Ata' }).first();
    await assignBtn.click();
    await expect(page.getByRole('heading', { name: /Öğrenci Ata/i })).toBeVisible({ timeout: 5000 });
    await expect(page.getByRole('button', { name: 'Atamayı Onayla' })).toBeVisible();

    // Select first assignable student if available
    const unassignedOption = page.locator('div.max-h-52 > div').filter({ hasNotText: 'Zaten Atanmış' }).first();
    const canAssign = await unassignedOption.count();
    if (canAssign > 0) {
      await unassignedOption.click();
      await page.getByRole('button', { name: 'Atamayı Onayla' }).click();
      await expect(page.getByText('Öğrenci koça başarıyla atandı')).toBeVisible({ timeout: 5000 });
      await expect(page.getByRole('heading', { name: /Öğrenci Ata/i })).not.toBeVisible({ timeout: 5000 });
    } else {
      await page.getByRole('button', { name: 'İptal', exact: true }).click();
    }

    // Responsive viewports capture
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.waitForTimeout(300);

      // Verify zero horizontal overflow
      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 2);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/admin-coaches-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
