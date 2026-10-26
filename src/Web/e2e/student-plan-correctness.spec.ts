import { test, expect } from '@playwright/test';

test.describe('Student Study Plan Date & Progress Correctness Tests', () => {
  // Test 1: Verify 0-based dayIndex calculation and unique 7-day schedule mapping
  test('1. Day schedule maps 0-based indices to unique consecutive dates without duplicates', async ({ page }) => {
    // Navigate to student plans page
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/plans');
    await page.waitForLoadState('networkidle');

    // Verify 7 distinct day buttons exist
    const dayButtons = page.locator('button[data-testid^="day-selector-"]');
    await expect(dayButtons).toHaveCount(7);

    // Verify day numbers/dates are strictly distinct
    const dateTexts: string[] = [];
    for (let i = 0; i < 7; i++) {
      const text = await dayButtons.nth(i).locator('[data-testid="day-date-number"]').textContent();
      if (text) dateTexts.push(text.trim());
    }

    // Verify all 7 days have unique calendar date strings (e.g., 21, 22, 23, 24, 25, 26, 27)
    const uniqueDates = new Set(dateTexts);
    expect(uniqueDates.size).toBe(7);
  });

  // Test 2: Verify selecting day 0 loads day 0 task (Trigonometri)
  test('2. Selecting day index 0 loads Trigonometri and does not drop day 0', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/plans');
    await page.waitForLoadState('networkidle');

    // Click Day 0 (first button)
    const firstDay = page.locator('button[data-testid="day-selector-0"]');
    await firstDay.click();

    // Verify day 0 task is visible
    await expect(page.getByText(/Trigonometri/i).first()).toBeVisible({ timeout: 5000 });
  });

  // Test 3: Verify inclusive end date display in plan header
  test('3. Plan header displays inclusive end-date range (not exclusive 28 Oct)', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/plans');
    await page.waitForLoadState('networkidle');

    // Verify date range text ends in 27 Oct, matching 7 days from 21 Oct
    const dateRangeEl = page.locator('[data-testid="plan-date-range"]');
    await expect(dateRangeEl).toBeVisible();
    const text = await dateRangeEl.textContent();
    expect(text).toContain('27');
    expect(text).not.toContain('28');
  });
});
