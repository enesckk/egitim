import { test, expect, Page } from '@playwright/test';

async function loginAs(page: Page, email: string, password: string = 'Demo1234!') {
  await page.goto('/login');
  await expect(page.locator('#login-email')).toBeVisible({ timeout: 10000 });
  await page.locator('#login-email').fill(email);
  await page.locator('#login-password').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

test.describe('Pre-Demo Audit Cleanup Verification', () => {

  test('1. Student role access: /student/today does not make unauthorized coach meeting requests', async ({ page }) => {
    const requestedUrls: string[] = [];
    const forbiddenUrls: string[] = [];
    const consoleErrors: string[] = [];

    page.on('request', (req) => {
      requestedUrls.push(req.url());
    });

    page.on('response', (res) => {
      if (res.status() === 403) {
        forbiddenUrls.push(res.url());
      }
    });

    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    await loginAs(page, 'student.demo@egitim.com');
    await page.goto('/student/today');
    await page.waitForTimeout(2000);

    // Verify /api/v1/coaching/meetings was NEVER requested by student
    const meetingRequests = requestedUrls.filter((u) => u.includes('/api/v1/coaching/meetings'));
    expect(meetingRequests).toHaveLength(0);

    // Verify no 403 Forbidden responses occurred on the student portal
    expect(forbiddenUrls).toHaveLength(0);

    // Verify Student dashboard rendered real data
    await expect(page.locator('body')).toContainText('Günlük İlerleme');
  });

  test('2. No-plan empty state: Students without active plan show empty state without error banner', async ({ page }) => {
    await loginAs(page, 'coach.demo@egitim.com');

    // S3 Ece Kaya has no active study plan in demo dataset
    const eceStudentId = 'd0000000-0000-0011-0000-000000000003';
    await page.goto(`/coach/students/${eceStudentId}`);
    await page.waitForTimeout(2000);

    // Verify header and badge render "Aktif Plan Yok"
    await expect(page.locator('body')).toContainText('Aktif Plan Yok');
    await expect(page.locator('body')).not.toContainText('Plan Yüklenemedi');

    // Switch to Plan tab
    const planTab = page.locator('button:has-text("Plan")');
    if (await planTab.isVisible()) {
      await planTab.click();
      await page.waitForTimeout(1000);
      // Verify empty state is displayed gracefully
      await expect(page.locator('body')).not.toContainText('Plan Yüklenemedi');
    }
  });

  test('3. Canceled navigation: Rapid route switching does not trigger misleading network error toasts', async ({ page }) => {
    await loginAs(page, 'student.demo@egitim.com');

    // Quickly trigger rapid route changes to simulate user rapid clicking
    page.goto('/student/academic');
    await page.waitForTimeout(50);
    page.goto('/student/plans');
    await page.waitForTimeout(50);
    page.goto('/student/today');

    // Allow the final destination to fully settle
    await page.waitForTimeout(2500);

    // Ensure no misleading network failure or generic unhandled abort error banners are shown
    await expect(page.locator('body')).not.toContainText('Sunucuya bağlanılamadı');
    await expect(page.locator('body')).not.toContainText('Ağ bağlantısı hatası oluştu');
    await expect(page.locator('body')).not.toContainText('The user aborted a request');
    await expect(page.locator('body')).toContainText('Günlük İlerleme');
  });

});
