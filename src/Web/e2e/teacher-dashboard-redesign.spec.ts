import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Dashboard Redesign & Navigation Tests', () => {
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
            branch: 'Ana Şube',
            gradeLevel: 11,
            studentCount: 17,
            teachers: [{ id: 't-1', fullName: 'Kemal Bey' }],
          },
        ]),
      });
    });
  };

  test('1. Dashboard renders teacher identity hero, assigned classes and stats', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/teachers/dashboard', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          teacherName: 'Kemal Bey',
          assignedSubjects: ['Matematik', 'Fizik'],
          totalAssignedStudents: 35,
          totalAssignedClasses: 2,
          academicGaps: [],
          studentNets: [],
        }),
      });
    });

    await page.goto('/teacher/today');
    await page.waitForLoadState('networkidle');

    // Header hero
    await expect(page.getByText('Öğretmen Akademik Kontrol Merkezi')).toBeVisible();
    await expect(page.getByRole('heading', { name: /Günaydın, Kemal Bey/i })).toBeVisible();
    await expect(page.getByText(/35 Öğrenci/i).first()).toBeVisible();
    await expect(page.getByText(/2 Atanmış Şube/i).first()).toBeVisible();

    // Quick-access classes
    await expect(page.getByTestId('quick-class-card-cls-1')).toBeVisible();
    await expect(page.getByTestId('quick-class-card-cls-2')).toBeVisible();
  });

  test('2. Grouped student attention cards display top topics and navigation', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/teachers/dashboard', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          teacherName: 'Kemal Bey',
          assignedSubjects: ['Matematik'],
          totalAssignedStudents: 35,
          totalAssignedClasses: 2,
          academicGaps: [
            { id: 'gap-1', studentId: 'st-1', studentName: 'Zeynep Demir', grade: '12. Sınıf', subject: 'Matematik', topic: 'Limit', detail: 'Kritik eksik', severity: 'critical', initials: 'ZD', lastExam: 'TYT-1' },
            { id: 'gap-2', studentId: 'st-1', studentName: 'Zeynep Demir', grade: '12. Sınıf', subject: 'Matematik', topic: 'Türev', detail: 'Kritik eksik', severity: 'critical', initials: 'ZD', lastExam: 'TYT-1' },
            { id: 'gap-3', studentId: 'st-2', studentName: 'Burak Arslan', grade: '11. Sınıf', subject: 'Matematik', topic: 'Trigonometri', detail: 'Takip', severity: 'warning', initials: 'BA', lastExam: 'TYT-1' },
          ],
          studentNets: [],
        }),
      });
    });

    await page.goto('/teacher/today');
    await page.waitForLoadState('networkidle');

    // Exactly 2 student attention cards (grouped by student)
    const cards = page.getByTestId('student-attention-card');
    await expect(cards).toHaveCount(2);

    // Zeynep Demir card has 2 critical gaps and top topics
    const zeynepCard = cards.first();
    await expect(zeynepCard.getByText('Zeynep Demir')).toBeVisible();
    await expect(zeynepCard.getByText('2 Kritik Açık')).toBeVisible();
    await expect(zeynepCard.getByText('Limit')).toBeVisible();
    await expect(zeynepCard.getByText('Türev')).toBeVisible();

    // Click leads to student detail
    await zeynepCard.click();
    await page.waitForURL('**/teacher/students/st-1');
  });

  test('3. Quick class card navigation opens classroom detail', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/teachers/dashboard', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          teacherName: 'Kemal Bey',
          assignedSubjects: ['Matematik'],
          totalAssignedStudents: 35,
          totalAssignedClasses: 2,
          academicGaps: [],
          studentNets: [],
        }),
      });
    });

    await page.goto('/teacher/today');
    await page.waitForLoadState('networkidle');

    await page.getByTestId('quick-class-card-cls-1').click();
    await page.waitForURL('**/teacher/classes/cls-1');
  });
});
