# Orchestration in a Modular Monolith  

This architecture uses DataArc Orchestration-style coordination to unify multiple persistence boundaries inside a modular monolith. It is important to understand what orchestration actually does and what it does not do.

---

What Orchestration Actually Does

Orchestration is infrastructure coordination.  
It is responsible for:

- SQL pipelines
- EF Core operations
- Multi-DbContext coordination
- Transaction fabric
- Context switching
- Schema generation
- Batch operations
- SQL -> EF -> SQL sequences
- External infrastructure events

These are all pure infrastructure concerns, not domain or application logic.

---

Why Orchestrators Belong in Infrastructure

Orchestrators sit alongside other infrastructure utilities such as:

- Repositories
- Message brokers
- Database gateways
- File system implementations
- Email senders
- External API clients

These components are called by application layers, but they never sit between Core and Infrastructure, and they never contain domain logic.

Orchestrators coordinate infrastructure; they do not run domain behavior.

---

Why Not Use DbContexts Directly?

This is where DataArc's orchestration model becomes superior to plain EF Core.

EF Core is not orchestration-aware.

A single DbContext cannot:

- Coordinate across multiple DbContexts
- Enforce multi-persistence-boundary consistency
- Sequence operations safely
- Share state across boundaries
- Unify transactions without DTC
- Build SQL pipelines
- Handle parallel EF pipelines safely
- Integrate schema evolution
- Apply multi-context DDL operations
- Produce immutable, ordered pipelines
- Emit consistent telemetry and summaries

A DbContext has one job:
Operate on a single persistence boundary in isolation.

---

Why Builders Exist

DataArc introduces builders because EF Core alone cannot perform multi-context orchestration.

1. Builders abstract coordination across multiple DbContexts  
   DbContexts know only themselves. Builders understand all participating contexts.

2. Builders coordinate operations EF Core cannot  
   Bulk operations, ordered pipelines, batching, optimized SQL.

3. Builders provide unified transaction safety  
   A cross-context transaction fabric without DTC. No other .NET library provides this.

4. Builders construct operations, not ad-hoc database calls  
   Core requests an operation.  
   Orchestrators compose it.  
   Builders transform it into EF plus SQL pipelines.

5. Builders enable tracing, ordering, and immutability  
   Direct DbContext usage cannot produce:
   - Execution timings
   - Telemetry
   - SQL batching
   - Cross-context summaries
   - Pipeline events

   Builders can.

6. Builders abstract infrastructure from the application layer  
   Applications never touch:
   - EF Core  
   - SQL  
   - Transactions  
   - DbSets  
   - DbCommands  
   - Query providers  

   They work exclusively with orchestration, keeping application logic clean.

---

Summary

Orchestration is not a domain layer.  
It is not an application layer.  
It is not a workflow engine.

It is a coordination layer inside Infrastructure, built to unify persistence boundaries and manage multi-context operations that EF Core cannot handle on its own.

This is why orchestrators live in the Infrastructure project of this solution.
