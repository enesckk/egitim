import { test, expect } from '@playwright/test';

test.describe('Parent Summary Redesign - Real Child Identity, Progress & Responsiveness', () => {
  test.beforeEach(async ({ page }) => {
    // Login as Parent Demo
    await page.goto('/login');
    await expect(page.locator('#login-email')).toBeVisible();
    await page.locator('#login-email').fill('parent.demo@egitim.com');
    await page.locator('#login-password').fill('Demo1234!');
    await page.locator('button[type="submit"]').click();

    // Wait for redirect to parent portal
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  });

  test('Parent Summary - Real child data, weekly progress, academic sections, and responsive viewports with zero horizontal overflow', async ({ page }) => {
    await page.goto('/parent/summary');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('parent-summary-view')).toBeVisible({ timeout: 10000 });

    // 1. Child Hero Banner: verify real child name and coach
    await expect(page.locator('h1', { hasText: 'Ahmet Yılmaz' })).toBeVisible();
    await expect(page.getByText('Öğrenci Koçu', { exact: false })).toBeVisible();

    // 2. Weekly Activity Card
    await expect(page.getByText('Haftalık Çalışma ve Plan Uyumu')).toBeVisible();

    // 3. AI Academic Summary & Question Style sections
    await expect(page.getByText('Akademik Değerlendirmem', { exact: false })).toBeVisible();
    await expect(page.getByText('Soru Tarzı Başarımı', { exact: false })).toBeVisible();

    // 4. Academic Status & Upcoming Events
    await expect(page.getByText('Akademik Durum ve Ders Odakları')).toBeVisible();
    await expect(page.getByText('Yaklaşan Faaliyetler ve Denemeler')).toBeVisible();

    // 5. Responsive viewports with zero horizontal overflow
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

      const screenshotPath = `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/parent-summary-redesign-${vp.name}.png`;
      await page.screenshot({ path: screenshotPath, fullPage: true });
    }
  });
});
