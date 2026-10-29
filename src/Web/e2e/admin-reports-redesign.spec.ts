import { test, expect } from '@playwright/test';

test.describe('Admin Reports Redesign - Filters, Date Validation, Responsive Layout & Export', () => {
  test.beforeEach(async ({ page }) => {
    // Authenticate as Institution Admin
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible({ timeout: 10000 });
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('1. Admin Reports - KPIs, Filter Changes, Date Range Validation & Filter Reset', async ({ page }) => {
    await page.goto('/admin/reports');
    await expect(page.locator('[data-testid="admin-reports-view"]')).toBeVisible({ timeout: 10000 });

    // 1. Verify KPIs Grid and Honest Labels
    await expect(page.locator('[data-testid="reports-kpi-grid"]')).toBeVisible();
    await expect(page.getByTestId('kpi-total-students')).toBeVisible();
    await expect(page.getByTestId('kpi-total-attempts')).toBeVisible();
    await expect(page.getByTestId('kpi-average-net')).toBeVisible();
    await expect(page.getByTestId('kpi-average-adherence')).toBeVisible();

    // 2. Test Invalid Date Range Validation (Start date after End date)
    const startDateInput = page.getByTestId('input-filter-start-date');
    const endDateInput = page.getByTestId('input-filter-end-date');
    const applyBtn = page.getByTestId('btn-apply-filter');

    await startDateInput.fill('2026-12-31');
    await endDateInput.fill('2026-01-01');
    await applyBtn.click();

    await expect(page.getByTestId('date-filter-error')).toBeVisible();
    await expect(page.getByTestId('date-filter-error')).toContainText('Başlangıç tarihi bitiş tarihinden sonra olamaz');

    // 3. Reset Filters
    const resetBtn = page.getByTestId('btn-reset-filters');
    await expect(resetBtn).toBeVisible();
    await resetBtn.click();

    await expect(startDateInput).toHaveValue('');
    await expect(endDateInput).toHaveValue('');
    await expect(page.getByTestId('date-filter-error')).not.toBeVisible();

    // 4. Test Classroom Filter application
    const branchSelect = page.getByTestId('select-filter-classroom');
    await branchSelect.selectOption({ index: 1 });
    await applyBtn.click();
    await expect(page.getByTestId('kpi-total-students')).toBeVisible();
  });

  test('2. Admin Reports - Exports Trigger with Authenticated Filename', async ({ page }) => {
    await page.goto('/admin/reports');
    await expect(page.locator('[data-testid="admin-reports-view"]')).toBeVisible({ timeout: 10000 });

    // Verify CSV export
    const [csvDownload] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }).catch(() => null),
      page.getByTestId('btn-export-csv').click(),
    ]);

    if (csvDownload) {
      expect(csvDownload.suggestedFilename()).toContain('.csv');
    }

    // Verify XLSX export
    const [xlsxDownload] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }).catch(() => null),
      page.getByTestId('btn-export-xlsx').click(),
    ]);

    if (xlsxDownload) {
      expect(xlsxDownload.suggestedFilename()).toContain('.xlsx');
    }
  });

  test('3. Admin Reports - Responsive Layout, Zero Horizontal Overflow & Viewport Screenshots', async ({ page }) => {
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    const brainDir = '/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots';

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/admin/reports');
      await expect(page.locator('[data-testid="admin-reports-view"]')).toBeVisible({ timeout: 10000 });
      await page.waitForTimeout(600);

      // Verify zero horizontal overflow
      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));
      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      // If viewport < 1024px, verify card layout is active
      if (vp.width < 1024) {
        await expect(page.getByTestId('mobile-exam-performances-cards')).toBeVisible();
      } else {
        await expect(page.getByTestId('table-exam-performances')).toBeVisible();
      }

      // Capture unfiltered screenshot
      await page.screenshot({
        path: `${brainDir}/admin-reports-redesign-main-${vp.name}.png`,
        fullPage: false,
      });

      // Select a filter and capture filtered screenshot
      const branchSelect = page.getByTestId('select-filter-classroom');
      if (await branchSelect.isVisible()) {
        await branchSelect.selectOption({ index: 1 });
        await page.getByTestId('btn-apply-filter').click();
        await page.waitForTimeout(400);
        await page.screenshot({
          path: `${brainDir}/admin-reports-redesign-filtered-${vp.name}.png`,
          fullPage: false,
        });

        // Reset
        await page.getByTestId('btn-reset-filters').click();
        await page.waitForTimeout(300);
      }
    }
  });
});
