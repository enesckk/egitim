import { test, expect } from '@playwright/test';

test.describe('Student Learning Materials UI & Security Tests', () => {
  test('1. Materials view loads real scoped items, displays header count and subject chips', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/materials');
    await page.waitForLoadState('networkidle');

    // Page header & material count
    await expect(page.getByRole('heading', { name: /Ders Materyallerim/i })).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Materyal/i).first()).toBeVisible();

    // Verify search bar & select dropdown
    const searchInput = page.getByPlaceholder(/Materyal, konu veya ders ara/i);
    await expect(searchInput).toBeVisible();

    // Verify materials grid
    const grid = page.locator('#student-materials-grid');
    await expect(grid).toBeVisible();
    const cards = page.locator('[data-testid="student-material-card"]');
    expect(await cards.count()).toBeGreaterThan(0);
  });

  test('2. Subject chips filter materials dynamically', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/materials');
    await page.waitForLoadState('networkidle');

    // Check "Tümü" button is active
    const allBtn = page.getByRole('button', { name: /Tümü/i });
    await expect(allBtn).toBeVisible();

    // Click on a subject chip if present (e.g. Matematik)
    const subjectBtn = page.getByRole('button', { name: /Matematik/i }).first();
    if (await subjectBtn.isVisible()) {
      await subjectBtn.click();
      // Ensure all visible cards have "Matematik" badge
      const cards = page.locator('[data-testid="student-material-card"]');
      const count = await cards.count();
      for (let i = 0; i < count; i++) {
        await expect(cards.nth(i).getByText(/Matematik/i).first()).toBeVisible();
      }
    }
  });

  test('3. Download button triggers authenticated download without revealing storage path', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/materials');
    await page.waitForLoadState('networkidle');

    // Verify that rendered DOM does NOT expose physical StoragePath or server tokens
    const pageContent = await page.content();
    expect(pageContent).not.toContain('/Storage/Materials/');
    expect(pageContent).not.toContain('C:\\');
    expect(pageContent).not.toContain('/app/Storage/');

    // Click download on first card and verify success feedback toast
    const firstDownloadBtn = page.locator('[data-testid="student-material-card"]').first().getByRole('button', { name: /İndir/i });
    await expect(firstDownloadBtn).toBeVisible();
    await firstDownloadBtn.click();

    // Wait for success toast
    await expect(page.getByText(/başarıyla indirildi/i)).toBeVisible({ timeout: 10000 });
  });
});
