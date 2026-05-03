---
compatibility: Works with or without a project management MCP. When ADO or Jira MCP is connected, reads team member list and iteration dates automatically. Falls back to manual input. Compatible with Claude Code, Cursor, GitHub Copilot, and any agentskills.io-compatible agent.
description: Calculates team capacity for the upcoming Sprint by accounting for team size, Sprint length, planned time off, ceremony overhead, and individual focus factors. Outputs total developer-days available and a recommended story point commitment range. Use when a Scrum Master or team says things like "plan our capacity", "how many points can we commit to", "capacity for next sprint", "how much can we take on", or "account for holidays in the sprint". Do not use for sprint planning facilitation — use sprint-planning for that. Do not use for velocity analysis — use sm-velocity-review for that.
license: MIT
metadata:
    ceremony: Sprint Planning
    github-path: skills/sm-capacity-planning
    github-ref: refs/heads/master
    github-repo: https://github.com/abrahamFerga/scrum-skills
    github-tree-sha: 05344620afd52a36991a425415ef92af9ec78428
    perspective: Scrum Master / Team
    scrum_guide_ref: https://scrumguides.org/scrum-guide.html
    version: 1.0.0
name: sm-capacity-planning
---
# Sprint Capacity Planning

## Purpose

Committing more than the team can actually deliver erodes trust, demoralises Developers, and makes velocity data meaningless. Committing too little wastes Sprint potential and frustrates the Product Owner. This skill does the arithmetic so the team can make an honest commitment.

---

## Tool detection

1. Check for active `mcp__azure-devops__*` tools → `$PM_TOOL = ado`
2. Check for active `mcp__jira__*` tools → `$PM_TOOL = jira`
3. If neither → `$PM_TOOL = manual`

---

## Step 1 — Get Sprint parameters

Collect or detect the following:

**Sprint dates**
- **ADO:** Before calling any iteration or team tool, resolve project and team in this exact order:
  1. Call `core_list_projects` — if one project exists use it automatically; if multiple, ask the user. Store as `$PROJECT`.
  2. Call `core_list_project_teams` with `$PROJECT` — if one team exists use it automatically; if multiple, ask the user. Store as `$TEAM`.
  3. Only then call `work_list_team_iterations` with `project: $PROJECT` and `team: $TEAM` to find the next iteration and its start/end dates. Never call this tool with null values for either parameter.
- **Jira:** use the sprint listing tool to find the upcoming sprint
- **Manual:** ask *"What are the start and end dates for the next Sprint?"*

Calculate `$WORKING_DAYS` — the number of working days between start and end (exclude weekends by default; ask about public holidays in the team's locale).

**Focus factor**
The percentage of working time a developer realistically spends on sprint work (excluding email, meetings outside Scrum, interruptions). Default: **70%**. Ask if the team has a known different value.

---

## Step 2 — Get team members

- **ADO:** use `work_get_team_capacity` or `work_get_iteration_capacities` for the upcoming iteration; read each team member and their planned days off
- **Jira:** use the team/member listing tool or ask manually
- **Manual:** ask *"Who is on the team this Sprint? List each person."*

For each team member, ask (or read from the tool):
- Days off during the Sprint (vacation, sick, training, public holidays)
- Reduced availability (part-time, shared with another team, on-call rotation)

---

## Step 3 — Calculate capacity

For each team member:

```
Available days = $WORKING_DAYS − days_off − reduced_availability_days
Focus hours = available_days × hours_per_day × focus_factor

Default: hours_per_day = 8, focus_factor = 0.70
```

**Ceremony overhead** — subtract the time the team spends in Scrum ceremonies:

| Ceremony | Typical duration |
|---|---|
| Sprint Planning | 2–4 hours (scale with Sprint length) |
| Daily Scrums | 15 min × working days |
| Sprint Review | 1–2 hours |
| Retrospective | 1–2 hours |

Subtract ceremony hours from total focus hours to get **net development hours** per person.

**Team total:**
```
Total developer-hours = sum of net development hours across all team members
Total developer-days  = total developer-hours ÷ hours_per_day
```

---

## Step 4 — Derive the story point range

Use the team's recent velocity to convert developer-days into story points.

Ask: *"What is the team's average velocity over the last 3 Sprints?"* (or fetch from `sm-velocity-review` if available).

```
Reference velocity = average story points completed per Sprint
Reference capacity = average developer-days in those Sprints

Points per developer-day = reference_velocity ÷ reference_capacity

Recommended commitment = total_developer_days × points_per_developer_day
```

Present as a range: `recommended × 0.85` to `recommended × 1.00` — the lower end accounts for uncertainty; the upper end is the ceiling.

If velocity data is not available, skip the point conversion and output developer-days only. Note that the team should track velocity for 2–3 Sprints before committing to point-based planning.

---

## Step 5 — Present the capacity summary

```
Sprint Capacity — [SPRINT NAME] — [DATE RANGE]

Team: [N] developers
Working days: [N]
Focus factor: [X]%

┌─────────────────────┬───────────┬──────────┬──────────────┐
│ Developer           │ Days off  │ Net days │ Net hours    │
├─────────────────────┼───────────┼──────────┼──────────────┤
│ [Name]              │ [N]       │ [N]      │ [N]          │
│ [Name]              │ [N]       │ [N]      │ [N]          │
│ [Name]              │ [N]       │ [N]      │ [N]          │
└─────────────────────┴───────────┴──────────┴──────────────┘

Ceremony overhead: [N] hours
Total team capacity: [N] developer-days / [N] hours

Recommended commitment: [N–N] story points
(based on [N]-point average velocity over last [N] Sprints)

⚠ Flags:
  - [Any individual with < 50% availability — flag for PO awareness]
  - [Any Sprint significantly shorter than normal due to holidays]
```

---

## Step 6 — Offer to save to ADO

Ask: *"Should I save this capacity plan to ADO for the team's iteration?"*

If yes, use `work_update_team_capacity` or equivalent to record each team member's days off and capacity in the iteration. This feeds burn-down charts automatically.

---

## Guardrails

- Never use 100% capacity as the commitment ceiling — humans are not machines and Scrum accounts for uncertainty.
- Never assume every team member works the same hours — ask about part-time, shared, or on-call arrangements.
- If a team member has more than 40% of the Sprint as days off, flag it to the Product Owner — the committed scope may need to shrink.
- Capacity is an input to Sprint Planning, not the Sprint Goal. The team commits to the Sprint Goal; capacity just bounds what is achievable.
