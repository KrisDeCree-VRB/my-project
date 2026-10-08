import { test, expect, createTrip, joinAs, setAttendance, daysFromNow } from '../support/app.js';

test.describe('My trips', () => {
  // SC-009: a browser that belongs to nothing is told about nothing.
  test("a browser with no trips gets an empty state, not other people's trips", async ({ page }) => {
    await page.goto('/trips');

    await expect(page.getByText("You're not part of any trip yet.")).toBeVisible();
    await expect(page.getByRole('main').getByRole('link', { name: 'Start a trip' })).toBeVisible();
    // Nothing of anyone else's leaks in: no trip is linked at all.
    await expect(page.getByRole('main').locator('a[href^="/trips/"]:not([href$="create"])')).toHaveCount(0);
  });

  test('a trip appears with its countdown and the answer you gave', async ({ page }) => {
    const trip = await createTrip(page, { startDate: daysFromNow(10), endDate: daysFromNow(12) });

    await setAttendance(page, "I'm in");
    await page.goto('/trips');

    const row = page.getByRole('listitem').filter({ hasText: trip.name });
    await expect(row.getByRole('link', { name: trip.name })).toHaveAttribute('href', trip.path);
    await expect(row).toContainText(trip.location);
    await expect(row).toContainText("✓ You're in");
    await expect(row).toContainText('In 10 days');
    await expect(row).toContainText('1 in');
  });

  test('a trip you joined shows up too, with your own answer', async ({ page, device }) => {
    const trip = await createTrip(page);

    const guest = await device();
    await joinAs(guest, trip.inviteUrl, 'Marie');
    await setAttendance(guest, "I'm out");
    await guest.goto('/trips');

    const row = guest.getByRole('listitem').filter({ hasText: trip.name });
    await expect(row).toContainText("✗ You're out");
    await expect(row.getByRole('link', { name: trip.name })).toHaveAttribute('href', trip.path);
  });

  // NFR-006: a stranger holding the trip URL learns nothing, not even that it exists.
  test('a trip URL is useless to someone who is not in the trip', async ({ page, device }) => {
    const trip = await createTrip(page);

    const stranger = await device();
    const response = await stranger.request.get(trip.url);
    expect(response.status()).toBe(404);

    // Indistinguishable from a trip id that was never issued.
    const neverExisted = await stranger.request.get('/trips/00000000-0000-0000-0000-000000000000');
    expect(neverExisted.status()).toBe(404);
  });
});
