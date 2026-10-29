import { test, expect } from '@playwright/test';

test.describe('Admin Exam Results Method Selection - Responsiveness & Navigation', () => {
  const examId = 'd0000000-0000-0000-0000-000000000015';

  test.beforeEach(async ({ page }) => {
    // Authenticate as InstitutionAdmin
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Method Selection Hub - Header, Back button to /admin/exams, 390/768/1440px responsiveness and no overflow', async ({ page }) => {
    await page.goto(`/admin/exams/${examId}/results/entry`);
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 10000 });

    // Verify Title & Subtitle
    await expect(page.getByRole('heading', { name: /Sınav Sonuç İşlemleri/i })).toBeVisible();
    await expect(page.getByRole('heading', { name: /Sonuç Giriş Yöntemini Seçin/i })).toBeVisible();

    // Verify Back button to /admin/exams
    const backBtn = page.locator('button[title="Geri Dön"]');
    await expect(backBtn).toBeVisible();
    await backBtn.click();
    await expect(page).toHaveURL(/\/admin\/exams$/);

    // Return to entry hub
    await page.goto(`/admin/exams/${examId}/results/entry`);
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 10000 });

    // Check 390px, 768px, 1440px responsiveness and zero horizontal scroll
    const viewports = [
      { name: 'mobile-390', width: 390, height: 844 },
      { name: 'tablet-768', width: 768, height: 1024 },
      { name: 'desktop-1440', width: 1440, height: 900 },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.waitForTimeout(300);

      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 2);

      await expect(page.getByTestId('btn-mode-manual-single')).toBeVisible();
      await expect(page.getByTestId('btn-mode-manual-bulk')).toBeVisible();
      await expect(page.getByTestId('btn-mode-excel-aggregate')).toBeVisible();
      await expect(page.getByTestId('btn-mode-excel-question')).toBeVisible();
    }
  });

  test('Mode Transitions & Return-to-Selection Navigation for all 4 entry modes (Read-only / No DB writes)', async ({ page }) => {
    await page.goto(`/admin/exams/${examId}/results/entry`);
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 10000 });

    // 1. Manual Single Mode Transition & Cancel
    await page.getByTestId('btn-mode-manual-single').click();
    await expect(page.getByRole('heading', { name: /Tekil Öğrenci Manuel Sonuç Girişi/i })).toBeVisible({ timeout: 5000 });
    // Click Cancel/Vazgeç button
    await page.getByRole('button', { name: 'Vazgeç' }).first().click();
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 5000 });

    // 2. Manual Bulk Mode Transition & Cancel
    await page.getByTestId('btn-mode-manual-bulk').click();
    await expect(page.getByRole('heading', { name: /Toplu Hızlı Tablo Sonuç Girişi/i })).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'Vazgeç' }).first().click();
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 5000 });

    // 3. Excel Aggregate Mode Transition & Cancel
    await page.getByTestId('btn-mode-excel-aggregate').click();
    await expect(page.getByRole('heading', { name: /Excel İle Sonuç Yükleme Sihirbazı/i })).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'Vazgeç' }).first().click();
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 5000 });

    // 4. Excel Question Answers Mode Transition & Cancel
    await page.getByTestId('btn-mode-excel-question').click();
    await expect(page.getByRole('heading', { name: /Excel İle Sonuç Yükleme Sihirbazı/i })).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'Vazgeç' }).first().click();
    await expect(page.locator('[data-testid="method-selection-container"]')).toBeVisible({ timeout: 5000 });
  });
});
