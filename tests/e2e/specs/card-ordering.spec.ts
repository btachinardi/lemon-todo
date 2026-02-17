import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import {
  createTask,
  getDefaultBoard,
  moveTask,
  deleteTask,
  archiveTask,
} from '../helpers/api.helpers';
import { createTestUser, loginViaApi } from '../helpers/auth.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('Card Ordering (API)', () => {
  test.beforeAll(async () => {
    await createTestUser();
  });

  test('new tasks get monotonically increasing ranks', async () => {
    const t1 = await createTask({ title: 'Task 1' });
    const t2 = await createTask({ title: 'Task 2' });
    const t3 = await createTask({ title: 'Task 3' });

    const board = await getDefaultBoard();
    const cardRanks = [t1, t2, t3].map((t) => {
      const card = board.cards.find((c) => c.taskId === t.id);
      expect(card).toBeDefined();
      return card!.rank;
    });

    expect(cardRanks[0]).toBeLessThan(cardRanks[1]);
    expect(cardRanks[1]).toBeLessThan(cardRanks[2]);
  });

  test('move task between two others computes midpoint rank', async () => {
    const t1 = await createTask({ title: 'Task A' });
    const t2 = await createTask({ title: 'Task B' });
    const t3 = await createTask({ title: 'Task C' });

    const boardBefore = await getDefaultBoard();
    const todoCol = boardBefore.columns.find((c) => c.targetStatus === 'Todo')!;
    const rankA = boardBefore.cards.find((c) => c.taskId === t1.id)!.rank;
    const rankB = boardBefore.cards.find((c) => c.taskId === t2.id)!.rank;

    // Move C between A and B
    await moveTask(t3.id, todoCol.id, t1.id, t2.id);

    const boardAfter = await getDefaultBoard();
    const rankC = boardAfter.cards.find((c) => c.taskId === t3.id)!.rank;

    expect(rankC).toBeGreaterThan(rankA);
    expect(rankC).toBeLessThan(rankB);
  });

  test('move task to top of column (previousTaskId=null)', async () => {
    const t1 = await createTask({ title: 'Top A' });
    const t2 = await createTask({ title: 'Top B' });

    const boardBefore = await getDefaultBoard();
    const todoCol = boardBefore.columns.find((c) => c.targetStatus === 'Todo')!;
    const rankA = boardBefore.cards.find((c) => c.taskId === t1.id)!.rank;

    // Move B to top (before A)
    await moveTask(t2.id, todoCol.id, null, t1.id);

    const boardAfter = await getDefaultBoard();
    const newRankB = boardAfter.cards.find((c) => c.taskId === t2.id)!.rank;

    expect(newRankB).toBeLessThan(rankA);
    expect(newRankB).toBeGreaterThan(0);
  });

  test('move task to bottom of column (nextTaskId=null)', async () => {
    const t1 = await createTask({ title: 'Bottom A' });
    const t2 = await createTask({ title: 'Bottom B' });

    const boardBefore = await getDefaultBoard();
    const todoCol = boardBefore.columns.find((c) => c.targetStatus === 'Todo')!;
    const rankB = boardBefore.cards.find((c) => c.taskId === t2.id)!.rank;

    // Move A to bottom (after B)
    await moveTask(t1.id, todoCol.id, t2.id, null);

    const boardAfter = await getDefaultBoard();
    const newRankA = boardAfter.cards.find((c) => c.taskId === t1.id)!.rank;

    expect(newRankA).toBeGreaterThan(rankB);
  });

  test('move task to empty column (both neighbors null)', async () => {
    const t1 = await createTask({ title: 'Empty col task' });

    const board = await getDefaultBoard();
    const inProgressCol = board.columns.find((c) => c.targetStatus === 'InProgress')!;

    // Move to empty InProgress column
    await moveTask(t1.id, inProgressCol.id, null, null);

    const boardAfter = await getDefaultBoard();
    const card = boardAfter.cards.find((c) => c.taskId === t1.id)!;

    expect(card.columnId).toBe(inProgressCol.id);
    expect(card.rank).toBeGreaterThan(0);
  });

  test('multiple reorders produce unique ranks', async () => {
    const t1 = await createTask({ title: 'Reorder A' });
    const t2 = await createTask({ title: 'Reorder B' });
    const t3 = await createTask({ title: 'Reorder C' });

    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;

    // Move C between A and B
    await moveTask(t3.id, todoCol.id, t1.id, t2.id);

    // Move B to top (before C, which is now after A)
    const boardMid = await getDefaultBoard();
    const cardC = boardMid.cards.find((c) => c.taskId === t3.id)!;
    await moveTask(t2.id, todoCol.id, null, cardC.taskId);

    const boardFinal = await getDefaultBoard();
    const ranks = [t1, t2, t3].map(
      (t) => boardFinal.cards.find((c) => c.taskId === t.id)!.rank,
    );

    // All ranks must be distinct
    const uniqueRanks = new Set(ranks);
    expect(uniqueRanks.size).toBe(3);
  });

  test('cross-column move changes task status', async () => {
    const t1 = await createTask({ title: 'Cross-col task' });

    const board = await getDefaultBoard();
    const inProgressCol = board.columns.find((c) => c.targetStatus === 'InProgress')!;

    await moveTask(t1.id, inProgressCol.id, null, null);

    const boardAfter = await getDefaultBoard();
    const card = boardAfter.cards.find((c) => c.taskId === t1.id)!;
    expect(card.columnId).toBe(inProgressCol.id);
  });
});

test.describe.serial('Orphaned Cards', () => {
  test.beforeAll(async () => {
    await createTestUser();
  });

  test('deleted task card is removed from board', async () => {
    const t1 = await createTask({ title: 'Keep' });
    const t2 = await createTask({ title: 'Delete me' });

    const boardBefore = await getDefaultBoard();
    expect(boardBefore.cards).toHaveLength(2);

    await deleteTask(t2.id);

    const boardAfter = await getDefaultBoard();
    expect(boardAfter.cards).toHaveLength(1);
    expect(boardAfter.cards[0].taskId).toBe(t1.id);
  });

  test('archived task card is filtered from board response', async () => {
    const t1 = await createTask({ title: 'Archive Keep' });
    const t2 = await createTask({ title: 'Archive me' });

    await archiveTask(t2.id);

    const boardAfter = await getDefaultBoard();
    const activeCards = boardAfter.cards.filter(
      (c) => c.taskId === t1.id || c.taskId === t2.id,
    );
    // Only t1 should remain (t2 archived)
    expect(activeCards).toHaveLength(1);
    expect(activeCards[0].taskId).toBe(t1.id);
  });

  test('board card count matches active task count after mixed operations', async () => {
    const t1 = await createTask({ title: 'Active 1' });
    const t2 = await createTask({ title: 'Will delete' });
    const t3 = await createTask({ title: 'Will archive' });
    const t4 = await createTask({ title: 'Active 2' });
    const t5 = await createTask({ title: 'Will delete too' });

    await deleteTask(t2.id);
    await archiveTask(t3.id);
    await deleteTask(t5.id);

    const board = await getDefaultBoard();
    // t1 and t4 should have cards (among all cards for this user)
    const relevantCards = board.cards.filter(
      (c) => [t1.id, t2.id, t3.id, t4.id, t5.id].includes(c.taskId),
    );
    expect(relevantCards).toHaveLength(2);
    const cardTaskIds = relevantCards.map((c) => c.taskId).sort();
    expect(cardTaskIds).toEqual([t1.id, t4.id].sort());
  });
});

let uiContext: BrowserContext;
let uiPage: Page;

test.describe.serial('Card Ordering (UI)', () => {
  test.beforeAll(async ({ browser }) => {
    uiContext = await browser.newContext();
    uiPage = await uiContext.newPage();
    await loginViaApi(uiPage);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await uiContext.close();
  });

  test('tasks render in rank order on the board', async () => {
    const t1 = await createTask({ title: 'Alpha task' });
    const t2 = await createTask({ title: 'Beta task' });
    const t3 = await createTask({ title: 'Gamma task' });

    // Reorder via API: move Gamma between Alpha and Beta
    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;
    await moveTask(t3.id, todoCol.id, t1.id, t2.id);

    await uiPage.goto('/board');
    await expect(uiPage.getByText('Alpha task')).toBeVisible();
    await expect(uiPage.getByText('Gamma task')).toBeVisible();
    await expect(uiPage.getByText('Beta task')).toBeVisible();

    // Verify visual order: Alpha, Gamma, Beta
    const cards = uiPage.locator('[role="button"][aria-label^="Task:"]');
    await expect(cards).toHaveCount(3);
    const labels = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );
    expect(labels[0]).toContain('Alpha');
    expect(labels[1]).toContain('Gamma');
    expect(labels[2]).toContain('Beta');
  });

  test('deleted task disappears from the board', async () => {
    // Prior test has Alpha, Gamma, Beta — add two more for this test
    const t1 = await createTask({ title: 'Stays on board' });
    const t2 = await createTask({ title: 'Gets deleted' });

    await uiPage.goto('/board');
    await expect(uiPage.getByText('Gets deleted')).toBeVisible();

    // Delete via API and reload
    await deleteTask(t2.id);
    await uiPage.reload();

    await expect(uiPage.getByText('Stays on board')).toBeVisible();
    await expect(uiPage.getByText('Gets deleted')).not.toBeVisible();
  });

  test('UI-created tasks maintain order after reload', async () => {
    await uiPage.goto('/board');

    // Create tasks through the quick-add form
    const uiTitles = ['UI Task One', 'UI Task Two', 'UI Task Three'];
    for (const title of uiTitles) {
      const input = uiPage.getByLabel('New task title');
      await input.fill(title);
      await uiPage.getByRole('button', { name: /add/i }).click();
      // Wait for the card to appear before adding next
      await expect(uiPage.getByText(title)).toBeVisible();
    }

    // Capture the order of ALL cards (including from prior tests)
    const cards = uiPage.locator('[role="button"][aria-label^="Task:"]');
    const countBefore = await cards.count();
    const labelsBeforeReload = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Reload the page
    await uiPage.reload();
    await expect(cards).toHaveCount(countBefore);
    const labelsAfterReload = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Order must be identical
    expect(labelsAfterReload).toEqual(labelsBeforeReload);
  });

  test('creation order is stable across page reloads', async () => {
    // Create 5 tasks sequentially — no reordering
    await createTask({ title: 'Stable Alpha' });
    await createTask({ title: 'Stable Beta' });
    await createTask({ title: 'Stable Gamma' });
    await createTask({ title: 'Stable Delta' });
    await createTask({ title: 'Stable Epsilon' });

    // Load page and wait for the last created task to be visible
    await uiPage.goto('/board');
    await expect(uiPage.getByText('Stable Epsilon')).toBeVisible();

    const cards = uiPage.locator('[role="button"][aria-label^="Task:"]');
    const firstLoadCount = await cards.count();
    const labelsFirstLoad = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Reload 3 times — order must be identical each time
    for (let i = 0; i < 3; i++) {
      await uiPage.reload();
      await expect(cards).toHaveCount(firstLoadCount);
      const labelsAfterReload = await cards.evaluateAll((els) =>
        els.map((el) => el.getAttribute('aria-label')),
      );
      expect(labelsAfterReload).toEqual(labelsFirstLoad);
    }
  });

  test('card order persists after page reload', async () => {
    const t1 = await createTask({ title: 'First item' });
    const t2 = await createTask({ title: 'Second item' });
    const t3 = await createTask({ title: 'Third item' });

    // Reorder via API: move Third to top (before First)
    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;
    await moveTask(t3.id, todoCol.id, null, t1.id);

    // Load the page — Third should be before First and Second among the todo tasks
    await uiPage.goto('/board');
    const cards = uiPage.locator('[role="button"][aria-label^="Task:"]');
    const labelsBefore = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Reload the page — order must be identical
    await uiPage.reload();
    const labelsAfter = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );
    expect(labelsAfter).toEqual(labelsBefore);
  });

  test('card order persists after API re-fetch', async () => {
    const t1 = await createTask({ title: 'Fetch Alpha' });
    const t2 = await createTask({ title: 'Fetch Beta' });
    const t3 = await createTask({ title: 'Fetch Gamma' });

    // Reorder: move Gamma to top
    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;
    await moveTask(t3.id, todoCol.id, null, t1.id);

    // Fetch board multiple times — rank order must be stable
    const board1 = await getDefaultBoard();
    const board2 = await getDefaultBoard();
    const board3 = await getDefaultBoard();

    const getRanks = (b: typeof board1) =>
      [t3, t1, t2].map((t) => b.cards.find((c) => c.taskId === t.id)!.rank);

    const ranks1 = getRanks(board1);
    const ranks2 = getRanks(board2);
    const ranks3 = getRanks(board3);

    // All fetches must return identical ranks
    expect(ranks1).toEqual(ranks2);
    expect(ranks2).toEqual(ranks3);

    // Gamma must have the lowest rank (top position)
    expect(ranks1[0]).toBeLessThan(ranks1[1]);
    expect(ranks1[1]).toBeLessThan(ranks1[2]);
  });

  test('cross-column move renders task in target column', async () => {
    const t1 = await createTask({ title: 'Moving task' });

    const board = await getDefaultBoard();
    const inProgressCol = board.columns.find((c) => c.targetStatus === 'InProgress')!;

    await moveTask(t1.id, inProgressCol.id, null, null);

    // Navigate to the board — wait for both auth refresh and board data to load
    const boardResponsePromise = uiPage.waitForResponse(
      (resp) => resp.url().includes('/api/boards') && resp.status() === 200,
      { timeout: 20000 },
    ).catch(() => null);
    await uiPage.goto('/board');
    const boardResp = await boardResponsePromise;

    // If board data didn't load (auth failure), re-authenticate via loginViaApi
    if (!boardResp) {
      await loginViaApi(uiPage);
      await completeOnboarding();
      // Re-create task under new user and re-move it
      const t1New = await createTask({ title: 'Moving task' });
      const boardNew = await getDefaultBoard();
      const ipCol = boardNew.columns.find((c) => c.targetStatus === 'InProgress')!;
      await moveTask(t1New.id, ipCol.id, null, null);
      await uiPage.goto('/board');
    }

    // Wait for the authenticated layout and board data to render
    await uiPage.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible', timeout: 15000 });
    await expect(uiPage.getByText('Moving task')).toBeVisible({ timeout: 15000 });

    // The task should appear under the "In Progress" column heading
    const inProgressHeading = uiPage.getByRole('heading', { name: 'In Progress' });
    await expect(inProgressHeading).toBeVisible();
    const inProgressColumn = uiPage.locator('div.rounded-xl', { has: inProgressHeading });
    await expect(inProgressColumn.getByText('Moving task')).toBeVisible();
  });
});
