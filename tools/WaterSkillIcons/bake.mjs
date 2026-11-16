// 물 스킬 아이콘. 32px 문양을 기존 등급 테두리 중앙에 올려 64px 카드 그림을 만듭니다.
import crypto from 'crypto';
import fs from 'fs';
import path from 'path';
import zlib from 'zlib';

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Za-z]:)/, '$1'), '../..');
const OUT = [6, 20, 40, 255];
const DEEP = [10, 52, 112, 255];
const MID = [36, 138, 220, 255];
const HI = [154, 216, 255, 255];
const FOAM = [232, 246, 255, 255];
const WHITE = [255, 255, 255, 255];

function canvas(n) {
  const px = new Uint8Array(n * n * 4);
  const g = {
    px, n,
    plot(x, y, c) {
      x = Math.round(x); y = Math.round(y);
      if (x < 0 || y < 0 || x >= n || y >= n || !c) return;
      const i = (y * n + x) * 4;
      px[i] = c[0]; px[i + 1] = c[1]; px[i + 2] = c[2]; px[i + 3] = c[3];
    },
    disc(x, y, r, c) {
      const R = Math.ceil(r + 0.5);
      for (let yy = -R; yy <= R; yy++) for (let xx = -R; xx <= R; xx++)
        if (xx * xx + yy * yy <= r * r + 0.35) g.plot(x + xx, y + yy, c);
    },
    line(x0, y0, x1, y1, r, c) {
      const steps = Math.max(1, Math.ceil(Math.hypot(x1 - x0, y1 - y0) * 2));
      for (let i = 0; i <= steps; i++) {
        const t = i / steps;
        g.disc(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, r, c);
      }
    }
  };
  return g;
}

function shift(x0, y0, x1, y1, dist) {
  const dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy) || 1;
  const px = -dy / len * dist, py = dx / len * dist;
  return [x0 + px, y0 + py, x1 + px, y1 + py];
}

function jet(g, x0, y0, x1, y1, r, body = MID, core = HI) {
  g.line(x0, y0, x1, y1, r + 1.15, OUT);
  g.line(x0, y0, x1, y1, r, body);
  if (core) g.line(x0, y0, x1, y1, Math.max(0.55, r * 0.42), core);
  g.disc(x1, y1, r * 0.55, FOAM);
}

function spark(g, x, y) {
  g.plot(x, y, WHITE);
  g.plot(x - 1, y, FOAM); g.plot(x + 1, y, FOAM);
  g.plot(x, y - 1, FOAM); g.plot(x, y + 1, FOAM);
}

function puddle(g, r) {
  g.disc(16, 18, r + 1.3, OUT);
  g.disc(16, 18, r, MID);
  g.disc(14, 16, Math.max(1.2, r * 0.38), HI);
  g.disc(12, 15, 0.8, FOAM);
}

function ring(g, outer, inner) {
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    const d = Math.hypot(x - 16, y - 18);
    if (d <= outer + 1.1 && d >= inner - 0.2) g.plot(x, y, d > outer - 0.4 || d < inner + 0.6 ? OUT : MID);
    if (d <= outer - 0.8 && d >= inner + 0.7) g.plot(x, y, HI);
  }
  g.disc(12, 14, 0.8, FOAM);
}

function wedge(g, halfDeg) {
  const rad = halfDeg * Math.PI / 180;
  g.disc(8, 22, 1.4, OUT);
  for (let step = -8; step <= 8; step++) {
    const a = -Math.PI / 4 + rad * (step / 8);
    const len = 16 - Math.abs(step) * 0.35;
    g.line(8, 22, 8 + Math.cos(a) * len, 22 - Math.sin(a) * len, step === 0 ? 1.5 : 0.85, step === 0 ? HI : MID);
  }
}

function shots(g, count) {
  for (let i = 0; i < count; i++) {
    const y = 8 + i * (16 / Math.max(1, count - 1));
    jet(g, 7, y + 4, 24, y - 2, count > 2 ? 0.9 : 1.3);
  }
}

const skills = [
  { id: 10022, rarity: 'Common', draw: (g) => { jet(g, 8, 24, 23, 9, 2.1); g.disc(7, 26, 1.1, MID); } },
  { id: 10023, rarity: 'Common', draw: (g) => puddle(g, 7) },
  { id: 10024, rarity: 'Uncommon', draw: (g) => { for (const d of [-3.2, 0, 3.2]) { const [a, b, c, e] = shift(9, 23, 23, 9, d); jet(g, a, b, c, e, 1.15); } } },
  { id: 10025, rarity: 'Uncommon', draw: (g) => shots(g, 1) },
  { id: 10026, rarity: 'Uncommon', draw: (g) => shots(g, 3) },
  { id: 10027, rarity: 'Rare', draw: (g) => { jet(g, 6, 26, 24, 8, 2.3); g.disc(25, 7, 1.6, WHITE); } },
  { id: 10028, rarity: 'Rare', draw: (g) => wedge(g, 35) },
  { id: 10029, rarity: 'Rare', draw: (g) => puddle(g, 8) },
  { id: 10030, rarity: 'Epic', draw: (g) => { jet(g, 4, 27, 27, 4, 1.25); spark(g, 27, 4); } },
  { id: 10031, rarity: 'Epic', draw: (g) => { puddle(g, 6); for (const y of [10, 14, 18]) g.line(12, y, 20, y, 0.6, FOAM); } },
  { id: 10032, rarity: 'Epic', draw: (g) => ring(g, 11, 7) },
  { id: 10033, rarity: 'Legendary', draw: (g) => puddle(g, 11) },
  { id: 10034, rarity: 'Legendary', draw: (g) => shots(g, 4) },
  { id: 10035, rarity: 'Legendary', draw: (g) => wedge(g, 70) },
  { id: 10036, rarity: 'Mythic', draw: (g) => { jet(g, 5, 27, 24, 8, 2.4, MID, WHITE); spark(g, 26, 6); spark(g, 23, 5); } },
  { id: 20016, rarity: 'Common', draw: (g) => { jet(g, 11, 21, 21, 11, 3.2); } },
  { id: 20017, rarity: 'Common', draw: (g) => wedge(g, 80) },
  { id: 20018, rarity: 'Uncommon', draw: (g) => wedge(g, 45) },
  { id: 20019, rarity: 'Uncommon', draw: (g) => shots(g, 2) },
  { id: 20020, rarity: 'Rare', draw: (g) => { jet(g, 6, 26, 18, 14, 2.2); g.disc(23, 9, 3.3, OUT); g.disc(23, 9, 2.2, MID); g.disc(23, 9, 1, WHITE); } },
  { id: 20021, rarity: 'Rare', draw: (g) => { puddle(g, 7); spark(g, 16, 12); spark(g, 20, 18); } },
  { id: 20022, rarity: 'Rare', draw: (g) => shots(g, 1) },
  { id: 20023, rarity: 'Epic', draw: (g) => ring(g, 12, 8) },
  { id: 20024, rarity: 'Epic', draw: (g) => { jet(g, 6, 26, 25, 7, 3.2, DEEP, WHITE); } },
  { id: 20025, rarity: 'Epic', draw: (g) => wedge(g, 90) },
  { id: 20026, rarity: 'Legendary', draw: (g) => { jet(g, 3, 28, 28, 3, 0.9, HI, WHITE); spark(g, 8, 20); spark(g, 18, 10); } },
  { id: 20027, rarity: 'Legendary', draw: (g) => puddle(g, 10) },
  { id: 20028, rarity: 'Mythic', draw: (g) => { jet(g, 5, 26, 26, 5, 4.2, MID, WHITE); } },
  { id: 20029, rarity: 'Mythic', draw: (g) => wedge(g, 100) },
  { id: 20030, rarity: 'Mythic', draw: (g) => { puddle(g, 11); g.line(8, 20, 24, 16, 1.1, FOAM); } }
];

function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return (~c) >>> 0;
}
function chunk(type, data) {
  const t = Buffer.from(type);
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(Buffer.concat([t, data])));
  return Buffer.concat([len, t, data, crc]);
}
function encodePng(w, h, px) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (w * 4 + 1)] = 0;
    Buffer.from(px.buffer, px.byteOffset + y * w * 4, w * 4).copy(raw, y * (w * 4 + 1) + 1);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))
  ]);
}
function decodePng(file) {
  const buf = fs.readFileSync(file);
  let o = 8, w = 0, h = 0, parts = [];
  while (o < buf.length) {
    const len = buf.readUInt32BE(o);
    const type = buf.toString('ascii', o + 4, o + 8);
    const data = buf.subarray(o + 8, o + 8 + len);
    if (type === 'IHDR') { w = data.readUInt32BE(0); h = data.readUInt32BE(4); }
    if (type === 'IDAT') parts.push(data);
    if (type === 'IEND') break;
    o += 12 + len;
  }
  const raw = zlib.inflateSync(Buffer.concat(parts));
  const px = new Uint8Array(w * h * 4);
  let i = 0;
  for (let y = 0; y < h; y++) {
    const filter = raw[i++];
    for (let x = 0; x < w; x++) {
      const di = (y * w + x) * 4;
      for (let c = 0; c < 4; c++) {
        const left = x ? px[di - 4 + c] : 0;
        const up = y ? px[di - w * 4 + c] : 0;
        const ul = x && y ? px[di - w * 4 - 4 + c] : 0;
        let v = raw[i++];
        if (filter === 1) v = (v + left) & 255;
        else if (filter === 2) v = (v + up) & 255;
        else if (filter === 3) v = (v + ((left + up) >> 1)) & 255;
        else if (filter === 4) {
          const p = left + up - ul;
          const pa = Math.abs(p - left), pb = Math.abs(p - up), pc = Math.abs(p - ul);
          v = (v + (pa <= pb && pa <= pc ? left : pb <= pc ? up : ul)) & 255;
        }
        px[di + c] = v;
      }
    }
  }
  return px;
}

function metaFor(template, guid, spriteId) {
  return template
    .replace(/guid: [0-9a-f]{32}/, `guid: ${guid}`)
    .replace(/spriteID: [0-9a-f]{32}/, `spriteID: ${spriteId}`);
}

const glyphMeta = fs.readFileSync(path.join(root, 'Assets/2.Resources/Art/SkillIcons/SkillIcon_10001.png.meta'), 'utf8');
const cardMeta = fs.readFileSync(path.join(root, 'Assets/Resources/SkillIcons/Skill_10001.png.meta'), 'utf8');
const frames = {};
for (const rarity of ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary', 'Mythic'])
  frames[rarity] = decodePng(path.join(root, `Assets/2.Resources/Art/SkillIcons/SkillFrame_${rarity}.png`));

const used = new Set();
function hexId() {
  let id = '';
  do { id = crypto.randomBytes(16).toString('hex'); } while (used.has(id));
  used.add(id);
  return id;
}

const sheetCols = 6, scale = 3, gap = 8;
const sheetW = sheetCols * (64 * scale) + (sheetCols + 1) * gap;
const sheetH = 5 * (64 * scale) + 6 * gap;
const sheet = new Uint8Array(sheetW * sheetH * 4);
for (let i = 0; i < sheet.length; i += 4) { sheet[i] = 18; sheet[i + 1] = 16; sheet[i + 2] = 22; sheet[i + 3] = 255; }

skills.forEach((skill, index) => {
  const g = canvas(32);
  skill.draw(g);
  const card = new Uint8Array(frames[skill.rarity]);
  for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
    const s = (y * 32 + x) * 4;
    if (g.px[s + 3] === 0) continue;
    const d = ((y + 16) * 64 + (x + 16)) * 4;
    card[d] = g.px[s]; card[d + 1] = g.px[s + 1]; card[d + 2] = g.px[s + 2]; card[d + 3] = g.px[s + 3];
  }
  const glyphPath = path.join(root, `Assets/2.Resources/Art/SkillIcons/SkillIcon_${skill.id}.png`);
  const cardPath = path.join(root, `Assets/Resources/SkillIcons/Skill_${skill.id}.png`);
  fs.writeFileSync(glyphPath, encodePng(32, 32, g.px));
  fs.writeFileSync(cardPath, encodePng(64, 64, card));
  if (!fs.existsSync(glyphPath + '.meta'))
    fs.writeFileSync(glyphPath + '.meta', metaFor(glyphMeta, hexId(), hexId()));
  if (!fs.existsSync(cardPath + '.meta'))
    fs.writeFileSync(cardPath + '.meta', metaFor(cardMeta, hexId(), hexId()));
  const col = index % sheetCols, row = Math.floor(index / sheetCols);
  const ox = gap + col * (64 * scale + gap), oy = gap + row * (64 * scale + gap);
  for (let y = 0; y < 64; y++) for (let x = 0; x < 64; x++) {
    const s = (y * 64 + x) * 4;
    for (let dy = 0; dy < scale; dy++) for (let dx = 0; dx < scale; dx++) {
      const d = ((oy + y * scale + dy) * sheetW + (ox + x * scale + dx)) * 4;
      if (card[s + 3] === 0) continue;
      sheet[d] = card[s]; sheet[d + 1] = card[s + 1]; sheet[d + 2] = card[s + 2]; sheet[d + 3] = 255;
    }
  }
});

fs.mkdirSync(path.join(root, '.utmp/water-icons'), { recursive: true });
fs.writeFileSync(path.join(root, '.utmp/water-icons/sheet.png'), encodePng(sheetW, sheetH, sheet));
console.log(`wrote ${skills.length} icons`);
