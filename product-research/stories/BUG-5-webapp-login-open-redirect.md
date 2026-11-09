# BUG-5: [FE - WebApp] - Open redirect after sign-in through a protocol-relative returnUrl

**Type:** Bug
**Severity:** High
**Category:** Security
**Found by:** test stack flow mapping (`testing/flows/`)

**Bug Report:**

Open redirect after sign-in through a protocol-relative returnUrl. Caught by the test "a protocol-relative returnUrl on /user/login must not redirect off-site" in `e2e/teststack/auth-login.spec.ts`.

**Steps to Reproduce:**

Start the test stack and load the scenario (see `testing/README.md`):

```bash
testing/teststack up
testing/teststack load auth-lockout
```

1. Open `http://localhost:18080/user/login?returnUrl=//evil.example` and sign in as alice (`Pass123$`).

**Expected:** After sign-in the user lands on a page of the WebApp (`/` when the return URL is not local).

**Actual:** `302 Location: http://evil.example/`: the browser leaves the site. This can be used for phishing links that start on the real domain.

**Acceptance Criteria:**

- **Given** the test stack is running with the `auth-lockout` scenario loaded
- **When** the steps above are repeated
    - **Then** the result matches **Expected**
- **When** the `test.fixme` marker is removed from `e2e/teststack/auth-login.spec.ts` › "a protocol-relative returnUrl on /user/login must not redirect off-site"
    - **Then** the test passes, and the rest of the file still passes:
      `npx playwright test -c playwright.teststack.config.ts --project scenarios --no-deps e2e/teststack/auth-login.spec.ts`
- **Then** the matching row is removed from the bugs table in `testing/flows/`

**Technical Notes:**

- `src/WebApp/Components/Pages/User/LogIn.razor:13-15` treats `//evil.example` as relative because `new Uri(..., RelativeOrAbsolute).IsAbsoluteUri` is false for protocol-relative URLs.
- **Suggested fix:** Accept only local paths: a single leading `/`, not followed by `/` or `\` (the same rule as `Url.IsLocalUrl` in MVC); otherwise use `/`.

**Related Stories:**

- **Part of:** [Test stack bug findings epic](../epic-teststack-bug-findings.md)
- **Related:** None
