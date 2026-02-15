import { test, expect } from '@playwright/test';
import {
  createTask,
  deleteAllTasks,
  getDefaultBoard,
  moveTask,
  deleteTask,
  archiveTask,
} from '../helpers/api.helpers';
import { loginViaStorage } from '../helpers/auth.helpers';

test.beforeEach(async () => {
  await deleteAllTasks();
});

test.describe('Card Ordering (API)', () => {
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
    const t1 = await createTask({ title: 'Task A' });
    const t2 = await createTask({ title: 'Task B' });

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
    const t1 = await createTask({ title: 'Task A' });
    const t2 = await createTask({ title: 'Task B' });

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
    const t1 = await createTask({ title: 'Task A' });

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
    const t1 = await createTask({ title: 'Task A' });
    const t2 = await createTask({ title: 'Task B' });
    const t3 = await createTask({ title: 'Task C' });

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
    const t1 = await createTask({ title: 'Move me' });

    const board = await getDefaultBoard();
    const inProgressCol = board.columns.find((c) => c.targetStatus === 'InProgress')!;

    await moveTask(t1.id, inProgressCol.id, null, null);

    const boardAfter = await getDefaultBoard();
    const card = boardAfter.cards.find((c) => c.taskId === t1.id)!;
    expect(card.columnId).toBe(inProgressCol.id);
  });
});

test.describe('Orphaned Cards', () => {
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
    const t1 = await createTask({ title: 'Keep' });
    const t2 = await createTask({ title: 'Archive me' });

    const boardBefore = await getDefaultBoard();
    expect(boardBefore.cards).toHaveLength(2);

    await archiveTask(t2.id);

    const boardAfter = await getDefaultBoard();
    // Archived task's card should not appear in the response
    expect(boardAfter.cards).toHaveLength(1);
    expect(boardAfter.cards[0].taskId).toBe(t1.id);
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
    // Only t1 and t4 should have cards
    expect(board.cards).toHaveLength(2);
    const cardTaskIds = board.cards.map((c) => c.taskId).sort();
    expect(cardTaskIds).toEqual([t1.id, t4.id].sort());
  });
});

test.describe('Card Ordering (UI)', () => {
  test('tasks render in rank order on the board', async ({ page }) => {
    const t1 = await createTask({ title: 'Alpha task' });
    const t2 = await createTask({ title: 'Beta task' });
    const t3 = await createTask({ title: 'Gamma task' });

    // Reorder via API: move Gamma between Alpha and Beta
    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;
    await moveTask(t3.id, todoCol.id, t1.id, t2.id);

    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByText('Alpha task')).toBeVisible();
    await expect(page.getByText('Gamma task')).toBeVisible();
    await expect(page.getByText('Beta task')).toBeVisible();

    // Verify visual order: Alpha, Gamma, Beta
    const cards = page.locator('[role="button"][aria-label^="Task:"]');
    await expect(cards).toHaveCount(3);
    const labels = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );
    expect(labels[0]).toContain('Alpha');
    expect(labels[1]).toContain('Gamma');
    expect(labels[2]).toContain('Beta');
  });

  test('deleted task disappears from the board', async ({ page }) => {
    const t1 = await createTask({ title: 'Stays on board' });
    const t2 = await createTask({ title: 'Gets deleted' });

    await loginViaStorage(page);
    await page.goto('/');
    await expect(page.getByText('Gets deleted')).toBeVisible();

    // Delete via API and reload
    await deleteTask(t2.id);
    await page.reload();

    await expect(page.getByText('Stays on board')).toBeVisible();
    await expect(page.getByText('Gets deleted')).not.toBeVisible();
  });

  test('UI-created tasks maintain order after reload', async ({ page }) => {
    await loginViaStorage(page);
    await page.goto('/');

    // Create tasks through the quick-add form
    for (const title of ['UI Task One', 'UI Task Two', 'UI Task Three']) {
      const input = page.getByLabel('New task title');
      await input.fill(title);
      await page.getByRole('button', { name: /add/i }).click();
      // Wait for the card to appear before adding next
      await expect(page.getByText(title)).toBeVisible();
    }

    // Capture the order
    const cards = page.locator('[role="button"][aria-label^="Task:"]');
    await expect(cards).toHaveCount(3);
    const labelsBeforeReload = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Reload the page
    await page.reload();
    await expect(cards).toHaveCount(3);
    const labelsAfterReload = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Order must be identical
    expect(labelsAfterReload).toEqual(labelsBeforeReload);
  });

  test('creation order is stable across page reloads', async ({ page }) => {
    // Create 5 tasks sequentially — no reordering
    await createTask({ title: 'Task Alpha' });
    await createTask({ title: 'Task Beta' });
    await createTask({ title: 'Task Gamma' });
    await createTask({ title: 'Task Delta' });
    await createTask({ title: 'Task Epsilon' });

    // Load page and capture order
    await loginViaStorage(page);
    await page.goto('/');
    const cards = page.locator('[role="button"][aria-label^="Task:"]');
    await expect(cards).toHaveCount(5);
    const labelsFirstLoad = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );

    // Reload 3 times — order must be identical each time
    for (let i = 0; i < 3; i++) {
      await page.reload();
      await expect(cards).toHaveCount(5);
      const labelsAfterReload = await cards.evaluateAll((els) =>
        els.map((el) => el.getAttribute('aria-label')),
      );
      expect(labelsAfterReload).toEqual(labelsFirstLoad);
    }
  });

  test('card order persists after page reload', async ({ page }) => {
    const t1 = await createTask({ title: 'First item' });
    const t2 = await createTask({ title: 'Second item' });
    const t3 = await createTask({ title: 'Third item' });

    // Reorder via API: move Third to top (before First)
    const board = await getDefaultBoard();
    const todoCol = board.columns.find((c) => c.targetStatus === 'Todo')!;
    await moveTask(t3.id, todoCol.id, null, t1.id);

    // Load the page and verify initial order: Third, First, Second
    await loginViaStorage(page);
    await page.goto('/');
    const cards = page.locator('[role="button"][aria-label^="Task:"]');
    await expect(cards).toHaveCount(3);
    const labelsBefore = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );
    expect(labelsBefore[0]).toContain('Third');
    expect(labelsBefore[1]).toContain('First');
    expect(labelsBefore[2]).toContain('Second');

    // Reload the page — order must be identical
    await page.reload();
    await expect(cards).toHaveCount(3);
    const labelsAfter = await cards.evaluateAll((els) =>
      els.map((el) => el.getAttribute('aria-label')),
    );
    expect(labelsAfter[0]).toContain('Third');
    expect(labelsAfter[1]).toContain('First');
    expect(labelsAfter[2]).toContain('Second');
  });

  test('card order persists after API re-fetch', async () => {
    const t1 = await createTask({ title: 'Alpha' });
    const t2 = await createTask({ title: 'Beta' });
    const t3 = await createTask({ title: 'Gamma' });

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

  test('cross-column move renders task in target column', async ({ page }) => {
    const t1 = await createTask({ title: 'Moving task' });

    const board = await getDefaultBoard();
    const inProgressCol = board.columns.find((c) => c.targetStatus === 'InProgress')!;

    await moveTask(t1.id, inProgressCol.id, null, null);

    await loginViaStorage(page);
    await page.goto('/');

    // The task should appear under the "In Progress" column heading
    // Column structure: div > div > h3 "In Progress" + div with cards
    const inProgressHeading = page.getByRole('heading', { name: 'In Progress' });
    await expect(inProgressHeading).toBeVisible();
    // Navigate from the h3 heading up to the column root div, then find the task within it
    const inProgressColumn = page.locator('div.rounded-xl', { has: inProgressHeading });
    await expect(inProgressColumn.getByText('Moving task')).toBeVisible();
  });
});
