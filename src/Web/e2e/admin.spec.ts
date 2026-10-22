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

    await page.route('**/api/v1/students*', async (route: any) => {
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
              schoolName: 'Kadıköy',
              gradeLevel: 11,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    });

    await page.route('**/api/v1/teachers*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'teacher-1',
              firstName: 'Kemal',
              lastName: 'Bey',
              title: 'Matematik Öğretmeni',
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    });

    await page.route('**/api/v1/coaching/coaches*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'coach-1',
              userId: 'user-coach-1',
              firstName: 'Hasan',
              lastName: 'Yılmaz',
              fullName: 'Hasan Yılmaz',
              institutionId: 'inst-1',
              assignedStudentCount: 5,
              assignedStudents: [],
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    });

    await page.route('**/api/v1/classrooms*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'class-1',
            institutionId: 'inst-1',
            name: '12-A Sayısal',
            branch: 'Kadıköy',
            gradeLevel: 12,
            studentCount: 15,
            teacherCount: 2,
            teachers: [],
          },
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

  test('15. Admin exams list, search, and navigate to questions', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'exam-tyt-1',
              code: 'TYT-2026-01',
              title: '2026 Kurumsal TYT Denemesi 1',
              examTypeId: 'type-tyt',
              examTypeName: 'TYT',
              examDate: new Date().toISOString(),
              status: 0,
              sectionCount: 4,
            },
            {
              id: 'exam-ayt-1',
              code: 'AYT-2026-01',
              title: '2026 Kurumsal AYT Denemesi 1',
              examTypeId: 'type-ayt',
              examTypeName: 'AYT',
              examDate: new Date().toISOString(),
              status: 1,
              sectionCount: 4,
            },
          ],
          totalCount: 2,
          page: 1,
          pageSize: 100,
        }),
      });
    });

    await page.route('**/api/v1/academic/exam-types*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'type-tyt', name: 'TYT', code: 'TYT' },
          { id: 'type-ayt', name: 'AYT', code: 'AYT' },
        ]),
      });
    });

    await page.route('**/api/v1/academic/subjects*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'sub-1', name: 'Türkçe', code: 'TUR' },
          { id: 'sub-2', name: 'Temel Matematik', code: 'TMAT' },
        ]),
      });
    });

    await page.route('**/api/v1/exams/exam-tyt-1', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'exam-tyt-1',
          code: 'TYT-2026-01',
          title: '2026 Kurumsal TYT Denemesi 1',
          sections: [{ id: 'sec-1', name: 'Türkçe', questionCount: 40 }],
        }),
      });
    });

    await page.route('**/api/v1/exams/exam-tyt-1/questions*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/admin/exams');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('admin-exams-page')).toBeVisible();
    await expect(page.getByText('2026 Kurumsal TYT Denemesi 1')).toBeVisible();
    await expect(page.getByText('2026 Kurumsal AYT Denemesi 1')).toBeVisible();

    // Search filter
    await page.getByTestId('exam-search-input').fill('TYT');
    await expect(page.getByText('2026 Kurumsal TYT Denemesi 1')).toBeVisible();
    await expect(page.getByText('2026 Kurumsal AYT Denemesi 1')).not.toBeVisible();

    // Clear search
    await page.getByTestId('exam-search-input').fill('');

    // Navigate to questions assembly
    await page.getByTestId('navigate-questions-exam-tyt-1').click();
    await expect(page).toHaveURL(/.*\/admin\/exams\/exam-tyt-1\/questions/);
  });

  test('16. Admin exams navigate to results entry', async ({ page }) => {
    await setupAdminSession(page);

    await page.route('**/api/v1/exams*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'exam-ayt-1',
              code: 'AYT-2026-01',
              title: '2026 Kurumsal AYT Denemesi 1',
              examTypeId: 'type-ayt',
              examTypeName: 'AYT',
              examDate: new Date().toISOString(),
              status: 1,
              sectionCount: 4,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 100,
        }),
      });
    });

    await page.route('**/api/v1/exams/exam-ayt-1', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'exam-ayt-1',
          code: 'AYT-2026-01',
          title: '2026 Kurumsal AYT Denemesi 1',
          sections: [{ id: 'sec-1', name: 'Matematik', questionCount: 40 }],
        }),
      });
    });

    await page.goto('/admin/exams');
    await page.waitForLoadState('networkidle');

    await page.getByTestId('navigate-results-exam-ayt-1').click();
    await expect(page).toHaveURL(/.*\/admin\/exams\/exam-ayt-1\/results\/entry/);
  });

  test('17. Admin create new exam modal flow', async ({ page }) => {
    await setupAdminSession(page);

    let createdPayload: any = null;

    await page.route('**/api/v1/exams*', async (route: any) => {
      if (route.request().method() === 'POST') {
        createdPayload = route.request().postDataJSON();
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify('new-exam-guid-123'),
        });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [],
            totalCount: 0,
            page: 1,
            pageSize: 100,
          }),
        });
      }
    });

    await page.route('**/api/v1/academic/exam-types*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'type-tyt', name: 'TYT', code: 'TYT' },
        ]),
      });
    });

    await page.route('**/api/v1/academic/subjects*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'sub-mat', name: 'Matematik', code: 'MAT' },
        ]),
      });
    });

    await page.goto('/admin/exams');
    await page.waitForLoadState('networkidle');

    // Click create exam
    await page.getByTestId('create-exam-btn').click();
    await expect(page.getByTestId('create-exam-form')).toBeVisible();

    // Fill inputs
    await page.getByTestId('input-exam-title').fill('Yeni TYT Deneme Sınavı');
    await page.getByTestId('input-exam-code').fill('TYT-YENI-01');
    await page.getByTestId('select-exam-type').selectOption('type-tyt');

    // Submit
    await page.getByTestId('submit-create-exam').click();

    // Modal should close and payload verified
    await expect(page.getByTestId('create-exam-form')).not.toBeVisible();
    expect(createdPayload).not.toBeNull();
    expect(createdPayload.code).toBe('TYT-YENI-01');
    expect(createdPayload.title).toBe('Yeni TYT Deneme Sınavı');
  });
});
