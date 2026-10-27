import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Academic Tracking Real Data & Security Tests', () => {
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

    await page.route('**/api/v1/classrooms*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'cls-1',
            name: '12-A',
            branch: 'Sayısal',
            gradeLevel: 12,
            studentCount: 2,
          },
        ]),
      });
    });

    await page.route('**/api/v1/classrooms/cls-1', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'cls-1',
          name: '12-A',
          branch: 'Sayısal',
          gradeLevel: 12,
          studentCount: 2,
          students: [
            { studentId: 'st-1', fullName: 'Ahmet Yılmaz' },
          ],
        }),
      });
    });

    await page.route('**/api/v1/teachers/dashboard', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          teacherName: 'Ayşe Öğretmen',
          assignedSubjects: ['Matematik'],
          totalAssignedStudents: 2,
          totalAssignedClasses: 1,
          academicGaps: [
            {
              id: 'gap-1',
              studentId: 'st-1',
              studentName: 'Ahmet Yılmaz',
              grade: '12. Sınıf',
              subject: 'Matematik',
              topic: 'Trigonometri',
              detail: 'Denemelerde toplam 10 yanlış tespit edildi',
              severity: 'critical',
              initials: 'AY',
              lastExam: '04 Eki 2026',
            },
          ],
          studentNets: [],
        }),
      });
    });

    await page.route('**/api/v1/students/st-1/academic-intelligence/classifications/topics*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            topicId: 'top-1',
            topicName: 'Temel Kavramlar',
            subjectId: 'sub-1',
            subjectName: 'Matematik',
            state: 'Strong',
            accuracy: 1.0,
            totalQuestions: 10,
            totalCorrect: 10,
            totalWrong: 0,
            totalBlank: 0,
          },
        ]),
      });
    });
  };

  test('1. Academic Tracking loads real assigned subjects, gaps, and topic analysis', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/academic');
    await page.waitForLoadState('networkidle');

    // Header & Scope
    await expect(page.getByRole('heading', { name: 'Akademik Takip & Kazanım Analizi' })).toBeVisible();
    await expect(page.getByText('Matematik').first()).toBeVisible();
    await expect(page.getByText('Yetkili Şube')).toBeVisible();
    await expect(page.getByText('Kayıtlı Öğrenci')).toBeVisible();

    // Critical Gaps Section
    await expect(page.getByRole('heading', { name: 'Öncelikli Konu Açıkları' })).toBeVisible();
    await expect(page.getByText('1 Kritik / 1 Toplam Açık')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();
    await expect(page.getByText('Trigonometri')).toBeVisible();

    // Topic Performance Section
    await expect(page.getByRole('heading', { name: 'Soru Bazlı Konu & Kazanım İlerlemesi' })).toBeVisible();
    await expect(page.getByText('Temel Kavramlar')).toBeVisible();
    await expect(page.getByText('%100')).toBeVisible();

    // Quick Action button navigates
    await page.getByRole('button', { name: /Sınıflarım/i }).click();
    await expect(page).toHaveURL(/\/teacher\/classes/);
  });

  test('2. Filters for subject, classroom and student search filter gaps and topic performance consistently', async ({ page }) => {
    await setupBaseMocks(page);

    await page.goto('/teacher/academic');
    await page.waitForLoadState('networkidle');

    // Initial state: Ahmet Yılmaz with Trigonometri and Temel Kavramlar visible
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();
    await expect(page.getByText('Temel Kavramlar')).toBeVisible();

    // 1. Filter by search query "Fizik" -> should show empty states
    const searchInput = page.getByPlaceholder('Öğrenci adı veya konu ara...');
    await searchInput.fill('Fizik');
    await expect(page.getByText('Kritik Akademik Açık Yok')).toBeVisible();
    await expect(page.getByText('Bu kapsamda kayıtlı konu kazanım verisi bulunmuyor.')).toBeVisible();

    // Clear search
    await searchInput.fill('');
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();

    // 2. Filter by search query "Ahmet" -> shows Ahmet
    await searchInput.fill('Ahmet');
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();

    // 3. Filter by search query "Zeynep" -> Ahmet disappears, empty gap state shown
    await searchInput.fill('Zeynep');
    await expect(page.getByText('Kritik Akademik Açık Yok')).toBeVisible();

    await searchInput.fill('');
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();

    // 4. Select assigned subject filter
    const subjectSelect = page.locator('select').nth(1);
    await subjectSelect.selectOption({ label: 'Matematik' });
    await expect(page.getByRole('heading', { name: 'Ahmet Yılmaz' })).toBeVisible();
    await expect(page.getByText('Trigonometri')).toBeVisible();
  });
});
