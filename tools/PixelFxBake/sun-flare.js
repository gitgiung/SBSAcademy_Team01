// The flash at the tip of a sun spike as a missile leaves it, pointing right (the way the missile goes): a white
// spiky star for two frames, the same star cooled to gold, a ring thrown off it, sparks shot forward.
// The flash lobes and spark streaks come from projectiles/_lib.js, which adapts Sprite Fusion's open-source
// muzzle-flash (https://www.spritefusion.com/pixel-art-vfx/muzzle-flash), MIT License, Copyright (c) 2026 Hugo Duprez.
import { rng, fit, ease, sparkle, WHITE } from "../../core/index.js";
import { flashShape, flash, spark } from "../projectiles/_lib.js";

const FPS = 30;

export const meta = {
  name: "Sun flare",
  desc: "A launch flash off the tip of a sun spike: a white spiky star, cooling to gold, a ring thrown off it and sparks shot forward.",
  tags: ["fire", "sun", "flash", "launch", "directional", "one-shot"],
  size: [72, 48],
  origin: [20, 24],
  duration: 7 / FPS,
  loop: false,
  fps: FPS,
  bg: "dark",
  options: {
    palette: ["#3a1408", "#7a2a0c", "#c2500f", "#f08a1c", "#ffb92e", "#ffdf5e", "#fff3a8", "#fffbe0", "#ffffff"],
    seed: 3,
  },
};

const LOBES = { size: 0.95, spike: [14, 18], diag: [6, 10], cross: [4, 7], back: [3, 5] };

export function draw(p, t, o) {
  const F = fit(o.palette, 9), S = o.seed, D = meta.duration;
  if (t > D - 0.005) return;
  const fr = Math.floor(t * FPS + 1e-6), k = Math.pow(1 - t / D, 2);

  // light first: a warm pool and a white heart, gone fast
  p.glow(3, 0, 22, F[4], 0.55 * k);
  p.glow(0, 0, 9, WHITE, 0.7 * k);

  // a ring thrown off the tip, opening and thinning
  const q = t / 0.17;
  if (q < 1) {
    const r = 2 + 8 * ease.outCubic(q);
    p.ring(0, 0, r, Math.max(1, 2.2 * (1 - q)), q < 0.35 ? F[6] : q < 0.7 ? F[4] : F[3], 1.1 - q);
  }

  // the star: white for two frames, then the same shape cooled and smaller
  if (fr < 2) flash(p, 0, 0, 0, flashShape(rng(S, 1), 1, LOBES), [WHITE, F[7], F[5], F[3]], S);
  else if (fr < 4) flash(p, 0, 0, 0, flashShape(rng(S, 1), 2, LOBES), [F[7], F[5], F[4], F[2]], S + 1);

  // sparks shot forward with the missile, slowing as they go
  for (let j = 0; j < 12; j++) {
    const r = rng(S, 9, j), a = r.range(-1, 1), sp = r.range(110, 260), life = r.range(0.09, 0.19), age = t - r.range(0, 0.03);
    if (age < 0 || age >= life) continue;
    const d = 2 + sp * age * (1 - age * 2), v = sp * (1 - age * 4);
    spark(p, Math.cos(a) * d, Math.sin(a) * d, Math.cos(a) * v, Math.sin(a) * v, 1 - age / life, [WHITE, F[6], F[4], F[2]], 0.4, 0.02, 7);
  }

  // the glint on the tip itself
  if (t < 0.1) sparkle(p, 0, 0, 6 * (1 - t / 0.1) + 1, WHITE, F[6], 0.8);
}
