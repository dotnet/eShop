import { execFileSync } from 'child_process';
import path from 'path';
import { expect, Page } from '@playwright/test';

/** Container CLI, as in testing/teststack (for example TESTSTACK_DOCKER=podman). */
export const DOCKER = process.env.TESTSTACK_DOCKER ?? 'docker';

const teststack = path.join(__dirname, '..', '..', 'testing', 'teststack');

/** Put the whole stack into the given scenario (testing/scenarios/<name>) and return the loader's JSON result. */
export function loadScenario(name: string): { ok: boolean; elapsedMs: number; layers: string[] } {
  // testing/teststack is a bash script; Windows cannot execute it directly.
  const command = process.platform === 'win32' ? 'bash' : teststack;
  const args = process.platform === 'win32' ? [teststack, 'load', name] : ['load', name];
  const stdout = execFileSync(command, args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'] });
  const result = JSON.parse(stdout.trim().split('\n').pop()!);
  if (!result.ok) {
    throw new Error(`teststack load ${name} failed: ${result.error}`);
  }
  return result;
}

/** Sign in through the Identity login page and wait until the WebApp shows the user menu. */
export async function login(page: Page, userName: string, password = 'Pass123$') {
  await page.goto('/');
  await page.getByLabel('Sign in').click();
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await page.getByPlaceholder('Username').fill(userName);
  await page.getByPlaceholder('Password').fill(password);
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
}
