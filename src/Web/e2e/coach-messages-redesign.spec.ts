import { test, expect } from '@playwright/test';

test.describe('Coach Messages Redesign - Real Backend & Master-Detail Responsive Layout', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Coach Demo
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('coach.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    // Wait for redirect
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Coach Messages - Real API load, conversation search, and responsive viewports with zero horizontal overflow', async ({ page }) => {
    await page.goto('/coach/messages');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('coach-messages-view')).toBeVisible({ timeout: 10000 });

    // Verify search input is visible
    const searchInput = page.locator('input[placeholder*="Kişi veya mesaj ara"]');
    await expect(searchInput).toBeVisible();

    // Verify conversation list has items
    const conversationButtons = page.locator('button:has(div.bg-\\[\\#14233E\\])');
    const count = await conversationButtons.count();
    expect(count).toBeGreaterThan(0);

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

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-messages-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });

  test('Coach Messages - Mobile master-detail toggle: select conversation and back button', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/coach/messages');
    await page.waitForLoadState('networkidle');

    // On mobile, clicking a conversation transitions to thread
    const firstConv = page.locator('button:has(div.bg-\\[\\#14233E\\])').first();
    await firstConv.click();

    // Back button should be visible in thread header on mobile
    const backBtn = page.getByTitle('Gelen Kutusuna Dön');
    await expect(backBtn).toBeVisible();

    // Clicking back returns to conversation list
    await backBtn.click();
    await expect(page.locator('input[placeholder*="Kişi veya mesaj ara"]')).toBeVisible();
  });
});
