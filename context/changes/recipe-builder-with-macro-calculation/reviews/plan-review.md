<!-- PLAN-REVIEW-REPORT -->
# Plan Review: Recipe Builder With Macro Calculation

- **Plan**: `context/changes/recipe-builder-with-macro-calculation/plan.md`
- **Mode**: Deep
- **Date**: 2026-06-01
- **Verdict**: SOUND (originally REVISE, all findings fixed during triage)
- **Findings**: [1 critical] [3 warnings] [0 observations]

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| End-State Alignment | PASS |
| Lean Execution | PASS |
| Architectural Fitness | PASS |
| Blind Spots | PASS |
| Plan Completeness | PASS |

## Grounding
Grounding: 5/5 paths ✓, 3/3 symbols ✓, brief↔plan ✓

## Findings

### F1 — Product Deletion Policy Contradiction

- **Severity**: ❌ CRITICAL
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Architectural Fitness
- **Location**: Phase 2 & References (docs/reference/contract-surfaces.md)
- **Detail**: docs/reference/contract-surfaces.md (line 18) and IProductRepository.cs (line 28) specify that deleting a product in use should return Product.InUse. However, the actual S-02 implementation in ProductRepository.DeleteAsync always deletes the product, relying on S-03 snapshotting. The implementation plan does not include updating these contradictory documents, leaving conflicting contracts.
- **Fix ⭐ Recommended**: Update the plan to explicitly include updating IProductRepository.cs and docs/reference/contract-surfaces.md to align with the snapshot-based deletion policy.
  - Strength: Aligns contract documentation with code, removing misleading instructions.
  - Tradeoff: None.
  - Confidence: HIGH — Direct doc-code alignment.
  - Blind spot: None.
- **Decision**: FIXED (via recommended fix)

### F2 — EF Core Owned Collection Replacement Semantics

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Architectural Fitness
- **Location**: Phase 2 — Recipe Repository Complete Update (RecipeRepository.cs)
- **Detail**: The plan states UpdateAsync will replace ingredient rows. Naive owned collection replacement (OwnsMany) in EF Core without loading/reconciling causes tracking exceptions or duplicate ingredient rows. The plan lacks details on the reconciliation strategy.
- **Fix A ⭐ Recommended**: Reconcile the collection by loading the recipe with Include(r => r.Ingredients), matching on ProductId, updating amounts, adding new, and removing missing.
  - Strength: Avoids tracking exceptions and duplicate ingredient rows.
  - Tradeoff: More verbose repository code.
  - Confidence: HIGH — Standard EF Core pattern for owned collection updates.
  - Blind spot: None.
- **Fix B**: Clear and save, then add new ingredients.
  - Strength: Shorter repository code.
  - Tradeoff: Can cause PK violations or require multiple database roundtrips.
  - Confidence: MEDIUM — Owned entities cannot easily be orphaned without being deleted.
- **Decision**: FIXED (via Fix A)

### F3 — JS Floating-Point Rounding Discrepancies

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Completeness
- **Location**: Phase 3 — React Recipe Builder UI (macroMath.ts)
- **Detail**: JavaScript floats can introduce binary rounding artifacts (e.g. 14.999999999998). The plan states UI math should match backend math but does not specify rounding. Without rounding, the UI preview will show floating-point display artifacts.
- **Fix**: Specify that macroMath.ts must round calculations to 1 decimal place (or 0 for calories) to match backend results.
  - Strength: Obvious and easy to do, prevents unpremium UI.
  - Tradeoff: None.
  - Confidence: HIGH.
  - Blind spot: None.
- **Decision**: FIXED (via recommended fix)

### F4 — MacroCalculator Parameter Cleanup

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Lean Execution
- **Location**: Phase 1 — Macro Calculator Recipe Overloads (MacroCalculator.cs)
- **Detail**: The plan leaves the dictionary parameter in MacroCalculator.RecipeTotal and RecipePerServing even though RecipeIngredient will now carry Per100Grams. Keeping the parameter adds unnecessary coupling and fetching logic in callers.
- **Fix**: Remove the dictionary parameter from the recipe calculator methods and update tests to build recipes with snapshotted ingredients directly.
  - Strength: Cleaner API surface, less coupling, simplifies use cases.
  - Tradeoff: None.
  - Confidence: HIGH.
  - Blind spot: None.
- **Decision**: FIXED (via recommended fix)
