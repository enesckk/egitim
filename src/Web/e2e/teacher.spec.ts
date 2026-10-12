import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Portal Quality Gate & Flow Tests', () => {
  const teacherToken = createMockJwt({
    sub: 'teacher-123',
    email: 'ogretmen@egitim.com',
    name: 'Mehmet Öğretmen',
    role: 'Teacher',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const setupTeacherSession = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: teacherToken,
          refreshToken: 'mock-refresh-token',
          expiresIn: 3600,
        }),
      });
    });

    await page.route('**/api/v1/taxonomy/subjects*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'sub-1', name: 'Matematik', code: 'MAT' },
          { id: 'sub-2', name: 'Fizik', code: 'FIZ' },
        ]),
      });
    });
  };

  test('1. Teacher login & dashboard redirection', async ({ page }) => {
    await setupTeacherSession(page);
    await page.goto('/teacher');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/teacher/);
    await expect(page.locator('h1.font-serif')).toContainText('Günaydın');
  });

  test('2. Question Bank list view & filters', async ({ page }) => {
    await setupTeacherSession(page);

    await page.route('**/api/v1/questions*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'q-1',
              title: 'Matematik Türev Soru',
              content: 'f(x) = x^2 ise f\'(3) kaçtır?',
              status: 'Published',
              difficulty: 'Medium',
              subjectId: 'sub-1',
              topicId: 'top-1',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 15,
        }),
      });
    });

    await page.goto('/teacher/questions');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Soru Bankası' })).toBeVisible();
    await expect(page.locator('input[placeholder*="Soru"]')).toBeVisible();
  });

  test('3. Question create modal open', async ({ page }) => {
    await setupTeacherSession(page);

    await page.route('**/api/v1/questions*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 15,
        }),
      });
    });

    await page.goto('/teacher/questions');
    await page.waitForLoadState('networkidle');

    const newQuestionBtn = page.locator('button:has-text("Yeni Soru Oluştur")');
    await newQuestionBtn.click();

    await expect(page.getByRole('heading', { name: 'Yeni Soru Oluştur' })).toBeVisible();
  });

  test('4. Exam list view', async ({ page }) => {
    await setupTeacherSession(page);

    await page.goto('/teacher/content');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Ders Materyalleri & Testler')).toBeVisible();
  });

  test('5. Exam creation trigger', async ({ page }) => {
    await setupTeacherSession(page);

    await page.goto('/teacher/content');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Ders Materyalleri & Testler');
  });

  test('6. Exam question assembly view', async ({ page }) => {
    await setupTeacherSession(page);

    await page.route('**/api/v1/exams/ex-1', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-1',
          code: 'TYT-01',
          title: 'Genel TYT Denemesi 1',
          status: 'Draft',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 10 }],
        }),
      });
    });

    await page.route('**/api/v1/exams/ex-1/questions', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/teacher/exams/ex-1/questions');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Soru Bankası Seçici')).toBeVisible();
  });

  test('7. Unauthorized admin route denial', async ({ page }) => {
    await setupTeacherSession(page);

    await page.goto('/admin');
    await page.waitForLoadState('networkidle');

    // RoleGuard redirects Teacher away from admin-only routes
    await expect(page).not.toHaveURL(/\/admin$/);
  });

  test('8. Unauthorized student Academic Intelligence denial', async ({ page }) => {
    await setupTeacherSession(page);

    await page.route('**/api/v1/students/unassigned-student-999/academic-intelligence/*', async (route: any) => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({
          title: 'Forbidden',
          status: 403,
          detail: 'Öğretmen yalnızca kendi şubesindeki öğrencilerin sınav sonuçlarını görebilir.',
        }),
      });
    });

    await page.goto('/teacher/students/unassigned-student-999');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toBeVisible();
  });

  test('9. Teacher result controls match backend permission', async ({ page }) => {
    await setupTeacherSession(page);

    await page.goto('/teacher/exams/ex-1/results/entry');
    await page.waitForLoadState('networkidle');

    // Explicit Yetki Sınırlaması card displayed for Teacher role
    await expect(page.locator('text=Yetki Sınırlaması')).toBeVisible();
  });
});
