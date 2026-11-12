import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Inspect the actual scene/prefab positions without opening or saving Unity.
const scene = readFileSync(new URL('../../Assets/9.Scenes/MainScene.unity', import.meta.url), 'utf8');
const prefab = readFileSync(new URL('../../Assets/Joystick Pack/Prefabs/Variable Joystick.prefab', import.meta.url), 'utf8');
function block(text, id) {
  const match = text.match(new RegExp(`^--- !u!\\d+ &${id}\\b[^\\n]*\\n([\\s\\S]*?)(?=^--- !u!|$(?![\\s\\S]))`, 'm'));
  assert.ok(match, `Missing serialized object ${id}`);
  return match[1];
}
function vector(text, field) {
  const match = text.match(new RegExp(`${field}: \\{x: ([^,]+), y: ([^,}]+)`));
  assert.ok(match, `Missing ${field}`);
  return match.slice(1).map(Number);
}
function rect(text, id) {
  const value = block(text, id);
  const min = vector(value, 'm_AnchorMin');
  const max = vector(value, 'm_AnchorMax');
  assert.deepEqual(min, max, `Expected point anchors for ${id}`);
  return { anchor: min, pos: vector(value, 'm_AnchoredPosition'), size: vector(value, 'm_SizeDelta'),
    pivot: vector(value, 'm_Pivot'), scale: vector(value, 'm_LocalScale') };
}
const group = rect(scene, '1865912158');
const tactical = rect(scene, '1724077528');
const buttonIds = ['1724077528', '452539070', '1415531112', '1437309639', '1125569979'];
const groupChildren = [...block(scene, '1865912158').match(/m_Children:([\s\S]*?)m_Father:/)[1].matchAll(/fileID: (\d+)/g)].map(m => m[1]);
assert.deepEqual(groupChildren, buttonIds, '전략 버튼은 강공격 위의 첫 번째 자식이어야 합니다.');
assert.ok(block(scene, '1724077528').includes('m_Father: {fileID: 1865912158}'));
const layout = block(scene, '742152530');
for (const reference of ['buttonGroup: {fileID: 1865912158}', 'joystickRoot: {fileID: 1269801975}', 'joystickBackground: {fileID: 1276861302}']) {
  assert.ok(layout.includes(reference), `MobileControlLayout reference: ${reference}`);
}
assert.ok(block(scene, '1276861302').includes('m_CorrespondingSourceObject: {fileID: 3881087803815115471,'));
const instance = block(scene, '1276861301');
const overrides = new Map([...instance.matchAll(/target: \{fileID: (\d+),[^\n]+\n\s+propertyPath: ([^\r\n]+)\r?\n\s+value: ([^\r\n]*)/g)]
  .map(m => [`${m[1]}/${m[2]}`, Number(m[3])]));
function prefabRect(id) {
  const result = rect(prefab, id);
  for (const [field, yaml] of Object.entries({ anchor: 'm_AnchorMin', pos: 'm_AnchoredPosition', size: 'm_SizeDelta', pivot: 'm_Pivot', scale: 'm_LocalScale' })) {
    for (let axis = 0; axis < 2; axis++) {
      const key = `${id}/${yaml}.${axis ? 'y' : 'x'}`;
      if (overrides.has(key)) result[field][axis] = overrides.get(key);
    }
  }
  return result;
}
const joystickRoot = prefabRect('3881087803549984436');
const joystickBackground = prefabRect('3881087803815115471');
const children = buttonIds.map(id => rect(scene, id));
const spacing = Number(block(scene, '1865912159').match(/m_Spacing: ([^\r\n]+)/)[1]);
const cases = [
  ['16:9', 1920, 1080, 0, 0, 0, 0],
  ['20:9', 2400, 1080, 0, 0, 0, 0],
  ['20:9 left cutout', 2400, 1080, 120, 0, 0, 0],
  ['20:9 right cutout + gesture bar', 2400, 1080, 0, 120, 60, 0],
  ['4:3 tablet', 2048, 1536, 0, 0, 0, 0],
  ['720p', 1280, 720, 0, 0, 0, 0],
];
let failures = 0;
for (const [name, width, height, left, right, bottom, top] of cases) {
  const safeWidth = width - left - right, safeHeight = height - bottom - top;
  const scale = Math.min(safeWidth / 1920, safeHeight / 1080);
  const point = r => [left + safeWidth * r.anchor[0] + r.pos[0] * scale,
    bottom + safeHeight * r.anchor[1] + r.pos[1] * scale];
  const groupPoint = point(group);
  const stripLeft = groupPoint[0] - group.size[0] * group.pivot[0] * group.scale[0] * scale;
  const stripRight = stripLeft + Math.max(...children.map(c => c.size[0])) * group.scale[0] * scale;
  const stripTop = groupPoint[1] + group.size[1] * (1 - group.pivot[1]) * group.scale[1] * scale;
  const stripBottom = stripTop - (children.reduce((n, c) => n + c.size[1], 0) + spacing * (children.length - 1)) * group.scale[1] * scale;
  const origin = point(joystickRoot).map((v, i) => v - joystickRoot.pivot[i] * joystickRoot.size[i] * scale);
  const joy = origin.map((v, i) => v + (joystickRoot.size[i] * joystickBackground.anchor[i] + joystickBackground.pos[i]) * scale);
  const radius = joystickBackground.size.map(v => v * scale / 2);
  const checks = [
    ['left skills stay at safe left edge', stripLeft >= left + 16 * scale && stripRight <= left + 180 * scale],
    ['all five skills fit vertically', stripBottom >= bottom + 16 * scale && stripTop <= height - top],
    ['joystick leaves thumb room at right/bottom', width - right - joy[0] - radius[0] >= 128 * scale && joy[1] - radius[1] - bottom >= 128 * scale],
    ['buttons and joystick have separate touch regions', stripRight + 24 * scale < joy[0] - radius[0]],
    ['tactical button has the same usable size and spacing', tactical.size.every(v => v >= 120) && spacing >= 24],
  ];
  const failed = checks.filter(([, passed]) => !passed);
  failures += failed.length;
  console.log(`${name}: ${checks.length - failed.length}/${checks.length}${failed.length ? ' — ' + failed.map(([label]) => label).join('; ') : ''}`);
}
assert.equal(failures, 0, `${failures} mobile layout checks failed`);
console.log('Mobile layout geometry: 30/30 checks passed (static, not device rendering).');
