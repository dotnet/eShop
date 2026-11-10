import { Browser, Page } from '@playwright/test';
import { test, expect } from '../fixtures';
import { loadScenario, login } from './scenario';
import { identitySql } from './auth-basket-helpers';

// Scenario auth-lockout: default users plus dave (locked out until 2099), erin (4 failed attempts),
// gina (lockout expired in 2020) and frank (lockout disabled). Everyone's password is Pass123$.
const HOME_HEADING = 'Ready for a new adventure?';
const INVALID = 'Invalid username or password';

test.describe.configure({ mode: 'serial' });

async function freshPage(browser: Browser): Promise<Page> {
  const context = await browser.newContext();
  return context.newPage();
}

/** Open the Identity login form from the WebApp header and submit it, without asserting the outcome. */
async function attemptLogin(page: Page, userName: string, password: string) {
  await page.goto('/');
  await page.getByLabel('Sign in').click();
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await page.getByPlaceholder('Username').fill(userName);
  await page.getByPlaceholder('Password').fill(password);
  await page.getByRole('button', { name: 'Login' }).click();
}

async function expectRejected(page: Page) {
  await expect(page.getByText(INVALID)).toBeVisible();
  await expect(page).toHaveURL(/host\.docker\.internal:18081\/Account\/Login/);
  await expect(page.getByRole('heading', { name: HOME_HEADING })).toHaveCount(0);
}

async function logOut(page: Page) {
  await page.goto('/');
  await page.getByRole('button', { name: 'Account' }).hover();
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page.getByLabel('Sign in')).toBeVisible();
}

function userRow(userName: string) {
  const [failed, lockoutEnd] = identitySql(
    `SELECT "AccessFailedCount", COALESCE(EXTRACT(EPOCH FROM "LockoutEnd")::bigint, 0) FROM "AspNetUsers" WHERE "UserName" = '${userName}'`,
  ).split('|');
  return { accessFailedCount: Number(failed), lockoutEnd: Number(lockoutEnd) * 1000 };
}

test.describe('auth-lockout scenario: sign-in', () => {
  test.beforeAll(() => {
    loadScenario('auth-lockout');
  });

  test('anonymous header shows Sign in; alice signs in and sees her name in the user menu', async ({ browser }) => {
    const page = await freshPage(browser);
    await page.goto('/');
    await expect(page.getByLabel('Sign in')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Account' })).toHaveCount(0);

    await login(page, 'alice');
    await expect(page.getByLabel('Sign in')).toHaveCount(0);
    await expect(page.locator('.user-menu-name')).toHaveText('alice');
    await page.context().close();
  });

  test('user names are case-insensitive', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'ALICE');
    await expect(page.locator('.user-menu-name')).toHaveText('alice');
    await page.context().close();
  });

  test('wrong password is rejected with a generic message and counted as a failed attempt', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, 'bob', 'wrong-password');
    await expectRejected(page);
    expect(userRow('bob').accessFailedCount).toBe(1);

    // A later successful sign-in resets the counter.
    await page.getByPlaceholder('Username').fill('bob');
    await page.getByPlaceholder('Password').fill('Pass123$');
    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page.getByRole('heading', { name: HOME_HEADING })).toBeVisible();
    expect(userRow('bob').accessFailedCount).toBe(0);
    await page.context().close();
  });

  test('unknown user gets the same generic message (no user enumeration)', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, 'mallory', 'Pass123$');
    await expectRejected(page);
    await page.context().close();
  });

  test('sign-in by e-mail address is not supported', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, 'AliceSmith@email.com', 'Pass123$');
    await expectRejected(page);
    await page.context().close();
  });

  test('empty user name and password show required-field errors', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, '', '');
    await expect(page.locator('[data-valmsg-for="Username"]')).toHaveText('The Username field is required.');
    await expect(page.locator('[data-valmsg-for="Password"]')).toHaveText('The Password field is required.');
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
    await page.context().close();
  });

  test('a locked-out user cannot sign in even with the correct password', async ({ browser }) => {
    const before = userRow('dave');
    const page = await freshPage(browser);
    await attemptLogin(page, 'dave', 'Pass123$');
    await expectRejected(page);
    const after = userRow('dave');
    expect(after.lockoutEnd).toBe(before.lockoutEnd);
    expect(after.lockoutEnd).toBeGreaterThan(Date.now());
    await page.context().close();
  });

  test('the fifth failed attempt locks the account; the correct password is then rejected', async ({ browser }) => {
    expect(userRow('erin')).toEqual({ accessFailedCount: 4, lockoutEnd: 0 });
    const page = await freshPage(browser);
    await attemptLogin(page, 'erin', 'wrong-password');
    await expectRejected(page);

    const locked = userRow('erin');
    expect(locked.lockoutEnd).toBeGreaterThan(Date.now());
    expect(locked.accessFailedCount).toBe(0);

    await page.getByPlaceholder('Username').fill('erin');
    await page.getByPlaceholder('Password').fill('Pass123$');
    await page.getByRole('button', { name: 'Login' }).click();
    await expectRejected(page);
    await page.context().close();
  });

  test('a user whose lockout has expired can sign in', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'gina');
    await expect(page.locator('.user-menu-name')).toHaveText('gina');
    await page.context().close();
  });

  test('a user with lockout disabled is never locked out by failed attempts', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, 'frank', 'wrong-password');
    await expectRejected(page);
    for (let i = 0; i < 5; i++) {
      await page.getByPlaceholder('Username').fill('frank');
      await page.getByPlaceholder('Password').fill('wrong-password');
      await page.getByRole('button', { name: 'Login' }).click();
      await expectRejected(page);
    }
    // Identity still records failures (and may even stamp LockoutEnd), but LockoutEnabled=false wins at sign-in.

    await page.getByPlaceholder('Username').fill('frank');
    await page.getByPlaceholder('Password').fill('Pass123$');
    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page.getByRole('heading', { name: HOME_HEADING })).toBeVisible();
    await page.context().close();
  });
});

test.describe('auth-lockout scenario: sessions, protected pages and returnUrl', () => {
  test.beforeAll(() => {
    loadScenario('auth-lockout');
  });

  for (const path of ['/cart', '/checkout', '/user/orders']) {
    test(`anonymous ${path} redirects to the Identity login page`, async ({ browser }) => {
      const page = await freshPage(browser);
      await page.goto(path);
      await expect(page).toHaveURL(/^http:\/\/host\.docker\.internal:18081\/Account\/Login\?ReturnUrl=/);
      await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
      await page.context().close();
    });
  }

  test('after signing in from a protected page the user lands back on it', async ({ browser }) => {
    const page = await freshPage(browser);
    await page.goto('/user/orders');
    await page.getByPlaceholder('Username').fill('alice');
    await page.getByPlaceholder('Password').fill('Pass123$');
    await page.getByRole('button', { name: 'Login' }).click();
    await expect(page).toHaveURL('http://localhost:18080/user/orders');
    await expect(page.getByRole('heading', { name: 'Orders', exact: true })).toBeVisible();

    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
    await page.context().close();
  });

  test('"Log in to purchase" on an item page returns to that item after sign-in without adding it', async ({ browser }) => {
    const page = await freshPage(browser);
    await page.goto('/item/7');
    await expect(page.getByRole('button', { name: 'Add to shopping bag' })).toHaveCount(0);
    await page.getByRole('button', { name: 'Log in to purchase' }).click();
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
    await page.getByPlaceholder('Username').fill('bob');
    await page.getByPlaceholder('Password').fill('Pass123$');
    await page.getByRole('button', { name: 'Login' }).click();

    await expect(page).toHaveURL('http://localhost:18080/item/7');
    await expect(page.getByRole('button', { name: 'Add to shopping bag' })).toBeVisible();
    await expect(page.locator('.in-cart')).toHaveCount(0);
    await page.context().close();
  });

  test('a failed login does not create a WebApp session', async ({ browser }) => {
    const page = await freshPage(browser);
    await attemptLogin(page, 'alice', 'nope');
    await expectRejected(page);
    await page.goto('/');
    await expect(page.getByLabel('Sign in')).toBeVisible();
    await page.context().close();
  });

  test('the session survives reloads and new tabs in the same browser context', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    await page.reload();
    await expect(page.locator('.user-menu-name')).toHaveText('alice');

    const second = await page.context().newPage();
    await second.goto('/cart');
    await expect(second.getByRole('heading', { name: 'Shopping bag', exact: true })).toBeVisible();
    await page.context().close();
  });

  test('separate browser contexts keep separate sessions', async ({ browser }) => {
    const alice = await freshPage(browser);
    await login(alice, 'alice');
    const anonymous = await freshPage(browser);
    await anonymous.goto('/');
    await expect(anonymous.getByLabel('Sign in')).toBeVisible();
    await alice.context().close();
    await anonymous.context().close();
  });

  test('"Remember me" makes the Identity session cookie persistent; without it the cookie is session-only', async ({ browser }) => {
    const sessionCookie = async (page: Page) =>
      (await page.context().cookies('http://host.docker.internal:18081')).find(c => c.name === '.AspNetCore.Identity.Application');

    const plain = await freshPage(browser);
    await login(plain, 'alice');
    expect((await sessionCookie(plain))?.expires).toBe(-1);
    await plain.context().close();

    const remembered = await freshPage(browser);
    await remembered.goto('/');
    await remembered.getByLabel('Sign in').click();
    await remembered.getByPlaceholder('Username').fill('alice');
    await remembered.getByPlaceholder('Password').fill('Pass123$');
    await remembered.getByLabel('Remember me').check();
    await remembered.getByRole('button', { name: 'Login' }).click();
    await expect(remembered.getByRole('heading', { name: HOME_HEADING })).toBeVisible();
    const expires = (await sessionCookie(remembered))!.expires * 1000;
    // ASP.NET Identity's default 14-day cookie lifetime applies (AccountOptions.RememberMeLoginDuration is unused).
    expect(expires).toBeGreaterThan(Date.now() + 13 * 24 * 3600 * 1000);
    await remembered.context().close();
  });

  test('log out ends both the WebApp and the Identity session', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    await logOut(page);
    await expect(page.getByRole('button', { name: 'Account' })).toHaveCount(0);

    // Without an Identity session, protected pages ask for credentials again instead of silently signing in.
    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
    await expect(page.getByPlaceholder('Username')).toBeVisible();

    await page.goto('/');
    await expect(page.getByLabel('Sign in')).toBeVisible();
    await page.context().close();
  });

  test('a different user can sign in after log out in the same browser', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    await logOut(page);
    await login(page, 'bob');
    await expect(page.locator('.user-menu-name')).toHaveText('bob');
    await page.context().close();
  });

  test('a relative returnUrl on /user/login is honoured', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    await page.goto('/user/login?returnUrl=item%2F7');
    await expect(page).toHaveURL('http://localhost:18080/item/7');
    await page.context().close();
  });

  test('an absolute returnUrl on /user/login falls back to the home page', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    const response = await page.context().request.get('/user/login?returnUrl=https%3A%2F%2Fevil.example%2F', { maxRedirects: 0 });
    expect(response.status()).toBe(302);
    expect(response.headers()['location']).toBe('http://localhost:18080/');
    await page.context().close();
  });

  // BUG: LogIn.razor only rejects URIs that parse as absolute. "//evil.example" is a protocol-relative
  // URL, so Nav.NavigateTo sends a 302 to http://evil.example/ - an open redirect after sign-in.
  test.fixme('a protocol-relative returnUrl on /user/login must not redirect off-site', async ({ browser }) => {
    const page = await freshPage(browser);
    await login(page, 'alice');
    const response = await page.context().request.get('/user/login?returnUrl=%2F%2Fevil.example', { maxRedirects: 0 });
    expect(response.status()).toBe(302);
    expect(new URL(response.headers()['location'], 'http://localhost:18080').host).toBe('localhost:18080');
    await page.context().close();
  });
});
