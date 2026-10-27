import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Coach Dashboard Redesign Tests', () => {
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
              firstName: 'Ali',
              lastName: 'Yılmaz',
              fullName: 'Ali Yılmaz',
              status: 'Active',
              institutionId: 'inst-1',
              createdAt: '2026-09-01T00:00:00Z',
              gradeLevel: 12,
            },
            {
              id: 'stu-2',
              firstName: 'Zeynep',
              lastName: 'Kaya',
              fullName: 'Zeynep Kaya',
              status: 'Active',
              institutionId: 'inst-1',
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

    await page.route('**/api/v1/coaching/meetings', async (route: any) => {
      const todayStr = new Date().toISOString().split('T')[0];
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'meet-1',
            studentId: 'stu-1',
            studentName: 'Ali Yılmaz',
            initials: 'AY',
            grade: '12. Sınıf',
            field: 'Sayısal',
            dateStr: 'Bugün',
            timeStr: '14:30',
            durationMinutes: 30,
            type: 'Haftalık İlerleme Değerlendirmesi',
            locationType: 'Online Video',
            status: 'upcoming',
            startTime: `${todayStr}T14:30:00Z`,
            endTime: `${todayStr}T15:00:00Z`,
          },
        ]),
      });
    });

    await page.route('**/api/v1/students/stu-1/coach-tasks*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'task-1',
            institutionId: 'inst-1',
            studentId: 'stu-1',
            coachUserId: 'coach-123',
            title: 'Türev Testi 3 & 4 Çözümü',
            description: 'Eksik konuları tekrar et',
            dueDate: '2026-10-20T12:00:00Z', // overdue
            durationMinutes: 60,
            subjectName: 'Matematik',
            status: 0, // Active
            isCompleted: false,
            createdAt: '2026-10-15T10:00:00Z',
          },
        ]),
      });
    });

    await page.route('**/api/v1/students/stu-2/coach-tasks*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.route('**/api/v1/academic/students/*/study-plan/active', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          plan: {
            taskProgressRate: 0.85,
            completedTaskMinutes: 240,
          },
        }),
      });
    });
  };

  test('1. Renders Coach Hero with real name, KPIs, attention alert and meetings', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/today');
    await page.waitForLoadState('networkidle');

    // Hero & coach greeting
    await expect(page.getByRole('heading', { name: /İyi günler, Hasan/i })).toBeVisible();

    // Portfolio KPIs
    await expect(page.getByText('Atanmış Öğrenci', { exact: true })).toBeVisible();
    await expect(page.getByText('Bugünkü Görüşme', { exact: true })).toBeVisible();
    await expect(page.getByText('Takip Gerektiren', { exact: true })).toBeVisible();
    await expect(page.getByText('Bekleyen Görev', { exact: true })).toBeVisible();

    // Attention Required alert
    await expect(page.getByText('Öncelikli Takip Gerektiren Öğrenciler')).toBeVisible();
    await expect(page.getByText('Ali Yılmaz').first()).toBeVisible();
    await expect(page.getByText(/Gecikmiş Görev/i)).toBeVisible();

    // Today's Meetings
    await expect(page.getByText('Bugünkü Görüşmeler')).toBeVisible();
    await expect(page.getByText('Haftalık İlerleme Değerlendirmesi')).toBeVisible();

    // Pending Tasks
    await expect(page.getByText('Türev Testi 3 & 4 Çözümü')).toBeVisible();
  });

  test('2. Navigation actions link correctly to real student profile and meetings', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/today');
    await page.waitForLoadState('networkidle');

    // Click "İncele" button on attention card
    await page.getByRole('button', { name: 'İncele' }).first().click();
    await expect(page).toHaveURL(/\/coach\/students\/stu-1\?tab=tasks/);

    // Return to dashboard
    await page.goto('/coach/today');
    await page.waitForLoadState('networkidle');

    // Click "Görüşmeler" button
    await page.getByRole('button', { name: 'Görüşmeler' }).first().click();
    await expect(page).toHaveURL(/\/coach\/meetings/);
  });

  test('3. Responsive layout zero overflow and capture screenshots', async ({ page }) => {
    await setupMocks(page);

    const viewports = [
      { width: 390, height: 844, name: 'mobile-390' },
      { width: 768, height: 1024, name: 'tablet-768' },
      { width: 1440, height: 900, name: 'desktop-1440' },
    ];

    for (const vp of viewports) {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/coach/today');
      await page.waitForLoadState('networkidle');

      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));

      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-dashboard-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
