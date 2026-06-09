<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Account Data Boundary

- **Plan**: context/changes/account-data-boundary/plan.md
- **Scope**: Phases 1-3 of 3 (full plan)
- **Date**: 2026-05-28
- **Verdict**: APPROVED
- **Findings**: 0 critical, 1 warning, 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | PASS |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | WARNING |

Verified: solution build green (0 warn / 0 err); 21/21 tests pass after fix
(API 10, Application 9, Domain 1, Infra 1); secret scan clean; no live
weatherforecast surface; UnitTest1.cs placeholders removed.

## Findings

### F1 — ICurrentUser explicit-failure paths are untested

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: src/Jadlify.API/Authentication/HttpContextCurrentUser.cs:26,33
- **Detail**: HttpContextCurrentUser.UserId throws on (a) unauthenticated principal and (b) missing/whitespace sub, but only the happy path (CurrentUser_ShouldExposeApplicationUserId_FromLiteralSubjectClaim) was tested. The plan's Testing Strategy explicitly lists the failure case as a unit test. Behavior was correct; the assertions were missing. Partly shielded at the HTTP layer by the fallback RequireClaim("sub") policy (403 before a handler resolves UserId).
- **Fix**: Added two unit tests in AuthBoundaryTests asserting UserId throws InvalidOperationException when the principal is unauthenticated and when the sub claim is absent.
- **Decision**: FIXED

### F2 — change.md status is `implemented`, plan said keep `planned`

- **Severity**: 📝 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: context/changes/account-data-boundary/change.md:4
- **Detail**: Phase 3 change #4 instructed keeping status `planned`, but actual is `implemented`. This is a flaw in the plan text, not the implementation — `implemented` is the correct terminal status. The implementer correctly ignored the stale instruction.
- **Fix**: None needed — implementation is correct as-is.
- **Decision**: ACCEPTED
