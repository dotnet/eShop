# Targeted Advertising Profiles: Tying New Users to Advertising Identity

## Overview

Large ad platforms (Meta, Google, Amazon) do not expose individual users'
demographic or preference profiles directly to third-party sites. Instead,
they operate on a **"send signals, receive attribution/audience" model**:

1. The site sends known user signals (hashed PII, click IDs, event data) to
   the platform.
2. The platform matches those signals against its own internal ad-identity
   graph (the user's logged-in account, device graph, etc.).
3. The platform returns **attribution** (which ad/campaign drove the
   conversion) and lets the site build **audiences** (retargeting,
   lookalikes) — but does not return raw demographic data about the
   individual user.

This keeps individual-level demographic/interest data inside each platform's
walled garden for privacy reasons, while still letting advertisers measure
and target effectively.

## Core Pattern (applies to all three platforms)

1. Capture the ad click ID when a user arrives from a paid ad:
   - Meta: `fbclid` query parameter
   - Google: `gclid` / `gbraid` / `wbraid` query parameters
   - Amazon: Amazon Attribution click/tag parameters
2. Persist the click ID against the user/session in your own database.
3. On a conversion event (signup, purchase, etc.), hash the user's PII
   (email, phone — SHA-256, lowercase, trimmed) and send a **server-side**
   event to the platform's conversion API, including the click ID and
   hashed identifiers.
4. The platform matches the event to its internal user graph, credits the
   originating ad, and updates audience/retargeting lists.

## Compliance Requirements

- Obtain user **consent** (GDPR/CCPA/ePrivacy) before sending any
  PII-derived data to third-party ad platforms.
- Maintain **Data Processing Agreements** with each platform.
- **Always hash PII before sending** — never transmit plaintext email,
  phone, or name fields.
- Honor opt-out/do-not-track signals and provide a way to disable tracking.

## Platform-Specific Integration Points & Official Documentation

### Meta (Facebook / Instagram)

- **Meta Pixel** (client-side browser tracking)
  https://developers.facebook.com/docs/meta-pixel
- **Conversions API (CAPI)** (server-side event matching)
  https://developers.facebook.com/docs/marketing-api/conversions-api
- **Conversions API + Pixel deduplication guide**
  https://developers.facebook.com/docs/marketing-api/conversions-api/deduplicate-pixel-and-server-events
- **Custom Audiences API** (building retargeting/lookalike audiences)
  https://developers.facebook.com/docs/marketing-api/audiences

### Google

- **Google Ads Enhanced Conversions**
  https://developers.google.com/google-ads/api/docs/conversions/enhanced-conversions
- **Google Tag (gtag.js) documentation**
  https://developers.google.com/tag-platform/gtagjs
- **Google Analytics 4 (GA4) Measurement Protocol**
  https://developers.google.com/analytics/devguides/collection/protocol/ga4
- **Google Ads API — Conversion Tracking**
  https://developers.google.com/google-ads/api/docs/conversions/overview
- **Customer Match (audience building)**
  https://developers.google.com/google-ads/api/docs/remarketing/audience-types/customer-match

### Amazon

- **Amazon Ads Conversion API**
  https://advertising.amazon.com/API/docs/en-us/conversion-events/overview
- **Amazon Attribution API**
  https://advertising.amazon.com/API/docs/en-us/amazon-attribution/
- **Amazon Advertising API — Audiences**
  https://advertising.amazon.com/API/docs/en-us/amazon-dsp-audiences

## What You Actually Get Back

| You send | You get back |
|---|---|
| Hashed email/phone + click ID + event data | Attribution: which ad/campaign drove the conversion |
| Hashed customer lists | Custom Audiences / Lookalike Audiences / Customer Match segments |
| — | **Not** raw per-user demographic or interest profiles |

## Suggested Next Steps for This Project

- Decide which platforms to integrate first (likely Meta CAPI + Google
  Enhanced Conversions, given typical ad spend priorities).
- Add a server-side event service (e.g., `/api/tracking/conversion`) that:
  - Accepts an internal event (signup, purchase) plus stored click ID.
  - Hashes PII fields.
  - Posts to each platform's conversion API.
- Add a consent-gating layer so no data is sent without user opt-in.
