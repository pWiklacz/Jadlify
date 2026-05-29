> **TL;DR for AI Agent:**
> **Topic:** Trunk-Based Development (GitHub Flow): main branch protection, branch naming conventions (`feature/`, `fix/`, `refactor/`), Conventional Commits, Pull Request strategy, SemVer.
> **When to read:** Before creating a branch, committing, or opening a Pull Request — this document defines naming conventions and Git workflows.
> **Do not read when:** Working on backend/frontend code without Git interaction.

# GitHub Workflow and Branching Strategy

## 1. Core Philosophy and Branch Protection
We use a lightweight approach based on Trunk-Based Development (GitHub Flow). This ensures rapid iteration, prevents "merge hell", and keeps the repository clean.

### The Golden Rule of the `main` branch
* The `main` branch is sacred. Code on the `main` branch **must always compile, run flawlessly, and be production-ready.**
* **Direct pushes to `main` are strictly forbidden.** All changes must be integrated using a Pull Request (PR).
* *Honesty Note for Free/Private repos:* If you are on a free GitHub tier and this repository is private, platform branch protection rules might be disabled. In such cases, this rule relies entirely on strict developer discipline. Do not use `git push origin main`.

---

## 2. Branch Naming Conventions
Every new task must be isolated into its own branch, created from the latest state of the `main` branch. Standard prefixes must be used to clearly define the branch's purpose:

* `feature/` – For new features and additions (e.g., `feature/audio-recording`, `feature/groq-integration`).
* `fix/` – For bug fixes (e.g., `fix/jwt-refresh-timeout`).
* `refactor/` – For code cleanup, optimizations, or structural changes without affecting user-facing functionality (e.g., `refactor/api-endpoints`).

---

## 3. Daily Development Process (Step-by-Step)

Follow this cycle for every single task:

1. **Sync with `main`:**
   Always start your day (and every new task) by pulling the latest changes.
   ```bash
   git checkout main
   git pull origin main
   ```
2. **Start a new Working Branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Incremental Commits:**
   Create small, logical commits as you write code, using the Conventional Commits format.
   * `feat: added microphone capture service`
   * `fix: resolved null reference in auth token`
4. **Push to Remote:**
   ```bash
   git push origin feature/your-feature-name
   ```
5. **Open a Pull Request (PR):**
   Go to GitHub and create a PR targeting the `main` branch. Before creating it, review the "Files changed" tab — this self-review helps catch leftover developer logs (like `Console.WriteLine`) or unnecessary commented-out code.
6. **"Squash and Merge":**
   Once the PR is accepted (or self-approved), **DO NOT use a standard "merge commit"**. Click the dropdown arrow next to the green button and select **"Squash and merge"**.
   * *Why?* This squashes numerous, messy development commits ("wip", "typo fix", "now it works") into a single clean and descriptive commit on the `main` branch.

---

## 4. Release Management and Versioning

We do not use "heavy" release branches. When the code on `main` reaches a desired milestone and the application is ready for users, we use GitHub Releases based on **Semantic Versioning (SemVer)** rules:

* Go to the GitHub repository > **Releases** tab > **Draft a new release**.
* Create a new Tag pointing to the current state of the `main` branch:
  * `v0.1.0-alpha` (For initial, unstable testing).
  * `v1.0.0` (Targeting the official MVP V1 deployment).
  * `v1.1.0` (Minor release: adding a small new feature, e.g., voice commands).
  * `v1.1.1` (Patch release: fixing a production bug).
* Generate release notes and publish.

---

## 5. CI/CD Pipelines and Deployments
This workflow is tightly integrated with GitHub Actions to automate testing, Docker image builds, and deployments across our distributed architecture.

To maintain system security (e.g., with OIDC authentication) and performance (e.g., through Docker layer caching and using `npm ci`), all CI/CD pipelines must adhere to the project's infrastructure standards.

👉 **Detailed information concerning pipeline architecture, caching strategies, and deployment rules can be found in:** [CI/CD Pipelines Architecture Standard](ci-cd-pipelines.md)