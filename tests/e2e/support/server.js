import os from 'node:os';
import path from 'node:path';

/**
 * Not 5202: that is the port `dotnet run` uses for development, and the suite must not
 * attach itself to — or write into — whatever a developer already has running there.
 */
export const PORT = 5203;

/**
 * A database of this run's own, created by `EnsureCreated()` when the server boots and
 * removed again by global-teardown.
 *
 * The name is unique rather than fixed-and-deleted-first: Playwright starts the web server
 * before global setup runs, so by the time anything could delete a fixed file the server
 * already holds it open — and on Windows that delete fails outright.
 */
export const dbPath = path.join(os.tmpdir(), `weekendaway-e2e-${process.pid}-${Date.now()}.db`);
