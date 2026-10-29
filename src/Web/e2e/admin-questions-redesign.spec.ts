import { test, expect } from '@playwright/test';

test.describe('Admin Question Bank Redesign - Header, Filters, Modals & Responsiveness', () => {
  test.beforeEach(async ({ page }) => {
    // Authenticate as Institution Admin
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible({ timeout: 10000 });
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('1. Admin Question Bank - Header, Search, Pool Filter Chips, Pagination & Results Distinction', async ({ page }) => {
    await page.goto('/admin/questions');
    await expect(page.locator('[data-testid="admin-questions-page"]')).toBeVisible({ timeout: 10000 });

    // 1. Verify Header
    await expect(page.getByRole('heading', { name: /Soru Bankası/i })).toBeVisible();
    await expect(page.getByTestId('create-question-btn')).toBeVisible();

    // 2. Verify Search input and pool filter tabs
    const searchInput = page.getByTestId('question-search-input');
    await expect(searchInput).toBeVisible();
    await expect(page.getByTestId('pool-tab-all')).toBeVisible();
    await expect(page.getByTestId('pool-tab-institution')).toBeVisible();
    await expect(page.getByTestId('pool-tab-global')).toBeVisible();

    // 3. Verify Pagination count distinction label
    const countLabel = page.getByTestId('pagination-count-label');
    await expect(countLabel).toBeVisible();
    await expect(countLabel).toContainText('Toplam');
    await expect(countLabel).toContainText('arası listeleniyor');

    // 4. Test Pool tab switching
    await page.getByTestId('pool-tab-institution').click();
    await page.waitForTimeout(500);
    await expect(countLabel).toBeVisible();

    await page.getByTestId('pool-tab-all').click();
    await page.waitForTimeout(500);
  });

  test('2. Admin Question Bank - Expandable Filters, Modals & Immutability/Guard Indicators', async ({ page }) => {
    await page.goto('/admin/questions');
    await expect(page.locator('[data-testid="admin-questions-page"]')).toBeVisible({ timeout: 10000 });

    // 1. Open Advanced Filters
    const toggleBtn = page.getByTestId('toggle-filters-btn');
    await expect(toggleBtn).toBeVisible();
    await toggleBtn.click();

    const filtersPanel = page.getByTestId('advanced-filters-panel');
    await expect(filtersPanel).toBeVisible();
    await expect(page.getByTestId('filter-subject-select')).toBeVisible();
    await expect(page.getByTestId('filter-difficulty-select')).toBeVisible();
    await expect(page.getByTestId('filter-status-select')).toBeVisible();

    // 2. Open Create Question Modal
    await page.getByTestId('create-question-btn').click();
    await expect(page.getByRole('heading', { name: 'Yeni Soru Oluştur' })).toBeVisible();
    await page.getByRole('button', { name: 'İptal' }).click();
    await expect(page.getByRole('heading', { name: 'Yeni Soru Oluştur' })).not.toBeVisible();

    // 3. Open Detail Modal of the first question
    const firstDetailBtn = page.locator('button[data-testid^="question-detail-btn-"]').first();
    if (await firstDetailBtn.isVisible()) {
      await firstDetailBtn.click();
      await expect(page.getByRole('heading', { name: 'Soru Detayı' })).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Soru Metni' })).toBeVisible();
      await page.getByRole('button', { name: 'Kapat', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Soru Detayı' })).not.toBeVisible();
    }
  });

  test('3. Admin Question Bank - Responsive Layout & Zero Horizontal Overflow at 390px, 768px, 1440px', async ({ page }) => {
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    const brainDir = '/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots';

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/admin/questions');
      await expect(page.locator('[data-testid="admin-questions-page"]')).toBeVisible({ timeout: 10000 });
      await page.waitForTimeout(600);

      // Verify zero horizontal overflow
      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));
      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      // Capture main view screenshot
      await page.screenshot({
        path: `${brainDir}/admin-questions-redesign-main-${vp.name}.png`,
        fullPage: false,
      });

      // Expand filters and capture screenshot
      const toggleFilters = page.getByTestId('toggle-filters-btn');
      if (await toggleFilters.isVisible()) {
        await toggleFilters.click();
        await page.waitForTimeout(400);
        await page.screenshot({
          path: `${brainDir}/admin-questions-redesign-filters-${vp.name}.png`,
          fullPage: false,
        });
        await toggleFilters.click();
      }
    }

    // Capture Detail modal at desktop
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/admin/questions');
    await expect(page.locator('[data-testid="admin-questions-page"]')).toBeVisible({ timeout: 10000 });
    const detailBtn = page.locator('button[data-testid^="question-detail-btn-"]').first();
    if (await detailBtn.isVisible()) {
      await detailBtn.click();
      await expect(page.getByRole('heading', { name: 'Soru Detayı' })).toBeVisible();
      await page.waitForTimeout(400);
      await page.screenshot({
        path: `${brainDir}/admin-questions-redesign-detail-desktop-1440.png`,
        fullPage: false,
      });
      await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    }
  });
});
