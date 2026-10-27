import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Question Bank Redesign Tests', () => {
  const teacherToken = createMockJwt({
    sub: 'teacher-123',
    email: 'teacher.demo@egitim.com',
    name: 'Ayşe Öğretmen',
    role: 'Teacher',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const setupBaseMocks = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: teacherToken,
          refreshToken: 'mock-rt',
          expiresIn: 3600,
        }),
      });
    });

    await page.route('**/api/v1/teachers/me', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'teacher-1',
          userId: 'teacher-123',
          firstName: 'Ayşe',
          lastName: 'Öğretmen',
          fullName: 'Ayşe Öğretmen',
          title: 'Matematik Öğretmeni',
          institutionId: 'inst-1',
          subjects: [
            { id: 'ts-1', subjectId: 'sub-mat', subjectName: 'Matematik' },
          ],
          assignedClassroomCount: 1,
          assignedStudentCount: 2,
        }),
      });
    });

    await page.route('**/api/v1/taxonomy/subjects', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'sub-mat', name: 'Matematik' },
          { id: 'sub-fiz', name: 'Fizik' },
        ]),
      });
    });

    await page.route('**/api/v1/exams*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'exam-1',
              code: 'EX-01',
              title: '12. Sınıf TYT Deneme Sınavı',
              examTypeId: 'type-1',
              examTypeName: 'TYT',
              examDate: '2026-11-01T09:00:00Z',
              status: 1,
              sectionCount: 4,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    await page.route('**/api/v1/questions*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'q-1',
              institutionId: 'inst-1',
              subjectId: 'sub-mat',
              topicId: 'top-1',
              questionType: 1,
              difficulty: 2,
              sourceType: 1,
              content: 'Örnek Matematik Fonksiyonlar ve Parabol Sorusu',
              status: 0,
              optionCount: 5,
              styles: [
                { id: 's-1', code: 'NEW_GEN', name: 'Yeni Nesil', displayOrder: 1 },
              ],
              createdAt: '2026-10-25T10:00:00Z',
            },
            {
              id: 'q-2',
              institutionId: null, // Global
              subjectId: 'sub-mat',
              topicId: 'top-2',
              questionType: 1,
              difficulty: 3,
              sourceType: 1,
              content: 'Global Havuz Trigonometrik Eşitsizlik Sorusu',
              status: 1,
              optionCount: 5,
              styles: [
                { id: 's-2', code: 'INTERP', name: 'Yorum', displayOrder: 2 },
              ],
              createdAt: '2026-10-26T12:00:00Z',
            },
          ],
          totalCount: 2,
          page: 1,
          pageSize: 15,
        }),
      });
    });
  };

  test('1. Renders premium hero, assigned subject badge, pool chips and cards', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/questions');
    await page.waitForLoadState('networkidle');

    // Header & Scope
    await expect(page.getByRole('heading', { name: 'Soru Bankası & Deneme Montajı' })).toBeVisible();
    await expect(page.getByText('Matematik').first()).toBeVisible();
    await expect(page.getByText('Havuz Sorusu')).toBeVisible();
    await expect(page.getByRole('button', { name: /Yeni Soru Ekle/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Sınav Montajı/i })).toBeVisible();

    // Pool Chips
    await expect(page.getByRole('button', { name: 'Tüm Havuzlar' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Kurum Havuzu' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Genel Havuz' })).toBeVisible();

    // Question Cards & Badges
    await expect(page.getByText('Örnek Matematik Fonksiyonlar ve Parabol Sorusu')).toBeVisible();
    await expect(page.getByText('Global Havuz Trigonometrik Eşitsizlik Sorusu')).toBeVisible();
    await expect(page.getByText('Kurumsal', { exact: true })).toBeVisible();
    await expect(page.locator('span').filter({ hasText: 'Genel Havuz' })).toBeVisible();
    await expect(page.getByText('Yeni Nesil')).toBeVisible();
  });

  test('2. Sınav Montajı button opens exam selection modal and navigates to assembly', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/questions');
    await page.waitForLoadState('networkidle');

    // Click Sınav Montajı
    await page.getByRole('button', { name: /Sınav Montajı/i }).click();

    // Modal opens
    await expect(page.getByRole('heading', { name: 'Deneme Sınavı Montajı' })).toBeVisible();
    await expect(page.getByText('12. Sınıf TYT Deneme Sınavı')).toBeVisible();

    // Click Soruları Yönet
    await page.getByRole('button', { name: 'Soruları Yönet' }).click();
    await expect(page).toHaveURL(/\/exams\/exam-1\/questions/);
  });

  test('3. Responsive layout zero horizontal overflow and screenshots', async ({ page }) => {
    await setupBaseMocks(page);

    const viewports = [
      { width: 390, height: 844, name: 'mobile-390' },
      { width: 768, height: 1024, name: 'tablet-768' },
      { width: 1440, height: 900, name: 'desktop-1440' },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/teacher/questions');
      await page.waitForLoadState('networkidle');

      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));

      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/teacher-questions-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
