import { test, expect } from '@playwright/test';
import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';

test.describe('P1-01: Mandatory Real Vite Production Bundle Environment Tests', () => {
  const webRoot = process.cwd();

  test('CASE A: Missing VITE_API_BASE_URL in production build fails closed with zero localhost fallback', () => {
    // 1. Build production bundle without VITE_API_BASE_URL
    execSync('npx vite build', {
      cwd: webRoot,
      env: { ...process.env, VITE_API_BASE_URL: '' },
      stdio: 'pipe',
    });

    const distAssets = path.join(webRoot, 'dist', 'assets');
    const jsFiles = fs.readdirSync(distAssets).filter((f) => f.endsWith('.js'));
    let mainBundleContent = '';

    for (const file of jsFiles) {
      const content = fs.readFileSync(path.join(distAssets, file), 'utf8');
      if (content.includes('CRITICAL CONFIGURATION ERROR') || content.includes('apiBaseUrl')) {
        mainBundleContent += content;
      }
    }

    // Must contain fail-closed critical error message in production bundle
    expect(mainBundleContent).toContain('CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL environment variable is required in production');

    // Must NOT have fallen back to localhost:5000 in production configuration
    expect(mainBundleContent).not.toMatch(/apiBaseUrl:\s*["']http:\/\/localhost:5000["']/);
  });

  test('CASE B: Valid VITE_API_BASE_URL in production build is statically replaced and used', () => {
    const customApiUrl = 'https://api.egitim-platformu.com/api';

    // 1. Build production bundle with explicit VITE_API_BASE_URL
    execSync('npx vite build', {
      cwd: webRoot,
      env: { ...process.env, VITE_API_BASE_URL: customApiUrl },
      stdio: 'pipe',
    });

    const distAssets = path.join(webRoot, 'dist', 'assets');
    const jsFiles = fs.readdirSync(distAssets).filter((f) => f.endsWith('.js'));
    let mainBundleContent = '';

    for (const file of jsFiles) {
      const content = fs.readFileSync(path.join(distAssets, file), 'utf8');
      if (content.includes('api.egitim-platformu.com')) {
        mainBundleContent += content;
      }
    }

    // Must statically inline the configured production API URL
    expect(mainBundleContent).toContain(customApiUrl);
  });
});
