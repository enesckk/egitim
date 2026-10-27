import { test, expect } from '@playwright/test';

function createMockJwt(payload: Record<string, unknown>): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + 3600;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('Teacher Messages Redesign Tests', () => {
  const teacherToken = createMockJwt({
    sub: 'teacher-123',
    email: 'teacher.demo@egitim.com',
    name: 'Ayşe Öğretmen',
    role: 'Teacher',
    institutionId: 'inst-1',
    tenantId: 'tenant-1',
  });

  const mockConversations = [
    {
      id: 'conv-1',
      institutionId: 'inst-1',
      otherUserId: 'student-456',
      otherUserName: 'Ali Yılmaz',
      otherUserRole: 'Student',
      lastMessageSnippet: 'Ödevimi tamamladım hocam, teşekkürler.',
      lastMessageAt: '2026-10-27T14:30:00Z',
      unreadCount: 1,
    },
    {
      id: 'conv-2',
      institutionId: 'inst-1',
      otherUserId: 'coach-789',
      otherUserName: 'Mehmet Koç',
      otherUserRole: 'Coach',
      lastMessageSnippet: 'Ali haftalık hedeflerine ulaştı.',
      lastMessageAt: '2026-10-27T12:15:00Z',
      unreadCount: 0,
    },
  ];

  const mockMessagesConv1 = [
    {
      id: 'm-1',
      conversationId: 'conv-1',
      senderUserId: 'teacher-123',
      senderUserName: 'Ayşe Öğretmen',
      senderRole: 'Teacher',
      content: 'Ali merhaba, parabol ödevini sisteme yükledin mi?',
      createdAt: '2026-10-27T14:20:00Z',
      isRead: true,
      readAt: '2026-10-27T14:25:00Z',
    },
    {
      id: 'm-2',
      conversationId: 'conv-1',
      senderUserId: 'student-456',
      senderUserName: 'Ali Yılmaz',
      senderRole: 'Student',
      content: 'Ödevimi tamamladım hocam, teşekkürler.',
      createdAt: '2026-10-27T14:30:00Z',
      isRead: false,
      readAt: null,
    },
  ];

  const setupMocks = async (page: any) => {
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

    await page.route('**/api/v1/communication/messages/conversations', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockConversations),
      });
    });

    await page.route('**/api/v1/communication/messages/conversations/conv-1*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockMessagesConv1),
      });
    });

    await page.route('**/api/v1/communication/messages/conversations/conv-2*', async (route: any) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'm-3',
            conversationId: 'conv-2',
            senderUserId: 'coach-789',
            senderUserName: 'Mehmet Koç',
            senderRole: 'Coach',
            content: 'Ali haftalık hedeflerine ulaştı.',
            createdAt: '2026-10-27T12:15:00Z',
            isRead: true,
            readAt: '2026-10-27T12:20:00Z',
          },
        ]),
      });
    });

    await page.route('**/api/v1/communication/messages/conversations/*/read', async (route: any) => {
      await route.fulfill({ status: 204 });
    });

    await page.route('**/api/v1/communication/messages', async (route: any) => {
      if (route.request().method() === 'POST') {
        const payload = JSON.parse(route.request().postData() || '{}');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'm-new-99',
            conversationId: payload.conversationId || 'conv-1',
            senderUserId: 'teacher-123',
            senderUserName: 'Ayşe Öğretmen',
            senderRole: 'Teacher',
            content: payload.content,
            createdAt: new Date().toISOString(),
            isRead: false,
            readAt: null,
          }),
        });
      } else {
        await route.continue();
      }
    });
  };

  test('1. Renders conversations list, search filter, and thread details on desktop', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/teacher/messages');
    await page.waitForLoadState('networkidle');

    // Header & badge
    await expect(page.getByRole('heading', { name: 'Mesajlar' })).toBeVisible();
    await expect(page.getByText('2 Konuşma')).toBeVisible();

    // Conversation items
    await expect(page.getByText('Ali Yılmaz').first()).toBeVisible();
    await expect(page.getByText('Mehmet Koç').first()).toBeVisible();

    // Search filter
    const searchInput = page.getByPlaceholder('Kişi veya mesaj ara...');
    await expect(searchInput).toBeVisible();
    await searchInput.fill('Mehmet');
    await expect(page.getByText('Mehmet Koç').first()).toBeVisible();
    await expect(page.locator('button').filter({ hasText: 'Ali Yılmaz' })).not.toBeVisible();

    // Clear search
    await searchInput.fill('');
    await expect(page.locator('button').filter({ hasText: 'Ali Yılmaz' })).toBeVisible();

    // Active conversation messages
    await expect(page.getByText('Ali merhaba, parabol ödevini sisteme yükledin mi?')).toBeVisible();
    await expect(page.locator('.whitespace-pre-wrap', { hasText: 'Ödevimi tamamladım hocam, teşekkürler.' })).toBeVisible();
  });

  test('2. Deep link ?studentId= selects matching conversation, sends message via Enter key', async ({ page }) => {
    await setupMocks(page);

    await page.goto('/teacher/messages?studentId=student-456');
    await page.waitForLoadState('networkidle');

    // Should load conv-1
    await expect(page.getByText('Ali Yılmaz').first()).toBeVisible();
    await expect(page.locator('.whitespace-pre-wrap', { hasText: 'Ödevimi tamamladım hocam, teşekkürler.' })).toBeVisible();

    // Compose message using Enter
    const input = page.locator('#message-content-input');
    await input.fill('Tebrikler Ali, yarın derste kontrol edeceğim.');
    await input.press('Enter');

    // Message appears in stream
    await expect(page.locator('.whitespace-pre-wrap', { hasText: 'Tebrikler Ali, yarın derste kontrol edeceğim.' })).toBeVisible();
  });

  test('3. Mobile and Tablet master-detail navigation with Back button', async ({ page }) => {
    await setupMocks(page);

    // Set mobile viewport 390px
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/teacher/messages');
    await page.waitForLoadState('networkidle');

    // Mobile initially shows conversation list
    await expect(page.getByText('Gelen Kutusu')).toBeVisible();
    await expect(page.getByRole('button', { name: /Ali Yılmaz/ })).toBeVisible();

    // Click conversation -> transitions to thread
    await page.getByRole('button', { name: /Ali Yılmaz/ }).click();
    await expect(page.getByRole('button', { name: 'Gelen Kutusuna Dön' })).toBeVisible();
    await expect(page.locator('.whitespace-pre-wrap', { hasText: 'Ödevimi tamamladım hocam, teşekkürler.' })).toBeVisible();

    // Click Back -> returns to list
    await page.getByRole('button', { name: 'Gelen Kutusuna Dön' }).click();
    await expect(page.getByText('Gelen Kutusu')).toBeVisible();
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
      await page.goto('/teacher/messages');
      await page.waitForLoadState('networkidle');

      const overflow = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));

      expect(overflow.scrollWidth).toBeLessThanOrEqual(vp.width);

      await page.screenshot({
        path: `/Users/enesckk/.gemini/antigravity/brain/84949167-d286-48ee-930f-ccf2d27d4f41/screenshots/teacher-messages-redesign-${vp.name}.png`,
        fullPage: true,
      });
    }
  });
});
