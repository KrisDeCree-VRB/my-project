import { test as base, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

/**
 * The suite's own `test`, adding one fixture: `device`.
 *
 * Identity in this app is an HttpOnly cookie, so "another person" can only mean "another
 * browser". A separate BrowserContext is exactly that — its own cookie jar — and it is
 * what every multi-person scenario here is built from.
 *
 * The built-in `page` is already a browser of its own, so it plays the first person in a
 * scenario; `device()` supplies the second and third. Only `page` carries tracing and
 * failure screenshots, so prefer it for the browser a test asserts on most.
 */
export const test = base.extend({
  device: async ({ browser, contextOptions }, use) => {
    const contexts = [];

    /** A browser nobody has ever used: no visitor cookie, no trips. */
    const openDevice = async () => {
      // contextOptions carries baseURL and the rest of the `use` block; a bare
      // browser.newContext() would not get them, and relative goto()s would fail.
      const context = await browser.newContext(contextOptions);
      contexts.push(context);
      return context.newPage();
    };

    await use(openDevice);
    await Promise.all(contexts.map((context) => context.close()));
  },
});

export { expect };

/**
 * Specs run in parallel against one database, and display names are unique per trip
 * (INV-3). Suffixing keeps one spec's "Kris" from colliding with another's.
 */
export const unique = (name) => `${name}-${randomUUID().slice(0, 8)}`;

/**
 * A date `days` from today as the yyyy-MM-dd a date input expects. Built from local
 * parts, not toISOString(): the server counts the days until the trip in its own local
 * time, and a UTC round-trip would move the date by one on most of the planet.
 */
export function daysFromNow(days) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const pad = (n) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Walks the create form and returns the trip's own URL plus its first invite link —
 * the two handles every later step needs.
 */
export async function createTrip(page, overrides = {}) {
  const trip = {
    name: unique('Ardennes Weekend'),
    location: 'Durbuy, Belgium',
    startDate: daysFromNow(30),
    endDate: daysFromNow(32),
    organiser: unique('Kris'),
    ...overrides,
  };

  await page.goto('/trips/create');
  await page.getByRole('textbox', { name: 'Trip name' }).fill(trip.name);
  await page.getByRole('textbox', { name: 'Location' }).fill(trip.location);
  await page.getByRole('textbox', { name: 'Start date' }).fill(trip.startDate);
  await page.getByRole('textbox', { name: 'End date' }).fill(trip.endDate);
  await page.getByRole('textbox', { name: 'Your name' }).fill(trip.organiser);
  await page.getByRole('button', { name: 'Create trip' }).click();

  await expect(page.getByRole('heading', { level: 1, name: trip.name })).toBeVisible();

  const url = stripQuery(page.url());
  return { ...trip, url, path: new URL(url).pathname, inviteUrl: await inviteUrl(page) };
}

/** The trip page keeps `?created=True` after the redirect; the bare URL is the shareable one. */
const stripQuery = (url) => url.split('?')[0];

/** The invite link currently on show, or null when the link has been revoked. */
export async function inviteUrl(page) {
  const field = page.locator('#inviteUrl');
  return (await field.count()) === 0 ? null : field.inputValue();
}

/** Opens an invite as a brand-new person and answers the "join as someone new" form. */
export async function joinAs(page, invite, displayName) {
  await page.goto(invite);
  await page.getByRole('textbox', { name: 'Your name' }).fill(displayName);
  await page.getByRole('button', { name: 'Join', exact: true }).click();
}

/** Claims an existing person from a second browser (FR-017). */
export async function resumeAs(page, invite, displayName) {
  await page.goto(invite);
  await page
    .getByRole('listitem')
    .filter({ hasText: displayName })
    .getByRole('button', { name: "That's me" })
    .click();
}

// --- Locators for the trip page -------------------------------------------------------

/** The "Who's coming" section, so roster assertions never stray into the nav or footer. */
export const roster = (page) =>
  page.locator('section').filter({ has: page.getByRole('heading', { name: /Who's coming/ }) });

/** One person's row in the roster. */
export const rosterEntry = (page, displayName) =>
  roster(page).getByRole('listitem').filter({ hasText: displayName });

/** An attendance button, matched on its prefix so "(your answer)" does not break it. */
export const attendanceButton = (page, label) =>
  page.getByRole('group', { name: 'Your attendance' }).getByRole('button', { name: new RegExp(`^${label}`) });

/** Answers for this browser and waits for the redirect-and-reload to settle. */
export async function setAttendance(page, label) {
  await attendanceButton(page, label).click();
  await expect(attendanceButton(page, label)).toHaveAttribute('aria-pressed', 'true');
}
