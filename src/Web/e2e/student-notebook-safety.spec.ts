import { test, expect } from '@playwright/test';

test.describe('Student Notebook UI & Data Safety Tests', () => {
  test('1. Notebook renders header, count, search bar and existing cards', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/notebook');
    await page.waitForLoadState('networkidle');

    // Page header & note count
    await expect(page.getByRole('heading', { name: /Defterim/i })).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Not/i).first()).toBeVisible();

    // Verify search input
    const searchInput = page.getByPlaceholder(/Not başlığı veya içeriğinde ara/i);
    await expect(searchInput).toBeVisible();

    // Verify "Yeni Not" button
    const newNoteBtn = page.getByRole('button', { name: /Yeni Not/i });
    await expect(newNoteBtn).toBeVisible();
  });

  test('2. Add note modal protects against unsaved changes', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/notebook');
    await page.waitForLoadState('networkidle');

    // Click "Yeni Not"
    await page.getByRole('button', { name: /Yeni Not/i }).click();

    // Fill some content
    const modal = page.locator('[role="dialog"]').first();
    await expect(modal).toBeVisible();
    await modal.locator('input[placeholder*="Örn: Logaritma"]').fill('Taslak Not Başlığı');

    // Click "İptal" or close
    await modal.getByRole('button', { name: /İptal/i }).click();

    // Verify Discard confirmation modal appears
    await expect(page.getByText(/Kaydedilmemiş Değişiklikler Var/i)).toBeVisible();
    await page.getByRole('button', { name: /Vazgeç ve Kapat/i }).click();

    // Both modals closed
    await expect(page.getByText(/Kaydedilmemiş Değişiklikler Var/i)).not.toBeVisible();
  });

  test('3. Delete note triggers confirmation modal before deletion', async ({ page }) => {
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/notebook');
    await page.waitForLoadState('networkidle');

    const entryCards = page.locator('[data-testid="notebook-entry-card"]');
    if ((await entryCards.count()) > 0) {
      const firstCard = entryCards.first();
      // Click delete button inside first card
      await firstCard.locator('button[title="Sil"]').click();

      // Verify deletion modal appears with note title
      const deleteModal = page.locator('[role="dialog"]').filter({ hasText: /Notu Sil/i });
      await expect(deleteModal).toBeVisible();
      await expect(deleteModal.getByRole('button', { name: /Evet, Sil/i })).toBeVisible();

      // Dismiss without deleting
      await deleteModal.getByRole('button', { name: /Vazgeç/i }).click();
      await expect(deleteModal).not.toBeVisible();
    }
  });
});
