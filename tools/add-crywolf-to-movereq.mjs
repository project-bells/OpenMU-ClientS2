import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const recordSize = 84;
const xorKey = [0xfc, 0xcf, 0xab];
const repositoryRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const files = [
  "src/bin/Data/Local/Eng/MoveReq_eng.bmd",
  "src/bin/Data/Local/Por/MoveReq_por.bmd",
  "src/bin/Data/Local/Spn/movereq_spn.bmd",
];

function crypt(record) {
  const result = Buffer.from(record);
  for (let index = 0; index < result.length; index += 1) {
    result[index] ^= xorKey[index % xorKey.length];
  }

  return result;
}

function readRecord(buffer, position) {
  const start = 4 + position * recordSize;
  return crypt(buffer.subarray(start, start + recordSize));
}

function createCrywolfRecord() {
  const record = Buffer.alloc(recordSize);
  record.writeInt32LE(26, 0);
  record.write("Crywolf", 4, 32, "ascii");
  record.write("Crywolf", 36, 32, "ascii");
  record.writeInt32LE(190, 68);
  record.writeInt32LE(400, 72);
  record.writeInt32LE(10000, 76);
  record.writeInt32LE(118, 80);
  return crypt(record);
}

for (const relativeFile of files) {
  const file = path.join(repositoryRoot, relativeFile);
  const buffer = fs.readFileSync(file);
  const count = buffer.readInt32LE(0);
  const capacity = (buffer.length - 4) / recordSize;

  if (!Number.isInteger(capacity) || count >= capacity) {
    throw new Error(`${relativeFile}: invalid or full MoveReq file`);
  }

  const records = Array.from({ length: count }, (_, position) =>
    buffer.subarray(4 + position * recordSize, 4 + (position + 1) * recordSize),
  );
  const existingPosition = records.findIndex((_, position) => readRecord(buffer, position).readInt32LE(0) === 26);
  if (existingPosition >= 0) {
    console.log(`${relativeFile}: Crywolf already exists at position ${existingPosition}`);
    continue;
  }

  const aida2Position = records.findIndex((_, position) => readRecord(buffer, position).readInt32LE(0) === 27);
  const insertionPosition = aida2Position >= 0 ? aida2Position : count;
  records.splice(insertionPosition, 0, createCrywolfRecord());

  const output = Buffer.alloc(buffer.length);
  output.writeInt32LE(records.length, 0);
  records.forEach((record, position) => record.copy(output, 4 + position * recordSize));
  fs.writeFileSync(file, output);
  console.log(`${relativeFile}: added Crywolf at position ${insertionPosition}`);
}
