// 필드 아이템(힐팩·자석·폭탄)과 폭탄 폭발 그림을 굽습니다. 외부 패키지 없이 Node만 있으면 됩니다.
//
//   node tools/FieldItemArt/bake.mjs                  Assets/Resources/FieldItems 에 PNG와 meta를 씁니다.
//   node tools/FieldItemArt/bake.mjs --check          저장된 PNG·meta가 이 소스가 만드는 것과 같은지만 확인합니다.
//   node tools/FieldItemArt/bake.mjs --out <폴더>     다른 폴더에 씁니다.
//   node tools/FieldItemArt/bake.mjs --sheet <png>    확대한 확인용 그림 한 장을 따로 씁니다(에셋 아님).
//
// 그림은 난수 없이 정해진 계산으로만 만들어 다시 구워도 같은 파일이 나옵니다. GUID도 파일 이름에서 계산합니다.
// 파일 이름·프레임 수·픽셀 크기는 FieldItemSprites.cs의 상수와 같아야 합니다.
import { deflateSync } from "node:zlib";
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const ARGS = process.argv.slice(2);
const option = (name) => { const i = ARGS.indexOf(name); return i >= 0 ? ARGS[i + 1] : null; };
const OUT = option("--out") ? resolve(option("--out")) : join(ROOT, "Assets/Resources/FieldItems");
const CHECK = ARGS.includes("--check");
const SHEET = option("--sheet");

const PIXELS_PER_UNIT = 32;
const ITEM_SIZE = 32;
const BLAST_SIZE = 160;
const BLAST_FRAMES = 14; // 30fps, 마지막 프레임은 비어 있습니다.
const RING_SIZE = 128;

// ── 캔버스 ───────────────────────────────────────────────────────────
const hex = (s) => [parseInt(s.slice(1, 3), 16), parseInt(s.slice(3, 5), 16), parseInt(s.slice(5, 7), 16)];

function canvas(w, h) {
  const data = new Uint8Array(w * h * 4);
  const inside = (x, y) => x >= 0 && y >= 0 && x < w && y < h;
  return {
    w, h, data,
    set(x, y, color, alpha = 255) {
      if (!inside(x, y)) return;
      const i = (y * w + x) * 4;
      data[i] = color[0]; data[i + 1] = color[1]; data[i + 2] = color[2]; data[i + 3] = alpha;
    },
    alpha: (x, y) => (inside(x, y) ? data[(y * w + x) * 4 + 3] : 0),
    /** 모든 픽셀에 대해 색(#rrggbb)을 돌려주면 칠하고 null이면 건너뜁니다. */
    paint(pick) {
      for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const c = pick(x, y); if (c) this.set(x, y, hex(c)); }
    },
    /** 칠해진 픽셀의 상하좌우 빈칸에 외곽선을 두릅니다. */
    outline(color) {
      const rgb = hex(color), edge = [];
      for (let y = 0; y < h; y++) for (let x = 0; x < w; x++)
        if (!this.alpha(x, y) && (this.alpha(x - 1, y) || this.alpha(x + 1, y) || this.alpha(x, y - 1) || this.alpha(x, y + 1)))
          edge.push([x, y]);
      for (const [x, y] of edge) this.set(x, y, rgb);
    },
  };
}

// ── PNG ──────────────────────────────────────────────────────────────
const CRC = new Uint32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c >>> 0; });
function crc32(buffer) { let c = 0xffffffff; for (const b of buffer) c = CRC[(c ^ b) & 0xff] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; }
function chunk(type, body) {
  const head = Buffer.alloc(8); head.writeUInt32BE(body.length, 0); head.write(type, 4, "latin1");
  const tail = Buffer.alloc(4); tail.writeUInt32BE(crc32(Buffer.concat([head.subarray(4), body])), 0);
  return Buffer.concat([head, body, tail]);
}
function encodePng(c) {
  const header = Buffer.alloc(13);
  header.writeUInt32BE(c.w, 0); header.writeUInt32BE(c.h, 4); header[8] = 8; header[9] = 6; // 8비트 RGBA
  const raw = Buffer.alloc((c.w * 4 + 1) * c.h);
  for (let y = 0; y < c.h; y++) Buffer.from(c.data.buffer, y * c.w * 4, c.w * 4).copy(raw, y * (c.w * 4 + 1) + 1);
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", header),
    chunk("IDAT", deflateSync(raw, { level: 9 })), chunk("IEND", Buffer.alloc(0))]);
}

// ── 아이템 ───────────────────────────────────────────────────────────
const INK = "#1a1423";

function healPack() {
  const c = canvas(ITEM_SIZE, ITEM_SIZE);
  const corner = (x, y, x0, x1, y0, y1) => (x === x0 || x === x1) && (y === y0 || y === y1);
  const body = (x, y) => x >= 4 && x <= 27 && y >= 10 && y <= 27 && !corner(x, y, 4, 27, 10, 27);
  const handle = (x, y) => x >= 11 && x <= 20 && y >= 5 && y <= 9 && !(x >= 13 && x <= 18 && y >= 7) && !corner(x, y, 11, 20, 5, 5);
  const cross = (x, y) => (x >= 14 && x <= 17 && y >= 16 && y <= 25) || (x >= 11 && x <= 20 && y >= 19 && y <= 22);
  const clasp = (x, y) => (x === 8 || x === 9 || x === 22 || x === 23) && y >= 13 && y <= 15;
  c.paint((x, y) => {
    if (handle(x, y)) return y === 5 ? "#aab3c6" : x >= 19 ? "#4f566a" : "#6b7488";
    if (!body(x, y)) return null;
    if (clasp(x, y)) return y === 13 ? "#9aa3b8" : "#5b6478";
    if (cross(x, y)) {
      if (!cross(x + 1, y) || !cross(x, y + 1)) return "#b62c40";
      return !cross(x, y - 1) ? "#f47474" : "#e5484d";
    }
    if (y === 14) return "#9aa3b8";       // 뚜껑 이음매
    if (y === 27) return "#a9b2c5";
    if (y >= 26) return "#c5cddc";
    if (y === 10 || x === 4) return "#ffffff";
    if (x === 27) return "#d3dae6";
    return y < 14 ? "#f6f8fb" : "#eaeef4";
  });
  c.outline(INK);
  return c;
}

function magnet() {
  const c = canvas(ITEM_SIZE, ITEM_SIZE);
  const CX = 16, CY = 18, INNER = 4.5, OUTER = 10.5; // 픽셀 중심 좌표 기준
  // 반환: 두께 방향 위치 u(0 안쪽~1 바깥쪽)와 바깥을 향한 방향. 자석 밖이면 null.
  const at = (x, y) => {
    const dx = x + 0.5 - CX, dy = y + 0.5 - CY, ax = Math.abs(dx);
    if (dy < 0) return y >= 5 && ax > INNER && ax < OUTER ? { u: (ax - INNER) / (OUTER - INNER), nx: Math.sign(dx), ny: 0 } : null;
    const r = Math.hypot(dx, dy);
    return r > INNER && r < OUTER ? { u: (r - INNER) / (OUTER - INNER), nx: dx / r, ny: dy / r } : null;
  };
  c.paint((x, y) => {
    const p = at(x, y);
    if (!p) return null;
    if (y === 10) return INK;                                   // 은색 끝과 붉은 몸통의 경계
    const lit = (2 * p.u - 1) * (p.nx * -0.6 + p.ny * -0.8);     // 왼쪽 위에서 오는 빛
    if (y < 10) return lit > 0.35 ? "#ffffff" : lit < -0.25 ? "#a9b3c7" : "#dfe5ee";
    return lit > 0.35 ? "#ff8a8a" : lit < -0.45 ? "#8f1c33" : lit < -0.25 ? "#b62c40" : "#e5484d";
  });
  c.outline(INK);
  // 끌어당기는 느낌의 작은 불꽃
  for (const [x, y, color] of [[2, 3, "#8fe3ff"], [3, 2, "#ffffff"], [29, 3, "#8fe3ff"], [28, 2, "#ffffff"]])
    c.set(x, y, hex(color));
  return c;
}

function bomb() {
  const c = canvas(ITEM_SIZE, ITEM_SIZE);
  const CX = 15, CY = 19, R = 9;
  const fuse = [[15, 6], [15, 5], [16, 4], [17, 3], [18, 3], [19, 3], [20, 3]];
  c.paint((x, y) => {
    const dx = x + 0.5 - CX, dy = y + 0.5 - CY, r = Math.hypot(dx, dy);
    if (r <= R) {
      const lit = (dx * -0.6 + dy * -0.8) / R;                  // 1이면 빛을 정면으로 받는 쪽
      const shine = Math.hypot(dx + 3.5, dy + 4);
      if (shine < 1) return "#f1f5ff";
      if (shine < 2.3) return "#aab4d2";
      if (lit < -0.55 && r > R - 2) return "#566086";            // 바닥에서 되비친 가장자리 빛
      return lit > 0.3 ? "#5a6488" : lit < -0.3 ? "#2a2f47" : "#40476a";
    }
    if (x >= 13 && x <= 17 && y >= 7 && y <= 9) return y === 7 ? "#aab3c6" : x === 17 ? "#4f566a" : "#737c92"; // 심지 마개
    const step = fuse.findIndex(([fx, fy]) => fx === x && fy === y);
    return step < 0 ? null : step % 2 ? "#8a5a2c" : "#c9924c";
  });
  c.outline(INK);
  // 심지 끝 불꽃은 외곽선 없이 빛나게 둡니다.
  const spark = [
    [22, 3, "#ffffff"], [21, 3, "#ffe26b"], [23, 3, "#ffe26b"], [22, 2, "#ffe26b"], [22, 4, "#ffe26b"],
    [21, 2, "#ff8a2a"], [23, 2, "#ff8a2a"], [21, 4, "#ff8a2a"], [23, 4, "#ff8a2a"],
    [24, 3, "#ffc23d"], [22, 1, "#ffc23d"], [22, 5, "#ffc23d"], [25, 1, "#ff8a2a"], [25, 5, "#f2561d"], [19, 1, "#f2561d"],
  ];
  for (const [x, y, color] of spark) c.set(x, y, hex(color));
  return c;
}

// ── 폭탄 폭발 ────────────────────────────────────────────────────────
// 다른 스킬 그림(tools/PixelFxBake)과 같은 불 색 단계입니다. 어두운 색 → 흰색.
const FIRE = ["#4a1712", "#8a1f17", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#ffe26b", "#fff3a6", "#ffffff"];
const SMOKE = ["#2b2633", "#453f52", "#625c70"];

function hash(x, y, seed) {
  let h = (Math.imul(x, 374761393) + Math.imul(y, 668265263) + Math.imul(seed, 1442695041)) | 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}
function noise(x, y, seed) {
  const x0 = Math.floor(x), y0 = Math.floor(y), fx = x - x0, fy = y - y0;
  const sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
  const top = hash(x0, y0, seed) * (1 - sx) + hash(x0 + 1, y0, seed) * sx;
  const bottom = hash(x0, y0 + 1, seed) * (1 - sx) + hash(x0 + 1, y0 + 1, seed) * sx;
  return top * (1 - sy) + bottom * sy;
}
const fbm = (x, y, seed) => 0.5 * noise(x, y, seed) + 0.3 * noise(x * 2, y * 2, seed + 1) + 0.2 * noise(x * 4, y * 4, seed + 2);
const clamp01 = (v) => Math.min(1, Math.max(0, v));
const easeOut = (v) => 1 - (1 - clamp01(v)) ** 3;

function blastFrame(index) {
  const c = canvas(BLAST_SIZE, BLAST_SIZE);
  if (index >= BLAST_FRAMES - 1) return c;
  const t = index / (BLAST_FRAMES - 1), half = BLAST_SIZE / 2;
  const radius = 16 + 50 * easeOut(t / 0.5);                    // 불덩이 반지름
  const ring = 14 + 62 * easeOut(t / 0.7);                      // 충격파 고리 반지름
  const ringWidth = t < 0.35 ? 3 : 2;

  for (let y = 0; y < BLAST_SIZE; y++) for (let x = 0; x < BLAST_SIZE; x++) {
    const dx = x + 0.5 - half, dy = y + 0.5 - half, r = Math.hypot(dx, dy), dirX = r ? dx / r : 1, dirY = r ? dy / r : 0;
    // 방향마다 다르게 부풀어 가장자리가 울퉁불퉁한 불덩이
    const edge = radius * (0.8 + 0.36 * fbm(dirX * 4.2 + 7, dirY * 4.2 + 7, 11));
    if (r < edge) {
      // 식어 가며 연기 사이로 구멍이 뚫려 흩어집니다.
      if (fbm(dx * 0.075 + 9, dy * 0.075 + 9, 37) < (t - 0.42) * 1.75) continue;
      const churn = fbm(dx * 0.06 + t * 1.6, dy * 0.06 - t * 2.2, 23) - 0.5;
      const heat = (1 - r / edge) ** 0.55 * (1.3 - 1.55 * t) + churn * 0.55;
      if (heat > 0.16) c.set(x, y, hex(FIRE[Math.min(8, Math.floor(clamp01((heat - 0.16) / 0.84) * 9))]));
      else if (t < 0.25) c.set(x, y, hex(FIRE[0]));
      else c.set(x, y, hex(SMOKE[Math.min(2, Math.floor(clamp01(churn + 0.55) * 3))]));
      continue;
    }
    // 불덩이 밖으로 먼저 퍼지는 얇은 충격파. 끝으로 갈수록 점점이 끊어집니다.
    if (t < 0.92 && Math.abs(r - ring) < ringWidth / 2 && hash(x, y, 51) > (t - 0.45) * 2)
      c.set(x, y, hex(t < 0.3 ? FIRE[8] : t < 0.6 ? FIRE[7] : FIRE[5]));
  }

  // 사방으로 튀는 불똥
  for (let i = 0; i < 26; i++) {
    const angle = (i / 26) * Math.PI * 2 + hash(i, 1, 71) * 0.5, life = 0.55 + 0.4 * hash(i, 2, 71);
    if (t > life) continue;
    const reach = (54 + 23 * hash(i, 3, 71)) * easeOut(t / life * 0.9 + 0.1), age = t / life;
    const x = Math.round(half + Math.cos(angle) * reach), y = Math.round(half + Math.sin(angle) * reach);
    const color = hex(age < 0.3 ? FIRE[7] : age < 0.55 ? FIRE[5] : age < 0.8 ? FIRE[4] : FIRE[2]);
    c.set(x, y, color);
    if (age < 0.5) { c.set(x + 1, y, color); c.set(x, y + 1, color); c.set(x + 1, y + 1, color); }
  }
  return c;
}

/** 화면 전체로 퍼지는 파동에 쓰는 흰 고리입니다. 코드에서 색을 입히고 늘려 씁니다. */
function blastRing() {
  const c = canvas(RING_SIZE, RING_SIZE), half = RING_SIZE / 2;
  for (let y = 0; y < RING_SIZE; y++) for (let x = 0; x < RING_SIZE; x++) {
    const distance = Math.hypot(x + 0.5 - half, y + 0.5 - half) / half;
    if (distance > 1) { c.set(x, y, [255, 255, 255], 0); continue; }
    const band = clamp01(1 - Math.abs(distance - 0.86) / 0.14);  // 가장자리 근처의 밝은 띠
    const fill = 0.16 * clamp01((1 - distance) / 0.1);          // 안쪽의 옅은 채움
    c.set(x, y, [255, 255, 255], Math.round(Math.max(band, fill) * 255));
  }
  return c;
}

// ── Unity meta ───────────────────────────────────────────────────────
const idFor = (text) => createHash("md5").update("SBSAcademy_Team01/FieldItems/" + text).digest("hex");

function platform(name) {
  return `  - serializedVersion: 4
    buildTarget: ${name}
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
`;
}

/** 단일 스프라이트, 가운데 피벗, 압축 없음. filterMode 0은 Point, 1은 Bilinear입니다. */
function textureMeta(name, filterMode) {
  return `fileFormatVersion: 2
guid: ${idFor(name + ".png")}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: ${filterMode}
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: ${PIXELS_PER_UNIT}
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
${platform("DefaultTexturePlatform")}${platform("Standalone")}${platform("Android")}  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: ${idFor(name + ".sprite")}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

const folderMeta = () => `fileFormatVersion: 2
guid: ${idFor("folder")}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;

// ── 굽기 ─────────────────────────────────────────────────────────────
const sprites = [
  { name: "HealPack", image: healPack(), filter: 0 },
  { name: "Magnet", image: magnet(), filter: 0 },
  { name: "Bomb", image: bomb(), filter: 0 },
  { name: "BlastRing", image: blastRing(), filter: 1 },
];
for (let i = 0; i < BLAST_FRAMES; i++)
  sprites.push({ name: "BombBlast_" + String(i).padStart(2, "0"), image: blastFrame(i), filter: 0 });

const files = [{ path: OUT + ".meta", bytes: Buffer.from(folderMeta()), text: true }];
for (const sprite of sprites) {
  files.push({ path: join(OUT, sprite.name + ".png"), bytes: encodePng(sprite.image), text: false });
  files.push({ path: join(OUT, sprite.name + ".png.meta"), bytes: Buffer.from(textureMeta(sprite.name, sprite.filter)), text: true });
}

// Git이 체크아웃에서 줄 끝을 바꿔도 같은 내용으로 봅니다.
const same = (file) => {
  if (!existsSync(file.path)) return false;
  const saved = readFileSync(file.path);
  return file.text ? saved.toString().replace(/\r\n/g, "\n") === file.bytes.toString() : saved.equals(file.bytes);
};

if (CHECK) {
  const stale = files.filter((file) => !same(file));
  for (const file of stale) console.log("다름: " + file.path);
  console.log(`${files.length - stale.length}/${files.length} 파일 일치`);
  process.exit(stale.length ? 1 : 0);
}

mkdirSync(OUT, { recursive: true });
let written = 0;
for (const file of files) if (!same(file)) { writeFileSync(file.path, file.bytes); written++; }
console.log(`${OUT}: ${sprites.length}개 스프라이트, ${written}개 파일 갱신`);

// ── 확인용 그림 ──────────────────────────────────────────────────────
if (SHEET) {
  const zoom = 6, pad = 12, floors = ["#56606c", "#474a60", "#7fcf4c", "#d8ccb2"]; // 아스팔트·네온 골목·잔디·광장 바닥색
  const itemCell = ITEM_SIZE * zoom + pad, blastZoom = 2, blastCell = BLAST_SIZE * blastZoom + pad, columns = 7;
  const itemsHeight = floors.length * itemCell + pad, rows = Math.ceil(BLAST_FRAMES / columns);
  const sheet = canvas(columns * blastCell + pad, itemsHeight + rows * blastCell + pad);
  sheet.paint(() => "#2a2d3a");
  const blit = (image, left, top, scale) => {
    for (let y = 0; y < image.h; y++) for (let x = 0; x < image.w; x++) {
      const i = (y * image.w + x) * 4, a = image.data[i + 3] / 255;
      if (!a) continue;
      for (let v = 0; v < scale; v++) for (let u = 0; u < scale; u++) {
        const px = left + x * scale + u, py = top + y * scale + v, j = (py * sheet.w + px) * 4;
        sheet.set(px, py, [0, 1, 2].map((k) => Math.round(image.data[i + k] * a + sheet.data[j + k] * (1 - a))));
      }
    }
  };
  floors.forEach((floor, row) => {
    const top = pad + row * itemCell, color = hex(floor);
    for (let y = top - pad / 2; y < top + itemCell - pad / 2; y++) for (let x = pad / 2; x < 3 * itemCell + pad / 2; x++) sheet.set(x, y, color);
    sprites.slice(0, 3).forEach((sprite, column) => blit(sprite.image, pad + column * itemCell, top, zoom));
  });
  blit(sprites[3].image, pad + 3 * itemCell + pad, pad, 3);
  // 실제 크기 비교: 같은 배율(1배)로 나란히
  sprites.slice(0, 3).forEach((sprite, column) => blit(sprite.image, pad + 3 * itemCell + pad + column * 40, pad + RING_SIZE * 3 + pad, 1));
  for (let i = 0; i < BLAST_FRAMES; i++)
    blit(sprites[4 + i].image, pad + (i % columns) * blastCell, itemsHeight + Math.floor(i / columns) * blastCell, blastZoom);
  writeFileSync(resolve(SHEET), encodePng(sheet));
  console.log("확인용 그림: " + resolve(SHEET));
}
