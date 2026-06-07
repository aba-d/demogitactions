# AI Agents for DevOps and Platform Engineering

> A practical guide for teams running GitHub EMU + GitHub Actions + Veracode + HashiCorp Vault on AWS EC2 + AWS IAM/OIDC + self-hosted runners on AWS CodeBuild + GitHub Copilot.

This guide shows how to design, deploy, and govern six AI agent archetypes — and how each integrates with your existing toolchain using **GitHub Copilot Cloud Agent**, **Copilot in VS Code**, **GitHub Actions**, and **AWS ECS**.

---

## Table of Contents

1. [Your Stack — Agent Integration Map](#your-stack--agent-integration-map)
2. [The Six DevOps Agent Archetypes](#the-six-devops-agent-archetypes)
3. [CI/CD Agents](#1-cicd-agents)
4. [Security Agents](#2-security-agents)
5. [Incident Response Agents](#3-incident-response-agents)
6. [Infrastructure Provisioning Agents](#4-infrastructure-provisioning-agents)
7. [Deployment Agents](#5-deployment-agents)
8. [Compliance Agents](#6-compliance-agents)
9. [Reference Architecture — Multi-Agent DevOps Platform on AWS](#reference-architecture--multi-agent-devops-platform-on-aws)
10. [Governance Cheat Sheet](#governance-cheat-sheet)
11. [Getting Started — 30-Day Adoption Plan](#getting-started--30-day-adoption-plan)

---

## Your Stack — Agent Integration Map

Before diving into archetypes, here is how AI agents plug into your existing tools.

```mermaid
flowchart TB
    subgraph DEV["Developer Workspace"]
        VSC["VS Code + Copilot<br/>agent mode"]
    end

    subgraph GH["GitHub EMU"]
        GHC["Copilot Cloud Agent<br/>GitHub-hosted, autonomous"]
        ISSUE["Issues / PRs"]
        REPO["Repos & Rulesets"]
    end

    subgraph CICD["CI/CD Layer"]
        GHA["GitHub Actions<br/>workflows"]
        SHR["Self-hosted runners<br/>on AWS CodeBuild"]
        MCP["GitHub MCP Server<br/>tool gateway"]
    end

    subgraph SEC["Security & Identity"]
        OIDC["OIDC trust<br/>GitHub → AWS"]
        IAM["AWS IAM Roles<br/>least-privilege"]
        VAULT["HashiCorp Vault<br/>on EC2"]
        VC["Veracode<br/>SAST / SCA"]
    end

    subgraph AWS["AWS Runtime"]
        ECS["ECS / Fargate<br/>long-running agents"]
        ECR["ECR<br/>agent images"]
        CW["CloudWatch + X-Ray<br/>observability"]
        SM["Secrets Manager<br/>KMS"]
    end

    VSC -->|copilot in chat| GHC
    VSC -->|edit code| REPO

    ISSUE -->|copilot assigned| GHC
    GHC -->|opens PR| REPO
    GHC -->|uses tools via| MCP

    REPO -->|push / PR| GHA
    GHA -->|runs on| SHR
    GHA -->|assume role via| OIDC --> IAM
    GHA -->|reads secrets| VAULT
    GHA -->|scans| VC

    GHA -->|deploys agents| ECS
    ECS -->|pulls image| ECR
    ECS -->|secrets| SM
    ECS -->|emits traces| CW
```

---

## The Six DevOps Agent Archetypes

```mermaid
flowchart LR
    A([Code commit]) --> CI["CI/CD Agent"]
    CI --> SEC[Security Agent]
    SEC --> COMP[Compliance Agent]
    COMP --> INFRA[Infrastructure Agent]
    INFRA --> DEPLOY[Deployment Agent]
    DEPLOY --> RUN([Running system])
    RUN -.->|incident fires| IR[Incident Response Agent]
    IR -.->|opens PR with fix| A
```

Each agent has a specific spot in the value stream. They communicate through GitHub Issues, PRs, and webhook events — using infrastructure you already have.

---

## Build vs Configure vs Custom Agent — Pick the Right Approach

Before you read each agent section, a critical decision: **for most of these agents, you are NOT writing new agent code.** You are configuring the existing **GitHub Copilot Cloud Agent** with your specific context. Here are the three options:

### The three approaches

| Approach | What you do | When to use it | Effort |
|---|---|---|---|
| **A. Base agent + custom instructions** | Drop a `.github/copilot-instructions.md` file. `@copilot` now behaves as your specialist. | Single use case per repo. Fastest path to value. | Hours |
| **B. Custom agent** | Define a named agent in `.github/agents/<name>.md`. Invoked via `@agent-name`. | Multiple specialist agents in same repo (security, infra, docs). | A day |
| **C. Build separately** | Write code (Python/TS), deploy to ECS Fargate. Triggered by webhooks. | Work that isn't code-centric: webhooks, scheduled scans, long-running watchers. | Days–weeks |

### Decisive question

> *"Does this work happen in repos and produce code, PRs, or comments?"*

- **Yes** → Use Copilot Cloud Agent (Approach A or B). Don't reinvent.
- **No** (it watches webhooks, queries cloud APIs, generates reports) → Build separately (Approach C).

### Per-agent recommendation

```mermaid
flowchart LR
    subgraph A["Approach A — Custom instructions"]
        A1[CI/CD agent]
    end

    subgraph B["Approach B — Custom agent"]
        B1[Security agent]
        B2[Infrastructure agent]
    end

    subgraph C["Approach C — Build on ECS"]
        C1[Incident response agent]
        C2[Deployment agent]
        C3[Compliance agent]
    end
```

| Agent | Approach | Reason |
|---|---|---|
| CI/CD agent | **A** | Single role, code-centric, repo-scoped |
| Security agent | **B** | Multiple specialists needed (Veracode, secret scanning, SCA) |
| Incident response | **C** | Webhook-driven, queries CloudWatch, posts to Slack — not code-centric |
| Infrastructure | **B** | Terraform-specialist with plan-only tool restrictions |
| Deployment | **C** | Too sensitive for autonomous Cloud Agent; orchestrated by Actions with HITL |
| Compliance | **C** | Long-running scheduled scans on read-only data sources |

> **For your first agent** — start with the Security Agent (Approach B). It is the highest-value, lowest-risk path. The walkthrough is in [section 2 below](#how-to-create-one-walkthrough-our-first-custom-agent).

---

## 1. CI/CD Agents

### Responsibilities

- Investigate failing pipelines and propose fixes (flaky tests, dependency conflicts, runner issues)
- Generate and maintain GitHub Actions workflows
- Optimize build times (caching strategy, parallelization, matrix builds)
- Manage release notes and semantic versioning
- Auto-bump dependencies and verify with test runs

### Inputs

| Input | Source |
|---|---|
| Workflow run logs | GitHub Actions API |
| Failed test output | `gh run view` / artifacts |
| Repo structure & history | Git, GitHub MCP server |
| Issue/PR context | GitHub Issues |
| Branch protection rules | GitHub Rulesets API |

### Outputs

- **Pull requests** — workflow patches, dependency bumps, fix commits
- **Comments on failing PRs** — root cause analysis with suggested fixes
- **GitHub Discussions / Issues** — release notes, optimization recommendations

### Required Tools

| Tool | Purpose |
|---|---|
| GitHub MCP Server | Read/write issues, PRs, workflows, run logs |
| `gh` CLI | Direct API access from agent shell |
| Self-hosted runner on AWS CodeBuild | Execute proposed fixes in your environment |
| Veracode API | Read prior scan results to avoid reintroducing fixed vulns |

### Security Considerations & Governance

- **No direct merges.** Agent opens PRs; humans (or rules) approve. Treat `@copilot` like any contributor under your branch protection rules.
- **Bypass actor configuration.** If your rulesets block bots, explicitly add Copilot Cloud Agent as a bypass actor — never disable the rule.
- **OIDC, not long-lived tokens.** When the agent's PR triggers a workflow, the workflow assumes an AWS role via OIDC; the agent itself never sees AWS credentials.
- **Tool allow-list.** Configure your MCP server to expose only the GitHub tools the agent needs. Restrict access to repo-scoped reads by default; widen only per task.
- **Audit log.** Every Copilot Cloud Agent action is recorded in the GitHub audit log under `copilot.*` events. Stream to your SIEM.

### Architecture

```mermaid
sequenceDiagram
    participant DEV as 👤 Developer
    participant ISS as GitHub Issue
    participant CCA as Copilot Cloud Agent
    participant ACT as GitHub Actions
    participant CB as Self-hosted runner (AWS CodeBuild)
    participant PR as Pull Request

    DEV->>ISS: "@copilot the nightly build fails on flaky integration test"
    ISS->>CCA: Task assigned
    CCA->>CCA: Research repo + last 10 workflow runs
    CCA->>ACT: Read failed run logs via gh CLI
    ACT-->>CCA: Stack trace + timing data
    CCA->>CCA: Identify race condition in test setup
    CCA->>PR: Open PR with fix + retry logic
    PR->>ACT: Triggers CI workflow
    ACT->>CB: Runs full test suite
    CB-->>PR: ✅ All tests pass
    DEV->>PR: Reviews diff + reasoning trace
    DEV->>PR: Merge
```

### How to Use This Agent

**In VS Code with Copilot (agent mode):**

```
@copilot Investigate why .github/workflows/ci.yml is failing for PR #1234.
Read the last failing run log and propose a minimal fix as a new branch.
```

**In CI/CD via GitHub Actions** — auto-triage failed builds:

```yaml
# .github/workflows/auto-triage-failed-builds.yml
name: Auto-triage failed CI

on:
  workflow_run:
    workflows: ["CI"]
    types: [completed]

permissions:
  contents: read
  issues: write
  pull-requests: write
  id-token: write   # for AWS OIDC

jobs:
  triage:
    if: ${{ github.event.workflow_run.conclusion == 'failure' }}
    runs-on: [self-hosted, codebuild]
    steps:
      - name: Configure AWS via OIDC
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/github-actions-cicd-agent
          aws-region: us-east-1

      - name: Fetch Vault token
        id: vault
        uses: hashicorp/vault-action@v3
        with:
          url: ${{ vars.VAULT_ADDR }}
          method: aws
          role: cicd-agent
          secrets: |
            secret/data/copilot api_key | COPILOT_API_KEY

      - name: Create triage issue for Copilot
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh issue create \
            --title "[auto-triage] CI failed on ${{ github.event.workflow_run.head_branch }}" \
            --body "$(cat <<EOF
          @copilot the workflow run below failed. Please investigate and open a PR with a fix.

          - Workflow: ${{ github.event.workflow_run.name }}
          - Run URL: ${{ github.event.workflow_run.html_url }}
          - Head SHA: ${{ github.event.workflow_run.head_sha }}

          Constraints:
          - Do NOT modify .github/workflows/ files that touch production deployment
          - Run the test suite locally in your sandbox before opening the PR
          - Cite the exact log line(s) that identify the root cause
          EOF
          )" \
            --label "copilot,auto-triage"
```

---

## 2. Security Agents

### Responsibilities

- Triage Veracode SAST/SCA findings — confirm true positive, suggest fix, open PR
- Triage GitHub Advanced Security alerts (code scanning, secret scanning, Dependabot)
- Review PRs for security anti-patterns (hardcoded secrets, unsafe deserialization, SQLi)
- Monitor for drift between IaC and deployed config that introduces risk
- Generate threat models from architecture changes

### Inputs

| Input | Source |
|---|---|
| Veracode findings | Veracode REST API (Findings API v2) |
| Code Scanning alerts | GitHub Advanced Security API |
| Secret scanning alerts | GitHub API |
| Dependabot alerts | GitHub API |
| PR diff | GitHub MCP / `gh pr diff` |
| Vault audit log | Vault API |

### Outputs

- **PRs that remediate vulnerabilities** with citations to the alert and the CWE
- **Review comments on PRs** flagging risky patterns
- **Dismissals with justification** for false positives (logged for audit)
- **Slack/Teams alerts** for high-severity findings

### Required Tools

| Tool | Purpose |
|---|---|
| Veracode REST API | Fetch findings, attach mitigations |
| GitHub Advanced Security | Read code scanning + secret alerts |
| MCP custom tools | Wrap Veracode SDK as agent-callable tool |
| Vault API | Read secret leases for credential rotation tasks |
| Static analysis (semgrep, gitleaks) | Pre-flight scan before opening PR |

### Security Considerations & Governance

- **The agent is a target.** A security agent has elevated visibility into vulnerabilities. Run it on isolated runners — never on shared GitHub-hosted runners.
- **Read-only by default.** Initial deployment: agent only reads + comments. Earn trust before granting write/PR-creation permissions.
- **No secret exfiltration risk.** Configure the [Copilot Cloud Agent firewall](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-firewall) to block all egress except the GitHub API, Veracode API, and your internal MCP server.
- **Pre-tool-use hooks.** Use [Copilot Cloud Agent hooks](https://docs.github.com/en/copilot/concepts/agents/coding-agent/about-hooks) to validate every tool call. Reject any call that would dismiss a critical Veracode finding without a documented justification.
- **Four-eyes for dismissals.** Agent can propose dismissing a false positive but a human security reviewer must approve.

### Architecture

```mermaid
flowchart LR
    subgraph SRC["Finding sources"]
        VC["Veracode SAST/SCA"]
        GHAS[GitHub Code Scanning]
        SS[Secret Scanning]
        DEP[Dependabot]
    end

    subgraph AGENT["Security Agent<br/>on ECS Fargate"]
        TRIAGE["Triage logic<br/>ReAct loop"]
        HOOKS["Pre/post-tool hooks<br/>policy enforcement"]
        TRIAGE -.-> HOOKS
    end

    subgraph TOOLS["MCP tool layer"]
        T1[get_veracode_findings]
        T2[get_github_alerts]
        T3[read_file]
        T4[propose_fix_pr]
        T5[dismiss_with_justification]
    end

    subgraph OUTCOMES["Actions"]
        PR[PR with fix]
        COMMENT["PR comment<br/>with risk note"]
        DISMISS["Justified dismissal<br/>logged to audit"]
        ALERT["Slack alert<br/>for SEV-1"]
    end

    SRC --> AGENT --> TOOLS --> OUTCOMES
    HOOKS -.->|blocks unsafe call| DISMISS
```

### How to Create One — Walkthrough: Our First Custom Agent

This is Approach B from the [Build vs Configure vs Custom Agent](#build-vs-configure-vs-custom-agent--pick-the-right-approach) section. We're going to create a named custom agent called **`veracode-triage`** that specializes in triaging Veracode findings on PRs.

#### What you'll end up with

```mermaid
flowchart LR
    PR([Developer opens PR]) --> WF[Actions workflow<br/>runs Veracode scan]
    WF --> ART[SARIF artifact<br/>uploaded to PR]
    WF -->|"@veracode-triage<br/>please review"| AGENT[Custom agent<br/>veracode-triage]

    AGENT -->|reads| MCP[MCP servers]
    MCP --> VC[Veracode MCP<br/>finding details]
    MCP --> GHM[GitHub MCP<br/>PR diff + files]

    AGENT --> OUT1[PR review comments<br/>per finding]
    AGENT --> OUT2[Fix PR if<br/>auto-fixable]
    AGENT --> OUT3[Escalation issue<br/>for SEV-1]
```

#### Step 1 — Define the custom agent

Create the file `.github/agents/veracode-triage.md` in your repo:

```markdown
---
name: veracode-triage
description: Triages Veracode SAST and SCA findings on PRs. Posts review comments with CWE-prioritized remediation guidance and opens fix PRs for low-risk findings.
tools:
  - github
  - veracode
model: claude-sonnet-4-5
---

# Veracode Triage Agent

You are a security specialist focused exclusively on triaging Veracode findings.
You do NOT review code for general quality, style, or architecture — only security.

## Inputs available to you

- The current PR diff (via GitHub MCP)
- The Veracode scan results attached as `pipeline-scan-results.json` artifact
- Repository files referenced by findings (via GitHub MCP)
- Historical findings on this repo (via Veracode MCP `get_application_findings`)

## Your task

For each Veracode finding with severity MEDIUM or higher, do the following:

1. **Verify it is a true positive.**
   - Read the affected code (`file:line` from finding)
   - Cross-reference with the PR diff to confirm the new code introduces the issue
   - If you cannot confirm — post a comment asking for clarification rather than guessing

2. **Classify it.**
   - Map to CWE (the finding includes this)
   - Prioritization order: CWE-119/787 (memory safety) > CWE-89/79 (injection) > CWE-287/863 (auth) > others
   - For SCA findings: check `transitive: false` first; transitive dependencies are usually lower priority

3. **Post a review comment** in this exact format:

   ```
   **[CWE-XXX] SEVERITY** — Brief finding name

   **Where:** `path/to/file.java:42`

   **What:** One-sentence explanation of the vulnerability.

   **Fix:** Concrete code change (use a fenced code block).

   **Reference:** Link to Veracode finding ID.
   ```

4. **If the fix is < 30 lines AND well-understood**, also open a PR with the fix branched from this PR's branch.

5. **For any CRITICAL finding**, additionally open an issue with:
   - Label: `security-critical`
   - Assignee: `@security-team`
   - Title prefix: `[SEV-1]`
   - Do NOT proceed with auto-fixing; humans must review first.

## Hard constraints

- **NEVER dismiss a finding as a false positive.** Only humans can dismiss. If you believe a finding is a false positive, post a comment explaining your reasoning and tag `@security-team`.
- **NEVER modify `.github/workflows/` files** as part of a fix.
- **NEVER touch files in `vendor/` or `node_modules/`** — these are vendored dependencies; address them via dependency updates instead.
- **Always cite the exact Veracode finding ID** in every comment for traceability.
- **One comment per finding.** Do not batch multiple findings into a single comment.

## Conventions for our stack

- Java 17 + Spring Boot 3.x
- Use parameterized queries via Spring `JdbcTemplate` for any SQL fixes
- Use `org.owasp.encoder.Encode` for output encoding fixes
- Secrets in code → always direct to Vault: `vault.read("secret/data/<service>/...")`
- Never suggest disabling a Veracode policy as a fix
```

The frontmatter (between `---`) tells GitHub how the agent is registered. The body becomes the agent's system prompt.

#### Step 2 — Configure the Veracode MCP server

The agent needs a way to call the Veracode API. We do this via an **MCP server**. Create the org-level MCP config at `.github/copilot/mcp_config.json`:

```json
{
  "mcpServers": {
    "veracode": {
      "type": "http",
      "url": "https://your-internal-mcp-gateway.company.com/veracode/mcp",
      "headers": {
        "Authorization": "Bearer ${VERACODE_MCP_TOKEN}"
      },
      "tools": [
        "get_application_findings",
        "get_finding_details",
        "get_sandbox_scan_results"
      ]
    }
  }
}
```

> **Note on the Veracode MCP server itself:** as of this writing, Veracode does not publish an official MCP server. You have two options:
>
> 1. **Run a community MCP server** — wraps the Veracode REST API. Examples exist on GitHub; deploy on AKS/ECS behind your network.
> 2. **Build a thin one yourself** — ~200 lines of Python using the `fastmcp` library wrapping `requests` calls to the Veracode REST API. The official Veracode REST API docs are at `https://docs.veracode.com/r/c_rest_landing`.
>
> Either way, the MCP server runs in *your* infrastructure (on ECS Fargate) so Veracode credentials never leave your environment — the agent just calls your gateway.

#### Step 3 — Restrict the agent's network egress

By default Copilot Cloud Agent can call any HTTPS endpoint. For a security agent, lock this down using the [agent firewall](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-firewall). Edit your org's Copilot policy to add an egress allow-list:

```yaml
# In GitHub.com → Organization settings → Copilot → Coding agent
allowed_endpoints:
  - api.github.com                              # GitHub MCP
  - your-internal-mcp-gateway.company.com       # Your Veracode MCP
  - api.anthropic.com                           # Model provider
# Block everything else
```

This means even if the agent is prompt-injected to exfiltrate data, it cannot reach an attacker-controlled endpoint.

#### Step 4 — Add a pre-tool-use hook for governance

We don't want the agent dismissing findings autonomously, even if our system prompt says so. Belt-and-braces: add a [pre-tool-use hook](https://docs.github.com/en/copilot/concepts/agents/coding-agent/about-hooks) that blocks any call to a `dismiss_*` tool:

```yaml
# .github/copilot/hooks.yml
hooks:
  pre_tool_use:
    - name: block-dismissals
      match:
        tool_name: ".*dismiss.*"
      action: deny
      message: |
        Dismissing security findings is not permitted for autonomous agents.
        Post a comment with your reasoning and tag @security-team instead.
```

The hook fires before any tool call matching the pattern. The agent receives the `deny` response and must adapt — exactly the kind of guardrail you want for high-risk agents.

#### Step 5 — Update the workflow to invoke the named agent

This replaces the generic `@copilot` mention with our specialized agent:

```yaml
# .github/workflows/pr-security-review.yml — invoke step only
- name: Assign findings to veracode-triage agent
  env:
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  run: |
    gh pr comment ${{ github.event.pull_request.number }} \
      --body "@veracode-triage please review the Veracode findings in the latest scan.
      The SARIF report is attached as workflow artifact 'pipeline-scan-results.json'
      (run ID: ${{ github.run_id }})."
```

#### Step 6 — Verify it works

A simple smoke test before deploying to all repos:

1. Open a PR with a deliberate vulnerability (e.g., a `String.format("SELECT * FROM users WHERE id = " + userId)` snippet)
2. The PR workflow runs Veracode scan → posts the comment invoking `@veracode-triage`
3. Within ~3 minutes, the agent should post a review comment with the SQL injection finding and a parameterized-query fix
4. Verify in the GitHub audit log under `copilot.session_started` events that the agent was invoked correctly

#### Step 7 — Roll out and monitor

Once it works in one repo, promote the agent definition to the **org level** so all repos can use it:

```bash
# Move from repo-level to org-level
gh api -X PUT /orgs/YOUR-ORG/copilot/agents/veracode-triage \
  --input .github/agents/veracode-triage.md
```

Then monitor:

```mermaid
flowchart LR
    LOG[Copilot audit log<br/>copilot.* events] --> SIEM
    AGENT[veracode-triage<br/>agent runs] --> METRICS[Metrics:<br/>findings triaged · time to comment · false positive rate]
    METRICS --> DASH[Grafana dashboard]
    SIEM --> ALERT[Alert on:<br/>dismissals attempted · firewall denials · cost spikes]
```

#### What you'll learn from this first agent

Three things worth instrumenting from day one:

| Metric | Why it matters |
|---|---|
| **Triage latency** — time from finding to comment | Below 5 min = developers stay in flow; above 30 min = they ignore findings |
| **Comment quality score** — sampled by security team weekly | Bad comments train developers to ignore the agent. Aim for ≥ 80% useful |
| **Hook denial rate** — how often guardrails block the agent | Rising = your prompts are drifting; revisit instructions |

These three numbers tell you whether the agent is earning its keep. **Pick one pilot repo, run it for two weeks, then decide whether to roll out broader.**

---

### How to Use This Agent

**In VS Code:**

```
@veracode-triage Review the open Veracode findings for severity ≥ medium on this branch.
For each true positive that you can fix in <50 lines, open a PR with the fix.
For others, post a comment with: CWE, file:line, suggested remediation, and risk score.
```

**In CI/CD — every PR triggers a security review:**

```yaml
# .github/workflows/pr-security-review.yml
name: Security agent PR review

on:
  pull_request:
    types: [opened, synchronize, ready_for_review]

permissions:
  contents: read
  pull-requests: write
  security-events: read
  id-token: write

jobs:
  veracode-scan:
    runs-on: [self-hosted, codebuild]
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS via OIDC
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/github-actions-security-agent
          aws-region: us-east-1

      - name: Get Veracode API creds from Vault
        uses: hashicorp/vault-action@v3
        with:
          url: ${{ vars.VAULT_ADDR }}
          method: aws
          role: security-agent
          secrets: |
            secret/data/veracode api_id | VERACODE_API_ID ;
            secret/data/veracode api_key | VERACODE_API_KEY

      - name: Run Veracode pipeline scan
        uses: veracode/Veracode-pipeline-scan-action@v1.0.16
        with:
          vid: ${{ env.VERACODE_API_ID }}
          vkey: ${{ env.VERACODE_API_KEY }}
          file: ./build/libs/app.jar
          fail_build: false

      - name: Assign findings to veracode-triage agent
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh pr comment ${{ github.event.pull_request.number }} \
            --body "@veracode-triage please review the Veracode findings in the latest scan.
            The SARIF report is attached as workflow artifact 'pipeline-scan-results.json'
            (run ID: ${{ github.run_id }})."
```

---

## 3. Incident Response Agents

### Responsibilities

- First-responder to alerts (PagerDuty, CloudWatch alarms, Datadog incidents)
- Run safe diagnostic commands (read logs, describe pods, query CloudWatch metrics)
- Correlate the incident with recent deploys, config changes, and infra drift
- Draft incident timeline and root cause hypothesis
- Page humans with summarized context — no autonomous remediation in prod

### Inputs

| Input | Source |
|---|---|
| Alert payload | PagerDuty / CloudWatch / Datadog webhook |
| Recent deploys | GitHub Deployments API |
| Recent merged PRs | GitHub API |
| Runtime logs | CloudWatch Logs Insights |
| Metrics | CloudWatch / Datadog API |
| Infra state | Terraform state, AWS APIs (read-only) |

### Outputs

- **Incident summary** posted to Slack incident channel
- **Timeline document** with correlated events
- **Hypothesis ranking** with confidence and evidence
- **Suggested next actions** — never executed without human approval

### Required Tools

| Tool | Purpose |
|---|---|
| AWS read-only IAM role | Describe resources, read CloudWatch |
| MCP custom tools | `query_cloudwatch_logs`, `get_recent_deploys`, `describe_ecs_service` |
| Slack API | Post incident updates to channel |
| GitHub API | Correlate with merged PRs |

### Security Considerations & Governance

- **Read-only is non-negotiable.** Incident response agents never get write permissions to production. Period.
- **Human-in-the-loop for any action.** The agent's output is *advice* to the on-call engineer, not a runbook executor.
- **Time-boxed runs.** Configure a max execution time (15 minutes) and a max iteration count (20 tool calls). Runaway agents during incidents are a real risk.
- **Sensitive data scrubbing.** Pre-output hooks scan agent responses for PII, customer data, secrets — redact before posting to Slack.
- **Separate identity.** The IR agent has its own AWS IAM role, separate from the deploy agent's role, separate from the security agent's role. Least privilege per agent.

### Architecture

```mermaid
sequenceDiagram
    participant ALERT as 🚨 PagerDuty
    participant IR as IR Agent (ECS Fargate)
    participant AWS as AWS APIs (read-only role)
    participant GH as GitHub API
    participant SLACK as Slack #incidents
    participant ONCALL as 👤 On-call engineer

    ALERT->>IR: Webhook: api-prod 5xx > 5%
    IR->>AWS: Get last 30 ECS deploys
    AWS-->>IR: deploy-789 at T-14min
    IR->>GH: Get PRs merged 0-30min ago
    GH-->>IR: PR #4521 (auth refactor)
    IR->>AWS: Query CloudWatch logs for auth errors
    AWS-->>IR: 412 errors spiking after deploy
    IR->>IR: Build hypothesis ranking
    IR->>SLACK: Post incident summary +<br/>top-3 hypotheses +<br/>suggested rollback command
    SLACK->>ONCALL: 📱 Notification
    ONCALL->>SLACK: Reviews hypothesis
    ONCALL->>AWS: Executes rollback (manually)

    Note over IR: Agent does NOT execute remediation
```

### How to Use This Agent

**In VS Code** (post-incident, for the writeup):

```
@copilot Generate a post-incident review document for incident INC-2024-0421.
Pull the Slack timeline from #incidents, correlate with deploys from GitHub,
and produce a doc with: timeline, root cause, contributing factors, action items.
```

**As a long-running ECS service** — agent listens to a webhook endpoint:

```yaml
# .github/workflows/deploy-ir-agent-to-ecs.yml
name: Deploy IR agent to ECS

on:
  push:
    branches: [main]
    paths: ['agents/incident-response/**']

permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: [self-hosted, codebuild]
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS via OIDC
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/github-actions-deploy
          aws-region: us-east-1

      - name: Build agent image
        run: |
          docker build -t $ECR_REPO:${{ github.sha }} agents/incident-response
          aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_REPO
          docker push $ECR_REPO:${{ github.sha }}

      - name: Update ECS service
        run: |
          aws ecs update-service \
            --cluster devops-agents \
            --service ir-agent \
            --task-definition ir-agent:${{ github.sha }} \
            --force-new-deployment
```

The ECS task definition pins the agent's IAM role to **read-only** scopes:

```json
{
  "family": "ir-agent",
  "taskRoleArn": "arn:aws:iam::ACCOUNT:role/ir-agent-readonly",
  "executionRoleArn": "arn:aws:iam::ACCOUNT:role/ecs-task-execution",
  "containerDefinitions": [{
    "name": "ir-agent",
    "image": "ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/ir-agent:latest",
    "secrets": [
      { "name": "SLACK_TOKEN", "valueFrom": "arn:aws:secretsmanager:...:secret:slack-token" },
      { "name": "ANTHROPIC_API_KEY", "valueFrom": "arn:aws:secretsmanager:...:secret:claude-key" }
    ],
    "environment": [
      { "name": "MAX_ITERATIONS", "value": "20" },
      { "name": "MAX_RUNTIME_SECONDS", "value": "900" }
    ]
  }]
}
```

---

## 4. Infrastructure Provisioning Agents

### Responsibilities

- Generate Terraform / CDK modules from natural-language requests
- Review IaC PRs for security, cost, and standards compliance
- Detect and reconcile drift between Terraform state and AWS reality
- Manage Vault policies and AppRole roles for new services
- Maintain golden-path module library

### Inputs

| Input | Source |
|---|---|
| Service request | GitHub Issue (templated form) |
| Existing modules | Internal Terraform module registry |
| AWS state | `terraform plan` output |
| Cost estimates | Infracost API |
| Policy as code | OPA / Conftest policies |

### Outputs

- **PRs to infrastructure repos** with new modules, version bumps, drift fixes
- **Plan summaries** posted as PR comments
- **Cost diff comments** showing impact of changes
- **Vault policy updates** for service identities

### Required Tools

| Tool | Purpose |
|---|---|
| Terraform CLI | `init`, `plan`, `validate` only (never `apply`) |
| Infracost | Cost estimation |
| Conftest / OPA | Policy validation |
| Vault CLI | Read existing policies; propose changes |
| AWS read-only role | Describe existing resources |

### Security Considerations & Governance

- **Plan-only, never apply.** The agent generates Terraform code and runs `plan`, but never executes `apply`. A protected GitHub Actions workflow with manual approval handles `apply`.
- **OIDC-only for AWS.** The plan runs under a tightly-scoped read-only OIDC role. Apply requires a separate role gated by environment protection rules.
- **Cost guardrails.** Any plan increasing monthly cost above a threshold (e.g., $500) requires FinOps approval — automated check before merge.
- **Module pinning.** Agent must use only modules from your approved registry; this is enforced by a pre-tool-use hook.
- **Vault policy diff in PR.** Any Vault policy change is shown as a clear diff for human review.

### Architecture

```mermaid
flowchart TD
    REQ(["Service team submits issue<br/>'I need a new RDS for service X'"])

    REQ --> AGENT["Infra Agent<br/>researches + generates"]

    subgraph PLAN["Plan phase — automated"]
        AGENT --> TF1[terraform init + plan]
        AGENT --> COST[infracost diff]
        AGENT --> POLICY[conftest verify]
        AGENT --> VAULT[vault policy preview]
    end

    PLAN --> PR["PR opened<br/>plan + cost + policy attached"]

    PR --> REV{Human review}
    REV -->|approve| APPLY["Protected workflow<br/>terraform apply"]
    REV -->|reject| FB[Feedback to agent]
    FB --> AGENT

    APPLY --> PROD([Resources provisioned])

    style APPLY fill:#fee2e2,stroke:#ef4444,color:#7f1d1d
```

### How to Use This Agent

**In VS Code:**

```
@copilot Create a Terraform module for a new ECS Fargate service called 'orders-api'.
Use our standard module from internal-tf-modules/ecs-service v3.1.
Requirements:
- 2 vCPU, 4GB memory
- ALB target group with health check at /health
- IAM task role with read access to dynamodb table orders-{env}
- Vault AppRole for the service to fetch DB credentials
Run terraform plan and infracost diff before opening the PR.
```

**In CI/CD — drift detection on a schedule:**

```yaml
# .github/workflows/drift-detection.yml
name: Infra drift detection

on:
  schedule:
    - cron: '0 6 * * 1-5'  # weekdays 6 AM UTC
  workflow_dispatch:

permissions:
  id-token: write
  contents: read
  issues: write

jobs:
  detect-drift:
    runs-on: [self-hosted, codebuild]
    strategy:
      matrix:
        environment: [dev, staging, prod]
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS via OIDC (read-only)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/tf-plan-${{ matrix.environment }}
          aws-region: us-east-1

      - name: Terraform plan
        id: plan
        run: |
          cd terraform/${{ matrix.environment }}
          terraform init
          terraform plan -detailed-exitcode -out=tfplan
        continue-on-error: true

      - name: File issue if drift detected
        if: steps.plan.outcome == 'failure'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          terraform show -no-color tfplan > plan.txt
          gh issue create \
            --title "[drift] ${{ matrix.environment }} environment has unmanaged changes" \
            --body "@copilot Drift detected in ${{ matrix.environment }}. Plan output attached.
            Investigate the cause, then open a PR that either:
            (a) updates the Terraform code to match reality (if change was intentional), or
            (b) provides a remediation plan to revert (if change was unauthorized).
            Cite the AWS resource ARN(s) in question." \
            --label "drift,infra,${{ matrix.environment }}"
```

---

## 5. Deployment Agents

### Responsibilities

- Orchestrate deployments across environments with environment-specific gates
- Run pre-deployment safety checks (config diff, migration safety, dependency health)
- Execute canary and blue/green deployments to ECS
- Monitor post-deploy SLIs for the bake window
- Trigger rollback if SLO breach is detected

### Inputs

| Input | Source |
|---|---|
| Build artifact | ECR image tag |
| Deploy intent | GitHub Deployment event |
| SLO thresholds | Datadog / CloudWatch SLO config |
| Migration scripts | Repo (with `safe-migration` label) |
| Environment config | AWS SSM Parameter Store |

### Outputs

- **GitHub Deployment status updates** through each phase
- **Pre-flight check report** — config diff, migration analysis, image vulnerability scan results
- **Canary results** — SLI deltas during canary window
- **Rollback execution** with full audit trail

### Required Tools

| Tool | Purpose |
|---|---|
| AWS ECS API | Update service, manage task definitions |
| GitHub Deployments API | Status updates |
| CloudWatch / Datadog API | Read SLI/SLO metrics |
| Veracode | Verify image scan status before deploy |
| Vault | Issue short-lived deploy tokens |

### Security Considerations & Governance

- **Production = mandatory human approval.** GitHub Environments with required reviewers. The agent prepares and proposes; a human clicks "approve."
- **Image attestation.** Only deploy images signed with cosign and with a passing Veracode scan in the last 7 days.
- **Time-window restriction.** Deploys to prod only allowed within defined change windows (configured via OPA policy at the workflow level).
- **Rollback authority is scoped.** The agent can roll back to the previous known-good task definition automatically *only if* SLO breach exceeds defined thresholds and the deploy is in its canary window.
- **Migration safety check.** For any deploy that includes a DB migration, the agent must verify the migration is backward-compatible (using a `safe-migration` annotation and automated checks like `migra` or `pt-online-schema-change` dry-run).

### Architecture

```mermaid
flowchart TD
    PR([PR merged to main]) --> BUILD["Build + scan<br/>Veracode"]
    BUILD -->|✅ passing| PUSH["Push image<br/>to ECR"]
    PUSH --> AGENT[Deployment Agent]

    AGENT --> PRE[Pre-flight checks]
    PRE --> CFGDIFF[Config diff]
    PRE --> MIGSAFE[Migration safety]
    PRE --> IMGSCAN[Image scan recent?]

    PRE --> ENV{Target env?}

    ENV -->|dev / staging| AUTODEPLOY["Auto-deploy<br/>ECS rolling update"]
    ENV -->|prod| APPROVE["GitHub Environment<br/>approval gate 👤"]

    APPROVE -->|approved| CANARY["Canary deploy<br/>10% traffic"]

    CANARY --> WATCH["Watch SLIs<br/>10 min bake"]
    WATCH -->|✅ within SLO| PROMOTE[Promote 100%]
    WATCH -->|❌ SLO breach| ROLLBACK["Auto-rollback<br/>prev task def"]

    PROMOTE --> DONE([Deploy complete])
    ROLLBACK --> ALERT["Alert + open PR<br/>with bug report"]

    style APPROVE fill:#fef3c7,stroke:#f59e0b,color:#78350f
    style ROLLBACK fill:#fee2e2,stroke:#ef4444,color:#7f1d1d
```

### How to Use This Agent

**In VS Code:**

```
@copilot Prepare a release of orders-api v2.4.0 to staging.
- Compare config between current staging and proposed v2.4.0
- Check if any DB migrations are present and verify they are backward-compatible
- Open the deploy PR with the pre-flight summary
- Do NOT initiate the actual deploy — just prepare it
```

**In CI/CD — canary deploy to prod via GitHub Environments:**

```yaml
# .github/workflows/deploy-prod-canary.yml
name: Deploy to prod (canary)

on:
  workflow_dispatch:
    inputs:
      image_tag:
        required: true
      service:
        required: true

permissions:
  id-token: write
  contents: read
  deployments: write

jobs:
  preflight:
    runs-on: [self-hosted, codebuild]
    outputs:
      can_deploy: ${{ steps.checks.outputs.ok }}
    steps:
      - uses: actions/checkout@v4
      - name: Configure AWS (read-only)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/preflight-readonly
          aws-region: us-east-1
      - id: checks
        run: |
          # Verify image has recent passing Veracode scan
          # Verify cosign signature
          # Verify migration safety
          # Verify no incident in progress
          ./scripts/preflight.sh \
            --service ${{ inputs.service }} \
            --image ${{ inputs.image_tag }}
          echo "ok=true" >> $GITHUB_OUTPUT

  canary:
    needs: preflight
    if: needs.preflight.outputs.can_deploy == 'true'
    runs-on: [self-hosted, codebuild]
    environment:
      name: production            # ← Required reviewers configured here
      url: https://api.example.com
    steps:
      - name: Configure AWS via OIDC
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/ecs-deploy-prod
          aws-region: us-east-1

      - name: Canary 10%
        run: |
          aws ecs update-service \
            --cluster prod \
            --service ${{ inputs.service }} \
            --task-definition ${{ inputs.service }}:${{ inputs.image_tag }} \
            --deployment-configuration "maximumPercent=110,minimumHealthyPercent=90"

      - name: Bake — monitor SLIs for 10 min
        run: ./scripts/watch-slis.sh --service ${{ inputs.service }} --duration 600s

      - name: Promote
        run: ./scripts/promote-canary.sh --service ${{ inputs.service }}
```

---

## 6. Compliance Agents

### Responsibilities

- Continuously verify control evidence (SOC 2, ISO 27001, PCI, HIPAA) against running systems
- Generate evidence packages for audits (logs, configs, screenshots, attestations)
- Detect control drift (e.g., MFA disabled on an IAM user)
- Map findings to specific compliance frameworks and controls
- Maintain control-to-artifact mapping in code

### Inputs

| Input | Source |
|---|---|
| Control catalog | YAML in compliance repo |
| AWS Config rules | AWS Config API |
| GitHub org settings | GitHub Enterprise API |
| Vault audit log | Vault API |
| Veracode policy results | Veracode API |
| Access logs | CloudTrail |

### Outputs

- **Evidence repository updates** — auto-generated attestations with timestamps and links
- **Compliance dashboard** showing control status (green/yellow/red)
- **Issues for control failures** assigned to the responsible team
- **Audit-ready PDF / markdown reports** generated on demand

### Required Tools

| Tool | Purpose |
|---|---|
| AWS Config / Security Hub | Read control state |
| CloudTrail Lake | Query access patterns |
| GitHub Enterprise Audit Log API | Verify access controls |
| Veracode | Verify SAST policy compliance |
| Vault audit log | Track secret access |

### Security Considerations & Governance

- **Read-only across the board.** Compliance agents observe and report, never remediate. Remediation is a separate workflow (often handled by infra or security agents under approval).
- **Tamper-evident evidence.** All evidence artifacts written to an S3 bucket with object lock and versioning. Agent can write-once but not modify.
- **Separation of duties.** The compliance agent's role must not be assumable by deploy/infra agents — auditors require independence.
- **PII redaction at source.** Logs queried for compliance evidence must pass through a PII scrubber before storage.
- **Versioned control catalog.** The control-to-artifact mapping lives in a Git repo with strict review; changes require security + compliance approval.

### Architecture

```mermaid
flowchart LR
    subgraph CAT["Control catalog repo"]
        CTL["controls/soc2.yaml<br/>controls/iso27001.yaml<br/>mapping.yaml"]
    end

    subgraph AGENT["Compliance Agent<br/>on ECS Fargate"]
        SCH[Scheduler]
        VER[Verifier]
        REP[Reporter]
        SCH --> VER --> REP
    end

    subgraph SRC["Evidence sources"]
        ACFG[AWS Config]
        CT[CloudTrail]
        GAUD[GitHub Audit Log]
        VAUD[Vault Audit Log]
        VC2[Veracode results]
    end

    subgraph OUT["Outputs"]
        S3["S3 evidence bucket<br/>object-locked"]
        DASH["Compliance dashboard<br/>Grafana"]
        ISS["GitHub Issues<br/>for failed controls"]
        RPT["Audit PDF<br/>on-demand"]
    end

    CAT --> AGENT
    SRC --> AGENT
    AGENT --> OUT
```

### How to Use This Agent

**In VS Code:**

```
@copilot Generate the evidence package for SOC 2 CC6.1 (logical access controls)
for the period 2024-01-01 to 2024-03-31. Include:
- All IAM users + their MFA status
- All exceptions and their approvals
- Sample access reviews from the period
Output as a markdown report with links to S3 artifacts.
```

**In CI/CD — continuous compliance checks:**

```yaml
# .github/workflows/compliance-check.yml
name: Continuous compliance verification

on:
  schedule:
    - cron: '0 */4 * * *'  # every 4 hours
  workflow_dispatch:

permissions:
  id-token: write
  contents: read
  issues: write

jobs:
  verify:
    runs-on: [self-hosted, codebuild]
    strategy:
      matrix:
        framework: [soc2, iso27001, pci-dss]
    steps:
      - uses: actions/checkout@v4

      - name: Configure AWS via OIDC (read-only audit role)
        uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::${{ vars.AWS_ACCOUNT_ID }}:role/compliance-readonly
          aws-region: us-east-1

      - name: Run control verification
        id: verify
        run: |
          ./bin/compliance-agent verify \
            --framework ${{ matrix.framework }} \
            --output-dir ./evidence/${{ matrix.framework }}/$(date +%Y-%m-%d-%H)

      - name: Upload evidence to S3 (object-locked)
        run: |
          aws s3 sync ./evidence/${{ matrix.framework }} \
            s3://compliance-evidence-prod/${{ matrix.framework }}/ \
            --metadata-directive REPLACE \
            --metadata "generated-by=compliance-agent,run-id=${{ github.run_id }}"

      - name: Open issues for control failures
        if: steps.verify.outputs.failed_controls != ''
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          for control in $(echo "${{ steps.verify.outputs.failed_controls }}" | jq -r '.[]'); do
            gh issue create \
              --title "[compliance/${{ matrix.framework }}] Control failure: $control" \
              --body "@copilot Investigate control failure for $control. Do NOT remediate — produce a root cause analysis and tag the responsible team." \
              --label "compliance,${{ matrix.framework }}" \
              --assignee compliance-team
          done
```

---

## Reference Architecture — Multi-Agent DevOps Platform on AWS

This is how all six agents fit together in production.

```mermaid
flowchart TB
    subgraph HUMAN["👤 Human Interfaces"]
        VSC["VS Code<br/>+ Copilot agent mode"]
        BROWSER["github.com<br/>Issues + PRs"]
        SLACK["Slack /<br/>MS Teams"]
    end

    subgraph GH["🐙 GitHub EMU"]
        CCA["Copilot Cloud Agent<br/>for code-centric tasks"]
        REPOS["Repos / Issues / PRs"]
        ACT[GitHub Actions]
        MCP[GitHub MCP Server]
        AUDIT["Audit log<br/>→ SIEM"]
    end

    subgraph IDENT["🔑 Identity & Secrets"]
        OIDC["OIDC trust<br/>GitHub → AWS"]
        IAM["IAM roles per agent<br/>least privilege"]
        VAULT["Vault on EC2<br/>AppRole + AWS auth"]
        SM[Secrets Manager]
    end

    subgraph RUNNERS["⚙️ Compute"]
        CB["Self-hosted runners<br/>on AWS CodeBuild<br/>for GHA workloads"]
        ECS["ECS Fargate<br/>for long-running agents"]
    end

    subgraph AGENTS["🤖 Agent Fleet"]
        CICD["CI/CD Agent"]
        SEC[Security Agent]
        IR["Incident Response<br/>Agent"]
        INFRA[Infra Agent]
        DEP[Deployment Agent]
        COMP[Compliance Agent]
    end

    subgraph TOOLS["🔧 Tool Integration"]
        VC[Veracode API]
        AWSAPI[AWS APIs]
        DD["Datadog /<br/>CloudWatch"]
        PD[PagerDuty]
        TF[Terraform]
    end

    subgraph OBS["📊 Observability"]
        XR[X-Ray traces]
        CW["CloudWatch<br/>Logs + Metrics"]
        LF["Langfuse<br/>LLM traces"]
    end

    HUMAN --> GH
    GH --> CCA
    CCA --> MCP --> AGENTS

    REPOS --> ACT
    ACT --> CB
    ACT --> OIDC --> IAM

    ECS --> AGENTS

    AGENTS --> TOOLS
    AGENTS --> VAULT
    AGENTS --> SM

    AGENTS --> OBS
    AUDIT --> OBS
```

---

## Governance Cheat Sheet

| Agent | Write to prod? | Auto-merge PRs? | Needs HITL? | Where it runs |
|---|---|---|---|---|
| CI/CD agent | No | No | Branch protection rules suffice | Copilot Cloud Agent + GHA |
| Security agent | No | No, opens PR only | Yes for dismissals | Copilot Cloud Agent + ECS |
| Incident response | Read-only | N/A | Always — proposes only | ECS Fargate |
| Infrastructure | Plan-only | No, GHA Environment gates apply | Yes for apply | GHA + ECS |
| Deployment | Yes, gated | No | Yes for prod | GHA with Environments |
| Compliance | Read-only | N/A | Findings → human triage | ECS Fargate |

### Universal rules

1. **Every agent has its own AWS IAM role.** No shared roles between agent types.
2. **OIDC over long-lived tokens.** Always. Bind `sub` claim to `repo:org/name:environment:env-name`.
3. **MCP firewall is configured.** Default-deny egress; allow only the specific APIs the agent needs.
4. **Pre-tool-use hooks enforce policy.** Reject destructive calls at the gateway, not by hoping the model behaves.
5. **Cost caps are hard limits.** Per-agent monthly token budget; hard-stop on breach with PagerDuty alert.
6. **All agent traces stream to SIEM.** Reasoning + tool calls + outcomes. Compliance and forensics need this.
7. **Treat agent prompts as code.** Version-controlled, peer-reviewed, eval-tested.

---

## Getting Started — 30-Day Adoption Plan

```mermaid
flowchart LR
    subgraph W1["Week 1 — Foundation"]
        W1A["Enable Copilot Cloud Agent<br/>in EMU for one pilot repo"]
        W1B["Configure OIDC trust<br/>GitHub ↔ AWS"]
        W1C["Define agent IAM roles<br/>least-privilege per type"]
    end

    subgraph W2["Week 2 — First agent: CI/CD"]
        W2A["Deploy CI/CD agent<br/>via Copilot Cloud Agent"]
        W2B["Auto-triage workflow<br/>on failed builds"]
        W2C["Measure: PR turnaround<br/>+ flake rate"]
    end

    subgraph W3["Week 3 — Security agent"]
        W3A["Veracode + Copilot<br/>integration via MCP"]
        W3B[PR review agent<br/>(read-only first)]
        W3C["Configure firewall<br/>+ pre-tool-use hooks"]
    end

    subgraph W4["Week 4 — Scale + govern"]
        W4A["Deploy compliance agent<br/>to ECS"]
        W4B["Stream all agent traces<br/>to SIEM"]
        W4C["Define agent registry<br/>+ ownership"]
    end

    W1 --> W2 --> W3 --> W4
```

### Concrete starting tasks

1. **Pick one repo.** Don't roll out to the whole org. A team that owns one service and is enthusiastic.
2. **Start with CI/CD agent.** Lowest risk, fastest visible value. Auto-triage of failed builds.
3. **Read-only first.** Every agent gets read-only AWS permissions for week one. Earn write access through demonstrated reliability.
4. **Instrument from day one.** Stream Copilot Cloud Agent audit events, GitHub Actions logs, and ECS task logs to your SIEM before granting any write permissions.
5. **Define an exit ramp.** Document how to disable each agent (single command), revoke its tokens, and surface this in your runbook.

---

## Further Reading

- [About GitHub Copilot Cloud Agent](https://docs.github.com/en/copilot/concepts/agents/coding-agent/about-coding-agent)
- [Customize the Copilot agent firewall](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-firewall)
- [Extending the Copilot agent with MCP](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/extend-coding-agent-with-mcp)
- [Configuring OpenID Connect in AWS](https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services)
- [Veracode pipeline scan action](https://github.com/veracode/Veracode-pipeline-scan-action)
- [HashiCorp vault-action](https://github.com/hashicorp/vault-action)
- [AWS configure-aws-credentials action](https://github.com/aws-actions/configure-aws-credentials)

---

*Generated with Claude — DevOps and Platform Engineering AI agent curriculum.*
