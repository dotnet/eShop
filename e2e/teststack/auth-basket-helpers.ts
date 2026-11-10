import http from 'http';
import http2 from 'http2';
import { execFileSync } from 'child_process';
import { DOCKER } from './scenario';

/** The issuer the containers trust; the host does not resolve it, so requests are pinned to 127.0.0.1. */
export const IDENTITY_URL = 'http://host.docker.internal:18081';
export const BASKET_GRPC_URL = 'http://localhost:18083';
export const USER_IDS: Record<string, string> = {
  alice: 'a11ce000-0000-4000-8000-000000000001',
  bob: 'b0b00000-0000-4000-8000-000000000002',
};

export interface HttpResult { status: number; headers: http.IncomingHttpHeaders; body: string }

const pinnedLookup = ((_host: string, options: unknown, cb: unknown) => {
  const callback = (typeof options === 'function' ? options : cb) as (...args: unknown[]) => void;
  const all = typeof options === 'object' && options !== null && (options as { all?: boolean }).all;
  if (all) {
    callback(null, [{ address: '127.0.0.1', family: 4 }]);
  } else {
    callback(null, '127.0.0.1', 4);
  }
}) as unknown as http.RequestOptions['lookup'];

/** Minimal HTTP client with a cookie jar; never follows redirects. */
export class IdentityClient {
  private cookies = new Map<string, string>();

  request(method: string, url: string, form?: Record<string, string>, headers: Record<string, string> = {}): Promise<HttpResult> {
    const body = form ? new URLSearchParams(form).toString() : undefined;
    return new Promise((resolve, reject) => {
      const req = http.request(url, {
        method,
        lookup: pinnedLookup,
        headers: {
          ...(body ? { 'content-type': 'application/x-www-form-urlencoded', 'content-length': Buffer.byteLength(body) } : {}),
          ...(this.cookies.size ? { cookie: [...this.cookies].map(([k, v]) => `${k}=${v}`).join('; ') } : {}),
          ...headers,
        },
      }, res => {
        for (const c of res.headers['set-cookie'] ?? []) {
          const [pair] = c.split(';');
          const i = pair.indexOf('=');
          this.cookies.set(pair.slice(0, i), pair.slice(i + 1));
        }
        const chunks: Buffer[] = [];
        res.on('data', d => chunks.push(d));
        res.on('end', () => resolve({ status: res.statusCode!, headers: res.headers, body: Buffer.concat(chunks).toString('utf8') }));
      });
      req.on('error', reject);
      req.end(body);
    });
  }

  get(url: string, headers?: Record<string, string>) { return this.request('GET', url, undefined, headers); }
  post(url: string, form: Record<string, string>) { return this.request('POST', url, form); }

  /** GET the Identity login page, then post the form. Returns the POST response. */
  async submitLogin(loginPageUrl: string, username: string, password: string, returnUrlOverride?: string): Promise<HttpResult> {
    const page = await this.get(loginPageUrl);
    const token = page.body.match(/name="__RequestVerificationToken" type="hidden" value="([^"]+)"/)?.[1];
    const returnUrl = page.body.match(/name="ReturnUrl" value="([^"]*)"/)?.[1]?.replace(/&amp;/g, '&') ?? '';
    if (!token) {
      throw new Error(`No antiforgery token on ${loginPageUrl} (status ${page.status})`);
    }
    return this.post(`${IDENTITY_URL}/Account/Login`, {
      Username: username, Password: password, ReturnUrl: returnUrlOverride ?? returnUrl, button: 'login', __RequestVerificationToken: token,
    });
  }
}

/** Run the webapp client's authorization-code flow headlessly and return the token response. */
export async function getTokens(username: string, password = 'Pass123$', scope = 'openid profile basket'):
  Promise<{ access_token: string; id_token: string; scope: string }> {
  const client = new IdentityClient();
  const redirectUri = 'http://localhost:18080/signin-oidc';
  const query = new URLSearchParams({ client_id: 'webapp', response_type: 'code', scope, redirect_uri: redirectUri, response_mode: 'query', state: 's', nonce: 'n' });
  const authorize = await client.get(`${IDENTITY_URL}/connect/authorize?${query}`);
  const loginUrl = new URL(authorize.headers.location!, IDENTITY_URL).toString();
  const login = await client.submitLogin(loginUrl, username, password);
  if (login.status !== 302) {
    throw new Error(`Login for ${username} failed with ${login.status}`);
  }
  const callback = await client.get(new URL(login.headers.location!, IDENTITY_URL).toString());
  const code = new URL(callback.headers.location!).searchParams.get('code');
  if (!code) {
    throw new Error(`No authorization code: ${callback.headers.location}`);
  }
  const token = await client.post(`${IDENTITY_URL}/connect/token`, {
    grant_type: 'authorization_code', code, redirect_uri: redirectUri, client_id: 'webapp', client_secret: 'secret',
  });
  if (token.status !== 200) {
    throw new Error(`Token exchange failed: ${token.status} ${token.body}`);
  }
  return JSON.parse(token.body);
}

export interface GrpcResult { grpcStatus: number; grpcMessage?: string; httpStatus: number; items: { productId: number; quantity: number }[] }

/** Encode an UpdateBasketRequest (items = 2; BasketItem.product_id = 2, quantity = 6). */
export function encodeItems(items: { productId: number; quantity: number }[]): Buffer {
  const varint = (n: number) => {
    const out: number[] = [];
    let v = BigInt.asUintN(64, BigInt(n));
    do {
      let b = Number(v & 0x7fn);
      v >>= 7n;
      if (v) b |= 0x80;
      out.push(b);
    } while (v);
    return out;
  };
  const parts = items.map(i => {
    const item = Buffer.from([0x10, ...varint(i.productId), 0x30, ...varint(i.quantity)]);
    return Buffer.from([0x12, ...varint(item.length), ...item]);
  });
  return Buffer.concat(parts);
}

function readVarint(buf: Buffer, pos: number): [number, number] {
  let result = 0n;
  let shift = 0n;
  for (;;) {
    const b = buf[pos++];
    result |= BigInt(b & 0x7f) << shift;
    if (!(b & 0x80)) break;
    shift += 7n;
  }
  return [Number(BigInt.asIntN(32, result)), pos];
}

function decodeItems(msg: Buffer) {
  const items: { productId: number; quantity: number }[] = [];
  let pos = 0;
  while (pos < msg.length) {
    const tag = msg[pos++];
    const [len, p] = readVarint(msg, pos);
    const sub = msg.subarray(p, p + len);
    pos = p + len;
    if (tag !== 0x0a) continue;
    const item = { productId: 0, quantity: 0 };
    let q = 0;
    while (q < sub.length) {
      const t = sub[q++];
      const [v, n] = readVarint(sub, q);
      q = n;
      if (t === 0x10) item.productId = v;
      if (t === 0x30) item.quantity = v;
    }
    items.push(item);
  }
  return items;
}

/** Call a unary Basket gRPC method over h2c with the given protobuf body. */
export function basketCall(method: 'GetBasket' | 'UpdateBasket' | 'DeleteBasket', body: Buffer = Buffer.alloc(0), accessToken?: string): Promise<GrpcResult> {
  return new Promise((resolve, reject) => {
    const session = http2.connect(BASKET_GRPC_URL);
    session.on('error', reject);
    const req = session.request({
      ':method': 'POST',
      ':path': `/BasketApi.Basket/${method}`,
      'content-type': 'application/grpc',
      te: 'trailers',
      ...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
    });
    let headers: http2.IncomingHttpHeaders = {};
    let trailers: http2.IncomingHttpHeaders = {};
    const chunks: Buffer[] = [];
    req.on('response', h => { headers = h; });
    req.on('trailers', t => { trailers = t; });
    req.on('data', d => chunks.push(d));
    req.on('error', reject);
    req.on('end', () => {
      session.close();
      const data = Buffer.concat(chunks);
      const status = trailers['grpc-status'] ?? headers['grpc-status'];
      resolve({
        httpStatus: Number(headers[':status']),
        grpcStatus: status === undefined ? -1 : Number(status),
        grpcMessage: (trailers['grpc-message'] ?? headers['grpc-message']) as string | undefined,
        items: data.length >= 5 ? decodeItems(data.subarray(5, 5 + data.readUInt32BE(1))) : [],
      });
    });
    const frame = Buffer.alloc(5 + body.length);
    frame.writeUInt32BE(body.length, 1);
    body.copy(frame, 5);
    req.end(frame);
  });
}

/** Read a basket straight from Redis (null when the key does not exist). */
export function redisBasket(userId: string): { BuyerId: string; Items: { ProductId: number; Quantity: number }[] } | null {
  const out = execFileSync(DOCKER, ['exec', 'eshop-teststack-redis-1', 'redis-cli', '-a', 'teststack-redis', '--no-auth-warning', 'GET', `/basket/${userId}`], { encoding: 'utf8' }).trim();
  return out ? JSON.parse(out) : null;
}

/** Run a SQL query against identitydb and return the raw psql (-At) output. */
export function identitySql(sql: string): string {
  return execFileSync(DOCKER, ['exec', '-e', 'PGPASSWORD=teststack-postgres', 'eshop-teststack-postgres-1', 'psql', '-U', 'postgres', '-d', 'identitydb', '-At', '-c', sql], { encoding: 'utf8' }).trim();
}
