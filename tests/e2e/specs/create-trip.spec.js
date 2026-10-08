import { test, expect, createTrip, daysFromNow, unique, rosterEntry } from '../support/app.js';

test.describe('Starting a trip', () => {
  test('the home page offers the two ways in', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Plan the weekend away');
    await expect(page.getByRole('main').getByRole('link', { name: 'Start a trip' }))
      .toHaveAttribute('href', '/trips/create');
    await expect(page.getByRole('main').getByRole('link', { name: 'My trips' }))
      .toHaveAttribute('href', '/trips');
  });

  test('an empty form is refused, one message per field', async ({ page }) => {
    await page.goto('/trips/create');
    await page.getByRole('button', { name: 'Create trip' }).click();

    // Still on the form — nothing was created.
    await expect(page).toHaveURL(/\/trips\/create$/);
    for (const message of [
      'Give the trip a name.',
      'Where are you going?',
      'Pick a start date.',
      'Pick an end date.',
      'Tell the group who you are.',
    ]) {
      await expect(page.getByText(message)).toBeVisible();
    }
  });

  test('an end date before the start date is refused', async ({ page }) => {
    await page.goto('/trips/create');
    await page.getByRole('textbox', { name: 'Trip name' }).fill(unique('Backwards'));
    await page.getByRole('textbox', { name: 'Location' }).fill('Nowhere');
    await page.getByRole('textbox', { name: 'Start date' }).fill(daysFromNow(20));
    await page.getByRole('textbox', { name: 'End date' }).fill(daysFromNow(10));
    await page.getByRole('textbox', { name: 'Your name' }).fill(unique('Kris'));
    await page.getByRole('button', { name: 'Create trip' }).click();

    await expect(page).toHaveURL(/\/trips\/create$/);
    await expect(page.getByText('The end date cannot be before the start date.')).toBeVisible();
  });

  test('a valid trip lands on its own page, greets the starter and lists them', async ({ page }) => {
    const trip = await createTrip(page);

    expect(trip.path).toMatch(/^\/trips\/[0-9a-f-]{36}$/);
    expect(trip.inviteUrl).toMatch(/\/join\/[\w-]+$/);

    await expect(page.getByText('Your trip is ready. Share the link below with the group.')).toBeVisible();
    await expect(page.getByRole('main')).toContainText(trip.location);
    await expect(page.getByRole('main')).toContainText(trip.startDate.slice(0, 4)); // year: locale-independent

    const me = rosterEntry(page, trip.organiser);
    await expect(me).toContainText('started the trip');
    await expect(me).toContainText('you');
    await expect(me).toContainText("Hasn't answered");
  });
});
