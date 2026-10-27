import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Student Detail Redesign Tests', () => {
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
  };

  test('1. Student detail renders identity card, tabs, and topic performances', async ({ page }) => {
    await setupBaseMocks(page);

    await page.route('**/api/v1/students/st-101/360', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          student: {
            id: 'st-101',
            firstName: 'Ahmet',
            lastName: 'Yılmaz',
            fullName: 'Ahmet Yılmaz',
            status: 'Active',
            institutionId: 'inst-1',
            createdAt: new Date().toISOString(),
            gradeLevel: 12,
            studentNumber: '1201',
          },
          currentGoals: [],
          coaches: [],
          parents: [],
          examTypes: [],
          recentExam: {
            latestExamId: 'ex-1',
            title: 'Demo TYT 5',
            examType: 'TYT',
            takenAt: '2026-10-04T10:00:00Z',
            totalNet: 45.5,
            previousNet: 42.0,
            delta: 3.5,
          },
        }),
      });
    });

    await page.route('**/api/v1/students/st-101/academic-intelligence/classifications/topics*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            topicId: 'top-1',
            topicName: 'Trigonometri',
            subjectId: 'sub-1',
            subjectName: 'Matematik',
            state: 'Critical',
            hasEnoughData: true,
            observationCount: 5,
            totalQuestions: 11,
            attemptedCount: 11,
            totalCorrect: 1,
            totalWrong: 10,
            totalBlank: 0,
            successRate: 0.09,
            accuracy: 0.09,
            coverage: 1,
          },
          {
            topicId: 'top-2',
            topicName: 'Temel Kavramlar',
            subjectId: 'sub-1',
            subjectName: 'Matematik',
            state: 'Strong',
            hasEnoughData: true,
            observationCount: 5,
            totalQuestions: 10,
            attemptedCount: 10,
            totalCorrect: 10,
            totalWrong: 0,
            totalBlank: 0,
            successRate: 1.0,
            accuracy: 1.0,
            coverage: 1,
          },
        ]),
      });
    });

    await page.goto('/teacher/students/st-101');
    await page.waitForLoadState('networkidle');

    // Header & identity
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();
    await expect(page.getByText('No: 1201')).toBeVisible();
    await expect(page.getByText('Branşınız: Matematik')).toBeVisible();

    // Topics tab
    await expect(page.getByText('Trigonometri')).toBeVisible();
    await expect(page.getByText('Kritik Eksik')).toBeVisible();
    await expect(page.getByText('Temel Kavramlar')).toBeVisible();
    await expect(page.getByText('Kazanıldı')).toBeVisible();

    // Exams tab
    await page.getByRole('button', { name: /Deneme Analizleri/i }).click();
    await expect(page.getByText('Demo TYT 5')).toBeVisible();
    await expect(page.getByText('45.5')).toBeVisible();

    // Homework modal trigger
    await page.getByRole('button', { name: /Ödev Ata/i }).click();
    await expect(page.getByRole('heading', { name: 'Öğrenciye Ödev / Test Ata' })).toBeVisible();
    await page.getByRole('button', { name: 'İptal' }).click();
  });
});
