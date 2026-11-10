// 태양 탄막(10016) 그림을 pixelfx로 구워 Assets/Resources/LightSkills/Blaze에 씁니다.
//
//   git clone --depth 1 https://github.com/patch-gg/pixelfx .utmp/pixelfx     (한 번만)
//   node tools/PixelFxBake/export-sun.mjs [pixelfx 경로]                        (저장소 루트에서)
//
// 이 폴더의 sun-*.js를 pixelfx의 src/fx로 복사한 뒤 굽습니다. 원본은 이 폴더의 파일입니다.
// 자기 접두사(SunCrest_, SunFlare_, SunMissile_)의 PNG만 쓰고 지웁니다. 다른 그림은 건드리지 않습니다.
// 이미 있는 프레임은 meta의 GUID를 유지하고 피벗만 맞춥니다. 새 프레임의 meta는 BlazeFork_00의 임포트 설정을 따릅니다.
import { copyFileSync, existsSync, readdirSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { randomBytes } from "node:crypto";

const here = dirname(fileURLToPath(import.meta.url)), repo = resolve(here, "..", "..");
const pixelfx = resolve(process.argv[2] ?? join(repo, ".utmp", "pixelfx"));
const outDir = join(repo, "Assets", "Resources", "LightSkills", "Blaze");
const templatePath = join(outDir, "BlazeFork_00.png.meta");
if (!existsSync(join(pixelfx, "tools", "lib", "fxlib.mjs"))) {
  console.error(`pixelfx 복제본이 없습니다: ${pixelfx}\n  git clone --depth 1 https://github.com/patch-gg/pixelfx .utmp/pixelfx`);
  process.exit(1);
}
if (!existsSync(templatePath)) {
  console.error(`meta 본보기가 없습니다: ${templatePath}`);
  process.exit(1);
}

// dropLast: 한 번 재생하는 그림의 마지막 프레임은 비어 있어서 굽지 않습니다(정말 비었을 때만).
const jobs = [
  { file: "sun-crest.js", id: "fire/sun-crest", prefix: "SunCrest_", dropLast: true },
  { file: "sun-flare.js", id: "fire/sun-flare", prefix: "SunFlare_", dropLast: true },
  { file: "sun-missile.js", id: "projectiles/sun-missile", prefix: "SunMissile_" },
];
for (const job of jobs) copyFileSync(join(here, job.file), join(pixelfx, "src", "fx", job.id + ".js"));

const load = (rel) => import(pathToFileURL(join(pixelfx, rel)).href);
const { loadEffect, renderFrames } = await load("tools/lib/fxlib.mjs");
const { encodePNG } = await load("tools/lib/png.mjs");
const template = readFileSync(templatePath, "utf8");
const withPivot = (meta, px, py) => meta.replace(/spritePivot: \{[^}]*\}/, `spritePivot: {x: ${px}, y: ${py}}`);
const empty = (rgba) => {
  for (let i = 3; i < rgba.length; i += 4) if (rgba[i]) return false;
  return true;
};

for (const job of jobs) {
  const fx = await loadEffect(job.id);
  const [ox, oy] = fx.meta.origin;
  const r = renderFrames(fx, { actor: false, bg: "none" });
  const frames = r.frames.map((f) => f.clear);
  if (job.dropLast && empty(frames[frames.length - 1])) frames.pop();
  const px = (ox / r.w).toFixed(8), py = ((r.h - oy) / r.h).toFixed(8);
  const names = new Set();
  frames.forEach((rgba, i) => {
    const name = job.prefix + String(i).padStart(2, "0");
    names.add(name);
    const metaPath = join(outDir, name + ".png.meta");
    const meta = existsSync(metaPath)
      ? readFileSync(metaPath, "utf8")
      : template.replace(/^guid: .*$/m, `guid: ${randomBytes(16).toString("hex")}`).replace(/spriteID: .*$/m, `spriteID: ${randomBytes(16).toString("hex")}`);
    writeFileSync(join(outDir, name + ".png"), encodePNG(r.w, r.h, rgba));
    writeFileSync(metaPath, withPivot(meta, px, py));
  });
  // 프레임 수가 줄었으면 남은 예전 프레임을 지웁니다.
  for (const f of readdirSync(outDir)) {
    const m = /^(.*)\.png(\.meta)?$/.exec(f);
    if (m && m[1].startsWith(job.prefix) && !names.has(m[1])) unlinkSync(join(outDir, f));
  }
  const extra = fx.mod.IN ? `  그려짐 ${fx.mod.IN}프레임, 반복 ${fx.mod.HOLD}프레임` : "";
  console.log(`${job.id} -> ${job.prefix}00..${String(frames.length - 1).padStart(2, "0")}  ${r.w}x${r.h}  ${r.fps}fps  피벗 ${ox},${oy}${extra}`);
}
