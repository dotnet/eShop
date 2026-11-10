import { test, expect } from '../fixtures';
import { loadScenario } from './scenario';
import { basketCall, encodeItems, getTokens, redisBasket, USER_IDS } from './auth-basket-helpers';

// Basket.API gRPC surface (h2c on localhost:18083), called directly with and without access tokens.
// Scenario basket-edge: alice has no basket; bob has products 7 x2 and 6 x1.
const OK = 0;
const UNAUTHENTICATED = 16;
const INVALID_ARGUMENT = 3;

test.describe.configure({ mode: 'serial' });

test.describe('basket-edge scenario: Basket gRPC API', () => {
  let aliceToken: string;
  let bobBasket: ReturnType<typeof redisBasket>;

  test.beforeAll(async () => {
    loadScenario('basket-edge');
    bobBasket = redisBasket(USER_IDS.bob);
    aliceToken = (await getTokens('alice')).access_token;
  });

  test.afterEach(() => {
    // Nothing in this file may touch bob's basket.
    expect(redisBasket(USER_IDS.bob)).toEqual(bobBasket);
  });

  test('anonymous GetBasket returns an empty basket, never someone else\'s', async () => {
    const res = await basketCall('GetBasket');
    expect(res.grpcStatus).toBe(OK);
    expect(res.items).toEqual([]);
  });

  test('anonymous UpdateBasket and DeleteBasket are rejected as unauthenticated', async () => {
    const update = await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: 1 }]));
    expect(update.grpcStatus).toBe(UNAUTHENTICATED);
    const del = await basketCall('DeleteBasket');
    expect(del.grpcStatus).toBe(UNAUTHENTICATED);
    expect(redisBasket(USER_IDS.alice)).toBeNull();
  });

  test('a malformed bearer token cannot update a basket', async () => {
    const res = await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: 1 }]), 'not-a-jwt');
    expect(res.grpcStatus).toBe(UNAUTHENTICATED);
  });

  test('a token with a tampered signature cannot update a basket', async () => {
    const [header, payload] = aliceToken.split('.');
    const forged = `${header}.${payload}.${Buffer.from('forged-signature').toString('base64url')}`;
    const res = await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: 1 }]), forged);
    expect(res.grpcStatus).toBe(UNAUTHENTICATED);
    expect(redisBasket(USER_IDS.alice)).toBeNull();
  });

  test('alice\'s token reads, writes and deletes only alice\'s basket', async () => {
    const empty = await basketCall('GetBasket', undefined, aliceToken);
    expect(empty).toMatchObject({ grpcStatus: OK, items: [] });

    const updated = await basketCall('UpdateBasket', encodeItems([{ productId: 1, quantity: 2 }, { productId: 6, quantity: 1 }]), aliceToken);
    expect(updated.grpcStatus).toBe(OK);
    expect(updated.items).toEqual([{ productId: 1, quantity: 2 }, { productId: 6, quantity: 1 }]);
    expect(redisBasket(USER_IDS.alice)).toMatchObject({
      BuyerId: USER_IDS.alice,
      Items: [{ ProductId: 1, Quantity: 2 }, { ProductId: 6, Quantity: 1 }],
    });

    const read = await basketCall('GetBasket', undefined, aliceToken);
    expect(read.items).toEqual(updated.items);

    const del = await basketCall('DeleteBasket', undefined, aliceToken);
    expect(del.grpcStatus).toBe(OK);
    expect(redisBasket(USER_IDS.alice)).toBeNull();
    expect((await basketCall('GetBasket', undefined, aliceToken)).items).toEqual([]);
  });

  test('UpdateBasket replaces the whole basket; an empty update leaves an empty basket', async () => {
    await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: 3 }]), aliceToken);
    const res = await basketCall('UpdateBasket', encodeItems([]), aliceToken);
    expect(res).toMatchObject({ grpcStatus: OK, items: [] });
    expect(redisBasket(USER_IDS.alice)?.Items).toEqual([]);
    await basketCall('DeleteBasket', undefined, aliceToken);
  });

  // BUG: Basket.API's BasketItem declares Quantity < 1 invalid (IValidatableObject), but the gRPC
  // UpdateBasket never validates, so quantities of 0 or -3 are stored in Redis and returned as-is.
  test.fixme('UpdateBasket rejects zero or negative quantities', async () => {
    const res = await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: -3 }, { productId: 6, quantity: 0 }]), aliceToken);
    expect(res.grpcStatus).toBe(INVALID_ARGUMENT);
    expect(redisBasket(USER_IDS.alice)).toBeNull();
  });

  // BUG: the ApiResources in Identity.API declare no scopes, so access tokens carry no "aud" claim, and
  // AddDefaultAuthentication sets ValidateAudience = false. Any token from the Identity server - even one
  // issued without the "basket" scope - is accepted by Basket.API.
  test.fixme('a token issued without the basket scope cannot update the basket', async () => {
    const token = (await getTokens('alice', 'Pass123$', 'openid profile orders')).access_token;
    const res = await basketCall('UpdateBasket', encodeItems([{ productId: 7, quantity: 1 }]), token);
    expect(res.grpcStatus).toBe(UNAUTHENTICATED);
    expect(redisBasket(USER_IDS.alice)).toBeNull();
  });
});
