import { test, expect } from '@playwright/test';

test.describe('Admin Exams Redesign - List, Filters, Mobile Cards and Modal', () => {
  test.beforeEach(async ({ page }) => {
    // Authenticate as InstitutionAdmin
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Admin Exams - Header, Search, Status Filter, Navigation & Zero Horizontal Scroll at 390px, 768px, 1440px', async ({ page }) => {
    await page.goto('/admin/exams');
    await expect(page.locator('[data-testid="admin-exams-page"]')).toBeVisible({ timeout: 10000 });

    // 1. Verify Header
    await expect(page.getByRole('heading', { name: /Deneme Sınavları/i })).toBeVisible();
    await expect(page.getByTestId('create-exam-btn')).toBeVisible();

    // 2. Verify Search & Filters
    const searchInput = page.getByTestId('exam-search-input');
    await expect(searchInput).toBeVisible();
    await searchInput.fill('TYT');
    await page.waitForTimeout(300);

    const typeFilter = page.getByTestId('exam-type-filter');
    await expect(typeFilter).toBeVisible();

    const statusFilter = page.getByTestId('exam-status-filter');
    await expect(statusFilter).toBeVisible();

    // 3. Verify Responsive Views & Zero Horizontal Overflow
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.waitForTimeout(300);

      // Verify no horizontal overflow
      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 2);

      if (vp.width < 1024) {
        // Mobile & Tablet cards should be visible below 1024px
        await expect(page.locator('[data-testid^="exam-card-"]').first()).toBeVisible();
      } else {
        // Desktop table should be visible at 1024px+
        await expect(page.locator('table')).toBeVisible();
        await expect(page.locator('[data-testid^="exam-row-"]').first()).toBeVisible();
      }
    }

    // 4. Test Create Exam Modal section builder on mobile (390px)
    await page.setViewportSize({ width: 390, height: 844 });
    await page.getByTestId('create-exam-btn').click();
    await expect(page.getByTestId('create-exam-form')).toBeVisible();

    // Check modal responsive inputs & section builder
    await expect(page.getByTestId('input-exam-title')).toBeVisible();
    await expect(page.getByTestId('input-exam-code')).toBeVisible();
    await expect(page.getByTestId('btn-add-section')).toBeVisible();

    // Add a section
    await page.getByTestId('btn-add-section').click();

    // Verify remove button aria-label exists
    const removeButtons = page.locator('button[aria-label="Bölümü Kaldır"]');
    await expect(removeButtons.first()).toBeVisible();

    // Close modal
    await page.getByRole('button', { name: 'İptal' }).click();
    await expect(page.getByTestId('create-exam-form')).not.toBeVisible();
  });
});
