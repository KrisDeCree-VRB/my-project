import { test, expect, createTrip, joinAs, resumeAs, roster, rosterEntry } from '../support/app.js';

test.describe('Joining by invite', () => {
  test('an invitee sees the trip and the two ways to identify themselves', async ({ page, device }) => {
    const trip = await createTrip(page);

    const invitee = await device();
    await invitee.goto(trip.inviteUrl);

    await expect(invitee.getByRole('heading', { level: 1, name: trip.name })).toBeVisible();
    await expect(invitee.getByText(`Already coming: ${trip.organiser}`)).toBeVisible();
    await expect(invitee.getByRole('heading', { name: 'Join as someone new' })).toBeVisible();
    await expect(invitee.getByRole('heading', { name: 'Already joined on another device?' })).toBeVisible();
  });

  test('joining as someone new puts them in the trip', async ({ page, device }) => {
    const trip = await createTrip(page);

    const invitee = await device();
    await joinAs(invitee, trip.inviteUrl, 'Marie');

    await expect(invitee).toHaveURL(trip.url);
    await expect(rosterEntry(invitee, 'Marie')).toContainText('you');
    await expect(roster(invitee)).toContainText("Who's coming (2)");

    await page.reload();
    await expect(rosterEntry(page, 'Marie')).toBeVisible();
  });

  // EC-1 / INV-3: two people called the same thing could not be told apart in the roster.
  test('a name already taken in the trip is refused', async ({ page, device }) => {
    const trip = await createTrip(page);

    const invitee = await device();
    await joinAs(invitee, trip.inviteUrl, trip.organiser);

    await expect(invitee.getByText('Someone in this trip already uses that name. Pick another one.')).toBeVisible();
    // Still on the invite, not let into the trip.
    await expect(invitee.getByRole('heading', { name: 'Join as someone new' })).toBeVisible();
  });

  test('a blank name is refused', async ({ page, device }) => {
    const trip = await createTrip(page);

    const invitee = await device();
    await invitee.goto(trip.inviteUrl);
    await invitee.getByRole('button', { name: 'Join', exact: true }).click();

    await expect(invitee.getByText('Enter a name of 1 to 50 characters.')).toBeVisible();
  });

  // FR-016 / SC-013: a member who opens the link again goes straight in, and is not
  // offered a second identity.
  test('someone already in the trip is taken straight to it', async ({ page }) => {
    const trip = await createTrip(page);

    await page.goto(trip.inviteUrl);

    await expect(page).toHaveURL(trip.url);
    await expect(page.getByRole('heading', { name: 'Join as someone new' })).toHaveCount(0);
  });

  // FR-017: the same person on a second device resumes, rather than appearing twice.
  test('"That\'s me" resumes an existing person instead of adding another', async ({ page, device }) => {
    const trip = await createTrip(page);

    const phone = await device();
    await joinAs(phone, trip.inviteUrl, 'Marie');

    const laptop = await device();
    await resumeAs(laptop, trip.inviteUrl, 'Marie');

    await expect(laptop).toHaveURL(trip.url);
    // Still two people, not three, and this browser *is* Marie.
    await expect(roster(laptop)).toContainText("Who's coming (2)");
    await expect(rosterEntry(laptop, 'Marie')).toContainText('you');
  });

  test('a resumed device answers for the person it claimed', async ({ page, device }) => {
    const trip = await createTrip(page);

    const phone = await device();
    await joinAs(phone, trip.inviteUrl, 'Marie');

    const laptop = await device();
    await resumeAs(laptop, trip.inviteUrl, 'Marie');
    await laptop.getByRole('button', { name: /^I'm in/ }).click();

    await phone.reload();
    await expect(rosterEntry(phone, 'Marie')).toContainText('✓ In');
    await expect(roster(phone)).toContainText('1 in');
  });
});
