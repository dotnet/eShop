---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, reads Sprint data and completed story points automatically. Falls back to manually entered data. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Analyses the team's Sprint velocity over recent Sprints to identify trends, variability, and planning implications — producing a velocity chart summary, average, and guidance for the next Sprint commitment. Use when a Scrum Master or team says things like "review our velocity", "what is our velocity", "how consistent are we", "velocity trend", "are we getting faster or slower", "how many points should we commit to", or "check our historical velocity". Do not use for capacity planning — use sm-capacity-planning for that. Do not use for individual sprint audits.
license: MIT
metadata:
    ceremony: Sprint Planning
    github-path: skills/sm-velocity-review
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: f340a0ad98cb19c8dca087e0dc300bda698dc247
    perspective: Scrum Master / Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: sm-velocity-review
---
# Velocity Review

## Purpose

Velocity is not a performance metric — it is a forecasting tool. A team with stable velocity can make reliable Sprint commitments and give stakeholders honest delivery timelines. A team with erratic velocity has a signal worth investigating: scope changes, underestimation, team instability, or interruptions. This skill surfaces the pattern so the Scrum Master and team can act on it.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If neither → `$PM_TOOL = manual`

---

## Step 1 — Collect Sprint history

Ask: *"How many Sprints should I look back? The last 5–8 is typically enough to identify a meaningful trend."*

Default: last **6 Sprints**.

- **ADO:** Before calling any iteration or team tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Only then call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to list recent iterations. Never call this tool with null values for either parameter.
  4. Then call `wit_get_work_items_for_iteration` for each iteration, filtering to Done/Closed state. Sum story points.
- **Jira:** use sprint reporting tools to retrieve completed story points per sprint.
- **Manual:** ask *"For each of the last [N] Sprints, provide: Sprint name/number, total story points committed, and total story points completed."*

For each Sprint, record:
- Sprint name and dates
- Points committed at Sprint start
- Points completed (Done by Sprint end)
- Team size (if known — helps normalise for team changes)
- Any notable context: holidays, team member absence, unplanned work, scope additions mid-Sprint

---

## Step 2 — Calculate the metrics

**Average velocity:**
```
Average = sum of completed points across all Sprints ÷ number of Sprints
```

**Predictability rate (commitment accuracy):**
```
Predictability = completed ÷ committed × 100, per Sprint
```
A rate consistently between 80–100% is healthy. Below 70% suggests chronic overcommitment. Above 110% suggests undercommitment or mid-Sprint scope additions.

**Velocity trend:**
Compare the rolling average of the first half of the window to the second half.
- Improving: later Sprints average higher than earlier ones
- Declining: later Sprints average lower
- Stable: within ±15% across the window

**Variability (standard deviation):**
```
High variability = std dev > 25% of the mean
Low variability = std dev < 15% of the mean
```
High variability means the team's capacity or scope is inconsistent — worth understanding why.

---

## Step 3 — Identify patterns and causes

For each notable data point (unusually high or low Sprint), note the context if available.

Common causes of velocity dips:
- Public holidays or team time off (expected — capacity planning accounts for this)
- Mid-Sprint scope additions (a process issue — discuss in retro)
- Team member joins or leaves (expected transition cost)
- Unplanned outages, incidents, or support load
- Stories carried over from a previous Sprint (inflates previous Sprint, deflates current)

Common causes of velocity spikes:
- Carryover stories completing early in the new Sprint
- Unusually simple Sprint content
- Overtime (not sustainable — flag if recurring)

---

## Step 4 — Present the velocity report

```
Velocity Review — [TEAM] — [DATE]

Sprints reviewed: [N] ([date range])

┌──────────────────────┬────────────┬───────────┬──────────────┐
│ Sprint               │ Committed  │ Completed │ Predictability│
├──────────────────────┼────────────┼───────────┼──────────────┤
│ [Sprint name]        │ [N] pts    │ [N] pts   │ [N]%         │
│ [Sprint name]        │ [N] pts    │ [N] pts   │ [N]%         │
│ [Sprint name]        │ [N] pts    │ [N] pts   │ [N]%         │
└──────────────────────┴────────────┴───────────┴──────────────┘

Average velocity (completed): [N] points
Velocity range: [min]–[max] points
Standard deviation: [N] points ([X]% of mean) — [Low / Moderate / High] variability
Trend: [Improving / Stable / Declining] — [one sentence explanation]

Average predictability: [N]% — [Healthy / Overcommitting / Undercommitting]

━━━  Recommended next Sprint commitment  ━━━━━━━━━━━━━━

Based on the last [N] Sprints:
  Conservative estimate: [average × 0.85] points
  Typical estimate:      [average] points
  Upper ceiling:         [average × 1.00] points

Adjust downward if next Sprint has known absences or a shorter schedule.
Use sm-capacity-planning for a developer-day based adjustment.

━━━  Notable patterns  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Sprint X]: [N] pts — [context note if available]
[Sprint Y]: [N] pts — [context note if available]

⚠ Flags:
  [Any flag worth raising to the team or PO]
```

---

## Step 5 — Coaching observations

Based on the data, surface one or two observations worth raising in the next Retrospective or with the team:

| Pattern | Observation |
|---|---|
| Consistent overcommitment | The team commits more than it delivers every Sprint. Sprint Planning estimates may be optimistic, or mid-Sprint interruptions are not being accounted for. |
| High variability with no clear cause | Velocity swings suggest the team's understanding of story size may be inconsistent. Consider a relative estimation calibration exercise. |
| Declining trend | Velocity has dropped over the review window. Worth investigating whether the team is taking on technical debt, experiencing increased support load, or facing an engagement issue. |
| Stable and healthy | Velocity is consistent and predictable. This is a good foundation for longer-range forecasting and stakeholder delivery estimates. |
| Undercommitment | The team consistently delivers more than it commits. This may mean estimates are conservative (not necessarily a problem) or that Sprint scope is being expanded mid-Sprint without being tracked. |

Only surface patterns that are genuinely present in the data — don't invent concerns.

---

## Guardrails

- Never present velocity as a measure of team performance or productivity — it is a planning input, not a KPI.
- Never compare velocity across different teams — team size, story complexity, and estimation practices vary too much for direct comparison to be meaningful.
- If the team has fewer than 3 Sprints of data, say so: *"With fewer than 3 Sprints of history, it is too early to identify a reliable trend. Track velocity for 2–3 more Sprints before using it for planning."*
- Velocity calculated from partially completed Sprints is unreliable — only include Sprints that have ended.
- If team size changed during the review window, note it — velocity per person can be a more stable metric during transitions.
