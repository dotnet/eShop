# Playwright Handoff

Use this when Playwright CLI is already configured and the next step is browser testing against a running Aspire app.

## Scenario: I Need The Right Frontend URL Before Browser Testing

Use these commands when the task is to discover the live frontend endpoint from Aspire state and then hand that URL to Playwright.

```bash
aspire describe --format Json
aspire describe --apphost <path> --format Json
playwright-cli --help
```

Keep these points in mind:

- Aspire discovers the endpoint first; Playwright uses the discovered endpoint after the handoff.
- Prefer `aspire describe --format Json` when the URL needs to be consumed by a script or passed to another tool.
- Use `--apphost <path>` when multiple AppHosts exist and the user is asking about one specific app.
- Do not guess frontend endpoints without first consulting Aspire state.
- If multiple frontends exist, use Aspire state to disambiguate which URL Playwright should use.
