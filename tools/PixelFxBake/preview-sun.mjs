// 태양 탄막 그림을 굽기 전에 눈으로 확인하는 contact sheet를 만듭니다. 결과는 .utmp/pixelfx/preview-*.png(생성물)입니다.
//
//   node tools/PixelFxBake/preview-sun.mjs <id> [배율] [배경] [all | 프레임,프레임,...] [이름]     (저장소 루트에서)
//   예: node tools/PixelFxBake/preview-sun.mjs fire/sun-crest 4 ground 4,7,12,21 crest
//
// id는 fire/sun-crest, fire/sun-flare, projectiles/sun-missile 중 하나입니다. 이 폴더의 sun-*.js를 먼저 pixelfx로 복사합니다.
import { copyFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = dirname(fileURLToPath(import.meta.url)), pixelfx = resolve(here, "..", "..", ".utmp", "pixelfx");
for (const [file, id] of [["sun-crest.js", "fire/sun-crest"], ["sun-flare.js", "fire/sun-flare"], ["sun-missile.js", "projectiles/sun-missile"]])
  copyFileSync(join(here, file), join(pixelfx, "src", "fx", id + ".js"));
const { loadEffect, renderFrames, contactSheet } = await import(pathToFileURL(join(pixelfx, "tools", "lib", "fxlib.mjs")).href);

const [id = "fire/sun-crest", zoom = "2", bg = "ground", pick = "all", name] = process.argv.slice(2);
const fx = await loadEffect(id);
const r = renderFrames(fx, { actor: false, bg });
if (pick !== "all") {
  r.frames = pick.split(",").map((i) => r.frames[+i]);
  r.n = r.frames.length;
}
const out = join(pixelfx, "preview-" + (name ?? id.split("/")[1]) + ".png");
writeFileSync(out, contactSheet(fx, r, { max: 400, zoom: +zoom, width: 1500 }));
console.log(id, `${r.w}x${r.h}`, "frames", r.n, "->", out);
