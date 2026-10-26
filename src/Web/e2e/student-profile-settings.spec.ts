import { test, expect } from '@playwright/test';

test.describe('Student Profile & Settings UI & Safety Tests', () => {
  test('1. Authoritative identity, name, institution and role rendering', async ({ page }) => {
    // Login as Student
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    // Navigate to Profile
    await page.goto('http://localhost:3000/student/profile');
    await page.waitForLoadState('networkidle');

    // Verify Title & Header
    await expect(page.getByTestId('profile-page-title')).toHaveText('Profilim ve Hesap Ayarlarım');

    // Verify Authoritative Full Name (never raw "student.demo")
    const profileName = page.getByTestId('profile-full-name');
    await expect(profileName).toBeVisible({ timeout: 10000 });
    const nameText = await profileName.textContent();
    expect(nameText).not.toContain('student.demo');
    expect(nameText).toBeTruthy();

    // Verify Settings cards & Logout CTA
    await expect(page.getByTestId('btn-open-notification-settings')).toBeVisible();
    await expect(page.getByTestId('btn-open-password-settings')).toBeVisible();
    await expect(page.getByTestId('btn-profile-logout')).toBeVisible();
  });

  test('2. Real notification preferences update, save and persistence', async ({ page }) => {
    // Login as Student
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    // Navigate to Profile
    await page.goto('http://localhost:3000/student/profile');
    await page.waitForLoadState('networkidle');

    // Open Notification Preferences modal
    await page.getByTestId('btn-open-notification-settings').click();
    await expect(page.getByRole('heading', { name: 'Bildirim Tercihleri' })).toBeVisible({ timeout: 5000 });

    // Toggle exam results checkbox
    const examCheckbox = page.getByTestId('pref-exam-results');
    await expect(examCheckbox).toBeVisible();
    const wasChecked = await examCheckbox.isChecked();
    await examCheckbox.click();
    expect(await examCheckbox.isChecked()).toBe(!wasChecked);

    // Save preferences
    await page.locator('#btn-save-preferences').click();
    await expect(page.getByText(/başarıyla veritabanına kaydedildi/i)).toBeVisible({ timeout: 10000 });

    // Close modal
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Bildirim Tercihleri' })).not.toBeVisible({ timeout: 5000 });

    // Reload page and re-open modal to verify persistence from server
    await page.reload();
    await page.waitForLoadState('networkidle');
    await page.getByTestId('btn-open-notification-settings').click();
    await expect(page.getByRole('heading', { name: 'Bildirim Tercihleri' })).toBeVisible({ timeout: 5000 });

    const reloadedCheckbox = page.getByTestId('pref-exam-results');
    expect(await reloadedCheckbox.isChecked()).toBe(!wasChecked);

    // Close preferences modal before attempting logout
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Bildirim Tercihleri' })).not.toBeVisible({ timeout: 5000 });

    // Verify persistence across complete logout and re-login
    await page.getByTestId('btn-profile-logout').click();
    await page.locator('[role="dialog"]').getByRole('button', { name: /Çıkış Yap/i }).click();
    await page.waitForURL('**/login**');

    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    await page.goto('http://localhost:3000/student/profile');
    await page.waitForLoadState('networkidle');
    await page.getByTestId('btn-open-notification-settings').click();
    await expect(page.getByRole('heading', { name: 'Bildirim Tercihleri' })).toBeVisible({ timeout: 5000 });

    const reloggedCheckbox = page.getByTestId('pref-exam-results');
    expect(await reloggedCheckbox.isChecked()).toBe(!wasChecked);

    // Revert toggle back to initial state for test idempotency
    await reloggedCheckbox.click();
    await page.locator('#btn-save-preferences').click();
    await expect(page.getByText(/başarıyla veritabanına kaydedildi/i)).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
  });

  test('3. Password change modal validation and safe submission states', async ({ page }) => {
    // Login as Student
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    // Navigate to Profile
    await page.goto('http://localhost:3000/student/profile');
    await page.waitForLoadState('networkidle');

    // Open Password modal
    await page.getByTestId('btn-open-password-settings').click();
    await expect(page.getByRole('heading', { name: 'Şifre ve Güvenlik' })).toBeVisible({ timeout: 5000 });

    // Submit with empty fields
    await page.locator('#btn-submit-password-change').click();
    await expect(page.getByText(/Mevcut şifrenizi girmelisiniz/i)).toBeVisible();

    // Fill current password but short new password
    await page.locator('#current-password').fill('Demo1234!');
    await page.locator('#new-password').fill('12345');
    await page.locator('#btn-submit-password-change').click();
    await expect(page.locator('[role="alert"]').filter({ hasText: /en az 8 karakter uzunluğunda olmalıdır/i })).toBeVisible();

    // Fill mismatched new passwords
    await page.locator('#new-password').fill('NewPassword123!');
    await page.locator('#confirm-password').fill('DifferentPassword123!');
    await page.locator('#btn-submit-password-change').click();
    await expect(page.getByText(/birbiriyle eşleşmiyor/i)).toBeVisible();

    // Fill matching valid passwords and submit
    await page.locator('#confirm-password').fill('NewPassword123!');
    await page.locator('#btn-submit-password-change').click();
    await expect(page.getByText(/Şifre güncelleme talebiniz alındı/i)).toBeVisible({ timeout: 10000 });

    // Close modal
    await page.getByRole('button', { name: 'Kapat', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Şifre ve Güvenlik' })).not.toBeVisible({ timeout: 5000 });
  });

  test('4. Logout modal confirmation redirects to login', async ({ page }) => {
    // Login as Student
    await page.goto('http://localhost:3000/login');
    await page.fill('input[type="email"], input[name="email"]', 'student.demo@egitim.com');
    await page.fill('input[type="password"], input[name="password"]', 'Demo1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('**/student**');

    // Navigate to Profile
    await page.goto('http://localhost:3000/student/profile');
    await page.waitForLoadState('networkidle');

    // Click logout button to open modal
    await page.getByTestId('btn-profile-logout').click();
    await expect(page.getByRole('heading', { name: 'Çıkış Yap' })).toBeVisible();

    // Confirm logout
    const modalConfirmBtn = page.locator('[role="dialog"]').getByRole('button', { name: /Çıkış Yap/i });
    await modalConfirmBtn.click();

    // Verify redirected to login
    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });
});
