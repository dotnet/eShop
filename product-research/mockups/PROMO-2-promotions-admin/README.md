# PROMO-2 Promotions Admin Mockup

A clickable mockup of an admin console built on the API in [PROMO-2](../../PROMO-2-promotion-rule-model-and-admin-api.md). PROMO-2 is a backend-only story, so this shows what a console using its endpoints could look like. No story covers building it yet.

- **Live, clickable version:** https://claude.ai/artifact/VBLaS4t2BnJNpW2aRkifZH (shared with the Slalom organization)
- **Exported:** 2026-11-09, including all review comments (4 resolved)

## Screens

| File | Screen | What it shows |
| --- | --- | --- |
| `Main.dc.html` | 1 · Promotions list | Status tabs with counts, a promotions table, Publish on drafts, and Manage on every row |
| `Editor.dc.html` | 2 · Create promotion | Rule-type switcher with conditional fields, targets, schedule, priority and options, a storefront badge preview, and the validation-error state shown after **Save draft** |
| `Detail.dc.html` | 3 · Active promotion detail | Edit locked (409), lifecycle timeline with integration events, public feed preview, and the **End now** confirmation leading to the Expired state |
| `canvas.json` | Canvas index | Artboard layout and sizes |

## Viewing these files

These are Design canvas source files (`.dc.html`). They load the canvas runtime (`support.js`) from the hosting page, so **opening them directly in a browser won't render them**. Use the live link above to view or click through, or for PDF/PNG exports use **Share › Export** on that page.

## Review changes applied

- "View · End", "Edit" and "View" on the list are now a single **Manage** button. It opens the editor for drafts and scheduled promotions, and the detail screen for active and expired ones.
- Status tab counts are now separate bold round badges, spaced away from their labels.
