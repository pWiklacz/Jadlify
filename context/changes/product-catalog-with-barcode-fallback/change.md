---
change_id: product-catalog-with-barcode-fallback
title: Product catalog with barcode fallback
status: implementing
created: 2026-05-31
updated: 2026-05-31
archived_at: null
---

## Notes

<!-- Free-form notes for this change: links, ad-hoc context, decisions that don't belong in research/frame/plan. -->

- **Barcode API decision (2026-05-31):** use the **Open Food Facts API** for barcode → product lookup; snapshot fields into the user's product row at add-time, manual entry as fallback (FR-006). Full rationale, integration notes, and alternatives in [`research-barcode-api.md`](./research-barcode-api.md). Resolves roadmap Open Question #3.
- **OFF API usage reference (2026-05-31):** endpoint, parameters, response envelope, `nutriments` field convention, field mapping, and a backend adapter checklist in [`off-api-reference.md`](./off-api-reference.md).
