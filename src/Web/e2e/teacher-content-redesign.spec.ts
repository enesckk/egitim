import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Content & Materials Redesign Tests', () => {
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
            { id: 'ts-1', subjectId: 'sub-1', subjectName: 'Matematik' },
          ],
          assignedClassroomCount: 1,
          assignedStudentCount: 2,
        }),
      });
    });

    await page.route('**/api/v1/classrooms*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'class-1',
            name: '12-A',
            gradeLevel: 12,
            branch: 'Sayısal',
            studentCount: 2,
          },
        ]),
      });
    });

    await page.route('**/api/content/materials/teacher', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'mat-1',
            title: 'Trigonometri Fasikülü',
            subject: 'Matematik',
            topic: 'Trigonometri',
            grade: '12. Sınıf',
            materialType: 'fasikul',
            originalFileName: 'trigonometri.pdf',
            fileSizeBytes: 2097152,
            assignedClassesCount: 1,
            createdAt: '2026-10-25T10:00:00Z',
            assignments: [
              {
                id: 'asg-1',
                classroomId: 'class-1',
                classroomName: '12-A',
                note: 'Sayfa 1-20 ödev',
                isActive: true,
                createdAt: '2026-10-25T10:05:00Z',
              },
            ],
          },
          {
            id: 'mat-2',
            title: 'Türev Tarama Testi',
            subject: 'Matematik',
            topic: 'Türev',
            grade: '12. Sınıf',
            materialType: 'test',
            originalFileName: 'turev_test.pdf',
            fileSizeBytes: 1048576,
            assignedClassesCount: 0,
            createdAt: '2026-10-26T14:00:00Z',
            assignments: [],
          },
        ]),
      });
    });
  };

  test('1. Content & Materials page renders premium header, filters, and cards', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/content');
    await page.waitForLoadState('networkidle');

    // Header & Scope
    await expect(page.getByRole('heading', { name: /Ders Materyallerim & Testler|Ders Materyalleri & Testler/i })).toBeVisible();
    await expect(page.getByText('Matematik').first()).toBeVisible();
    await expect(page.getByText('Materyal', { exact: true })).toBeVisible();
    await expect(page.getByText('Aktif Atama')).toBeVisible();

    // Cards
    await expect(page.getByText('Trigonometri Fasikülü')).toBeVisible();
    await expect(page.getByText('2.0 MB')).toBeVisible();
    await expect(page.getByText('Türev Tarama Testi')).toBeVisible();
    await expect(page.getByText('1.0 MB')).toBeVisible();
    await expect(page.getByText('12-A')).toBeVisible();
    await expect(page.getByRole('button', { name: /Yeni Materyal Yükle/i })).toBeVisible();

    // Filter by search
    const searchInput = page.getByPlaceholder('Materyal adı veya konu ara...');
    await searchInput.fill('Türev');
    await expect(page.getByText('Türev Tarama Testi')).toBeVisible();
    await expect(page.getByText('Trigonometri Fasikülü')).not.toBeVisible();

    await searchInput.fill('');
    await expect(page.getByText('Trigonometri Fasikülü')).toBeVisible();
  });

  test('2. Revoke action opens in-app confirmation modal instead of native dialog', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/content');
    await page.waitForLoadState('networkidle');

    // Find revoke button on assigned classroom chip
    const revokeBtn = page.getByTitle('Erişimi İptal Et');
    await expect(revokeBtn).toBeVisible();
    await revokeBtn.click();

    // Accessible in-app modal should appear
    await expect(page.getByRole('heading', { name: 'Erişimi Sonlandır' })).toBeVisible();
    await expect(page.getByRole('dialog').getByText('12-A')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Vazgeç' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Erişimi Kaldır' })).toBeVisible();

    // Cancel modal
    await page.getByRole('button', { name: 'Vazgeç' }).click();
    await expect(page.getByRole('heading', { name: 'Erişimi Sonlandır' })).not.toBeVisible();
  });
});
