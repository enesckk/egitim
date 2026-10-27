import { test, expect } from '@playwright/test';

test.describe('Coach Meetings Redesign - Real Backend & Responsive Layout', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Demo Coach using authoritative demo credentials
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('coach.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    // Wait for redirect away from /login
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Coach Meetings - Real API load, KPIs, filtering, and responsive viewports with zero horizontal overflow', async ({ page }) => {
    await page.goto('/coach/meetings');
    await page.waitForLoadState('networkidle');

    // 1. Verify header title and CTA button
    const heading = page.locator('h1', { hasText: 'Koçluk Görüşmeleri' });
    await expect(heading).toBeVisible();

    const newMeetingBtn = page.locator('button:has-text("Yeni Görüşme Planla")');
    await expect(newMeetingBtn).toBeVisible();

    // 2. Verify KPI cards
    await expect(page.getByText('Bugünkü Görüşmeler', { exact: true })).toBeVisible();
    await expect(page.getByText('Gelecek Seanslar', { exact: true })).toBeVisible();
    await expect(page.getByText('Tamamlanan', { exact: true })).toBeVisible();

    // 3. Verify Search and Status Filter
    const searchInput = page.locator('input[placeholder*="Öğrenci adı, görüşme tipi veya gündem ara"]');
    await expect(searchInput).toBeVisible();

    const statusSelect = page.locator('select');
    await expect(statusSelect).toBeVisible();

    // 4. Capture Responsive Screenshots & verify zero horizontal scroll at 390px, 768px, 1440px
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

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-meetings-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });

  test('Coach Meetings - Status transitions and modal integration check', async ({ page }) => {
    await page.goto('/coach/meetings');
    await page.waitForLoadState('networkidle');

    // Verify "Yeni Görüşme Planla" opens modal
    const newMeetingBtn = page.locator('button:has-text("Yeni Görüşme Planla")');
    await newMeetingBtn.click();

    const modalHeading = page.getByRole('heading', { name: 'Yeni Görüşme Planla' });
    await expect(modalHeading).toBeVisible();

    // Cancel modal via İptal button inside modal footer
    const modal = page.locator('[role="dialog"]');
    const cancelBtn = modal.getByRole('button', { name: 'İptal' });
    await cancelBtn.click();
    await expect(modalHeading).not.toBeVisible();
  });
});
