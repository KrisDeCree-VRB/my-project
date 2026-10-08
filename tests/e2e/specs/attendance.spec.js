import {
  test, expect, createTrip, joinAs, roster, rosterEntry, attendanceButton, setAttendance,
} from '../support/app.js';

test.describe('Answering "are you coming?"', () => {
  test('a new participant starts out as unanswered', async ({ page }) => {
    const trip = await createTrip(page);

    await expect(attendanceButton(page, 'Not sure yet')).toHaveAttribute('aria-pressed', 'true');
    await expect(attendanceButton(page, "I'm in")).toHaveAttribute('aria-pressed', 'false');
    await expect(attendanceButton(page, "I'm out")).toHaveAttribute('aria-pressed', 'false');
    await expect(rosterEntry(page, trip.organiser)).toContainText("Hasn't answered");
  });

  test('one click records the answer and moves the headcount', async ({ page }) => {
    const trip = await createTrip(page);

    await expect(roster(page)).toContainText('0 in');
    await expect(roster(page)).toContainText('1 yet to answer');

    await setAttendance(page, "I'm in");

    await expect(roster(page)).toContainText('1 in');
    await expect(roster(page)).toContainText('0 yet to answer');
    await expect(rosterEntry(page, trip.organiser)).toContainText('✓ In');
  });

  test('the answer can be changed again afterwards', async ({ page }) => {
    const trip = await createTrip(page);

    await setAttendance(page, "I'm in");
    await setAttendance(page, "I'm out");

    await expect(roster(page)).toContainText('0 in');
    await expect(roster(page)).toContainText('1 out');
    await expect(rosterEntry(page, trip.organiser)).toContainText('✗ Out');
    await expect(attendanceButton(page, "I'm in")).toHaveAttribute('aria-pressed', 'false');
  });

  // FR-007 / SUC-03: the point of the roster is seeing other people's answers, not just
  // your own, so this has to be checked across two browsers.
  test("each person sees the others' answers", async ({ page, device }) => {
    const trip = await createTrip(page);

    const guest = await device();
    await joinAs(guest, trip.inviteUrl, 'Marie');

    await setAttendance(page, "I'm in");
    await setAttendance(guest, "I'm out");

    await page.reload();
    await expect(roster(page)).toContainText('1 in');
    await expect(roster(page)).toContainText('1 out');
    await expect(rosterEntry(page, trip.organiser)).toContainText('✓ In');
    await expect(rosterEntry(page, 'Marie')).toContainText('✗ Out');

    // SC-005: everyone's status is shown, but only your own is offered as a control.
    await expect(rosterEntry(guest, trip.organiser).getByRole('button')).toHaveCount(0);
  });

  test('a participant can rename themselves', async ({ page }) => {
    const trip = await createTrip(page);

    await page.getByRole('textbox', { name: 'Your display name' }).fill('Kris the Organiser');
    await page.getByRole('button', { name: 'Save' }).click();

    await expect(rosterEntry(page, 'Kris the Organiser')).toContainText('you');
    await expect(rosterEntry(page, trip.organiser)).toHaveCount(0);
  });

  test('a rename onto a name already in the trip is refused', async ({ page, device }) => {
    const trip = await createTrip(page);

    const guest = await device();
    await joinAs(guest, trip.inviteUrl, 'Marie');

    await guest.getByRole('textbox', { name: 'Your display name' }).fill(trip.organiser);
    await guest.getByRole('button', { name: 'Save' }).click();

    await expect(guest.getByText('Someone in this trip already uses that name.')).toBeVisible();
    await expect(rosterEntry(guest, 'Marie')).toContainText('you');
  });
});
