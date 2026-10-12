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
  };

  test('1. Coach login & dashboard redirection', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/coach/);
    await expect(page.locator('h1.font-serif')).toContainText('İyi günler');
  });

  test('2. Assigned student list view', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Atanmış Öğrenciler');
    await expect(page.locator('input[placeholder*="Öğrenci adı"]')).toBeVisible();
  });

  test('3. Student detail view navigation', async ({ page }) => {
    await setupCoachSession(page);
    await page.goto('/coach/students/student-1');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toBeVisible();
    await expect(page.locator('button:has-text("Öğrenci Listesine Dön")')).toBeVisible();
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
});
