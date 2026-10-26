import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Academic Data Integrity & Empty States', () => {
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
          assignedClassroomCount: 1,
          assignedStudentCount: 20,
        }),
      });
    });
  };

  test('1. Dashboard Academic Gaps explicitly distinguishes critical and total counts', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/teachers/dashboard', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          teacherName: 'Kemal Bey',
          assignedSubjects: ['Matematik'],
          totalAssignedStudents: 20,
          totalAssignedClasses: 1,
          academicGaps: [
            { id: 'gap-1', studentId: 'st-1', studentName: 'Ali Demir', grade: '12. Sınıf', subject: 'Matematik', topic: 'Limit', detail: 'Kritik eksik', severity: 'critical', initials: 'AD', lastExam: 'TYT-1' },
            { id: 'gap-2', studentId: 'st-2', studentName: 'Veli Can', grade: '12. Sınıf', subject: 'Matematik', topic: 'Türev', detail: 'Kritik eksik', severity: 'critical', initials: 'VC', lastExam: 'TYT-1' },
            { id: 'gap-3', studentId: 'st-3', studentName: 'Canan Berk', grade: '12. Sınıf', subject: 'Matematik', topic: 'İntegral', detail: 'Tekrar', severity: 'warning', initials: 'CB', lastExam: 'TYT-1' },
          ],
          studentNets: [],
        }),
      });
    });

    await page.goto('/teacher/today');
    await page.waitForLoadState('networkidle');

    // Verify badge text explicitly distinguishes critical vs total
    await expect(page.getByText('2 Kritik Konu Açığı / 3 Toplam Açık')).toBeVisible();
  });

  test('2. Classrooms view renders honest empty states without hardcoded 65.4 net', async ({ page }) => {
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
        ]),
      });
    });

    await page.goto('/teacher/classes');
    await page.waitForLoadState('networkidle');

    // Must NOT contain hardcoded 65.4
    await expect(page.getByText('65.4 net')).not.toBeVisible();
    await expect(page.getByText(/Henüz sınav verisi yok/i)).toBeVisible();
  });

  test('3. Classroom detail view renders honest empty state for topic mastery and nets', async ({ page }) => {
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
            { id: 'cs-1', studentId: 'st-1', fullName: 'Zeynep Demir' },
          ],
        }),
      });
    });

    await page.goto('/teacher/classes/cls-1');
    await page.waitForLoadState('networkidle');

    // Verify honest empty state for topic mastery
    await expect(page.getByText(/Bu şube için henüz konu hakimiyet verisi bulunmuyor/i)).toBeVisible();
    // Verify no hardcoded 65.4 net in header
    await expect(page.getByText('65.4 net')).not.toBeVisible();
    await expect(page.getByText(/Henüz sınav verisi yok/i)).toBeVisible();
  });

  test('4. Student detail view renders honest empty state without hardcoded 24.5/9.0 nets', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/students/st-1/360', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          student: {
            id: 'st-1',
            firstName: 'Zeynep',
            lastName: 'Demir',
            fullName: 'Zeynep Demir',
            status: 'Active',
            institutionId: 'inst-1',
            createdAt: new Date().toISOString(),
            gradeLevel: 12,
          },
          currentGoals: [],
          coaches: [],
          parents: [],
          examTypes: [],
          recentExam: null,
        }),
      });
    });

    await page.goto('/teacher/students/st-1');
    await page.waitForLoadState('networkidle');

    // Must NOT contain hardcoded 24.5 or 9.0 net
    await expect(page.getByText('24.5 net')).not.toBeVisible();
    await expect(page.getByText('9 net')).not.toBeVisible();
    await expect(page.getByText(/Henüz sınav verisi yok/i)).toBeVisible();
    // Verify empty topics list
    await expect(page.getByText(/Konu performansı verisi bulunmuyor/i)).toBeVisible();
  });
});
