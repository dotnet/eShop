# README: Live Coding Presentation Package

## GitHub Copilot Custom Subagents - TDD Workflow Demo

This package contains everything you need to deliver a compelling 1-hour live coding presentation demonstrating GitHub Copilot's custom subagent capabilities using Test-Driven Development.

---

## 📦 Package Contents

| File | Purpose |
|------|---------|
| **PRESENTATION_PLAN.md** | High-level overview, repo recommendations, feature spec, structure |
| **PRESENTATION_SCRIPT.md** | Detailed minute-by-minute script with exactly what to say and do |
| **FEATURE_SPECIFICATION.md** | Complete technical spec for the discount system feature |
| **SETUP_GUIDE.md** | Step-by-step setup instructions, troubleshooting, day-of prep |
| **PROMPT_TEMPLATES.md** | Copy-paste ready prompts for each agent and phase |
| **QUICK_REFERENCE.md** | One-page cheatsheet to print or keep on second screen |
| **README.md** | This file - overview of the package |

---

## 🎯 Presentation Overview

**Duration**: 60 minutes
**Audience**: Developers, tech leads, engineering managers
**Goal**: Demonstrate how custom GitHub Copilot agents collaborate using TDD

### What You'll Demonstrate

1. **Research & Planning** - Agents explore unfamiliar codebases and create implementation plans
2. **Red Phase (TDD)** - Automated comprehensive test creation before implementation
3. **Green Phase (TDD)** - AI-driven code implementation to make tests pass
4. **Orchestration** - Autonomous multi-agent coordination for complex workflows

### Custom Agents Featured

- **DomainSpecialist** - Researches codebases and assesses feasibility
- **Planner** - Creates detailed step-by-step development plans
- **tdd-red** - Writes comprehensive failing tests (Red phase)
- **tdd-green** - Implements code to make tests pass (Green phase)
- **Orchestrator** - Manages complete SDLC workflows

---

## 🚀 Quick Start (First Time)

### 1. Read the Plan (15 minutes)
Start with **PRESENTATION_PLAN.md** to understand:
- Repository info (eShop - successor to archived eShopOnContainers)
- Feature overview (promotional discount system)
- Overall structure and timing
- Why this works well for a demo

### 2. Follow Setup Guide (1-2 hours)
Use **SETUP_GUIDE.md** to:
- Clone and configure the demo repository
- Set up custom agents
- Verify build and test framework
- Create backup checkpoints

### 3. Review the Script (30 minutes)
Read through **PRESENTATION_SCRIPT.md**:
- Understand flow for each phase
- Note key talking points
- Review troubleshooting section
- Customize for your style

### 4. Practice Run (1-2 hours)
- Do a full dry run following the script
- Time each phase
- Identify where you need to adjust
- Create your backup checkpoints

---

## 📅 Day Before Presentation

- [ ] Review **QUICK_REFERENCE.md** cheatsheet
- [ ] Verify repo builds and tests run
- [ ] Test that custom agents respond correctly
- [ ] Prepare **PROMPT_TEMPLATES.md** for easy access
- [ ] Increase VS Code font size
- [ ] Test screen recording setup
- [ ] Print **QUICK_REFERENCE.md** if helpful
- [ ] Get good sleep!

---

## 🎤 Day of Presentation

### 30 Minutes Before
- [ ] Reset repo to clean state
- [ ] Start screen recording (backup)
- [ ] Open **QUICK_REFERENCE.md** on second screen
- [ ] Have **PROMPT_TEMPLATES.md** ready
- [ ] Clear terminal, increase font size
- [ ] Test Copilot agents are working
- [ ] Take a deep breath 😊

### During Presentation
- Follow **PRESENTATION_SCRIPT.md** structure
- Use **PROMPT_TEMPLATES.md** for copy-paste
- Glance at **QUICK_REFERENCE.md** for timing
- Stay calm if something goes wrong
- Engage audience with questions
- Have fun!

---

## 📂 Repository: eShop

**Official .NET Reference Application**
- ⭐ ~10k stars on GitHub
- 💻 .NET 9 with .NET Aspire orchestration
- 🏢 Production-quality microservices architecture  
- 🔧 Modern and actively maintained
- 📦 Rich domain models perfect for business logic demo
- 🎯 Target: Ordering.API microservice
- 🔗 https://github.com/dotnet/eShop
- 📝 Successor to archived eShopOnContainers

**Feature to Implement**: Promotional Discount System
- Complex enough to showcase agent capabilities
- Not too large for 1-hour implementation
- Pure business logic (minimal UI concerns)
- Real-world requirement everyone understands
- Perfect for demonstrating TDD workflow

---

## ⏱️ Time Breakdown

| Phase | Minutes | Activities |
|-------|---------|------------|
| Introduction | 5 | Welcome, repo tour, feature overview, agent intro |
| Research & Planning | 10 | DomainSpecialist + Planner agents |
| Red Phase | 15 | tdd-red creates comprehensive tests |
| Green Phase | 20 | tdd-green implements feature |
| Orchestration | 8 | Orchestrator coordinates full workflow |
| Wrap-up & Q&A | 2 | Summary, key takeaways, questions |
| **Total** | **60** | |

---

## 🎓 What Attendees Will Learn

1. **Custom Agent Architecture** - How specialized agents work together
2. **TDD with AI** - Red-Green-Refactor cycle automated
3. **Context Awareness** - How agents understand business requirements
4. **Code Quality** - AI-generated code following best practices
5. **Workflow Orchestration** - Multiple agents coordinating autonomously
6. **Practical Application** - Real-world use cases for their teams

---

## 💡 Key Benefits to Emphasize

- ⚡ **Speed**: Hours of work in minutes
- ✅ **Quality**: Built-in test coverage and best practices
- 📚 **Learning**: Junior devs learn from generated code
- 🎯 **Focus**: Developers focus on 'what', agents handle 'how'
- 🔄 **Consistency**: Follows established patterns automatically
- 🤝 **Collaboration**: Human-AI partnership, not replacement

---

## 🔧 Technical Requirements

### Software
- Visual Studio Code with GitHub Copilot
- .NET SDK 8.0+
- Git
- xUnit test framework (included in eShop)

### GitHub Copilot
- Active GitHub Copilot subscription
- Custom agents configured (see SETUP_GUIDE.md)
- Access to chat and inline suggestions

### Optional but Recommended
- Second monitor for reference materials
- Screen recording software (OBS, Game Bar)
- Presentation timer
- Good microphone for virtual presentations

---

## 🆘 Emergency Procedures

### If Agent Doesn't Respond
1. Reload VS Code window
2. Check Copilot status
3. Use @workspace as fallback
4. Have pre-written code ready

### If Running Behind Schedule
- Skip BOGO orchestration demo
- Jump to checkpoint 3
- Summarize instead of showing
- Extend Q&A

### If Technical Failure
- Switch to backup checkpoints
- Show pre-completed version
- Turn into code review session
- Stay calm - it's reality!

---

## 📊 Success Criteria

Your presentation is successful if attendees:
- ✅ Understand how custom agents work together
- ✅ See the value of TDD with AI assistance
- ✅ Can envision using this in their workflow
- ✅ Ask engaged questions about implementation
- ✅ Leave excited about the technology

---

## 📝 Post-Presentation

### Immediate (Same Day)
- [ ] Save recording
- [ ] Commit final code state
- [ ] Tag repository with date
- [ ] Collect immediate feedback

### Follow-up (Within Week)
- [ ] Share recording link
- [ ] Send presentation materials
- [ ] Distribute prompt templates
- [ ] Schedule follow-up Q&A session
- [ ] Create summary document

### Long-term
- [ ] Track adoption in attendee teams
- [ ] Refine based on feedback
- [ ] Create advanced version
- [ ] Share success stories

---

## 🔗 Additional Resources

### GitHub Copilot
- Documentation: https://docs.github.com/copilot
- Blog: https://github.blog/category/copilot/
- Community: GitHub Copilot Discord

### Test-Driven Development
- Martin Fowler's TDD: https://martinfowler.com/bliki/TestDrivenDevelopment.html
- "Test Driven Development: By Example" - Kent Beck

### Demo Repository
- eShop: https://github.com/dotnet/eShop
- .NET Aspire documentation: https://learn.microsoft.com/dotnet/aspire/
- Architecture documentation: https://docs.microsoft.com/dotnet/architecture/microservices/

---

## 🤝 Customization Tips

This package is designed to be customized:

### For Your Audience
- Adjust technical depth based on experience level
- Add industry-specific examples
- Emphasize pain points they face
- Use terminology they know

### For Your Style
- Make the script your own voice
- Add personal anecdotes
- Include your favorite techniques
- Adjust timing to your pace

### For Different Repos
- All prompts work with any codebase
- Just update paths and framework names
- Keep the same overall structure
- Feature can be adapted to any domain

---

## 📞 Support & Questions

If you have questions about this presentation package:
1. Review the specific guide for your question
2. Check troubleshooting sections
3. Practice runs will surface most issues
4. Trust the structure - it's been designed for success

---

## 🌟 Final Tips

**DO:**
- ✅ Practice at least once
- ✅ Show real failures and problem-solving
- ✅ Engage audience throughout
- ✅ Be enthusiastic about the tech
- ✅ Narrate your thought process
- ✅ Have fun with it!

**DON'T:**
- ❌ Memorize the script word-for-word
- ❌ Hide mistakes or errors
- ❌ Rush through explanations
- ❌ Assume deep TDD knowledge
- ❌ Go off-script too much
- ❌ Stress about perfection

---

## 🎯 Remember

The goal isn't a perfect demo - it's to show how GitHub Copilot custom agents can genuinely enhance development workflows. Authentic demos with real challenges are more engaging than scripted perfection.

Your enthusiasm and ability to work through issues will resonate more than flawless execution.

**You've got everything you need. Now go inspire your audience! 🚀**

---

## 📄 License & Usage

This presentation package is provided as-is for educational and demonstration purposes. Feel free to:
- Customize for your needs
- Share with colleagues
- Adapt for different audiences
- Use in commercial presentations
- Provide feedback for improvements

---

**Version**: 1.0
**Last Updated**: January 2026
**Feedback**: [Your contact info if desired]

Good luck with your presentation! 🎉
