import { writeFileSync, mkdirSync, readdirSync, readFileSync, existsSync, unlinkSync } from "node:fs";
import { join } from "node:path";
import { randomBytes } from "node:crypto";
import { loadEffect, renderFrames } from "./tools/lib/fxlib.mjs";
import { encodePNG } from "./tools/lib/png.mjs";

const outDir = "C:/Users/User/Documents/GitHub/SBSAcademy_Team01/Assets/Resources/LightSkills/Blaze";
mkdirSync(outDir, { recursive: true });
const FIRE8 = ["#2a0c0a", "#6a1710", "#b3241a", "#f2561d", "#ff8a2a", "#ffc23d", "#fff0a0", "#ffffff"];

// keep GUIDs of frames that already exist so scene references survive a re-bake
const oldMeta = new Map();
for (const name of readdirSync(outDir)) if (name.endsWith(".png.meta")) oldMeta.set(name.slice(0, -9), readFileSync(join(outDir, name), "utf8"));
for (const name of readdirSync(outDir)) if (name.endsWith(".png")) unlinkSync(join(outDir, name));

const jobs = [
  { id: "projectiles/plasma-shot", prefix: "PlasmaOrb_", options: { palette: FIRE8, size: 9 } },
  { id: "fire/blaze-fork", prefix: "BlazeFork_", options: {} },
];
for (const job of jobs) {
  const fx = await loadEffect(job.id);
  const [ox, oy] = fx.meta.origin;
  const r = renderFrames(fx, { actor: false, bg: "none", options: job.options });
  const pivotX = (ox / r.w).toFixed(8), pivotY = ((r.h - oy) / r.h).toFixed(8);
  for (let i = 0; i < r.n; i++) {
    const file = job.prefix + String(i).padStart(2, "0");
    writeFileSync(join(outDir, file + ".png"), encodePNG(r.w, r.h, r.frames[i].clear));
    writeFileSync(join(outDir, file + ".png.meta"), oldMeta.get(file) ?? meta(randomBytes(16).toString("hex"), randomBytes(16).toString("hex"), pivotX, pivotY));
  }
  console.log(job.id, "frames", r.n, "size", r.w, r.h, "pivot", pivotX, pivotY);
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
