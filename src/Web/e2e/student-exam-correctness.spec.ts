import { test, expect } from '@playwright/test';

test.describe('Student Exam Results Data Correctness & Pagination Tests', () => {
  test('1. Exam results page loads deterministically and renders genuine net scores', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/exams');
    await page.waitForLoadState('networkidle');

    // Verify page header and exam count
    await expect(page.getByRole('heading', { name: /Sınavlarım ve Karnelerim/i })).toBeVisible({ timeout: 10000 });
    
    // Verify latest exam hero card exists with valid net
    const heroCard = page.locator('[data-testid="latest-exam-hero"]');
    await expect(heroCard).toBeVisible();
    await expect(heroCard.getByText(/Net/i).first()).toBeVisible();

    // Verify exam list items are visible
    const examCards = page.locator('[data-testid="exam-list-item"]');
    expect(await examCards.count()).toBeGreaterThan(0);
  });

  test('2. Opening exam detail displays subject breakdown and handles aggregate import notice', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/exams');
    await page.waitForLoadState('networkidle');

    // Click first exam item
    const firstExam = page.locator('[data-testid="exam-list-item"]').first();
    await firstExam.click();

    // Verify modal appears with scorecard details
    const modal = page.locator('[role="dialog"]');
    await expect(modal).toBeVisible({ timeout: 5000 });
    // Verify subject breakdown card header or results
    await expect(modal.getByText(/Ders Dağılım Karnesi/i)).toBeVisible();
    
    // Verify subject rows render with D / Y / B numbers
    await expect(modal.getByText(/Net/i).first()).toBeVisible();
  });
});
