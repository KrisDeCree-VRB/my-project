import fs from 'node:fs';
import { dbPath } from './server.js';

/**
 * Drops this run's database, including the write-ahead log SQLite keeps beside it.
 * Best-effort: the server process may still be letting go of the file, and a leftover in
 * the temp directory is not worth failing a green run over.
 */
export default function globalTeardown() {
  for (const file of [dbPath, `${dbPath}-wal`, `${dbPath}-shm`]) {
    try {
      fs.rmSync(file, { force: true });
    } catch {
      // The OS will clear the temp directory eventually.
    }
  }
}
