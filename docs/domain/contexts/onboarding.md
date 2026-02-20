# Onboarding Context

> **Source**: Extracted from docs/DOMAIN.md §6
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 6.1 Entities

### OnboardingProgress (Aggregate Root)

```
OnboardingProgress
├── Id: OnboardingProgressId
├── UserId: UserId
├── Steps: IReadOnlyList<OnboardingStep>
├── IsCompleted: bool
├── IsSkipped: bool
├── StartedAt: DateTimeOffset
├── CompletedAt: DateTimeOffset?
│
├── Methods:
│   ├── Start() -> OnboardingStartedEvent
│   ├── CompleteStep(stepType) -> OnboardingStepCompletedEvent
│   ├── Skip() -> OnboardingSkippedEvent
│   └── Complete() -> OnboardingCompletedEvent
│
└── Invariants:
    ├── Steps must be completed in order
    ├── Cannot complete if already completed or skipped
    └── Cannot skip if already completed
```

## 6.2 Value Objects

```
OnboardingStep
├── Type: OnboardingStepType (WelcomeViewed, FirstTaskCreated, FirstTaskCompleted, KanbanExplored)
├── CompletedAt: DateTimeOffset?
├── IsCompleted: bool
```
