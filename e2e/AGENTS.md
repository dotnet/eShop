# E2E Rules

- Playwright config is `playwright.config.ts`; `webServer` starts AppHost and waits for `http://localhost:5045`.
- CI uses one worker, retries twice, and has a longer web-server timeout.
- Logged-in tests depend on `login.setup.ts`, which writes storage state to `playwright\.auth\user.json`.
- Login setup requires `USERNAME1` and `PASSWORD` from `.env` or process environment.
- Current project split: setup, logged-in cart tests (`AddItemTest`, `RemoveItemTest`), and anonymous browsing (`BrowseItemTest`).
