# 22 — Legal & Compliance

> Not legal advice. This is a checklist of obligations to satisfy and, where flagged, to have reviewed by a lawyer before launch.
> ★ Voice chat and IAP are what make this document mandatory rather than optional.

---

## 1. Documents required before launch

| Document | Where | Notes |
|---|---|---|
| **Privacy Policy** | Public URL + in-app | Must explicitly cover ★ voice, analytics, IAP |
| **Terms of Service** | Public URL + in-app | Code of conduct, ban policy, virtual-goods terms |
| **EULA** | Bundled in ToS | Licence, not sale, of virtual goods |
| **Community Guidelines** | In-app | ★ Voice and chat behaviour rules |
| **Attribution / Credits** | In-app settings | CC-BY assets (`18 §2`) |
| **Gacha rate disclosure** | In-app, at point of pull | Legal requirement in several markets |

⚠️ **Have the Privacy Policy and ToS reviewed by a lawyer.** With voice chat, minors, and payments in scope, template documents are not sufficient. This is the one place where spending money before launch is clearly worth it.

---

## 2. Privacy: what we collect and why

| Data | Purpose | Basis | Retention |
|---|---|---|---|
| Device ID | Guest account identity | Contract | Account lifetime |
| Email (optional) | Account recovery | Consent | Account lifetime |
| Declared age | ★ Voice gating, COPPA | Legal obligation | Account lifetime |
| Game state | Provide the service | Contract | Account lifetime |
| Purchase receipts | Fulfil purchases, disputes | Contract / legal | 7 years (tax) |
| Analytics events | Improve the game | Consent | 90 days raw |
| Chat messages | Moderation | Legitimate interest | 30 days |
| **★ Reported voice clips** | Moderation only | Legitimate interest | **7 days, auto-deleted** |
| IP address | Security, abuse prevention | Legitimate interest | 30 days |

**What we do not collect:** advertising ID, contacts, precise location, biometrics, continuous voice recordings.

### ★ The voice privacy design
Voice audio is **never recorded or stored** in normal operation. A 30-second ring buffer lives on the device; audio leaves the device only when a user files a report, is encrypted at rest, and is deleted automatically after 7 days (`10 §6.2`).

This is worth stating plainly in the privacy policy in user-facing language, because it is both true and unusually protective. It also drastically reduces our exposure: there is no archive of user conversations to breach, subpoena, or mishandle.

---

## 3. Regulatory obligations

### GDPR (EU/UK)
- [ ] Lawful basis documented per data type (table above)
- [ ] Consent before analytics; refusal fully functional
- [ ] Right of access — data export in-app
- [ ] Right to erasure — account deletion in-app, cascading to analytics and voice clips
- [ ] Right to rectification — edit display name, email
- [ ] Data portability — JSON export
- [ ] Breach notification within 72 h (have the process written before you need it)
- [ ] Records of processing activities
- [ ] No DPA needed for voice since we self-host, but one is needed for any third-party processor (PostHog cloud, Sentry, Google Play)

### COPPA (US, under 13)
- [ ] Neutral age gate at registration
- [ ] Under-13 → **★ voice disabled entirely, no override** (`10 §6.1`)
- [ ] Under-13 → text chat restricted to preset phrases, or disabled
- [ ] Under-13 → no behavioural analytics, no ads
- [ ] No collection of personal information from under-13 accounts beyond what's needed to run the game

**Design decision:** we avoid COPPA's verifiable-parental-consent machinery by simply not offering the features that would require it to minors. That is far cheaper than building consent infrastructure, and safer.

### DSA (EU)
- [ ] In-app reporting for chat and ★ voice
- [ ] Transparency: tell a reported user that action was taken
- [ ] Appeal path for moderation decisions
- [ ] Published moderation statistics (annual)

### CCPA/CPRA (California)
- [ ] "Do Not Sell My Personal Information" — trivially satisfied: we don't sell data
- [ ] Disclosure of categories collected
- [ ] Deletion on request

### Other markets
| Market | Requirement |
|---|---|
| **Brazil** | LGPD — mirrors GDPR closely |
| **Turkey** | KVKK — mirrors GDPR |
| **Saudi/UAE** | Content standards: no gambling imagery, cultural sensitivity review |
| **China** | Not a launch target (requires a licence + local publisher) |
| **Belgium/Netherlands** | Loot box scrutiny → published rates, no real-money direct loot boxes |
| **South Korea** | Published probabilities are mandatory |

---

## 4. Google Play requirements

- [ ] Target API 35, AAB format
- [ ] **Data Safety form** completed accurately, including **microphone use for ★ voice**
- [ ] Content rating questionnaire (IARC) — declaring user-generated voice/text raises the rating
- [ ] Families policy: **not** opting into Designed for Families (voice chat makes this untenable)
- [ ] Ads declaration (rewarded video, Phase 8)
- [ ] Play Billing v7 for all digital goods
- [ ] Account deletion accessible **from within the app and from a web URL** (mandatory)
- [ ] Permissions: microphone requested only at first ★ voice use, with a clear in-context rationale
- [ ] No misleading store assets — screenshots and video must show actual gameplay

**The content rating consequence is worth understanding up front:** shipping voice chat means declaring user-generated content, which raises the age rating and removes access to some family-oriented placements. That's an accepted cost of feature #1.

---

## 5. Virtual goods & consumer law

- Virtual currency and items are **licensed, not sold** — no ownership transfer, no real-money value
- No cash-out, no trading for real money, no player-to-player currency transfer (also an anti-fraud measure)
- Refund handling follows Play's policy; we honour it and revoke granted currency where feasible
- **Service shutdown clause:** if the game closes, we commit to a notice period and to disabling purchases before shutdown. Write this into the ToS now — it is the honest thing to do and several jurisdictions increasingly expect it
- Price changes for subscriptions require advance notice and opt-in per Play's rules

---

## 6. Moderation policy (published)

| Violation | First | Repeat | Severe |
|---|---|---|---|
| Profanity | Warning | 1 h mute | 24 h mute |
| Harassment | 24 h mute | 7 d ban | Permanent |
| Hate speech | 7 d ban | Permanent | Permanent |
| Threats of violence | Permanent | — | Report to authorities |
| Sexual content involving minors | **Permanent + immediate report to authorities** | — | — |
| Cheating | 7 d ban + rollback | Permanent | Permanent |
| Account selling | Permanent | — | — |
| Real-money trading | Permanent | — | — |

**Appeals:** a written appeal path with a target 72-hour response. Every moderation action carries actor, reason, evidence reference, and timestamp in an append-only log (`15 §6`).

The child-safety row is absolute and has no discretionary tier. It is also the reason the age gate and reporting flow are launch blockers rather than nice-to-haves.

---

## 7. Accessibility & other obligations

- WCAG-informed contrast and touch-target sizes (`19 §6`)
- ★ Voice has full text-chat parity so the feature is never a barrier
- Note: full WCAG conformance for a game requires manual testing with assistive technologies and expert review; we implement the measurable items and are honest about the limits of self-assessment

---

## 8. Pre-launch legal checklist
- [ ] Privacy Policy — **lawyer reviewed** ⚠️
- [ ] Terms of Service — **lawyer reviewed** ⚠️
- [ ] Community Guidelines published
- [ ] Attribution page complete and accurate
- [ ] Gacha rates visible in-app at the point of pull
- [ ] Age gate implemented and tested (including the under-13 voice block)
- [ ] Account deletion works in-app and via web URL
- [ ] Data export works
- [ ] Voice clip 7-day auto-deletion verified in production
- [ ] Play Data Safety form matches actual behaviour
- [ ] Content rating completed with UGC declared
- [ ] DPAs signed with every third-party processor
- [ ] Breach response process written
- [ ] Business entity registered, tax handling for IAP revenue understood ⚠️

---

## Next
- `10-FEATURE-voice-chat.md §6` — the technical implementation of these obligations
- `25-launch-ua-plan.md`
