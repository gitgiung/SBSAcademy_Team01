import { writeFileSync } from "node:fs";
import { loadEffect, renderFrames, contactSheet } from "./tools/lib/fxlib.mjs";

const FIRE8 = ["#2a0c0a", "#6a1710", "#b3241a", "#f2561d", "#ff8a2a", "#ffc23d", "#fff0a0", "#ffffff"];
for (const [id, options, out] of [
  ["projectiles/plasma-shot", { palette: FIRE8, size: 8 }, "preview-plasma.png"],
  ["fire/blaze-fork", {}, "preview-fork.png"],
]) {
  const fx = await loadEffect(id);
  const r = renderFrames(fx, { actor: false, bg: "dark", options });
  writeFileSync(out, contactSheet(fx, r, { max: 12, width: 1800 }));
  console.log(id, r.w, r.h, r.n);
}
