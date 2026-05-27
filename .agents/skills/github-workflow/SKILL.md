---
name: github-workflow
description: >
  GitHub CLI (gh) workflow skill enforcing this project's trunk-based development (GitHub Flow) conventions.
  Use this skill whenever performing git/GitHub operations: creating branches, committing, pushing,
  opening PRs, merging, or creating releases. Also use when the user mentions PRs, branches, merging,
  releases, versioning, or any GitHub-related workflow — even if they don't explicitly say "use gh".
  This ensures consistent branch naming, squash-merge policy, conventional commits, and SemVer releases.
---

# GitHub Workflow — gh CLI Best Practices

This project follows **GitHub Flow (Trunk-Based Development)** with strict conventions. All git/GitHub operations must respect these rules.

## The Golden Rule

The `master` branch is sacred and always production-ready. **Never push directly to `master`.** All changes go through Pull Requests.

> This repo is private on a free GitHub account — branch protection may not be enforced by the platform. Discipline is the only safeguard. Double-check before any push to `master`.

---

## Branch Naming

Always create branches from the latest `master`. Use these prefixes:

| Prefix       | Purpose                                      | Example                          |
|--------------|----------------------------------------------|----------------------------------|
| `feature/`   | New features and additions                   | `feature/audio-recording`        |
| `fix/`       | Bug fixes and patches                        | `fix/jwt-refresh-timeout`        |
| `refactor/`  | Code cleanup, structural changes (no new behavior) | `refactor/api-endpoints`   |

```bash
# Sync first, then branch
git checkout master && git pull origin master
git checkout -b feature/your-feature-name
```

Choose short, descriptive kebab-case names that convey intent.

---

## Commits — Conventional Commits Format

Write small, logical commits using **Conventional Commits**:

```
feat: add microphone capture service
fix: resolve null reference in auth token
refactor: extract endpoint mapping to extension method
docs: update API versioning notes
chore: bump .NET SDK to 10.0.2
```

The prefix must match the type of change. The description is lowercase, imperative, and concise.

---

## Opening a Pull Request

Use `gh pr create` targeting `master`. Every PR must:

1. Have a clear, concise title (under 70 characters).
2. Include a summary in the body describing **what** and **why**.
3. Be self-reviewed before requesting merge — check the diff for:
   - Forgotten debug logs (`Console.WriteLine`, `Debug.Log`)
   - Commented-out code
   - Hardcoded secrets or paths

```bash
# Push the branch
git push origin feature/your-feature-name

# Create PR
gh pr create \
  --base master \
  --title "feat: add microphone capture service" \
  --body "$(cat <<'EOF'
## Summary
- Implemented cross-platform mic capture using NAudio
- Audio saved to RAM buffer, no disk writes

## Test plan
- [ ] Verify recording starts/stops with PTT hotkey
- [ ] Check memory usage stays stable during long sessions
EOF
)"
```

---

## Merging — Always Squash and Merge

**Never use standard merge commits.** Always use **Squash and Merge** to keep `master` history clean.

This collapses all developmental commits ("wip", "typo fix", "it works") into one clean commit on `master`.

```bash
# Merge with squash via gh CLI
gh pr merge <PR_NUMBER> --squash --delete-branch
```

The `--delete-branch` flag cleans up the remote branch after merge.

---

## Releases & Versioning (SemVer)

Use **Semantic Versioning** via GitHub Releases. No release branches — tag directly from `master`.

| Tag Format        | When to use                                    |
|-------------------|------------------------------------------------|
| `v0.1.0-alpha`    | Initial unstable testing                       |
| `v1.0.0`          | First official MVP release                     |
| `v1.1.0`          | Minor release (new feature, e.g., voice cmds)  |
| `v1.1.1`          | Patch release (bug fix in production)           |

```bash
# Create a release with auto-generated notes
gh release create v1.0.0 \
  --target master \
  --title "v1.0.0 — MVP Release" \
  --generate-notes
```

---

## Quick Reference — Full Workflow Cycle

```bash
# 1. Sync
git checkout master && git pull origin master

# 2. Branch
git checkout -b feature/groq-integration

# 3. Work & commit incrementally
git add src/Features/Dictation/TranscribeHandler.cs
git commit -m "feat: integrate Groq Whisper API for audio transcription"

# 4. Push
git push origin feature/groq-integration

# 5. Open PR
gh pr create --base master --title "feat: integrate Groq Whisper STT" --body "..."

# 6. After approval — squash merge & cleanup
gh pr merge --squash --delete-branch

# 7. Sync master locally
git checkout master && git pull origin master
```

---

## What NOT to Do

- **Do not** run `git push origin master` — ever.
- **Do not** use standard merge commits on PRs — always squash.
- **Do not** create long-lived release branches — use tags + GitHub Releases.
- **Do not** forget to sync `master` before creating a new branch.
- **Do not** leave merged remote branches around — use `--delete-branch`.
