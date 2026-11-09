import { test, expect } from '@playwright/test';
import { loadScenario } from './scenario';
import { IdentityClient, IDENTITY_URL, getTokens, USER_IDS } from './auth-basket-helpers';

// Identity.API's OIDC/OAuth surface and its login form, exercised over plain HTTP (no browser).
const WEBAPP_REDIRECT = 'http://localhost:18080/signin-oidc';

test.describe.configure({ mode: 'serial' });

function claims(jwt: string) {
  return JSON.parse(Buffer.from(jwt.split('.')[1], 'base64url').toString('utf8'));
}

test.describe('default scenario: Identity.API endpoints', () => {
  test.beforeAll(() => {
    loadScenario('default');
  });

  test('discovery document advertises the issuer, endpoints and scopes the apps rely on', async () => {
    const res = await new IdentityClient().get(`${IDENTITY_URL}/.well-known/openid-configuration`);
    expect(res.status).toBe(200);
    const doc = JSON.parse(res.body);
    expect(doc.issuer).toBe(IDENTITY_URL);
    expect(doc.authorization_endpoint).toBe(`${IDENTITY_URL}/connect/authorize`);
    expect(doc.token_endpoint).toBe(`${IDENTITY_URL}/connect/token`);
    expect(doc.end_session_endpoint).toBe(`${IDENTITY_URL}/connect/endsession`);
    expect(doc.scopes_supported).toEqual(expect.arrayContaining(['openid', 'profile', 'basket', 'orders', 'webhooks', 'offline_access']));
    expect(doc.code_challenge_methods_supported).toContain('S256');
  });

  test('JWKS exposes an RSA signing key', async () => {
    const res = await new IdentityClient().get(`${IDENTITY_URL}/.well-known/openid-configuration/jwks`);
    expect(res.status).toBe(200);
    const { keys } = JSON.parse(res.body);
    expect(keys.length).toBeGreaterThan(0);
    expect(keys[0]).toMatchObject({ kty: 'RSA', use: 'sig' });
  });

  test('authorization-code flow for the webapp client issues tokens for the signed-in user', async () => {
    const tokens = await getTokens('alice', 'Pass123$', 'openid profile basket orders');
    const access = claims(tokens.access_token);
    expect(access.iss).toBe(IDENTITY_URL);
    expect(access.sub).toBe(USER_IDS.alice);
    expect(access.client_id).toBe('webapp');
    expect(access.scope).toEqual(expect.arrayContaining(['basket', 'orders']));
    // The ApiResources declare no scopes, so no "aud" claim is issued (the APIs skip audience validation).
    expect(access.aud).toBeUndefined();
    expect(claims(tokens.id_token).sub).toBe(USER_IDS.alice);

    const userinfo = await new IdentityClient().get(`${IDENTITY_URL}/connect/userinfo`, { authorization: `Bearer ${tokens.access_token}` });
    expect(userinfo.status).toBe(200);
    expect(JSON.parse(userinfo.body)).toMatchObject({ sub: USER_IDS.alice, preferred_username: 'alice' });
  });

  test('token endpoint rejects an unknown client', async () => {
    const res = await new IdentityClient().post(`${IDENTITY_URL}/connect/token`, { grant_type: 'client_credentials', client_id: 'nope', client_secret: 'x' });
    expect(res.status).toBe(400);
    expect(JSON.parse(res.body).error).toBe('invalid_client');
  });

  test('token endpoint rejects a wrong client secret', async () => {
    const res = await new IdentityClient().post(`${IDENTITY_URL}/connect/token`, { grant_type: 'client_credentials', client_id: 'webapp', client_secret: 'wrong' });
    expect(res.status).toBe(400);
    expect(JSON.parse(res.body).error).toBe('invalid_client');
  });

  test('the webapp client may not use the password or client_credentials grants', async () => {
    for (const form of [
      { grant_type: 'password', username: 'alice', password: 'Pass123$' },
      { grant_type: 'client_credentials' },
    ]) {
      const res = await new IdentityClient().post(`${IDENTITY_URL}/connect/token`, { ...form, client_id: 'webapp', client_secret: 'secret' });
      expect(res.status).toBe(400);
      expect(JSON.parse(res.body).error).toBe('unauthorized_client');
    }
  });

  test('an unknown authorization code is rejected', async () => {
    const res = await new IdentityClient().post(`${IDENTITY_URL}/connect/token`, {
      grant_type: 'authorization_code', code: 'bogus', redirect_uri: WEBAPP_REDIRECT, client_id: 'webapp', client_secret: 'secret',
    });
    expect(res.status).toBe(400);
    expect(JSON.parse(res.body).error).toBe('invalid_grant');
  });

  test('userinfo requires a valid bearer token', async () => {
    const anonymous = await new IdentityClient().get(`${IDENTITY_URL}/connect/userinfo`);
    expect(anonymous.status).toBe(401);
    const garbage = await new IdentityClient().get(`${IDENTITY_URL}/connect/userinfo`, { authorization: 'Bearer garbage' });
    expect(garbage.status).toBe(401);
    expect(garbage.headers['www-authenticate']).toContain('invalid_token');
  });

  for (const [name, clientId, redirectUri] of [
    ['an unknown client', 'nope', WEBAPP_REDIRECT],
    ['an unregistered redirect_uri', 'webapp', 'http://evil.example/cb'],
  ]) {
    test(`authorize with ${name} shows the Identity error page instead of redirecting`, async () => {
      const query = new URLSearchParams({ client_id: clientId, response_type: 'code', scope: 'openid', redirect_uri: redirectUri });
      const res = await new IdentityClient().get(`${IDENTITY_URL}/connect/authorize?${query}`);
      expect(res.status).toBe(302);
      expect(res.headers.location).toMatch(/\/home\/error\?errorId=/);
      expect(res.headers.location).not.toContain('evil.example');
    });
  }

  test('Identity.API has no self-service registration page', async () => {
    const res = await new IdentityClient().get(`${IDENTITY_URL}/Account/Register`);
    expect(res.status).toBe(404);
  });

  test('direct login without an OIDC request redirects to the Identity home page', async () => {
    const client = new IdentityClient();
    const res = await client.submitLogin(`${IDENTITY_URL}/Account/Login`, 'alice', 'Pass123$');
    expect(res.status).toBe(302);
    expect(res.headers.location).toBe('/');
    expect(res.headers['set-cookie']?.some(c => c.startsWith('.AspNetCore.Identity.Application='))).toBe(true);
  });

  test('login form posts without an antiforgery token are rejected', async () => {
    const res = await new IdentityClient().post(`${IDENTITY_URL}/Account/Login`, { Username: 'alice', Password: 'Pass123$', button: 'login' });
    expect(res.status).toBe(400);
  });

  test('a tampered off-site ReturnUrl on the login form is never followed', async () => {
    const res = await new IdentityClient().submitLogin(`${IDENTITY_URL}/Account/Login`, 'alice', 'Pass123$', 'https://evil.example/');
    // The quickstart controller throws "invalid return URL" (500) rather than redirecting.
    expect(res.headers.location ?? '').not.toContain('evil.example');
    expect(res.status).not.toBe(302);
  });

  test('a wrong password over HTTP re-renders the form with the generic error and sets no auth cookie', async () => {
    const res = await new IdentityClient().submitLogin(`${IDENTITY_URL}/Account/Login`, 'alice', 'wrong');
    expect(res.status).toBe(200);
    expect(res.body).toContain('Invalid username or password');
    expect(res.headers['set-cookie']?.some(c => c.startsWith('.AspNetCore.Identity.Application=')) ?? false).toBe(false);
  });
});
