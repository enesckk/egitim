import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Classes & Classroom Detail Redesign Tests', () => {
  const teacherToken = createMockJwt({
    sub: 'teacher-123',
    email: 'teacher.demo@egitim.com',
    name: 'Kemal Bey',
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
          firstName: 'Kemal',
          lastName: 'Bey',
          fullName: 'Kemal Bey',
          title: 'Matematik & Fizik',
          institutionId: 'inst-1',
          subjects: [
            { id: 'ts-1', subjectId: 'sub-1', subjectName: 'Matematik' },
          ],
          assignedClassroomCount: 2,
          assignedStudentCount: 35,
        }),
      });
    });
  };

  test('1. Classes list renders Sınıflarım heading, branch badges, and filtering', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/classrooms*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'cls-1',
            name: '12-A Sayısal',
            branch: 'Ana Şube',
            gradeLevel: 12,
            studentCount: 18,
            teachers: [{ id: 't-1', fullName: 'Kemal Bey' }],
          },
          {
            id: 'cls-2',
            name: '11-B Eşit Ağırlık',
            branch: 'Şube B',
            gradeLevel: 11,
            studentCount: 17,
            teachers: [{ id: 't-1', fullName: 'Kemal Bey' }],
          },
        ]),
      });
    });

    await page.goto('/teacher/classes');
    await page.waitForLoadState('networkidle');

    // Heading and badge
    await expect(page.getByRole('heading', { name: 'Sınıflarım' })).toBeVisible();
    await expect(page.getByText('2 Atanmış Şube')).toBeVisible();
    await expect(page.getByText('Toplam Kayıtlı: 35 Öğrenci')).toBeVisible();

    // Cards visible
    await expect(page.getByText('12-A Sayısal')).toBeVisible();
    await expect(page.getByText('11-B Eşit Ağırlık')).toBeVisible();

    // Test Search Filter
    const searchInput = page.getByPlaceholder('Sınıf adı, şube veya ders ara...');
    await searchInput.fill('12-A');
    await expect(page.getByText('12-A Sayısal')).toBeVisible();
    await expect(page.getByText('11-B Eşit Ağırlık')).not.toBeVisible();

    await searchInput.fill('');
    await expect(page.getByText('11-B Eşit Ağırlık')).toBeVisible();
  });

  test('2. Classroom Detail renders roster, back link, and student profile navigation', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/classrooms/cls-1', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'cls-1',
          name: '12-A Sayısal',
          branch: 'Ana Şube',
          gradeLevel: 12,
          teachers: [{ id: 't-1', fullName: 'Kemal Bey' }],
          students: [
            { id: 'cs-1', studentId: 'st-101', fullName: 'Zeynep Demir' },
            { id: 'cs-2', studentId: 'st-102', fullName: 'Burak Yılmaz' },
          ],
        }),
      });
    });

    await page.goto('/teacher/classes/cls-1');
    await page.waitForLoadState('networkidle');

    // Header & stats
    await expect(page.getByRole('heading', { name: '12-A Sayısal' })).toBeVisible();
    await expect(page.getByText('Sınıf Öğrencileri (2)')).toBeVisible();
    await expect(page.getByText('Zeynep Demir')).toBeVisible();
    await expect(page.getByText('Burak Yılmaz')).toBeVisible();

    // Click student navigates to /teacher/students/:studentId
    await page.getByText('Zeynep Demir').click();
    await expect(page).toHaveURL(/\/teacher\/students\/st-101/);
  });
});
