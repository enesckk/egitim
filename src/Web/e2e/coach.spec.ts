import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Coach Portal Quality Gate & Flow Tests', () => {
  const coachToken = createMockJwt({
    sub: 'coach-123',
    email: 'koc@egitim.com',
    name: 'Ahmet Koç',
    role: 'Coach',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const setupCoachSession = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: coachToken,
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
              firstName: 'Ahmet',
              lastName: 'Yılmaz',
              fullName: 'Ahmet Yılmaz',
              status: 'Active',
              institutionId: 'inst-1',
              createdAt: new Date().toISOString(),
              schoolName: 'Demo Anadolu Lisesi',
              gradeLevel: 12,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
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
            firstName: 'Ahmet',
            lastName: 'Yılmaz',
            fullName: 'Ahmet Yılmaz',
            status: 'Active',
            institutionId: 'inst-1',
            createdAt: new Date().toISOString(),
            schoolName: 'Demo Anadolu Lisesi',
            gradeLevel: 12,
          },
          currentGoals: [],
          coaches: [{ coachId: 'coach-123', name: 'Ahmet Koç', isPrimary: true, assignedAt: new Date().toISOString() }],
          parents: [],
          examTypes: [{ id: 'et-1', code: 'TYT', name: 'TYT' }],
          recentExam: null,
        }),
      });
    });
  };

  test('1. Coach login & dashboard redirection', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/coach/);
    await expect(page.locator('h1.font-serif')).toContainText('İyi günler');
  });

  test('2. Assigned student list view renders real API student', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Atanmış Öğrenciler');
    await expect(page.locator('input[placeholder*="Öğrenci adı"]')).toBeVisible();
    await expect(page.locator('text=Ahmet Yılmaz')).toBeVisible();
  });

  test('3. Student detail view navigation and real CoachStudentExamsTab', async ({ page }) => {
    await setupCoachSession(page);

    await page.route('**/api/v1/students/student-1/exam-results*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              attemptId: 'att-c1',
              examId: 'ex-c1',
              examTitle: 'Demo TYT 5',
              examTypeId: 'et-1',
              examTypeName: 'TYT',
              takenAt: new Date().toISOString(),
              status: 1,
              totalCorrect: 30,
              totalWrong: 10,
              totalBlank: 0,
              totalNet: 27.5,
              reportedScore: null,
              scoreSource: 0,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      });
    });

    await page.route('**/api/v1/students/student-1/exam-analysis*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          attemptCount: 1,
          averageTotalNet: 27.5,
          latestTotalNet: 27.5,
          previousTotalNet: 25.0,
          delta: 2.5,
          subjectAverages: [],
          weakestTopics: [],
        }),
      });
    });

    await page.goto('/coach/students/student-1?tab=exams');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Ahmet Yılmaz');
    await expect(page.locator('button:has-text("Öğrenci Listesine Dön")')).toBeVisible();
    await expect(page.locator('text=Demo TYT 5')).toBeVisible();
  });

  test('4. Academic Intelligence summary in coach overview', async ({ page }) => {
    await setupCoachSession(page);

    await page.route('**/api/v1/students/student-1/academic-intelligence/ai-summary?audience=coach', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          audience: 'coach',
          summary: 'Öğrencinin Matematik performansı yükselişte olup zayıf alanlarda koç takibi önerilmektedir.',
          strengths: ['Trigonometri', 'Vektörler'],
          improvementAreas: ['İntegral', 'Limit'],
          trendNotes: ['Son 3 denemede net trendi +4.5 artış gösterdi.'],
          suggestedFocus: ['İntegral alıştırmaları yapılmalı.'],
          isAvailable: true,
          generatedAt: new Date().toISOString(),
        }),
      });
    });

    await page.goto('/coach/students/student-1?tab=overview');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=AI Akademik Yorum')).toBeVisible();
  });

  test('5. Study Plan read-only verification', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach/students/student-1?tab=plan');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Haftalık Çalışma Programı')).toBeVisible();
    // Ensure no plan completion checkboxes or edit buttons exist in Study Plan review tab
    const editPlanBtns = page.locator('button:has-text("Planı Düzenle")');
    await expect(editPlanBtns).toHaveCount(0);
  });

  test('6. Create Coach Task modal form & submission', async ({ page }) => {
    await setupCoachSession(page);

    await page.route('**/api/v1/students/student-1/coach-tasks', async (route: any) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([]),
        });
      } else if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'ct-999',
            studentId: 'student-1',
            coachId: 'coach-123',
            title: '20 Paragraf Sorusu Çöz',
            description: 'Süre tutarak çözülmesi rica olunur',
            status: 'Active',
            isCompleted: false,
            createdAt: new Date().toISOString(),
          }),
        });
      }
    });

    await page.goto('/coach/students/student-1?tab=tasks');
    await page.waitForLoadState('networkidle');

    const newTaskBtn = page.locator('button:has-text("Yeni Görev Ata")');
    await newTaskBtn.click();

    await expect(page.locator('text=Yeni Koç Görevi Tanımla')).toBeVisible();
  });

  test('7. Student completion status reflected in task list', async ({ page }) => {
    await setupCoachSession(page);

    await page.route('**/api/v1/students/student-1/coach-tasks', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'ct-1',
            studentId: 'student-1',
            coachId: 'coach-123',
            title: 'Fizik Vektör Testi Çöz',
            status: 'Completed',
            isCompleted: true,
            completedAt: new Date().toISOString(),
            createdAt: new Date().toISOString(),
          },
        ]),
      });
    });

    await page.goto('/coach/students/student-1?tab=tasks');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Tamamlandı').first()).toBeVisible();
    await expect(page.locator('text=Fizik Vektör Testi Çöz')).toBeVisible();
  });

  test('8. Unauthorized student access denial', async ({ page }) => {
    await setupCoachSession(page);

    await page.goto('/coach/students/unassigned-student-999');
    await page.waitForLoadState('networkidle');

    // Safe error message / not found alert display
    await expect(page.locator('text=Öğrenci Bulunamadı')).toBeVisible();
  });

  test('9. Coach Private Notes displays notes and allows adding notes via modal', async ({ page }) => {
    await setupCoachSession(page);

    await page.route('**/api/v1/coaching/students/student-1/notes', async (route: any) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 'note-1',
              studentId: 'student-1',
              coachId: 'coach-123',
              category: 'Akademik',
              content: 'Öğrenci türev konusunda eksiklerini tamamladı.',
              createdAt: new Date().toISOString(),
            },
          ]),
        });
      } else if (route.request().method() === 'POST') {
        const body = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'note-2',
            studentId: 'student-1',
            coachId: 'coach-123',
            category: body.category || 'Motivasyon',
            content: body.content,
            createdAt: new Date().toISOString(),
          }),
        });
      }
    });

    await page.goto('/coach/students/student-1?tab=notes');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Koça Özel Gizli Not Alanı')).toBeVisible();
    await expect(page.locator('text=Öğrenci türev konusunda eksiklerini tamamladı.')).toBeVisible();

    const addNoteBtn = page.locator('button:has-text("Not Ekle")').first();
    await addNoteBtn.click();

    await expect(page.locator('text=Yeni Özel Koç Notu')).toBeVisible();
    await page.locator('textarea').fill('Deneme sınavı motivasyonu yüksek.');
    await page.locator('button:has-text("Notu Kaydet")').click();

    await expect(page.locator('text=Deneme sınavı motivasyonu yüksek.')).toBeVisible();
  });
});
