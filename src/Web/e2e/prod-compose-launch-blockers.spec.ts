import { test, expect, request as playwrightRequest } from '@playwright/test';
import { execFileSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '../../..');

const COMPOSE_PROJECT = process.env.PROD_TEST_COMPOSE_PROJECT || 'egitim-prod-blocker-test';
const GATEWAY_URL = process.env.PROD_TEST_GATEWAY_URL || 'https://localhost:18443';
const SUPERADMIN_EMAIL = process.env.PROD_TEST_SUPERADMIN_EMAIL || 'superadmin@bilimakademi.test';
const SUPERADMIN_PASSWORD = process.env.PROD_TEST_SUPERADMIN_PASSWORD || 'ProdBlocker_SuperAdmin_2026!';
const INSTITUTION_ID = process.env.PROD_TEST_INSTITUTION_ID || '11111111-2222-3333-4444-555555555555';

function runCompose(args: string[]): string {
  return execFileSync(
    'docker',
    ['compose', '-p', COMPOSE_PROJECT, '-f', 'docker-compose.prod.yml', ...args],
    {
      cwd: repoRoot,
      encoding: 'utf8',
      env: {
        ...process.env,
        MSSQL_SA_PASSWORD: process.env.MSSQL_SA_PASSWORD || 'ProdBlocker_Mssql_2026!',
        JWT_SIGNING_KEY:
          process.env.JWT_SIGNING_KEY || 'ProdBlocker_JwtSigningKey_Min32Chars_2026_Secure!',
        CORS_ALLOWED_ORIGIN: GATEWAY_URL,
        GATEWAY_HTTP_PORT: '18080',
        GATEWAY_HTTPS_PORT: '18443',
      },
    }
  ).trim();
}

function waitForServiceHealthy(service: string, timeoutMs = 60_000): void {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const containerId = runCompose(['ps', '-q', service]);
    if (containerId) {
      const status = execFileSync(
        'docker',
        ['inspect', '--format', '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}', containerId],
        { encoding: 'utf8' }
      ).trim();
      if (status === 'healthy') {
        return;
      }
    }
    execFileSync('sleep', ['2']);
  }
  throw new Error(`Timed out waiting for service '${service}' to become healthy.`);
}

test.use({
  ignoreHTTPSErrors: true,
  launchOptions: {
    args: ['--ignore-certificate-errors'],
  },
});

test.describe('Production Launch Blockers Regression Suite (Disposable Compose Stack)', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeAll(() => {
    const mssqlPassword = process.env.MSSQL_SA_PASSWORD || 'ProdBlocker_Mssql_2026!';
    runCompose([
      'exec',
      '-T',
      'mssql',
      '/opt/mssql-tools18/bin/sqlcmd',
      '-S',
      'localhost',
      '-U',
      'sa',
      '-P',
      mssqlPassword,
      '-C',
      '-I',
      '-d',
      'EgitimPlatform',
      '-b',
      '-Q',
      `SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM Institutions WHERE Id = '${INSTITUTION_ID}')
BEGIN
    INSERT INTO Institutions (Id, Name, IsActive, IsDeleted, CreatedAt)
    VALUES ('${INSTITUTION_ID}', N'Bilim Akademi Test Kurumu', 1, 0, SYSDATETIMEOFFSET());
END`,
    ]);
  });

  test('1. Default frontend production build renders over Caddy HTTPS without startup exceptions', async ({
    browser,
  }) => {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();

    const pageErrors: string[] = [];
    const criticalConsoleErrors: string[] = [];

    page.on('pageerror', (err) => {
      pageErrors.push(err.message);
    });
    page.on('console', (msg) => {
      if (msg.type() === 'error' && msg.text().includes('CRITICAL CONFIGURATION ERROR')) {
        criticalConsoleErrors.push(msg.text());
      }
    });

    const response = await page.goto(`${GATEWAY_URL}/login`, { waitUntil: 'networkidle' });
    expect(response?.status()).toBe(200);

    await expect(page.locator('input[type="email"]')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('input[type="password"]')).toBeVisible();

    expect(pageErrors).toEqual([]);
    expect(criticalConsoleErrors).toEqual([]);

    await context.close();
  });

  test('2. Non-root API container mounts writable /app/Storage volume and persists uploaded PDF across restart and recreation', async () => {
    test.setTimeout(120_000);

    // 1. Verify non-root execution and /app/Storage ownership inside the running API container
    const uidOutput = runCompose(['exec', '-T', 'api', 'id', '-u']);
    expect(uidOutput).not.toBe('0');

    const statOutput = runCompose([
      'exec',
      '-T',
      'api',
      'stat',
      '-c',
      '%u:%g %a %n',
      '/app/Storage',
      '/app/Storage/Materials',
    ]);
    expect(statOutput).toContain(`${uidOutput}:${uidOutput} 750 /app/Storage`);
    expect(statOutput).toContain(`${uidOutput}:${uidOutput} 750 /app/Storage/Materials`);

    const apiContext = await playwrightRequest.newContext({
      baseURL: GATEWAY_URL,
      ignoreHTTPSErrors: true,
      extraHTTPHeaders: {
        Origin: GATEWAY_URL,
      },
    });

    // 2. Verify unauthenticated upload is rejected (401)
    const unauthRes = await apiContext.post('/api/content/materials', {
      multipart: {
        Title: 'Unauth PDF',
        Subject: 'Matematik',
        File: {
          name: 'unauth.pdf',
          mimeType: 'application/pdf',
          buffer: Buffer.from('%PDF-1.4 unauthorized test'),
        },
      },
    });
    expect(unauthRes.status()).toBe(401);

    // 3. Authenticate as bootstrapped SuperAdmin and provision a Teacher in the test institution
    const superAdminLoginRes = await apiContext.post('/api/v1/auth/login', {
      data: {
        email: SUPERADMIN_EMAIL,
        password: SUPERADMIN_PASSWORD,
      },
    });
    expect(superAdminLoginRes.status()).toBe(200);
    const { accessToken: superAdminToken } = (await superAdminLoginRes.json()) as {
      accessToken: string;
    };

    const teacherEmail = `teacher.${Date.now()}@bilimakademi.test`;
    const teacherPassword = 'Teacher_ProdTest_2026!';

    const createTeacherRes = await apiContext.post('/api/v1/teachers', {
      headers: {
        Authorization: `Bearer ${superAdminToken}`,
      },
      data: {
        institutionId: INSTITUTION_ID,
        firstName: 'Ayse',
        lastName: 'Ogretmen',
        title: 'Matematik Ogretmeni',
        email: teacherEmail,
        password: teacherPassword,
      },
    });
    expect(createTeacherRes.status()).toBe(201);

    // 4. Authenticate as Teacher
    const teacherLoginRes = await apiContext.post('/api/v1/auth/login', {
      data: {
        email: teacherEmail,
        password: teacherPassword,
      },
    });
    expect(teacherLoginRes.status()).toBe(200);
    const { accessToken: teacherToken } = (await teacherLoginRes.json()) as {
      accessToken: string;
    };

    // 5. Verify magic-byte validation rejects a spoofed PDF file
    const spoofedRes = await apiContext.post('/api/content/materials', {
      headers: {
        Authorization: `Bearer ${teacherToken}`,
      },
      multipart: {
        Title: 'Spoofed PDF',
        Subject: 'Matematik',
        MaterialType: 'fasikul',
        File: {
          name: 'spoofed.pdf',
          mimeType: 'application/pdf',
          buffer: Buffer.from('MZ-not-a-valid-pdf-header'),
        },
      },
    });
    expect(spoofedRes.status()).toBeGreaterThanOrEqual(400);

    // 6. Upload a valid PDF material through the non-root API container
    const pdfPayload = Buffer.from(
      `%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Marker (PersistentStorageTest_${Date.now()}) >>\nendobj\n%%EOF\n`,
      'utf8'
    );

    const uploadRes = await apiContext.post('/api/content/materials', {
      headers: {
        Authorization: `Bearer ${teacherToken}`,
      },
      multipart: {
        Title: 'Turev Uygulama Fasikulu',
        Subject: 'Matematik',
        Topic: 'Turev',
        Grade: '12. Sinif',
        MaterialType: 'fasikul',
        Description: 'Production persistent storage regression test',
        PageCount: '16',
        QuestionCount: '40',
        DurationMinutes: '50',
        File: {
          name: 'turev-uygulama.pdf',
          mimeType: 'application/pdf',
          buffer: pdfPayload,
        },
      },
    });
    expect(uploadRes.status()).toBe(200);
    const createdMaterial = (await uploadRes.json()) as { id: string; title: string };
    expect(createdMaterial.id).toBeTruthy();
    expect(createdMaterial.title).toBe('Turev Uygulama Fasikulu');

    // 7. Immediate download verification before restart
    const downloadBeforeRestart = await apiContext.get(
      `/api/content/materials/${createdMaterial.id}/download`,
      {
        headers: {
          Authorization: `Bearer ${teacherToken}`,
        },
      }
    );
    expect(downloadBeforeRestart.status()).toBe(200);
    expect(Buffer.from(await downloadBeforeRestart.body()).equals(pdfPayload)).toBe(true);

    // 8. Restart API container and verify uploaded file survives restart
    runCompose(['restart', 'api']);
    waitForServiceHealthy('api');

    const downloadAfterRestart = await apiContext.get(
      `/api/content/materials/${createdMaterial.id}/download`,
      {
        headers: {
          Authorization: `Bearer ${teacherToken}`,
        },
      }
    );
    expect(downloadAfterRestart.status()).toBe(200);
    expect(Buffer.from(await downloadAfterRestart.body()).equals(pdfPayload)).toBe(true);

    // 9. Force-recreate API container and verify uploaded file survives container replacement
    runCompose(['up', '-d', '--force-recreate', '--no-deps', 'api']);
    waitForServiceHealthy('api');

    const downloadAfterRecreate = await apiContext.get(
      `/api/content/materials/${createdMaterial.id}/download`,
      {
        headers: {
          Authorization: `Bearer ${teacherToken}`,
        },
      }
    );
    expect(downloadAfterRecreate.status()).toBe(200);
    expect(Buffer.from(await downloadAfterRecreate.body()).equals(pdfPayload)).toBe(true);

    await apiContext.dispose();
  });
});
