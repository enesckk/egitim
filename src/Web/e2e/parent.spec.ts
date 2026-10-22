import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Parent Portal Quality Gate & Flow Tests', () => {
  const parentToken = createMockJwt({
    sub: 'parent-123',
    email: 'veli@egitim.com',
    name: 'Fatma Hanım',
    role: 'Parent',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const setupParentSession = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: parentToken,
          refreshToken: 'mock-refresh-token',
          expiresIn: 3600,
        }),
      });
    });

    await page.route('**/api/v1/students?*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'student-1',
              firstName: 'Ayşe',
              lastName: 'Kaya',
              fullName: 'Ayşe Kaya',
              status: 'Active',
              institutionId: 'inst-1',
              createdAt: new Date().toISOString(),
              schoolName: 'Sayısal',
              gradeLevel: 11,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    await page.route('**/api/v1/students/student-1/360', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          student: {
            id: 'student-1',
            firstName: 'Ayşe',
            lastName: 'Kaya',
            fullName: 'Ayşe Kaya',
            status: 'Active',
            institutionId: 'inst-1',
            createdAt: new Date().toISOString(),
            schoolName: 'Sayısal',
            gradeLevel: 11,
          },
          currentGoals: [],
          coaches: [{ coachId: 'coach-1', name: 'Hasan Yılmaz', isPrimary: true, assignedAt: new Date().toISOString() }],
          parents: [{ parentId: 'parent-123', name: 'Fatma Hanım', relationshipType: 'Mother' }],
          examTypes: [{ id: 'et-1', code: 'AYT', name: 'AYT' }],
          recentExam: {
            latestExamId: 'ex-1',
            title: 'TYT Denemesi',
            examType: 'TYT',
            takenAt: new Date().toISOString(),
            totalNet: 92.5,
            previousNet: 88.0,
            delta: 4.5,
          },
        }),
      });
    });

    await page.route('**/api/v1/students/student-1/academic-intelligence/classifications/subjects*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            subjectId: 'sub-mat',
            subjectName: 'Matematik',
            state: 'Strong',
            hasEnoughData: true,
            basis: 'RecentWindow',
            observationCount: 5,
            totalQuestions: 50,
            attemptedCount: 50,
            totalCorrect: 45,
            totalWrong: 5,
            totalBlank: 0,
            successRate: 0.875,
            accuracy: 0.9,
            coverage: 1,
            trendDirection: 'Increasing',
            successRateDelta: 0.1,
            policyVersion: 'v1',
            insufficiencyReason: '',
          },
        ]),
      });
    });

    await page.route('**/api/v1/students/student-1/study-plans/active', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          plan: {
            id: 'plan-1',
            institutionId: 'inst-1',
            studentId: 'student-1',
            title: 'Haftalık Plan',
            startDate: new Date().toISOString(),
            endDate: new Date().toISOString(),
            status: 'Active',
            source: 'AcademicIntelligence',
            planDays: 7,
            dailyStudyMinutes: 60,
            sessionMinutes: 30,
            totalAvailableMinutes: 420,
            totalPlannedMinutes: 60,
            unallocatedMinutes: 360,
            utilizationRate: 0.14,
            completedTaskCount: 1,
            totalTaskCount: 1,
            remainingTaskCount: 0,
            completedTaskMinutes: 30,
            taskProgressRate: 0.82,
            minuteProgressRate: 0.82,
            createdAt: new Date().toISOString(),
          },
          tasks: [
            {
              id: 't-1',
              studyPlanId: 'plan-1',
              studentId: 'student-1',
              dayIndex: 1,
              plannedDate: new Date().toISOString(),
              sequence: 1,
              durationMinutes: 30,
              nodeType: 'Subject',
              nodeId: 'sub-mat',
              subjectId: 'sub-mat',
              subjectName: 'Matematik',
              titleSnapshot: 'Matematik Tekrar',
              classificationStateSnapshot: 'Strong',
              priorityScoreSnapshot: 50,
              recommendationType: 'ReinforceDeveloping',
              isCompleted: true,
              completedAt: new Date().toISOString(),
            },
          ],
        }),
      });
    });

    await page.route('**/api/v1/students/student-1/coach-tasks*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });
  };

  test('1. Parent login & home page redirection', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/parent/);
    await expect(page.locator('h1.font-serif')).toContainText('Öğrenci Özeti');
  });

  test('2. Linked student visible in hero summary', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Ayşe Kaya')).toBeVisible();
    await expect(page.locator('text=11. Sınıf').first()).toBeVisible();
  });

  test('3. Unlinked student access denial', async ({ page }) => {
    await setupParentSession(page);

    await page.goto('/parent/summary?studentId=unlinked-student-999');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Öğrenci Özeti');
    await expect(page.locator('text=Bağlı Öğrenci Kaydı Bulunamadı')).toBeVisible();
  });

  test('4. Academic summary display', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Akademik Durum')).toBeVisible();
    await expect(page.locator('text=Güçlenen Alan')).toBeVisible();
  });

  test('5. Exam results display', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Yaklaşan')).toBeVisible();
  });

  test('6. Study Plan progress read-only verification', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Plan Uyumu')).toBeVisible();

    // Ensure zero plan acceptance or editing buttons exist
    const acceptBtns = page.locator('button:has-text("Planı Kabul Et")');
    await expect(acceptBtns).toHaveCount(0);
  });

  test('7. Coach tasks read-only verification', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    // Ensure zero task creation or edit buttons exist
    const createTaskBtns = page.locator('button:has-text("Görev Ata")');
    await expect(createTaskBtns).toHaveCount(0);
  });

  test('8. AI interpretation display', async ({ page }) => {
    await setupParentSession(page);

    await page.route('**/api/v1/students/student-1/academic-intelligence/ai-summary?audience=parent', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          audience: 'parent',
          summary: 'Çocuğunuzun çalışma disiplini ve sınav başarısı düzenli bir gelişim göstermektedir.',
          strengths: ['Matematik', 'Fizik'],
          improvementAreas: ['Türkçe Paragraf'],
          trendNotes: ['Son 3 denemede net seviyesi stabil yükselişte.'],
          suggestedFocus: ['Haftalık paragraf etütlerine devam edilmeli.'],
          isAvailable: true,
          generatedAt: new Date().toISOString(),
        }),
      });
    });

    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=AI Akademik Yorum')).toBeVisible();
  });

  test('9. No mutation controls in parent view', async ({ page }) => {
    await setupParentSession(page);
    await page.goto('/parent');
    await page.waitForLoadState('networkidle');

    // Ensure no form inputs or checkboxes exist in parent summary
    const formInputs = page.locator('input:not([type="hidden"])');
    await expect(formInputs).toHaveCount(0);

    const checkboxes = page.locator('input[type="checkbox"]');
    await expect(checkboxes).toHaveCount(0);
  });
});
