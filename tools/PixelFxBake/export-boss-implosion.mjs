#!/usr/bin/env node
// Bakes pixelfx explosions/implosion into BossAttackVfx frames for the phase-two area hit.
//
//   node tools/PixelFxBake/export-boss-implosion.mjs
//   node tools/PixelFxBake/export-boss-implosion.mjs --preview
//   node tools/PixelFxBake/export-boss-implosion.mjs --pixelfx <dir>
//
// Needs a clone of https://github.com/patch-gg/pixelfx (MIT) at .utmp/pixelfx by default.
// Existing .meta files are kept so Unity GUIDs survive a re-bake.

import { mkdirSync, writeFileSync, readdirSync, readFileSync, existsSync, unlinkSync } from "node:fs";
import { join, dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, "..", "..");
const args = process.argv.slice(2);
const flag = (name) => args.includes(name);
const value = (name) => (args.includes(name) ? args[args.indexOf(name) + 1] : undefined);
const pixelfx = resolve(value("--pixelfx") ?? join(repo, ".utmp", "pixelfx"));
const previewOnly = flag("--preview");
const outDir = join(repo, "Assets", "Resources", "BossAttackVfx");
const previewDir = join(repo, ".utmp", "pixelfx-preview");
const prefix = "Implosion_";
// Red/void palette so the suck-in matches the boss phase-two red look.
const BOSS_RED = ["#0a0404", "#2a0c0a", "#6a1710", "#b3241a", "#f2561d", "#ffc23d", "#ffffff"];

if (!existsSync(join(pixelfx, "tools", "lib", "fxlib.mjs"))) {
  console.error(`pixelfx not found at ${pixelfx}. Clone https://github.com/patch-gg/pixelfx there or pass --pixelfx <dir>.`);
  process.exit(1);
}

const load = (rel) => import(pathToFileURL(join(pixelfx, rel)).href);
const { loadEffect, renderFrames, contactSheet } = await load("tools/lib/fxlib.mjs");
const { encodePNG } = await load("tools/lib/png.mjs");

const fx = await loadEffect("explosions/implosion");
const [ox, oy] = fx.meta.origin;
const r = renderFrames(fx, { actor: false, bg: "none", options: { palette: BOSS_RED, seed: 2 } });
const pivotX = (ox / r.w).toFixed(8);
const pivotY = ((r.h - oy) / r.h).toFixed(8);

mkdirSync(previewDir, { recursive: true });
writeFileSync(
  join(previewDir, "boss-implosion.png"),
  contactSheet(fx, renderFrames(fx, { actor: false, bg: "dark", options: { palette: BOSS_RED, seed: 2 } }), {
    max: 400,
    zoom: 2,
    width: 1500,
  }),
);
console.log("preview", join(previewDir, "boss-implosion.png"), "frames", r.n, "size", r.w, "x", r.h);

if (previewOnly) process.exit(0);

const folderMeta = `fileFormatVersion: 2
guid: ${randomBytes(16).toString("hex")}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;

mkdirSync(outDir, { recursive: true });
keep(outDir + ".meta", folderMeta);

const oldMeta = new Map();
for (const name of readdirSync(outDir)) {
  if (name.startsWith(prefix) && name.endsWith(".png.meta"))
    oldMeta.set(name.slice(0, -9), readFileSync(join(outDir, name), "utf8"));
}
for (const name of readdirSync(outDir)) {
  if (name.startsWith(prefix) && name.endsWith(".png")) unlinkSync(join(outDir, name));
}

for (let i = 0; i < r.n; i++) {
  const file = prefix + String(i).padStart(2, "0");
  writeFileSync(join(outDir, file + ".png"), encodePNG(r.w, r.h, r.frames[i].clear));
  writeFileSync(
    join(outDir, file + ".png.meta"),
    oldMeta.get(file) ?? meta(randomBytes(16).toString("hex"), randomBytes(16).toString("hex"), pivotX, pivotY),
  );
}
console.log("wrote", r.n, "frames to", outDir, "pivot", pivotX, pivotY, "ppU 32");

function keep(path, contents) {
  if (!existsSync(path)) writeFileSync(path, contents);
}

function meta(guid, spriteId, px, py) {
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
  maxTextureSize: 2048
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
  alignment: 9
  spritePivot: {x: ${px}, y: ${py}}
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
  - serializedVersion: 4
    buildTarget: Standalone
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
  - serializedVersion: 4
    buildTarget: Android
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
