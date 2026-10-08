import { test, expect, createTrip, joinAs, inviteUrl } from '../support/app.js';

test.describe('The invite link', () => {
  test('the starter gets the link and the controls for it', async ({ page }) => {
    await createTrip(page);

    await expect(page.locator('#inviteUrl')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Copy' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Revoke link' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Replace link' })).toBeVisible();
  });

  // FR-009 / FR-010: everyone may share the link; only the starter may change it.
  test('a participant who did not start the trip sees the link but cannot change it', async ({ page, device }) => {
    const trip = await createTrip(page);

    const guest = await device();
    await joinAs(guest, trip.inviteUrl, 'Marie');

    await expect(guest.locator('#inviteUrl')).toHaveValue(trip.inviteUrl);
    await expect(guest.getByRole('button', { name: 'Revoke link' })).toHaveCount(0);
    await expect(guest.getByRole('button', { name: 'Replace link' })).toHaveCount(0);
  });

  test('replacing the link kills the old one and hands out a working new one', async ({ page, device }) => {
    const trip = await createTrip(page);

    await page.getByRole('button', { name: 'Replace link' }).click();
    const replacement = await inviteUrl(page);
    expect(replacement).not.toBe(trip.inviteUrl);

    const holderOfOldLink = await device();
    await holderOfOldLink.goto(trip.inviteUrl);
    await expect(holderOfOldLink).toHaveURL(/\/InviteNotValid$/);

    const invitee = await device();
    await joinAs(invitee, replacement, 'Marie');
    await expect(invitee).toHaveURL(trip.url);
  });

  test('revoking the link shuts the door, and a new link opens it again', async ({ page, device }) => {
    const trip = await createTrip(page);

    await page.getByRole('button', { name: 'Revoke link' }).click();

    await expect(page.getByText('There is no active invite link. Nobody new can join.')).toBeVisible();
    await expect(page.locator('#inviteUrl')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Revoke link' })).toHaveCount(0);

    const turnedAway = await device();
    await turnedAway.goto(trip.inviteUrl);
    await expect(turnedAway).toHaveURL(/\/InviteNotValid$/);

    await page.getByRole('button', { name: 'Create a new link' }).click();
    const reopened = await inviteUrl(page);
    expect(reopened).toMatch(/\/join\/[\w-]+$/);
    expect(reopened).not.toBe(trip.inviteUrl);

    const invitee = await device();
    await joinAs(invitee, reopened, 'Marie');
    await expect(invitee).toHaveURL(trip.url);
  });

  // SC-010 / SC-011: a dead token and an invented one must be told apart by nobody.
  test('an invalid token reveals nothing about the trip', async ({ page, device }) => {
    const trip = await createTrip(page);
    await page.getByRole('button', { name: 'Revoke link' }).click();
    await expect(page.getByText('There is no active invite link. Nobody new can join.')).toBeVisible();

    const revoked = await device();
    await revoked.goto(trip.inviteUrl);
    const revokedText = await revoked.getByRole('main').innerText();

    const invented = await device();
    await invented.goto('/join/a-token-that-was-never-issued');
    const inventedText = await invented.getByRole('main').innerText();

    expect(revokedText).toBe(inventedText);
    expect(revokedText).toContain("This invite isn't valid");
    expect(revokedText).not.toContain(trip.name);
    expect(revokedText).not.toContain(trip.location);
  });
});
