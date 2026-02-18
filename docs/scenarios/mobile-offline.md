# Mobile & Offline Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S06: Mobile Offline Usage (Sarah)

**Context**: Sarah is on a flight with no internet. She needs to review and add tasks.

```
Step 1: Sarah opens LemonDo (PWA) on her phone
  -> App loads from service worker cache
  -> A subtle banner: "You're offline. Changes will sync when you're back online."
  -> All previously loaded tasks are visible
  [analytics: session_started, connectivity: offline]

Step 2: Sarah adds tasks while offline
  -> Quick-add works normally
  -> Tasks appear in the UI with a subtle "pending sync" indicator
  -> She adds 3 tasks for the week ahead

Step 3: Sarah completes a task offline
  -> Taps checkmark -> task animates to done
  -> Subtle sync indicator remains

Step 4: Sarah's flight lands, phone reconnects
  -> LemonDo background syncs automatically
  -> "Pending sync" indicators disappear
  -> All changes are now on the server
  [analytics: offline_sync_completed, items_synced: 4]
```
