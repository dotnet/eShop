import { UserManager, WebStorageStateStore, type User, type UserProfile } from 'oidc-client-ts';

const identityUrl = import.meta.env.VITE_IDENTITY_URL || 'https://localhost:5243';

export const oidcConfig = {
  authority: identityUrl,
  client_id: 'admin-ui',
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  response_type: 'code',
  scope: 'openid profile roles warehouse orders offline_access',
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  automaticSilentRenew: true,
  silent_redirect_uri: `${window.location.origin}/silent-renew.html`,
};

export const userManager = new UserManager(oidcConfig);

export const login = (): Promise<void> => userManager.signinRedirect();
export const logout = async (): Promise<void> => {
  localStorage.removeItem('admin_user');
  return userManager.signoutRedirect();
};
export const getUser = (): Promise<User | null> => userManager.getUser();
export const handleCallback = (): Promise<User> => userManager.signinRedirectCallback();

export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  scope: string;
  id_token?: string;
}

export interface LoginResult {
  success: boolean;
  error?: string;
  user?: User;
}

function parseJwt(token: string): Record<string, unknown> {
  const base64Url = token.split('.')[1];
  const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
  const jsonPayload = decodeURIComponent(
    atob(base64)
      .split('')
      .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
      .join('')
  );
  return JSON.parse(jsonPayload);
}

export const loginWithCredentials = async (
  username: string,
  password: string
): Promise<LoginResult> => {
  try {
    const tokenEndpoint = `${identityUrl}/connect/token`;

    const body = new URLSearchParams({
      grant_type: 'password',
      client_id: 'admin-ui',
      username,
      password,
      scope: 'openid profile roles warehouse orders offline_access',
    });

    const response = await fetch(tokenEndpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: body.toString(),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return {
        success: false,
        error: errorData.error_description || errorData.error || 'Login failed',
      };
    }

    const tokenResponse: TokenResponse = await response.json();

    const claims = parseJwt(tokenResponse.access_token);

    const profile: UserProfile = {
      sub: claims.sub as string,
      name: claims.name as string || username,
      email: claims.email as string,
      role: claims.role as string | string[],
      iss: claims.iss as string,
      aud: claims.aud as string,
      exp: claims.exp as number,
      iat: claims.iat as number,
    };

    const user: User = {
      access_token: tokenResponse.access_token,
      token_type: tokenResponse.token_type,
      expires_in: tokenResponse.expires_in,
      expired: false,
      expires_at: Math.floor(Date.now() / 1000) + tokenResponse.expires_in,
      scopes: tokenResponse.scope.split(' '),
      profile,
      refresh_token: tokenResponse.refresh_token,
      id_token: tokenResponse.id_token,
      session_state: null,
      state: null,
      toStorageString: () => JSON.stringify({
        access_token: tokenResponse.access_token,
        token_type: tokenResponse.token_type,
        expires_in: tokenResponse.expires_in,
        expires_at: Math.floor(Date.now() / 1000) + tokenResponse.expires_in,
        scope: tokenResponse.scope,
        profile,
        refresh_token: tokenResponse.refresh_token,
        id_token: tokenResponse.id_token,
      }),
    };

    localStorage.setItem('admin_user', JSON.stringify({
      access_token: tokenResponse.access_token,
      token_type: tokenResponse.token_type,
      expires_in: tokenResponse.expires_in,
      expires_at: user.expires_at,
      scope: tokenResponse.scope,
      profile,
      refresh_token: tokenResponse.refresh_token,
      id_token: tokenResponse.id_token,
    }));

    return { success: true, user };
  } catch (error) {
    console.error('Login request failed:', error);
    let errorMessage = 'An unexpected error occurred';
    if (error instanceof TypeError && error.message.includes('fetch')) {
      errorMessage = `Unable to connect to identity server at ${identityUrl}. Please check if the server is running.`;
    } else if (error instanceof Error) {
      errorMessage = error.message;
    }
    return {
      success: false,
      error: errorMessage,
    };
  }
};

export const getStoredUser = (): User | null => {
  const stored = localStorage.getItem('admin_user');
  if (!stored) return null;

  try {
    const userData = JSON.parse(stored);

    if (userData.expires_at && userData.expires_at < Math.floor(Date.now() / 1000)) {
      localStorage.removeItem('admin_user');
      return null;
    }

    return {
      ...userData,
      expired: userData.expires_at < Math.floor(Date.now() / 1000),
      scopes: userData.scope?.split(' ') || [],
      session_state: null,
      state: null,
      toStorageString: () => stored,
    } as User;
  } catch {
    localStorage.removeItem('admin_user');
    return null;
  }
};

export const logoutDirect = (): void => {
  localStorage.removeItem('admin_user');
};

export const isAdmin = (user: User | null): boolean => {
  if (!user) return false;
  const roles = user.profile.role;
  if (Array.isArray(roles)) {
    return roles.includes('Admin');
  }
  return roles === 'Admin';
};

export const getAccessToken = async (): Promise<string | null> => {
  const storedUser = getStoredUser();
  if (storedUser?.access_token) {
    return storedUser.access_token;
  }

  const user = await getUser();
  return user?.access_token ?? null;
};
