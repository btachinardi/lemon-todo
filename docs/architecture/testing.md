# Testing Standards

> **Source**: Extracted from GUIDELINES.md §1 (Development Methodology: TDD)
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Development Methodology: TDD

LemonDo follows strict Test-Driven Development with RED-GREEN-VALIDATE phases.

### The TDD Cycle

```
1. RED:      Write a failing test that describes the desired behavior
2. GREEN:    Write the minimum code to make the test pass
3. VALIDATE: Refactor while keeping tests green, then verify full suite
```

### Rules

- Never write production code without a failing test first
- Tests are the specification. If there's no test for it, it doesn't exist
- Each test should test one behavior, not one method
- Test names describe behavior: `should_create_task_with_valid_title`, not `test_create_task`
- Property tests cover domain invariants (value objects, entities)
- Integration tests cover API endpoints
- E2E tests cover user scenarios

---

## Testing Pyramid

```
         +-------------------------+
        /      E2E (Playwright)     \     <- Few, slow, full-stack
       /      Integration (API)      \    <- Moderate, test endpoints
      /   Unit (Domain + Components)  \   <- Many, fast, isolated
     /   Property (Domain invariants)  \  <- Many, fast, generated
    +-----------------------------------+
```

---

## Coverage Targets

| Layer | Target |
|-------|--------|
| Domain entities & value objects | 90% |
| Use cases / handlers | 80% |
| API endpoints (integration) | 100% of happy + error paths |
| Frontend components | 80% |
| E2E scenarios | 100% of SCENARIOS.md storyboards |
