# PRESENTATION QUICK REFERENCE CHEATSHEET

**Print this or keep on second screen during presentation**

---

## ⏱️ TIMING (60 minutes total)

| Phase | Duration | Status |
|-------|----------|--------|
| Introduction | 5 min | ⬜ |
| Research & Planning | 10 min | ⬜ |
| Red Phase (Tests) | 15 min | ⬜ |
| Green Phase (Implementation) | 20 min | ⬜ |
| Orchestration Demo | 8 min | ⬜ |
| Wrap-up & Q&A | 2 min | ⬜ |

---

## 🎯 KEY AGENTS

| Agent | Purpose | When to Use |
|-------|---------|-------------|
| @DomainSpecialist | Research codebase | Phase 2 (Research) |
| @Planner | Create implementation plan | Phase 2 (Planning) |
| @tdd-red | Write failing tests | Phase 3 (Red Phase) |
| @tdd-green | Implement code | Phase 4 (Green Phase) |
| @Orchestrator | Coordinate full workflow | Phase 5 (Orchestration) |

---

## 📋 PHASE CHECKLIST

### Phase 1: Introduction ✓
- [ ] Welcome audience
- [ ] Show repository structure
- [ ] Explain feature to build
- [ ] Introduce custom agents
- [ ] Set expectations

### Phase 2: Research & Planning ✓
- [ ] Run @DomainSpecialist (5 min)
- [ ] Show findings
- [ ] Run @Planner (5 min)
- [ ] Review plan
- [ ] Commit: "Add research and planning"

### Phase 3: Red Phase ✓
- [ ] Run @tdd-red for unit tests (7 min)
- [ ] Show generated tests
- [ ] Run @tdd-red for integration tests (3 min)
- [ ] Run tests - confirm all fail
- [ ] Commit: "Add comprehensive test suite (Red phase)"

### Phase 4: Green Phase ✓
- [ ] Run @tdd-green for models (5 min)
- [ ] Run tests - some pass
- [ ] Run @tdd-green for business logic (8 min)
- [ ] Run tests - all unit tests pass
- [ ] Run @tdd-green for integration (7 min)
- [ ] Run all tests - everything passes! 🎉
- [ ] Commit: "Implement discount system (Green phase)"

### Phase 5: Orchestration ✓
- [ ] Introduce new requirement (BOGO)
- [ ] Run @Orchestrator with full requirement
- [ ] Narrate as orchestrator coordinates agents
- [ ] Run tests - all pass
- [ ] Commit: "Add BOGO support via Orchestrator"

### Phase 6: Wrap-up ✓
- [ ] Summarize what was built
- [ ] Highlight key benefits
- [ ] Open for questions

---

## 💻 ESSENTIAL COMMANDS

### Git Commands
```powershell
# Commit progress
git add -A
git commit -m "[MESSAGE]"

# Go back if needed
git reset --hard checkpoint-2

# Check status
git status
git log --oneline
```

### Test Commands
```powershell
dotnet test
dotnet test --logger "console;verbosity=detailed"
```

### Build Commands
```powershell
dotnet build
dotnet run --project src/Services/Ordering/Ordering.API
```

---

## 🚨 TROUBLESHOOTING QUICK FIXES

### Agent not responding
1. Check Copilot is active (bottom right)
2. Reload window: Ctrl+Shift+P → "Developer: Reload Window"
3. Fallback to @workspace if needed

### Tests won't run
```powershell
# .NET
dotnet restore
dotnet build

# TypeScript
npm install
```

### Code won't compile
1. Ask Copilot: "Why is this code not compiling?"
2. Check imports/using statements
3. Reset to checkpoint: `git reset --hard checkpoint-X`

### Behind schedule?
- Skip BOGO orchestrator demo
- Jump to checkpoint 3 and review
- Summarize instead of showing

### Ahead of schedule?
- Deep dive into tests
- Show refactoring
- Extra Q&A time
- Add another feature

---

## 🎤 KEY TALKING POINTS

### Introduction
> "Today we'll see how AI agents work together using TDD to build a real feature in production-grade code."

### After DomainSpecialist
> "Notice how it explored the codebase and found integration points. This would take a developer 30-60 minutes manually."

### After Planner
> "Clear roadmap with test scenarios and edge cases identified upfront."

### During Red Phase
> "These tests document our requirements and will guide implementation."

### When Tests Fail
> "Perfect! All tests failing. We're in the Red phase. This is exactly what we want."

### During Green Phase
> "Watch as it implements just enough to make tests pass, following TDD principles."

### When Tests Start Passing
> "Tests turning green! We're making real progress."

### All Tests Pass
> "🎉 All tests passing! Feature complete with full test coverage."

### Orchestrator Demo
> "Now the magic - the Orchestrator coordinates all agents autonomously."

### Wrap-up
> "In one hour: researched unfamiliar code, planned a feature, wrote comprehensive tests, and implemented production-ready code. This is the future of development."

---

## 📊 SUCCESS METRICS TO MENTION

- ✅ Complete feature implementation
- ✅ Full test coverage
- ✅ All tests passing
- ✅ Follows existing patterns
- ✅ Production-ready code
- ✅ Time: 1 hour (vs. 1-2 days manually)

---

## 🎯 AUDIENCE ENGAGEMENT

### Questions to Ask
- "How long would this take in your workflow?"
- "Who's used TDD before?"
- "What features would you want agents to help with?"
- "Any questions about what you just saw?"

### What to Highlight
- Context awareness
- Code quality
- Test comprehensiveness
- Time savings
- Learning opportunity for juniors

---

## 🔄 BACKUP PLAN

### If live coding fails:
1. Stay calm - it's reality
2. Use checkpoints to recover
3. Or switch to explaining pre-built version
4. Turn it into debugging demo

### Checkpoint Recovery
```powershell
git reset --hard checkpoint-1  # After planning
git reset --hard checkpoint-2  # After Red phase
git reset --hard checkpoint-3  # After Green phase
```

---

## 📝 POST-PRESENTATION TODO

- [ ] Commit final work
- [ ] Tag: `git tag demo-complete-[DATE]`
- [ ] Push to repository
- [ ] Share recording link
- [ ] Send follow-up resources
- [ ] Collect feedback

---

## 💡 REMEMBER

✅ Narrate what you're doing
✅ Show prompts to audience
✅ Explain agent responses
✅ Stay enthusiastic
✅ It's okay if something fails
✅ Engage with questions
✅ Have fun!

❌ Don't rush through explanations
❌ Don't skip showing failures
❌ Don't assume audience knowledge
❌ Don't panic if timing off
❌ Don't just copy-paste silently

---

## 📞 EMERGENCY CONTACTS (Optional)

- Tech support: [NAME / NUMBER]
- Backup presenter: [NAME]
- IT/AV help: [NUMBER]

---

**GOOD LUCK! YOU'VE GOT THIS! 🚀**

Remember: The goal is to show how agents enhance development, not to be perfect. Authentic demos with real challenges are more engaging than scripted perfection.
