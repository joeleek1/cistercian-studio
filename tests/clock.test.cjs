const test=require('node:test');
const assert=require('node:assert/strict');
const {partsAt}=require('../dist/clock.js');

test('UTC clock advances seconds and rolls into a new date and year',()=>{
  const before=partsAt(new Date('2026-12-31T23:59:59Z'),'UTC');
  const after=partsAt(new Date('2027-01-01T00:00:00Z'),'UTC');
  assert.deepEqual([before.hours,before.minutes,before.seconds,before.day,before.month,before.year],['23','59','59','31','12','2026']);
  assert.deepEqual([after.hours,after.minutes,after.seconds,after.day,after.month,after.year],['00','00','00','01','01','2027']);
  assert.equal(after.label,'Friday, 1 January 2027');
});

test('London uses GMT in winter and BST in summer with correct date rollover',()=>{
  const winter=partsAt(new Date('2026-11-17T12:34:56Z'));
  const summer=partsAt(new Date('2026-07-01T23:30:05Z'));
  assert.deepEqual([winter.hours,winter.minutes,winter.seconds],['12','34','56']);
  assert.deepEqual([summer.hours,summer.day,summer.month],['00','02','07']);
});

test('London clock handles the daylight-saving transitions',()=>{
  const spring=partsAt(new Date('2026-03-29T01:00:00Z'));
  const autumnBefore=partsAt(new Date('2026-10-25T00:59:59Z'));
  const autumnAfter=partsAt(new Date('2026-10-25T01:00:00Z'));
  assert.equal(spring.hours,'02');
  assert.deepEqual([autumnBefore.hours,autumnBefore.minutes],['01','59']);
  assert.deepEqual([autumnAfter.hours,autumnAfter.minutes],['01','00']);
});

test('calendar shows leap days and the selected device time zone',()=>{
  const leap=partsAt(new Date('2028-02-29T12:00:00Z'),'UTC');
  assert.deepEqual([leap.day,leap.month,leap.year],['29','02','2028']);
  const device=partsAt(new Date('2026-11-17T12:00:00Z'),'device');
  assert.equal(device.zone,Intl.DateTimeFormat().resolvedOptions().timeZone);
});
