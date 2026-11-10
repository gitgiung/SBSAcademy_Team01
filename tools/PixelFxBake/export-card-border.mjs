// 강화 카드 테두리 빛(card-border-shine)을 구워 Assets/Resources/UI/LevelUp에 씁니다.
//
//   git clone --depth 1 https://github.com/patch-gg/pixelfx .utmp/pixelfx     (한 번만)
//   node tools/PixelFxBake/export-card-border.mjs [pixelfx 경로]               (저장소 루트에서)
//
// 자기 접두사(CardBorderShine_) PNG만 쓰고 지웁니다. 이미 있는 프레임은 GUID를 유지합니다.
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const here = dirname(fileURLToPath(import.meta.url)), repo = resolve(here, "..", "..");
const pixelfx = resolve(process.argv[2] ?? join(repo, ".utmp", "pixelfx"));
const outDir = join(repo, "Assets", "Resources", "UI", "LevelUp");
const templatePath = join(repo, "Assets", "Resources", "LightSkills", "Blaze", "BlazeFork_00.png.meta");
if (!existsSync(join(pixelfx, "tools", "lib", "fxlib.mjs"))) {
  console.error(`pixelfx 복제본이 없습니다: ${pixelfx}\n  git clone --depth 1 https://github.com/patch-gg/pixelfx .utmp/pixelfx`);
  process.exit(1);
}
if (!existsSync(templatePath)) {
  console.error(`meta 본보기가 없습니다: ${templatePath}`);
  process.exit(1);
}

mkdirSync(outDir, { recursive: true });
copyFileSync(join(here, "card-border-shine.js"), join(pixelfx, "src", "fx", "ui", "card-border-shine.js"));

const load = (rel) => import(pathToFileURL(join(pixelfx, rel)).href);
const { loadEffect, renderFrames } = await load("tools/lib/fxlib.mjs");
const { encodePNG } = await load("tools/lib/png.mjs");
const template = readFileSync(templatePath, "utf8");
const withPivot = (meta, px, py) => meta.replace(/spritePivot: \{[^}]*\}/, `spritePivot: {x: ${px}, y: ${py}}`);

const prefix = "CardBorderShine_";
const fx = await loadEffect("ui/card-border-shine");
const [ox, oy] = fx.meta.origin;
const r = renderFrames(fx, { actor: false, bg: "none" });
const px = (ox / r.w).toFixed(8), py = ((r.h - oy) / r.h).toFixed(8);
const names = new Set();
r.frames.forEach((frame, i) => {
  const name = prefix + String(i).padStart(2, "0");
  names.add(name);
  const metaPath = join(outDir, name + ".png.meta");
  const meta = existsSync(metaPath)
    ? readFileSync(metaPath, "utf8")
    : template.replace(/^guid: .*$/m, `guid: ${randomBytes(16).toString("hex")}`).replace(/spriteID: .*$/m, `spriteID: ${randomBytes(16).toString("hex")}`);
  writeFileSync(join(outDir, name + ".png"), encodePNG(r.w, r.h, frame.clear));
  writeFileSync(metaPath, withPivot(meta, px, py));
});
for (const f of readdirSync(outDir)) {
  const m = /^(.*)\.png(\.meta)?$/.exec(f);
  if (m && m[1].startsWith(prefix) && !names.has(m[1])) unlinkSync(join(outDir, f));
}
console.log(`ui/card-border-shine -> ${prefix}00..${String(r.n - 1).padStart(2, "0")}  ${r.w}x${r.h}  ${r.fps}fps  피벗 ${ox},${oy}`);
