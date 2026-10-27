import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Coach Student Detail Redesign Tests', () => {
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

    await page.route('**/api/v1/students/stu-1/360*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          student: {
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
          targetGoal: {
            targetUniversity: 'Boğaziçi Üniversitesi',
            targetDepartment: 'Bilgisayar Mühendisliği',
          },
        }),
      });
    });

    await page.route('**/api/v1/students/stu-1/study-plans/active', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          plan: {
            taskProgressRate: 0.75,
            completedTaskMinutes: 180,
            startDate: '2026-10-20T00:00:00Z',
          },
          tasks: [
            {
              id: 't-1',
              titleSnapshot: 'Limit ve Süreklilik Kavramı',
              subjectName: 'Matematik',
              durationMinutes: 45,
              dayIndex: 1,
              isCompleted: true,
            },
          ],
        }),
      });
    });

    await page.route('**/api/v1/students/stu-1/coach-tasks*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'ct-1',
            institutionId: 'inst-1',
            studentId: 'stu-1',
            coachUserId: 'coach-123',
            title: 'AYT Matematik Deneme 1 Analizi',
            status: 0,
            isCompleted: false,
            createdAt: '2026-10-25T10:00:00Z',
          },
        ]),
      });
    });

    await page.route('**/api/v1/coaching/students/stu-1/notes*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'note-1',
            category: 'Akademik',
            content: 'Öğrenci trigonometri sorularında işlem hatası yapıyor.',
            createdAt: '2026-10-26T14:00:00Z',
          },
        ]),
      });
    });

    await page.route('**/api/v1/exams/students/stu-1/results*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [],
          totalCount: 0,
        }),
      });
    });

    await page.route('**/api/v1/academic/interpretation/academic-summary?*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          summary: 'Öğrencinin matematik performansı istikrarlı ilerlemektedir.',
          keyStrengths: ['Fonksiyonlar'],
          criticalGaps: ['İntegral'],
        }),
      });
    });
  };

  test('1. Renders premium hero, student identity, plan status and all tabs', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/students/stu-1');
    await page.waitForLoadState('networkidle');

    // Header & identity
    await expect(page.getByRole('heading', { name: 'Zeynep Demir' })).toBeVisible();
    await expect(page.getByText('12. Sınıf').first()).toBeVisible();
    await expect(page.getByText('Demo Anadolu Lisesi').first()).toBeVisible();
    await expect(page.getByText('%75 Uyum')).toBeVisible();

    // Actions
    await expect(page.getByRole('button', { name: 'Görev Ata' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Mesaj Gönder' })).toBeVisible();

    // Tabs
    await expect(page.getByRole('button', { name: 'Özet' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Görevler/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Çalışma Planı/ })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Denemeler' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Özel Notlar/ })).toBeVisible();

    // Overview Tab content
    await expect(page.getByText('Aktif Plan Uyumu')).toBeVisible();
    await expect(page.getByText('Tamamlanan Çalışma')).toBeVisible();
    await expect(page.getByText('Özel Koçluk Notları')).toBeVisible();
  });

  test('2. Tab switching works and preserves query params', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/coach/students/stu-1');
    await page.waitForLoadState('networkidle');

    // Switch to Notes
    await page.getByRole('button', { name: /Özel Notlar/ }).click();
    await expect(page).toHaveURL(/tab=notes/);
    await expect(page.getByText('Öğrenci trigonometri sorularında işlem hatası yapıyor.')).toBeVisible();

    // Switch to Tasks via "Görev Ata" button
    await page.getByRole('button', { name: 'Görev Ata' }).click();
    await expect(page).toHaveURL(/tab=tasks/);
    await expect(page.getByText('AYT Matematik Deneme 1 Analizi')).toBeVisible();
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
      await page.goto('/coach/students/stu-1');
      await page.waitForLoadState('networkidle');

      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));

      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/coach-student-detail-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
