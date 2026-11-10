// One prong of a forked fire burst, facing right from its root. A fat fan of flame tongues shoots out almost
// at once, flares white-hot, then burns down to red embers. The game plays 6 to 8 of these turned round the
// impact. Built from fire/_lib.js (flame blades, puffs, embers), which is adapted from Sprite Fusion's
// open-source fire (https://www.spritefusion.com/pixel-art-vfx/fire), MIT License, Copyright (c) 2026 Hugo Duprez.
import { fit, mix, clamp, smooth, ease, rng, WHITE } from "../../core/index.js";
import { noiseFor, hotFirst, flameBlade, firePuff, spark, ember, solid, BIG_LAYERS, BACK_LAYERS } from "./_lib.js";

export const meta = {
  name: "Blaze fork",
  desc: "One prong of a forked fire burst: a fat fan of flame tongues lashes out in a blink, flares white-hot, then burns down to red embers.",
  tags: ["fire", "fork", "burst", "aoe", "directional", "one-shot"],
  size: [120, 76],
  origin: [8, 38],
  duration: 0.7,
  loop: false,
  fps: 30,
  bg: "dark",
  options: {
    palette: ["#4a1712", "#8a1f17", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#ffe26b", "#fff3a6", "#ffffff"],
    reach: 88,
    seed: 4,
  },
};

const NL = noiseFor(0);
// the dying flames: deep orange and red, no white heart
const EMBER_LAYERS = [[1, 1, 7], [0.7, 0.8, 6], [0.4, 0.55, 5]];
// [angle off the axis, length share, half-width at the root]: outer tongues first, the main one last
const TONGUES = [[0.62, 0.55, 8], [-0.62, 0.55, 8], [0.34, 0.8, 11], [-0.34, 0.8, 11], [0.1, 0.95, 13], [-0.1, 0.95, 13], [0, 1, 14]];

export function draw(p, t, o) {
  const H = hotFirst(fit(o.palette, 9)), L = o.reach, S = o.seed;
  const N = (rate, seed = 0, off = 0) => NL(t, rate, seed, off);
  const GLOW = mix(mix(H[3], H[4], 0.5), WHITE, 0.25);
  const front = L * ease.outCubic(clamp(t / 0.06));
  const burn = 1 - smooth(0.2, 0.64, t);
  const hot = t < 0.18 ? BIG_LAYERS : t < 0.34 ? BACK_LAYERS : EMBER_LAYERS;

  // the tongues: fanned out from the root, each one swaying, the root end burning longest
  TONGUES.forEach(([a, share, w], i) => {
    const n = (S * 11 + i) * 17, len = front * share * (0.86 + 0.2 * N(4, n)) * (0.3 + 0.7 * Math.pow(burn, 0.7));
    const ux = Math.cos(a), uy = Math.sin(a), wid = w * (0.5 + 0.5 * Math.sqrt(burn)) * (0.92 + 0.12 * N(3, n + 2));
    flameBlade(p, 0, 0, ux, uy, len, wid, N(3, n + 5) * 4.5 + a * 6, N, n, H, hot, 0.7, 1.4 + len * 0.02);
  });

  // flame balls lashed off along the prong: big and thick near the root, smaller toward the tip
  const R = rng(S, 301);
  for (let i = 0; i < 34; i++) {
    const u = (i + R.next()) / 34, x0 = u * L, y0 = R.range(-1, 1) * (1 - u * 0.65) * 13, t0 = (x0 / L) * 0.05 + R.range(0, 0.16);
    const life = R.range(0.22, 0.42), age = t - t0;
    if (age < 0 || age > life) continue;
    const x = x0 + age * R.range(10, 34), y = y0 + age * R.range(-12, 12), size = R.range(3, 5.6) * (1.05 - u * 0.55);
    firePuff(p, x, y, 1 - age / life, size, R.next(), H, 0.1, 1, 0);
  }

  // embers and sparks thrown out of the burst
  for (let i = 0; i < 18; i++) {
    const r = rng(S, 401, i), a = r.range(-0.7, 0.7), sp = r.range(70, 190), life = r.range(0.25, 0.6), age = t - r.range(0, 0.1);
    if (age < 0 || age > life) continue;
    const x = Math.cos(a) * sp * age * (1 - age * 0.6), y = Math.sin(a) * sp * age * (1 - age * 0.6) + 18 * age * age;
    if (i % 3) spark(p, x + 4, y, Math.cos(a) * sp, Math.sin(a) * sp, 1 - age / life, [H[1], H[2], H[4], H[5]]);
    else ember(p, x + 4, y, 1 - age / life, r.chance(0.3) ? 2 : 1.2, [H[1], H[2], H[4], H[6]], age * 40);
  }

  // the root: a white blast that goes out fast
  const flash = Math.max(0, 1 - t / 0.14);
  if (flash > 0) {
    solid(p, 2, 0, 10 * flash + 2, H[3]);
    solid(p, 2, 0, 7 * flash + 1, H[5]);
    solid(p, 3, 0, 4 * flash, WHITE);
  }

  // light: a hot streak down the prong, a pool at the root, a point at the tip
  const k = Math.pow(burn, 0.6);
  p.glowLine(0, 0, front * 0.9, 0, 20, GLOW, 0.42 * k);
  p.glow(4, 0, 26, GLOW, 0.5 * k + 0.4 * flash);
  p.glow(front * 0.85, 0, 16, H[3], 0.4 * k);
}
