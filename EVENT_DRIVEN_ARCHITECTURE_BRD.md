# Business Requirements Document: Event-Driven Architecture (Ask 3)

## Source Context & Tracability

**Original Product Owner Asks (.NET Adventure Works):**

1. **Ask 1: Decoupled Business Rules**
   > "We need to support rapid experimentation on pricing, promotions, and fulfillment strategies. The current system is too rigid. **Re-architect so business rules can evolve without redeploying the entire system**."

2. **Ask 2: Multi-Tenancy & Custom Isolation**
   > "We're onboarding enterprise partners. **Each customer needs custom pricing rules, workflows, and data isolation.** We cannot fork the codebase."

3. **Ask 3: Event-Driven Architecture (This BRD)**
   > "Downstream systems want real-time events. We can't pause development or do a full rewrite. **Introduce event-driven behavior incrementally**."

**WSJF Assessment Summary:**
This BRD addresses Ask 3, assessed as the highest-priority initiative (Score: 3.5) due to optimal value-to-effort ratio. Event-driven architecture creates the foundation enabling both Ask 1 (business rules decoupling) and Ask 2 (multi-tenant isolation) while maintaining backward compatibility and requiring no full system rewrite.

---

## Executive Summary
Incrementally introduce event-driven capabilities to decouple services, enable real-time downstream integration, and unlock the architectural flexibility needed for Asks 1 & 2. No full rewrite. Phased rollout over 8–12 weeks, starting with the highest-ROI event sources (orders, inventory, payments).

---

## Strategic Goals
1. **Unblock downstream systems** – Provide real-time events without polling or custom integrations
2. **Reduce coupling** – Isolate domain services so business rule changes (Ask 1) don't require coordinated redeployment
3. **Foundation for multi-tenancy** – Event isolation and replay support prerequisites for Ask 2's tenant workflows
4. **Minimize disruption** – Maintain all current API contracts; add events alongside existing synchronous paths

---

## Epics

### Epic 1: Event Infrastructure & Broker Selection
**Acceptance Criteria:**
- Message broker (RabbitMQ, Azure Service Bus, or equivalent) provisioned and running
- Event bus abstraction layer added to `eShop.ServiceDefaults`
- Publisher/subscriber patterns documented and tested
- Local dev environment supports event broker (Docker Compose or Aspire integration)
- Monitoring and dead-letter queue strategy in place

**Team Capabilities Required:**
- Message broker architecture experience
- Integration with Aspire orchestration
- DevOps/infrastructure setup

**Dependencies:** None (foundational)

---

### Epic 2: Event Schema & Contract Management
**Acceptance Criteria:**
- Event contract library established (shared types across services)
- Versioning strategy defined (backward compatibility rules)
- Documentation template for event payloads (schema registry or OpenAPI extension)
- Example events from Order, Catalog, Basket, and Payment domains
- Contract testing framework in place

**Team Capabilities Required:**
- Domain modeling
- API versioning patterns
- Testing framework expertise

**Dependencies:** Epic 1

---

### Epic 3: Order Domain Event Emission
**Acceptance Criteria:**
- OrderCreated, OrderStatusChanged, OrderFailed, OrderShipped events published
- Events include order ID, customer ID, affected items, timestamps
- Existing Order API behavior unchanged; events fire alongside current logic
- Integration tests verify event publication
- Events flow to downstream subscribers without blocking order service

**Team Capabilities Required:**
- Domain-driven design
- C# async patterns
- Test automation

**Dependencies:** Epic 1, Epic 2

---

### Epic 4: Payment & Catalog Event Emission
**Acceptance Criteria:**
- PaymentProcessed, PaymentFailed events from Payment Processor
- InventoryReserved, InventoryReleased events from Catalog/Basket
- Fulfillment status events from OrderProcessor
- All events follow established contract patterns (Epic 2)
- End-to-end integration tests

**Team Capabilities Required:**
- Multi-service orchestration knowledge
- Understanding of fulfillment & payment workflows

**Dependencies:** Epic 1, Epic 2, Epic 3

---

### Epic 5: Downstream Subscription & Handler Framework
**Acceptance Criteria:**
- Generic event handler pattern available to all services
- Webhooks.API enhanced to subscribe to and forward events to external partners
- Event payload transformation/mapping examples
- Retry and idempotency logic documented
- Monitoring dashboard shows event flow and lag

**Team Capabilities Required:**
- Event handler patterns
- Webhook/HTTP callback patterns
- Distributed tracing (Aspire OTel integration)

**Dependencies:** Epic 1, Epic 2, Epic 3, Epic 4

---

### Epic 6: Event Persistence & Replay
**Acceptance Criteria:**
- Event log persisted to database (outbox pattern or event store)
- Recovery process documented for failed subscribers
- Replay capability for debugging or new subscriber onboarding
- Integration with Aspire dashboard to visualize event flow

**Team Capabilities Required:**
- Database design (outbox/event store patterns)
- Transaction management
- Aspire monitoring integration

**Dependencies:** Epic 1, Epic 2, Epic 3, Epic 4

---

### Epic 7: Testing, Monitoring & Documentation
**Acceptance Criteria:**
- Unit tests for event emission and handling
- Integration tests for end-to-end event flows
- Performance tests: event latency and throughput baselines
- Runbooks for operational support (broker issues, stuck events, replay)
- Developer guide for adding events to new domain features

**Team Capabilities Required:**
- Test automation
- APM/observability tools
- Technical writing

**Dependencies:** Epic 1–6

---

## Team Capabilities Required (Summary)

| Capability | Effort | Risk |
| --- | --- | --- |
| Message broker administration | Low | Low |
| Event schema design & versioning | Medium | Low |
| Async/await patterns in C# | Medium | Low |
| Domain event modeling | Medium–High | Medium |
| Distributed tracing & monitoring | Medium | Medium |
| Database transaction patterns (outbox) | Medium | Medium |
| Integration testing at scale | Medium | Low |

**Recommended Team Structure:**
- 1 architect/lead (overall design, broker selection, patterns)
- 2–3 service developers (emit events from Order, Payment, Catalog services)
- 1 integration/testing specialist (end-to-end flows, monitoring)
- 1 DevOps/infrastructure person (broker provisioning, observability)

---

## Phased Delivery Roadmap (High-Level)

### Phase 0 (Weeks 1–2): Foundation
- Epic 1: Broker provisioned & Aspire-integrated
- Epic 2: Event schema contracts defined
- Success metric: Local event producer/consumer test works end-to-end

### Phase 1 (Weeks 3–4): Order Event Emission
- Epic 3: OrderCreated, OrderStatusChanged events flowing
- Success metric: External systems (webhooks) receive order events in real-time

### Phase 2 (Weeks 5–7): Catalog, Payment, Fulfillment Events
- Epic 4: Payment, inventory, and fulfillment events emitted
- Success metric: Downstream partners confirm receipt of all critical events

### Phase 3 (Weeks 8–10): Persistence & Reliability
- Epic 6: Event log, outbox pattern, replay capability operational
- Success metric: Broker outage doesn't lose events; subscribers can catch up

### Phase 4 (Weeks 11–12): Observability & Handoff
- Epic 5 (finalize): Webhooks.API fully integrated with event handlers
- Epic 7: Tests, docs, runbooks complete; team trained
- Success metric: New developers can emit an event end-to-end

---

## Success Criteria (Executive Level)

1. **Real-time capability:** Downstream systems receive events within <500ms of domain event occurrence
2. **Reliability:** 99.9% event delivery guarantee (with replay/outbox guarantees)
3. **No disruption:** All existing APIs and workflows function identically; events are additive
4. **Time-to-value:** Phase 0 foundation in place by Week 2; first production events by Week 5
5. **Team capability:** Developers can add new domain events to any service following established patterns within 1 sprint

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Broker becomes single point of failure | Redundancy in broker setup; outbox pattern ensures no event loss |
| Event schema versioning causes downstream breaks | Strict contract versioning & backward compatibility rules in Epic 2 |
| Performance overhead of event emission | Async publishing; load testing in Phase 1 |
| Team unfamiliar with event patterns | Pair programming; architecture review gates; spike on patterns in Phase 0 |

---

This BRD provides the roadmap clarity needed for leadership buy-in while giving the team a clear execution path without getting bogged down in task-level details.
