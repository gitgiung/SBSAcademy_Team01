// Shared by the skill effects baked for SBSAcademy_Team01 (the leading _ keeps it from being listed as an effect).
// Palettes, noise, flashes, shock rings, rings of fire, radiating flames and flung embers. Built on fire/_lib.js
// (flame blades, tongues, puffs, embers, sparks), which is adapted from Sprite Fusion's open-source fire
// (https://www.spritefusion.com/pixel-art-vfx/fire), MIT License, Copyright (c) 2026 Hugo Duprez.
import { fit, mix, rng, tileFbm, TAU, WHITE } from "../../core/index.js";
import { noiseFor, hotFirst, flameBlade, ember, spark, solid, LAYERS } from "../fire/_lib.js";

export const FPS = 30;

/** Ramps dark to bright. fire is the one the earlier bakes (blaze fork, flame whirl, sundering fire) use. */
export const PALS = {
  fire: ["#4a1712", "#8a1f17", "#c9281f", "#f2561d", "#ff8a2a", "#ffc23d", "#ffe26b", "#fff3a6", "#ffffff"],
  sun: ["#3a1408", "#7a2a0c", "#c2500f", "#f08a1c", "#ffb92e", "#ffdf5e", "#fff3a8", "#fffbe0", "#ffffff"],
  crimson: ["#2a060c", "#5e0a14", "#9c0f1c", "#d81e22", "#f5471f", "#ff7f2a", "#ffb648", "#ffe59a", "#ffffff"],
  hell: ["#1c0509", "#4a0810", "#860c14", "#c21616", "#ee3a18", "#ff7420", "#ffac3a", "#ffe08a", "#fff6d8"],
  lava: ["#2a0a06", "#5a1408", "#96220a", "#d2400c", "#f56a14", "#ff9a22", "#ffc840", "#ffe98a", "#fff8d0"],
  ice: ["#0c1430", "#162c5c", "#1f4f94", "#2f7fcc", "#58b4ec", "#9bdcff", "#d8f4ff", "#f2fbff", "#ffffff"],
  wind: ["#0e2a22", "#1a4a3a", "#2a7456", "#48a878", "#7cd4a0", "#aeecc4", "#d6f8e2", "#f0fff6", "#ffffff"],
  earth: ["#140e0a", "#2a1e14", "#47321e", "#69492a", "#8e6638", "#b88a52", "#dcb47c", "#f0d8a8", "#fff4dc"],
  volt: ["#24160a", "#5e3a0c", "#a8680f", "#e8a218", "#ffd23c", "#ffe670", "#fff6a8", "#fffbe0", "#ffffff"],
};

/** A ramp hot-first, nine steps: H[0] white .. H[8] the darkest. */
export const ramp = (name) => hotFirst(fit(PALS[name], 9));
/** A ramp dark to bright with n steps, for the helpers that want it that way round (melee slashes). */
export const rampUp = (name, n = 7) => fit(PALS[name], n);
/** The colour of a ramp's light. */
export const glowOf = (H) => mix(mix(H[3], H[4], 0.5), WHITE, 0.25);
/** The effect's noise at this frame: N(rate, seed, off). */
export const noiseAt = (NL, t) => (rate, seed = 0, off = 0) => NL(t, rate, seed, off);
/** Plain (non-looping) noise for one-shots. */
export const ONCE = noiseFor(0);

/** A white-hot flash at (x, y) with strength k (1 new .. 0 gone): white heart, gold, an orange rim, light. */
export function flash(p, x, y, r, k, H) {
  if (k <= 0) return;
  solid(p, x, y, r * k + 2, H[4]);
  solid(p, x, y, r * 0.72 * k + 1, H[2]);
  solid(p, x, y, r * 0.45 * k, WHITE);
  p.glow(x, y, r * 2.6, H[2], 0.55 * k);
}

/** A thin shock ring: a white wire with a gold skin and its light. k dithers it out. */
export function shockRing(p, cx, cy, R, H, k, w = 1.6) {
  if (R < 1.5 || k <= 0.02) return;
  p.ring(cx, cy, R, w, (v) => (v > 0.45 ? H[0] : H[2]), k);
  p.glowRing(cx, cy, R, 7, H[3], 0.35 * k);
}

/**
 * A face-on ring of fire at radius R: a white-hot wire, gold either side, an orange rim whose outer edge licks
 * outwards. o: { thick (1 is about 6 px), lick (px the edge licks out), cool (steps down the ramp), alpha (below
 * 1 it breaks up in patches), ph (scrolls the licks), seed, keep(angle from +x) }.
 */
export function fireRing(p, cx, cy, R, H, o = {}) {
  const th = o.thick ?? 1, lick = o.lick ?? 2.6, cool = o.cool ?? 0, alpha = o.alpha ?? 1, S = o.seed ?? 0, ph = o.ph ?? 0;
  if (R < 2 || alpha <= 0.02) return;
  const cells = Math.max(8, 2 * Math.round((TAU * R) / 8.6)), RO = R + (2.7 + lick) * th + 2;
  p.shade(cx - RO, cy - RO, cx + RO, cy + RO, (x, y, out) => {
    const dx = x - cx, dy = y - cy, s = Math.hypot(dx, dy) - R;
    if (s < -4 * th || s > (2.7 + lick) * th + 1) return;
    if (o.keep && !o.keep(Math.atan2(dy, dx))) return;
    let a = Math.atan2(dx, -dy);
    if (a < 0) a += TAU;
    const cell = (a / TAU) * cells;
    const lk = 0.5 + 0.5 * tileFbm(cell, ph, cells, 0, S, 2), nick = tileFbm(cell + 9, ph, cells, 0, S + 3, 2);
    const inner = (-2.9 + nick * 0.4) * th, outer = (2.7 + lk * lick) * th;
    const dep = Math.min(s - inner, outer - s) / Math.max(0.6, th);
    if (dep < 0) return;
    const i = dep < 0.9 ? 6 : dep < 1.7 ? 4 : dep < 2.5 ? 2 : 0;
    out.c = H[Math.min(8, i + cool + (cool && i === 0 ? 1 : 0))];
    out.a = alpha >= 1 ? 1 : alpha * (1.15 + 0.6 * tileFbm(cell * 0.5, 2.5, cells >> 1, 0, S + 8, 2));
  });
}

/**
 * Flame blades standing out from a circle of radius r0 round (cx, cy), n of them over o.span radians from o.a0
 * (a full turn by default), each len long and w half-wide at its root.
 */
export function rays(p, cx, cy, n, r0, len, w, N, S, H, layers = LAYERS, o = {}) {
  if (len < 1.5) return;
  const span = o.span ?? TAU, a0 = o.a0 ?? 0;
  for (let i = 0; i < n; i++) {
    const r = rng(S, 311, i), a = a0 + ((i + 0.5 + r.range(-0.3, 0.3)) / n) * span, L = len * r.range(0.7, 1.1), ux = Math.cos(a), uy = Math.sin(a);
    flameBlade(p, cx + ux * r0, cy + uy * r0, ux, uy, L, w * r.range(0.8, 1.15), N(3, S + i * 7) * 3, N, (S * 13 + i) * 17, H, layers, 0.6, 1.2 + L * 0.02);
  }
}

/**
 * Embers and sparks flung from (o.x, o.y), in closed form: fast out, dragged to a crawl. o: { n, a0, a1 (angles,
 * a full turn by default), sp: [lo, hi] px/s, life: [lo, hi] s, t0 (when), spread (s over which they leave),
 * rmin..r0 (px from the centre they start), g (px/s² pulling them down) }.
 */
export function fling(p, t, S, H, o) {
  const EMB = [H[1], H[3], H[5], H[6]], SPK = [H[0], H[1], H[3], H[5]], g = o.g ?? 0;
  for (let i = 0; i < o.n; i++) {
    const r = rng(S, 907, i), a = r.range(o.a0 ?? 0, o.a1 ?? TAU), sp = r.range(o.sp[0], o.sp[1]), life = r.range(o.life[0], o.life[1]);
    const age = t - (o.t0 ?? 0) - r.range(0, o.spread ?? 0.04), r0 = r.range(o.rmin ?? 0, o.r0 ?? o.rmin ?? 0), big = r.chance(0.3);
    if (age < 0 || age >= life) continue;
    const u = age / life, d = r0 + sp * life * (u - 0.45 * u * u), v = sp * (1 - 0.9 * u), ca = Math.cos(a), sa = Math.sin(a);
    const x = (o.x ?? 0) + ca * d, y = (o.y ?? 0) + sa * d + g * age * age;
    if (i % 3 === 0) ember(p, x, y, 1 - u, big ? 2 : 1.2, EMB, age * 40 + i);
    else spark(p, x, y, ca * v, sa * v + 2 * g * age, 1 - u, SPK);
  }
}

/** Which whole frame t falls in (for things that should jump once a frame, like lightning). */
export const frameOf = (t) => Math.floor(t * FPS + 1e-6);
