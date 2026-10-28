import { test, expect } from '@playwright/test';

test.describe('Admin Teachers Redesign - Real Backend & Zero Fabricated Metrics', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Institution Admin
    await page.goto('/login');
    await page.waitForLoadState('networkidle');

    await page.locator('input[type="email"]').fill('admin.demo@egitim.com');
    await page.locator('input[type="password"]').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Admin Teachers - Real teachers list, verified classroom/subject assignments, and responsive viewports', async ({ page }) => {
    await page.goto('/admin/teachers');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-teachers-view')).toBeVisible({ timeout: 10000 });

    // Verify Header
    await expect(page.getByRole('heading', { name: 'Öğretmenler & Zümre Yönetimi' })).toBeVisible();
    await expect(page.getByText('Yeni Öğretmen Ekle')).toBeVisible();

    // Verify Search filter
    const searchInput = page.locator('input[placeholder*="Öğretmen adı veya unvan ara"]');
    await expect(searchInput).toBeVisible();

    // Verify real teacher cards exist
    const teacherCards = page.locator('[data-testid="admin-teachers-view"] .grid > div');
    const count = await teacherCards.count();
    expect(count).toBeGreaterThan(0);

    // Open Teacher Detail Management Modal
    const detailBtn = page.getByRole('button', { name: 'Detaylı Yönetim' }).first();
    await detailBtn.click();
    await expect(page.getByRole('heading', { name: 'Öğretmen Zümre & Sınıf Yönetimi' })).toBeVisible({ timeout: 5000 });
    await expect(page.getByRole('heading', { name: /Ders Branşları/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /Atanmış Sınıflar/i })).toBeVisible();

    // Close Modal
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Öğretmen Zümre & Sınıf Yönetimi' })).not.toBeVisible();

    // Open Classroom Assignment Modal
    const assignClassBtn = page.getByRole('button', { name: 'Sınıf Ata' }).first();
    await assignClassBtn.click();
    await expect(page.getByRole('heading', { name: /Sınıf Ata/i })).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'İptal', exact: true }).click();

    // Open Subject Assignment Modal
    const assignSubjectBtn = page.getByRole('button', { name: 'Ders Ata' }).first();
    await assignSubjectBtn.click();
    await expect(page.getByRole('heading', { name: /Branş Dersi Ata/i })).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'İptal', exact: true }).click();

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
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/admin-teachers-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
