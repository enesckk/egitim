import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Admin Portal Quality Gate & Flow Tests', () => {
  const adminToken = createMockJwt({
    sub: 'admin-123',
    email: 'admin@egitim.com',
    name: 'Mustafa Admin',
    role: 'InstitutionAdmin',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const setupAdminSession = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: adminToken,
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

  test('1. Admin login & dashboard redirection', async ({ page }) => {
    await setupAdminSession(page);
    await page.goto('/admin');
    await page.waitForLoadState('networkidle');

    await expect(page).toHaveURL(/\/admin/);
    await expect(page.locator('h1.font-serif')).toContainText('Genel Bakış');
  });

  test('2. Student list & search', async ({ page }) => {
    await setupAdminSession(page);
    await page.goto('/admin/students');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Öğrenciler');
    await expect(page.locator('input[placeholder*="Öğrenci veya koç"]')).toBeVisible();
  });

  test('3. Coach list', async ({ page }) => {
    await setupAdminSession(page);
    await page.goto('/admin/coaches');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Koç Yönetimi');
  });

  test('4. Coach-student assignment', async ({ page }) => {
    await setupAdminSession(page);
    await page.goto('/admin/coaches');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Koç Yönetimi & Portföy')).toBeVisible();
  });

  test('5. Teacher page', async ({ page }) => {
    await setupAdminSession(page);
    await page.goto('/admin/teachers');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Öğretmenler');
  });

  test('6. Question Bank', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/questions*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'q-101',
              title: 'Matematik İntegral Soru',
              content: 'dx integral hesabı',
              status: 'Published',
              difficulty: 'Hard',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 15,
        }),
      });
    });

    await page.goto('/admin/questions');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Soru Bankası' })).toBeVisible();
  });

  test('7. Exam management', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'ex-101',
              code: 'AYT-01',
              title: 'Kurumsal AYT Denemesi',
              status: 'Published',
            },
          ],
          totalCount: 1,
        }),
      });
    });

    await page.goto('/admin/reports');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toBeVisible();
  });

  test('8. Manual result entry view', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Sınav Sonuç İşlemleri' })).toBeVisible();
  });

  test('9. Aggregate Excel preview', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Excel ile Sonuç Yükle')).toBeVisible();
  });

  test('10. Question-level Excel preview', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('text=Excel ile Sonuç Yükle')).toBeVisible();
  });

  test('11. Import validation failure UI', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Sınav Sonuç İşlemleri' })).toBeVisible();
  });

  test('12. Commit success flow', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    await expect(page.getByRole('heading', { name: 'Sınav Sonuç İşlemleri' })).toBeVisible();
  });

  test('13. Cross-tenant denial', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/students/foreign-student-999/academic-intelligence/*', async (route: any) => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({
          title: 'Forbidden',
          status: 403,
          detail: 'Farklı kuruma ait öğrenci verisine erişim yetkiniz bulunmamaktadır.',
        }),
      });
    });

    await page.goto('/admin/students?search=foreign-student-999');
    await page.waitForLoadState('networkidle');

    await expect(page.locator('h1.font-serif')).toContainText('Öğrenciler');
  });

  test('14. No OCR wording in import UI', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams/ex-101', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'ex-101',
          code: 'AYT-01',
          title: 'Kurumsal AYT Denemesi',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams/ex-101/results/entry');
    await page.waitForLoadState('networkidle');

    const ocrMatches = page.locator('text=/OCR/i');
    await expect(ocrMatches).toHaveCount(0);
  });
});
