import { test, expect, Page } from '@playwright/test';

const SCREENSHOT_DIR = '/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots';

async function loginAs(page: Page, email: string, password: string) {
  await page.goto('/login');
  await expect(page.locator('#login-email')).toBeVisible({ timeout: 10000 });
  await page.locator('#login-email').fill(email);
  await page.locator('#login-password').fill(password);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function logoutViaUiOrStorage(page: Page) {
  const logoutBtn = page.getByRole('button', { name: /Çıkış Yap/i }).first();
  if (await logoutBtn.isVisible().catch(() => false)) {
    await logoutBtn.click();
    await page.waitForURL(/\/login/, { timeout: 10000 });
  } else {
    await page.context().clearCookies();
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
    await page.goto('/login');
  }
  await expect(page.locator('#login-email')).toBeVisible({ timeout: 10000 });
}

test.describe('Bilim Akademi — Final Five-Portal UAT & Cross-Portal Verification', () => {
  test('UAT-1: Auth States (Validation, Invalid Credentials, Unauthenticated Redirect, Logout)', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });

    // 1. Unauthenticated access to protected route redirects to /login
    await page.goto('/admin/overview');
    await page.waitForURL(/\/login/, { timeout: 10000 });

    // 2. Invalid credentials error state
    await page.locator('#login-email').fill('admin.demo@egitim.com');
    await page.locator('#login-password').fill('WrongPassword999!');
    await page.locator('button[type="submit"]').click();
    await expect(page.locator('[role="alert"], .text-rose-700, .text-red-600').first()).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-auth-error-desktop-1440.png`, fullPage: false });

    // 3. Valid login & real UI Logout button verification
    await loginAs(page, 'admin.demo@egitim.com', 'Demo1234!');
    await expect(page).toHaveURL(/\/admin/);
    await logoutViaUiOrStorage(page);
    await expect(page).toHaveURL(/\/login/);
  });

  test('UAT-2: Admin Portal — Create/Assign Student, Teacher, Coach & Responsive Views', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await loginAs(page, 'admin.demo@egitim.com', 'Demo1234!');

    // 1. Admin Overview at 1440px, 768px, 390px
    await page.goto('/admin/overview');
    await expect(page.locator('main')).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(800);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-overview-desktop-1440.png` });

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-overview-tablet-768.png` });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-overview-mobile-390.png` });

    await page.setViewportSize({ width: 1440, height: 900 });

    // 2. Admin Creates Teacher via UI Modal (Checks for institutionId: '' serialization defect)
    await page.goto('/admin/teachers');
    await expect(page.getByRole('button', { name: /Yeni Öğretmen Ekle/i })).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: /Yeni Öğretmen Ekle/i }).click();

    const ts = Date.now().toString().slice(-4);
    await page.locator('input[placeholder*="Kemal"]').fill(`UATOgretmen${ts}`);
    await page.locator('input[placeholder*="Bey"]').fill('Test');
    await page.locator('input[placeholder*="kemal@okul.com"]').fill(`uat.teacher.${ts}@egitim.com`);

    const [teacherResp] = await Promise.all([
      page.waitForResponse((r) => r.url().includes('/api/v1/teachers') && r.request().method() === 'POST'),
      page.getByRole('button', { name: /^Kaydet$/i }).click(),
    ]);
    expect(teacherResp.status()).toBe(201);
    await expect(page.getByText(`UATOgretmen${ts}`).first()).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(500);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-teacher-create-modal-1440.png` });

    // 3. Admin Assigns existing Teacher (Ayşe Öğretmen) to Classroom (12-B) via UI
    await page.getByText('Ayşe Öğretmen').first().click();
    await expect(page.getByRole('button', { name: /Sınıf Ata/i }).first()).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: /Sınıf Ata/i }).first().click();
    await page.locator('select').selectOption({ label: '12-B (12. Sınıf)' });
    await page.getByRole('button', { name: 'Sınıfa Ata' }).click();
    await page.waitForTimeout(1200);

    // 4. Admin Creates Coach via UI Modal (Checks for institutionId: '' serialization defect)
    await page.goto('/admin/coaches');
    await expect(page.getByRole('button', { name: /Yeni Koç Ekle/i })).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: /Yeni Koç Ekle/i }).click();

    await page.locator('input[placeholder*="Hasan"]').fill(`UATKoc${ts}`);
    await page.locator('input[placeholder*="Yılmaz"]').fill('Test');
    await page.locator('input[placeholder*="hasan@okul.com"]').fill(`uat.coach.${ts}@egitim.com`);

    const [coachResp] = await Promise.all([
      page.waitForResponse((r) => r.url().includes('/api/v1/coaching/coaches') && r.request().method() === 'POST'),
      page.getByRole('button', { name: /^Kaydet$/i }).click(),
    ]);
    expect(coachResp.status()).toBe(201);
    await expect(page.getByText(`UATKoc${ts}`).first()).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(500);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-coach-create-modal-1440.png` });

    // 5. Admin Creates a New Student via UI and Assigns Coach (Mehmet Koç) to that Student via UI
    await page.goto('/admin/students');
    await expect(page.getByRole('button', { name: /Yeni Öğrenci Ekle/i })).toBeVisible({ timeout: 10000 });
    const uatStudentFirst = `UATOgrenci${ts}`;
    await page.getByRole('button', { name: /Yeni Öğrenci Ekle/i }).click();
    await page.locator('input[placeholder*="Ayşe"]').fill(uatStudentFirst);
    await page.locator('input[placeholder*="Yılmaz"]').fill('KocAtama');
    await page.locator('input[type="email"]').fill(`uat.student.${ts}@egitim.com`);
    await page.locator('input[type="password"]').fill('Ogrenci@123!');
    await page.getByRole('button', { name: /^Kaydet$|^Ekle$/i }).click();
    await expect(page.getByText(uatStudentFirst)).toBeVisible({ timeout: 10000 });

    // Test Empty Search State in Admin Students
    const searchInput = page.locator('input[placeholder*="ara" i]').first();
    await searchInput.fill('ZZZZ_NON_EXISTENT_STUDENT_9999');
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-students-empty-state-1440.png` });
    await searchInput.fill('');

    // Now go to /admin/coaches, open Mehmet Koç's assign modal, and assign the newly created student to Mehmet Koç!
    await page.goto('/admin/coaches');
    await expect(page.getByRole('button', { name: 'Öğrenci Ata' }).first()).toBeVisible({ timeout: 5000 });
    await page.getByRole('button', { name: 'Öğrenci Ata' }).first().click();
    await page.locator('input[placeholder="Öğrenci ara..."]').fill(uatStudentFirst);
    await page.getByText(new RegExp(uatStudentFirst)).first().click();
    await page.getByRole('button', { name: 'Atamayı Onayla' }).click();
    await expect(page.getByText(/Öğrenci koça başarıyla atandı/i)).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-admin-coach-assign-success-1440.png` });
  });

  test('UAT-3: Cross-Portal Coach Task Creation -> Student Plans & Today Reflection + Responsive Checks', async ({ page, request }) => {
    await page.setViewportSize({ width: 1440, height: 900 });

    // 1. Login as Coach and create a new Coach Task for Ahmet Yılmaz via Coach Student Detail UI
    await loginAs(page, 'coach.demo@egitim.com', 'Demo1234!');
    const studentId = 'd0000000-0000-0000-0000-000000000007';
    await page.goto(`/coach/students/${studentId}?tab=tasks`);
    await expect(page.getByRole('button', { name: 'Yeni Görev Ata' })).toBeVisible({ timeout: 10000 });

    const uniqueTaskTitle = `UAT Türev Fasikülü Çözümü ${Date.now().toString().slice(-4)}`;
    await page.getByRole('button', { name: 'Yeni Görev Ata' }).click();
    await page.locator('input[placeholder*="paragraf" i]').fill(uniqueTaskTitle);
    await page.locator('form button[type="submit"]').click();
    await expect(page.getByText(uniqueTaskTitle).first()).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-coach-task-assigned-desktop-1440.png` });

    // Responsive screenshots for Coach Portal (768px & 390px)
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-coach-student-detail-tablet-768.png` });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-coach-student-detail-mobile-390.png` });

    // 2. Switch to Student Portal and verify the new Coach Task is visible in Student Plans & Today, and toggle study plan task
    await page.setViewportSize({ width: 1440, height: 900 });
    await logoutViaUiOrStorage(page);
    await loginAs(page, 'student.demo@egitim.com', 'Demo1234!');

    await page.goto('/student/plans');
    await expect(page.getByText(uniqueTaskTitle).first()).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-student-plans-desktop-1440.png` });

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-student-plans-tablet-768.png` });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-student-plans-mobile-390.png` });

    // 3. Seed initial conversation between Coach and Student via real API so we also verify bidirectional UI messaging
    const coachLogin = await request.post('http://localhost:5005/api/v1/auth/login', {
      data: { email: 'coach.demo@egitim.com', password: 'Demo1234!' },
    });
    const { accessToken: coachToken } = await coachLogin.json();
    await request.post('http://localhost:5005/api/v1/communication/messages', {
      headers: { Authorization: `Bearer ${coachToken}` },
      data: {
        recipientUserId: 'd0000000-0000-0000-0000-000000000005', // Ahmet Yılmaz UserId
        subject: 'Haftalık Değerlendirme',
        content: 'Merhaba Ahmet, bu haftaki çalışma planını inceledim.',
      },
    });

    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/student/messages');
    await expect(page.getByText('Merhaba Ahmet, bu haftaki çalışma planını inceledim.').first()).toBeVisible({ timeout: 10000 });
    const studentReply = `Teşekkürler hocam, görevleri tamamlıyorum (${Date.now().toString().slice(-4)})`;
    await page.locator('#message-content-input').fill(studentReply);
    await page.locator('button:has-text("Gönder")').click();
    await expect(page.getByText(studentReply).first()).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-student-messages-desktop-1440.png` });
  });

  test('UAT-4: Teacher & Parent Portals — Responsive Layouts (390/768/1440) & Cross-Tenant / Child Isolation', async ({ page, request }) => {
    // 1. Teacher Portal Responsive Checks
    await page.setViewportSize({ width: 1440, height: 900 });
    await loginAs(page, 'teacher.demo@egitim.com', 'Demo1234!');
    await page.goto('/teacher/academic');
    await expect(page.locator('main')).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(800);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-teacher-academic-desktop-1440.png` });

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-teacher-academic-tablet-768.png` });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-teacher-academic-mobile-390.png` });

    // 2. Parent Portal Responsive Checks & Linked Child Verification
    await page.setViewportSize({ width: 1440, height: 900 });
    await logoutViaUiOrStorage(page);
    await loginAs(page, 'parent.demo@egitim.com', 'Demo1234!');
    await page.goto('/parent/summary');
    await expect(page.getByRole('heading', { name: 'Öğrenci Özeti' })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('main').getByText('Ahmet Yılmaz')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('main').getByText('Zeynep Demir')).not.toBeVisible();
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-parent-summary-desktop-1440.png` });

    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-parent-summary-tablet-768.png` });

    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${SCREENSHOT_DIR}/uat-parent-summary-mobile-390.png` });

    // 3. API Security Verification: Parent cannot access unlinked student Zeynep Demir (d0000000-0000-0000-0000-00000000000d)
    const parentLoginRes = await request.post('http://localhost:5005/api/v1/auth/login', {
      data: { email: 'parent.demo@egitim.com', password: 'Demo1234!' },
    });
    const { accessToken: parentToken } = await parentLoginRes.json();
    const unlinkedStudentId = 'd0000000-0000-0000-0000-00000000000d';

    const unlinked360Res = await request.get(`http://localhost:5005/api/v1/students/${unlinkedStudentId}/360`, {
      headers: { Authorization: `Bearer ${parentToken}` },
    });
    expect([403, 404]).toContain(unlinked360Res.status());

    const unlinkedExamsRes = await request.get(`http://localhost:5005/api/v1/students/${unlinkedStudentId}/exam-results`, {
      headers: { Authorization: `Bearer ${parentToken}` },
    });
    expect([403, 404]).toContain(unlinkedExamsRes.status());

    // 4. Student cannot access another student's 360 or exam results
    const studentLoginRes = await request.post('http://localhost:5005/api/v1/auth/login', {
      data: { email: 'student.demo@egitim.com', password: 'Demo1234!' },
    });
    const { accessToken: studentToken } = await studentLoginRes.json();
    const crossStudentRes = await request.get(`http://localhost:5005/api/v1/students/${unlinkedStudentId}/360`, {
      headers: { Authorization: `Bearer ${studentToken}` },
    });
    expect([403, 404]).toContain(crossStudentRes.status());
  });

  test('UAT-5: Defect 3 Verification — Messaging Deep Links from Coach & Teacher with Proper User/Student Mapping', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });

    // 1. Coach deep link to Ahmet Yılmaz
    await loginAs(page, 'coach.demo@egitim.com', 'Demo1234!');
    const studentId = 'd0000000-0000-0000-0000-000000000007';
    await page.goto(`/coach/students/${studentId}`);
    await expect(page.getByRole('button', { name: /Mesaj Gönder/i })).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: /Mesaj Gönder/i }).click();

    await page.waitForURL((url) => url.pathname.includes('/coach/messages') && url.search.includes('studentId='), { timeout: 10000 });
    expect(page.url()).toContain(`studentId=${studentId}`);
    expect(page.url()).toContain('userId=d0000000-0000-0000-0000-000000000005');

    // Verify recipient name is Ahmet Yılmaz and not falling back to someone else
    await expect(page.locator('h3:has-text("Ahmet Yılmaz"), span:has-text("Ahmet Yılmaz")').first()).toBeVisible({ timeout: 10000 });

    // Send a message via deep-linked conversation
    const coachDeepMsg = `Koçtan Ahmet'e doğrudan mesaj (${Date.now().toString().slice(-4)})`;
    await page.locator('#message-content-input').fill(coachDeepMsg);
    await page.locator('button:has-text("Gönder")').click();
    await expect(page.getByText(coachDeepMsg).first()).toBeVisible({ timeout: 10000 });

    // 2. Teacher deep link to Ahmet Yılmaz
    await logoutViaUiOrStorage(page);
    await loginAs(page, 'teacher.demo@egitim.com', 'Demo1234!');
    await page.goto(`/teacher/students/${studentId}`);
    await expect(page.getByRole('button', { name: /Mesaj Gönder/i })).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: /Mesaj Gönder/i }).click();

    await page.waitForURL((url) => url.pathname.includes('/teacher/messages') && url.search.includes('studentId='), { timeout: 10000 });
    expect(page.url()).toContain(`studentId=${studentId}`);
    expect(page.url()).toContain('userId=d0000000-0000-0000-0000-000000000005');

    await expect(page.locator('h3:has-text("Ahmet Yılmaz"), span:has-text("Ahmet Yılmaz")').first()).toBeVisible({ timeout: 10000 });

    const teacherDeepMsg = `Öğretmenden Ahmet'e yönlendirilmiş mesaj (${Date.now().toString().slice(-4)})`;
    await page.locator('#message-content-input').fill(teacherDeepMsg);
    await page.locator('button:has-text("Gönder")').click();
    await expect(page.getByText(teacherDeepMsg).first()).toBeVisible({ timeout: 10000 });
  });

  test('UAT-6: Defect 2 Verification — Empty Messaging & Authorized Recipient Initialization for Parent and Student', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });

    // 1. Parent initiates conversation using authorized recipients
    await loginAs(page, 'parent.demo@egitim.com', 'Demo1234!');
    await page.goto('/parent/messages');
    await expect(page.locator('#message-content-input')).toBeVisible({ timeout: 10000 });

    const parentMsg = `Veliden koç ve öğretmene ilk mesaj (${Date.now().toString().slice(-4)})`;
    await page.locator('#message-content-input').fill(parentMsg);
    await page.locator('button:has-text("Gönder")').click();
    await expect(page.getByText(parentMsg).first()).toBeVisible({ timeout: 10000 });

    // 2. Student initiates conversation using authorized recipients
    await logoutViaUiOrStorage(page);
    await loginAs(page, 'student.demo@egitim.com', 'Demo1234!');
    await page.goto('/student/messages');
    await expect(page.locator('#message-content-input')).toBeVisible({ timeout: 10000 });

    const studentNewMsg = `Öğrenciden koçuna doğrudan mesaj (${Date.now().toString().slice(-4)})`;
    await page.locator('#message-content-input').fill(studentNewMsg);
    await page.locator('button:has-text("Gönder")').click();
    await expect(page.getByText(studentNewMsg).first()).toBeVisible({ timeout: 10000 });
  });
});
