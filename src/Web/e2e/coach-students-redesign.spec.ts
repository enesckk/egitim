import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Coach Students Redesign Tests', () => {
  const coachToken = createMockJwt({
    sub: 'coach-123',
    email: 'coach.demo@egitim.com',
    name: 'Hasan Koç',
    role: 'Coach',
    institutionId: 'inst-1',
    institutionName: 'Kadıköy Şubesi',
    tenantId: 'tenant-1',
  });

  const setupMocks = async (page: any) => {
    await page.route('**/api/v1/auth/refresh', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: coachToken,
          refreshToken: 'mock-rt',
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
              id: 'stu-1',
              firstName: 'Zeynep',
              lastName: 'Demir',
              fullName: 'Zeynep Demir',
              status: 'Active',
              institutionId: 'inst-1',
              schoolName: 'Demo Anadolu Lisesi',
              createdAt: '2026-09-01T00:00:00Z',
              gradeLevel: 12,
            },
            {
              id: 'stu-2',
              firstName: 'Ahmet',
              lastName: 'Yılmaz',
              fullName: 'Ahmet Yılmaz',
              status: 'Active',
              institutionId: 'inst-1',
              schoolName: 'Kadıköy Fen Lisesi',
              createdAt: '2026-09-01T00:00:00Z',
              gradeLevel: 11,
            },
          ],
          totalCount: 2,
          page: 1,
          pageSize: 50,
        }),
      });
    });

    // stu-1 has active plan with 80% progress
    await page.route('**/api/v1/students/stu-1/study-plans/active', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          plan: {
            taskProgressRate: 0.8,
            completedTaskMinutes: 240,
          },
        }),
      });
    });

    // stu-2 has NO active plan (404)
    await page.route('**/api/v1/students/stu-2/study-plans/active', async (route: any) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'No active plan found' }),
      });
    });
  };

  test('1. Renders header, real student cards, plan progress and honest "Aktif Plan Yok"', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    // Header & badge
    await expect(page.getByRole('heading', { name: 'Atanmış Öğrencilerim' })).toBeVisible();
    await expect(page.getByText('2 Kayıtlı Öğrenci')).toBeVisible();

    // Student 1 (Zeynep Demir): Plan Uyumlu (%80)
    await expect(page.getByText('Zeynep Demir')).toBeVisible();
    await expect(page.locator('span').filter({ hasText: '12. Sınıf' })).toBeVisible();
    await expect(page.getByText('Demo Anadolu Lisesi')).toBeVisible();
    await expect(page.getByText('Plan Uyumlu')).toBeVisible();
    await expect(page.getByText('%80')).toBeVisible();

    // Student 2 (Ahmet Yılmaz): Aktif Plan Yok
    await expect(page.getByText('Ahmet Yılmaz')).toBeVisible();
    await expect(page.locator('span').filter({ hasText: '11. Sınıf' })).toBeVisible();
    await expect(page.getByText('Kadıköy Fen Lisesi')).toBeVisible();
    await expect(page.getByText('Aktif Plan Yok')).toBeVisible();
    await expect(page.getByText('Plan Hazırlanmadı')).toBeVisible();
  });

  test('2. Filters by search query and plan status', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    // Search for Ahmet
    const searchInput = page.getByPlaceholder('Öğrenci adı veya okul ara...');
    await searchInput.fill('Ahmet');
    await expect(page.getByText('Ahmet Yılmaz')).toBeVisible();
    await expect(page.getByText('Zeynep Demir')).not.toBeVisible();

    // Clear search
    await searchInput.fill('');
    await expect(page.getByText('Zeynep Demir')).toBeVisible();

    // Filter by "Aktif Planı Olanlar"
    const select = page.locator('select').first();
    await select.selectOption('active');
    await expect(page.getByText('Zeynep Demir')).toBeVisible();
    await expect(page.getByText('Ahmet Yılmaz')).not.toBeVisible();

    // Filter by "Aktif Planı Olmayanlar"
    await select.selectOption('no_plan');
    await expect(page.getByText('Ahmet Yılmaz')).toBeVisible();
    await expect(page.getByText('Zeynep Demir')).not.toBeVisible();
  });

  test('3. Card actions navigate to student detail and messages', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    // Click student row -> navigates to detail
    await page.getByText('Zeynep Demir').click();
    await expect(page).toHaveURL(/\/coach\/students\/stu-1/);

    // Return to directory
    await page.goto('/coach/students');
    await page.waitForLoadState('networkidle');

    // Click direct message button
    await page.getByRole('button', { name: 'Mesaj Gönder' }).first().click();
    await expect(page).toHaveURL(/\/coach\/messages\?studentId=stu-1/);
  });

  test('4. Responsive layout zero overflow and capture screenshots', async ({ page }) => {
    await setupMocks(page);

    const viewports = [
      { width: 390, height: 844, name: 'mobile-390' },
      { width: 768, height: 1024, name: 'tablet-768' },
      { width: 1440, height: 900, name: 'desktop-1440' },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/coach/students');
      await page.waitForLoadState('networkidle');

      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));

      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-students-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
