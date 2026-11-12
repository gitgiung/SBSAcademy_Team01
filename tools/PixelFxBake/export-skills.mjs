#!/usr/bin/env node
// Bakes the skill effects in tools/PixelFxBake/skills into sprite sheets the game slices at run time.
//
//   node tools/PixelFxBake/export-skills.mjs                      bake everything into Assets/Resources/LightSkills/Fx
//   node tools/PixelFxBake/export-skills.mjs --only FireBullet,IceShard
//   node tools/PixelFxBake/export-skills.mjs --preview            only write contact sheets to .utmp/pixelfx-preview
//   node tools/PixelFxBake/export-skills.mjs --pixelfx <dir>      where patch-gg/pixelfx is cloned (default .utmp/pixelfx)
//
// Needs a clone of https://github.com/patch-gg/pixelfx (MIT). The sources are copied into its src/fx/skill first.
// Each effect becomes <Name>.png (frames in a grid, trimmed to what is drawn) and Frames/<Name>.json (frame size,
// columns, count, pivot, fps, loop, reach). Existing .meta files are kept, so GUIDs survive a re-bake.

import { cpSync, mkdirSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const here = dirname(fileURLToPath(import.meta.url)), repo = resolve(here, "..", "..");
const args = process.argv.slice(2), flag = (name) => args.includes(name), value = (name) => (args.includes(name) ? args[args.indexOf(name) + 1] : undefined);
const pixelfx = resolve(value("--pixelfx") ?? join(repo, ".utmp", "pixelfx"));
const only = value("--only")?.split(",");
const previewOnly = flag("--preview");
const outDir = join(repo, "Assets", "Resources", "LightSkills", "Fx"), jsonDir = join(outDir, "Frames");
const previewDir = join(repo, ".utmp", "pixelfx-preview");
const MAX_SHEET = 2048; // the importer's maxTextureSize: a bigger sheet would be scaled down and no longer slice

if (!existsSync(join(pixelfx, "tools", "lib", "fxlib.mjs"))) {
  console.error(`pixelfx not found at ${pixelfx}. Clone https://github.com/patch-gg/pixelfx there or pass --pixelfx <dir>.`);
  process.exit(1);
}
const srcDir = join(here, "skills"), dstDir = join(pixelfx, "src", "fx", "skill");
mkdirSync(dstDir, { recursive: true });
for (const f of readdirSync(srcDir)) if (f.endsWith(".js")) cpSync(join(srcDir, f), join(dstDir, f));

const load = (rel) => import(pathToFileURL(join(pixelfx, rel)).href);
const { renderFrames, contactSheet } = await load("tools/lib/fxlib.mjs");
const { encodePNG } = await load("tools/lib/png.mjs");

const effects = [];
for (const f of readdirSync(dstDir).sort()) {
  if (!f.endsWith(".js") || f.startsWith("_")) continue;
  const mod = await import(pathToFileURL(join(dstDir, f)).href + `?v=${Date.now()}`);
  for (const e of mod.effects ?? []) effects.push(e);
}

mkdirSync(previewDir, { recursive: true });
if (!previewOnly) {
  mkdirSync(jsonDir, { recursive: true });
  keep(outDir + ".meta", folderMeta);
  keep(jsonDir + ".meta", folderMeta);
}

let total = 0;
const montage = [];
for (const e of effects) {
  if (only && !only.includes(e.name)) continue;
  const meta = { fps: 30, bg: "dark", loop: false, duration: 1, tags: [], options: {}, ...e.meta };
  const fx = { id: "skill/" + e.name, mod: { meta, draw: e.draw }, meta };
  const [ox, oy] = meta.origin;

  const shown = renderFrames(fx, { actor: false, bg: e.previewBg ?? "dark" });
  writeFileSync(join(previewDir, e.name + (value("--tag") ? "-" + value("--tag") : "") + ".png"), contactSheet(fx, shown, { max: 400, zoom: e.zoom ?? (meta.size[0] > 150 ? 1 : meta.size[0] > 90 ? 2 : 3), width: 1500 }));

  const r = renderFrames(fx, { actor: false, bg: "none" });
  let frames = r.frames.map((f) => f.clear);
  // a one-shot ends on an empty frame: the game hides the sprite there instead
  const empty = (rgba) => { for (let i = 3; i < rgba.length; i += 4) if (rgba[i]) return false; return true; };
  while (!meta.loop && frames.length > 1 && empty(frames[frames.length - 1])) frames.pop();

  // trim every frame to the box that holds all of them, and keep the pivot where the origin was
  let x0 = r.w, y0 = r.h, x1 = -1, y1 = -1;
  for (const rgba of frames)
    for (let y = 0; y < r.h; y++)
      for (let x = 0; x < r.w; x++)
        if (rgba[(y * r.w + x) * 4 + 3]) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
  if (x1 < 0) { console.error(`${e.name}: draws nothing`); process.exitCode = 1; continue; }
  const touches = [x0 === 0 && "left", y0 === 0 && "top", x1 === r.w - 1 && "right", y1 === r.h - 1 && "bottom"].filter(Boolean);
  x0 = Math.max(0, x0 - 1); y0 = Math.max(0, y0 - 1); x1 = Math.min(r.w - 1, x1 + 1); y1 = Math.min(r.h - 1, y1 + 1);
  const w = x1 - x0 + 1, h = y1 - y0 + 1, n = frames.length;

  let cols = 0;
  for (let c = 1; c <= n; c++) {
    const rows = Math.ceil(n / c);
    if (c * w > MAX_SHEET || rows * h > MAX_SHEET) continue;
    if (!cols || Math.max(c * w, rows * h) < Math.max(cols * w, Math.ceil(n / cols) * h)) cols = c;
  }
  if (!cols) { console.error(`${e.name}: ${n} frames of ${w}x${h} do not fit a ${MAX_SHEET} px sheet`); process.exitCode = 1; continue; }
  const rows = Math.ceil(n / cols), W = cols * w, H = rows * h, sheet = new Uint8ClampedArray(W * H * 4);
  frames.forEach((rgba, i) => {
    const dx = (i % cols) * w, dy = Math.floor(i / cols) * h;
    for (let y = 0; y < h; y++) {
      const s = ((y0 + y) * r.w + x0) * 4;
      sheet.set(rgba.subarray(s, s + w * 4), ((dy + y) * W + dx) * 4);
    }
  });

  const info = {
    name: e.name, source: "pixelfx (https://github.com/patch-gg/pixelfx), tools/PixelFxBake/skills",
    fps: meta.fps, loop: !!meta.loop, reach: e.reach ?? 0, frameWidth: w, frameHeight: h, columns: cols, count: n,
    pivotX: +((ox - x0) / w).toFixed(6), pivotY: +((h - (oy - y0)) / h).toFixed(6),
  };
  total += W * H * 4;
  montage.push({ name: e.name, w: r.w, h: r.h, frames: r.frames.map((f) => f.clear) });
  console.log(`${e.name.padEnd(18)} ${String(n).padStart(2)} frames ${w}x${h} sheet ${W}x${H} ${(W * H * 4 / 1048576).toFixed(2)} MB${touches.length ? "  !! touches " + touches.join(",") : ""}`);
  if (previewOnly) continue;
  writeFileSync(join(outDir, e.name + ".png"), encodePNG(W, H, sheet));
  writeFileSync(join(jsonDir, e.name + ".json"), JSON.stringify(info, null, 1) + "\n");
  keep(join(outDir, e.name + ".png.meta"), textureMeta);
  keep(join(jsonDir, e.name + ".json.meta"), textMeta);
}
console.log(`total ${(total / 1048576).toFixed(1)} MB uncompressed${previewOnly ? " (preview only, nothing written to Assets)" : ""}`);

// --montage [frames]: the chosen effects side by side as the game shows them (on transparency), over a dark and
// a light ground, a few evenly spaced frames each: .utmp/pixelfx-preview/_montage[-tag].png (--tag names it)
if (flag("--montage") && montage.length) {
  const { canvas, upscale } = await load("tools/lib/fxlib.mjs");
  const count = +(value("--montage") ?? 6) || 6, zoom = +(value("--zoom") ?? 2), pad = 6, label = 14;
  const cw = Math.max(...montage.map((m) => m.w)) * zoom, grounds = [[18, 16, 24, 255], [96, 112, 88, 255]];
  const width = pad + count * (cw + pad), height = montage.reduce((s, m) => s + label + grounds.length * (m.h * zoom + 2) + pad, pad);
  const img = canvas(width, height, [40, 38, 48, 255]);
  let y = pad;
  for (const m of montage) {
    img.text(`${m.name}  ${m.w}x${m.h}  ${m.frames.length}F`, pad, y + 2, 2);
    y += label;
    const picks = Array.from({ length: Math.min(count, m.frames.length) }, (_, i) => Math.round((i * (m.frames.length - 1)) / Math.max(1, Math.min(count, m.frames.length) - 1)));
    for (const ground of grounds) {
      picks.forEach((fi, j) => {
        const x = pad + j * (cw + pad);
        img.rect(x, y, m.w * zoom, m.h * zoom, ground);
        img.paste(upscale(m.frames[fi], m.w, m.h, zoom), m.w * zoom, m.h * zoom, x, y);
      });
      y += m.h * zoom + 2;
    }
    y += pad;
  }
  const montageFile = join(previewDir, `_montage${value("--tag") ? "-" + value("--tag") : ""}.png`);
  writeFileSync(montageFile, img.png());
  console.log(`montage ${width}x${height} -> ${montageFile}`);
}

/** Write a .meta only when there is none, so a re-bake keeps the GUID. */
function keep(file, make) {
  if (!existsSync(file)) writeFileSync(file, make(randomBytes(16).toString("hex"), randomBytes(16).toString("hex")));
}

function folderMeta(guid) {
  return `fileFormatVersion: 2
guid: ${guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

function textMeta(guid) {
  return `fileFormatVersion: 2
guid: ${guid}
TextScriptImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

// The same importer settings as the earlier bakes (export-blaze.mjs): point filter, no compression, 32 px per unit.
function textureMeta(guid, spriteId) {
  return `fileFormatVersion: 2
guid: ${guid}
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
  maxTextureSize: ${MAX_SHEET}
  textureSettings:
    serializedVersion: 2
    filterMode: 0
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
  spritePixelsToUnits: 32
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
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: ${MAX_SHEET}
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
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: ${MAX_SHEET}
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
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: ${MAX_SHEET}
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
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: ${spriteId}
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
