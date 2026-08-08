# Third-party attributions

Every third-party asset shipped in the game is recorded here, per `18 §2`.

**Add the row in the same commit that adds the asset.** Retrofitting this list months later
means reconstructing where a file came from from memory, which in practice means guessing. A
wrong guess about a licence is a legal problem and a store-removal risk, not a tidiness one.

**Before adding anything, check:**

- The licence is stated explicitly by the source. A pack described as "free" with no licence
  file is not usable — "free to download" and "free to ship in a commercial product" are
  different claims.
- The licence permits commercial use. This is a commercial product from day one.
- If it is CC-BY, the credit must also appear in-app under **Settings → Credits**. Listing it
  here alone does not satisfy the licence.
- Nothing AI-generated where the training-data provenance is unclear (`18 §2`).

Record the URL you actually downloaded from, not the site's homepage — the specific page is
what proves the licence terms you relied on.

---

## Art

| Asset | Source | URL | Licence | Date added | In-app credit required |
|---|---|---|---|---|---|
| _none yet_ | | | | | |

## Audio

| Asset | Source | URL | Licence | Date added | In-app credit required |
|---|---|---|---|---|---|
| _none yet_ | | | | | |

## Fonts

| Asset | Source | URL | Licence | Date added | In-app credit required |
|---|---|---|---|---|---|
| _none yet_ | | | | | |

## Code & packages

Unity packages from the registry are covered by the Unity licence and are not listed here.
This section is for third-party source that is vendored into the repo, where the licence
travels with the file rather than a package manifest.

| Component | Source | URL | Licence | Date added |
|---|---|---|---|---|
| _none yet_ | | | | |

---

## Licence quick reference

| Licence | Commercial use | Attribution required | In-app credit |
|---|---|---|---|
| CC0 | Yes | No | No |
| CC-BY | Yes | **Yes** | **Yes** — Settings → Credits |
| CC-BY-SA | Yes, but **share-alike** — avoid for game assets | Yes | Yes |
| CC-BY-NC | **No — non-commercial only, unusable here** | — | — |
| OFL (fonts) | Yes | Yes, in credits | Yes |
| Own (self-made) | Yes | No | No |

CC-BY-SA and CC-BY-NC are called out because they look permissive at a glance and are not.
NC forbids commercial use outright; SA can propagate its terms into derived work.
