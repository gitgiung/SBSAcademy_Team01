// Fan and arc skills for SBSAcademy_Team01. Each opens to the right from its apex (the origin); the game turns it
// to the aim and scales it so `reach` px is the skill's range, and hits whatever is inside the angle on the first
// frame. The crescents are melee/_lib.js's slash, adapted from Sprite Fusion's open-source slash
// (https://www.spritefusion.com/pixel-art-vfx/slash); the flames are fire/_lib.js, adapted from Sprite Fusion's
// open-source fire (https://www.spritefusion.com/pixel-art-vfx/fire). MIT License, Copyright (c) 2026 Hugo Duprez.
import { clamp, smooth, ease, rng, TAU, WHITE, sparkle } from "../../core/index.js";
import { flameBlade, firePuff, solid, LAYERS, BIG_LAYERS, BACK_LAYERS } from "../fire/_lib.js";
import { swing, slash, onSwing, star } from "../melee/_lib.js";
import { ramp, rampUp, glowOf, noiseAt, ONCE, flash, fling, fireRing, rays } from "./_kit.js";

const EMBER_LAYERS = [[1, 1, 7], [0.7, 0.8, 6], [0.4, 0.55, 5]];
const cooling = (u) => (u < 0.35 ? LAYERS : u < 0.7 ? BACK_LAYERS : EMBER_LAYERS);
const rad = (deg) => (deg * Math.PI) / 180;
// the hit lands on the first frame, so a cut is already a frame into its sweep when it first shows
const LEAD = 1 / 30;

/** A cut round the apex: radius R, span radians wide and centred on the aim, swept clockwise (dir 1) or back. */
const cut = (R, span, W, t0, dir, pal, extra = {}) => swing({ cx: 0, cy: 0, R, sy: 1, rot: 0, a0: (-dir * span) / 2, dir, span, W, t0, sweep: 0.06, hold: 0.06, trail: 0.24, ramp: pal, ...extra });

/** Flames left burning on a cut's edge as it passes: n of them, leaning out (o.out 1) or back along the cut (0). */
function edgeFlames(p, t, s, n, H, N, S, o) {
  for (let i = 0; i < n; i++) {
    const r = rng(S, 81, i), u = (s.span * (i + r.range(0.2, 0.8))) / n, age = t - s.t0 - s.sweep * (u / s.span) * 0.7, life = r.range(0.18, 0.34);
    if (age < 0 || age > life) continue;
    const k = age / life, a = s.a0 + s.dir * u, ca = Math.cos(a), sa = Math.sin(a), out = o.out ?? 0.6;
    let dx = ca * out + s.dir * sa * (1 - out), dy = sa * out - s.dir * ca * (1 - out);
    const l = Math.hypot(dx, dy) || 1, [x, y] = onSwing(s, s.R - s.W * (o.inset ?? 0.5), u);
    const len = o.len * r.range(0.7, 1.15) * ease.outBack(clamp(age / 0.05)) * (1 - 0.75 * k);
    flameBlade(p, x, y, dx / l, dy / l, len, o.w * r.range(0.85, 1.1) * (1 - 0.4 * k), 0, N, (S * 7 + i) * 13, H, o.big && k < 0.35 ? BIG_LAYERS : cooling(k), 0.5, 1);
  }
}

/** The glint where a cut's edge stops. */
function glint(p, t, s, H) {
  const g = t - s.t0 - 0.04;
  if (g <= 0 || g >= 0.2) return;
  const [x, y] = onSwing(s, s.R - s.W * 0.3, s.span);
  sparkle(p, x, y, 9 * ease.outBack(Math.min(1, g / 0.05)) * (1 - g / 0.2), WHITE, H[2], 0.6);
}

// 10007 태양 검: two cuts of sunlight, one for each hit: down, then back up a little wider, rays thrown off both.
function sunBlade(p, t0, o) {
  const t = t0 + LEAD, H = ramp("sun"), PAL = rampUp("sun"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), span = rad(140);
  const A = cut(R - 7, span, 15, 0, 1, PAL), B = cut(R - 1, span, 18, 0.16, -1, PAL, { hot: 0.25 });
  p.glow(R * 0.6, 0, R * 0.9, G, 0.3 * (1 - smooth(0.25, 0.55, t)));
  edgeFlames(p, t, A, 9, H, N, S, { len: 17, w: 2.8, out: 0.9, inset: 0.15 });
  edgeFlames(p, t, B, 10, H, N, S + 50, { len: 19, w: 3, out: 0.9, inset: 0.15 });
  slash(p, A, t);
  slash(p, B, t);
  glint(p, t, A, H);
  glint(p, t, B, H);
  fling(p, t, S, H, { n: 14, a0: -span / 2, a1: span / 2, rmin: R * 0.7, r0: R * 0.9, sp: [40, 150], life: [0.15, 0.35], t0: 0.03 });
  fling(p, t, S + 9, H, { n: 16, a0: -span / 2, a1: span / 2, rmin: R * 0.7, r0: R * 0.95, sp: [40, 170], life: [0.15, 0.35], t0: 0.19 });
}

// 10011 화염 부채: a fan of flame breathed out from the apex, tongues to the edge of the reach and a rolling front.
const FAN = [[0.62, 0.86, 9], [-0.62, 0.86, 9], [0.4, 0.94, 11], [-0.4, 0.94, 11], [0.2, 1, 12], [-0.2, 1, 12], [0, 1, 12]];
function flameFan(p, t, o) {
  const H = ramp("fire"), L = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H);
  const front = L * ease.outCubic(clamp((t + 0.03) / 0.09)), burn = 1 - smooth(0.18, 0.48, t);
  const hot = t < 0.18 ? BIG_LAYERS : t < 0.34 ? BACK_LAYERS : EMBER_LAYERS;
  FAN.forEach(([a, share, w], i) => {
    const n = (S * 11 + i) * 17, len = front * share * (0.86 + 0.2 * N(4, n)) * (0.3 + 0.7 * Math.pow(burn, 0.7)) * (1 - smooth(0.36, 0.5, t));
    flameBlade(p, 0, 0, Math.cos(a), Math.sin(a), len, w * (0.5 + 0.5 * Math.sqrt(burn)) * (0.92 + 0.12 * N(3, n + 2)), N(3, n + 5) * 4.5 + a * 6, N, n, H, hot, 0.7, 1.4 + len * 0.02);
  });
  // the edge of the fan: an arc of fire that races out to the reach and breaks up
  fireRing(p, 0, 0, front - 5, H, { thick: 0.8, lick: 3.2, ph: t * 7, seed: S, keep: (a) => Math.abs(a) < 0.7, alpha: 1 - smooth(0.12, 0.36, t), cool: t > 0.22 ? 2 : 0 });
  // the breath rolling out behind it: balls of flame, bigger the further they get
  for (let i = 0; i < 56; i++) {
    const r = rng(S, 301, i), a = r.range(-0.66, 0.66), d0 = r.range(0.25, 0.92) * L, age = t + 0.03 - (d0 / L) * 0.07 - r.range(0, 0.13), life = r.range(0.2, 0.38);
    if (age < 0 || age > life) continue;
    const d = d0 + age * r.range(10, 40);
    firePuff(p, Math.cos(a) * d, Math.sin(a) * d, 1 - age / life, r.range(3.6, 6.6) * (0.5 + 0.85 * (d0 / L)), r.next(), H, 0.1, Math.cos(a), Math.sin(a));
  }
  fling(p, t, S, H, { n: 22, a0: -0.7, a1: 0.7, rmin: L * 0.2, r0: L * 0.6, sp: [70, 220], life: [0.2, 0.5], t0: 0.02 });
  const fl = Math.max(0, 1 - t / 0.14);
  if (fl > 0) {
    solid(p, 2, 0, 10 * fl + 2, H[3]);
    solid(p, 2, 0, 7 * fl + 1, H[5]);
    solid(p, 3, 0, 4 * fl, WHITE);
  }
  const k = Math.pow(burn, 0.6);
  p.glow(L * 0.5, 0, L * 0.75, G, 0.4 * k);
  p.glow(4, 0, 26, G, 0.4 * k + 0.4 * fl);
}

// 20006 화염 베기: one short, thick cut that leaves its whole edge on fire.
function flameSlash(p, t0, o) {
  const t = t0 + LEAD, H = ramp("fire"), PAL = rampUp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), span = rad(90);
  const A = cut(R - 3, span, 17, 0, 1, PAL, { hot: 0.2, hold: 0.08, trail: 0.26 });
  p.glow(R * 0.6, 0, R * 0.9, G, 0.35 * (1 - smooth(0.2, 0.5, t)));
  edgeFlames(p, t, A, 11, H, N, S, { len: 21, w: 4, out: 0.35, inset: 0.05, big: true });
  slash(p, A, t);
  glint(p, t, A, H);
  fling(p, t, S, H, { n: 22, a0: -span / 2, a1: span / 2, rmin: R * 0.65, r0: R * 0.9, sp: [50, 190], life: [0.18, 0.42], t0: 0.03 });
}

// 20010 태양 강타: gathered sunlight brought down in front. A wedge of light for a frame, a star where it lands,
// an arc of fire racing out to the edge of the reach.
const SPIKES = Array.from({ length: 8 }, (_, i) => ({ a: (i * TAU) / 8 + 0.2, len: i % 2 ? 12 : 21, w: 3 }));
function sunSmash(p, t, o) {
  const H = ramp("sun"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), half = rad(40), C = R * 0.55;
  p.glow(C, 0, R * 0.9, G, 0.4 * (1 - smooth(0.15, 0.5, t)));
  const rr = R * (0.5 + 0.5 * ease.outCubic(clamp((t + 0.02) / 0.14)));
  fireRing(p, 0, 0, rr - 5, H, { thick: 1.1, lick: 3, ph: t * 6, seed: S, keep: (a) => Math.abs(a) < half, alpha: 1 - smooth(0.2, 0.47, t), cool: t > 0.3 ? 2 : 0 });
  rays(p, C, 0, 7, 5, R * 0.42 * ease.outBack(clamp(t / 0.05)) * (1 - smooth(0.08, 0.42, t)), 4, N, S, H, t < 0.2 ? LAYERS : BACK_LAYERS, { a0: -1, span: 2 });
  if (t < 0.1) {
    const k = 1 - t / 0.1;
    p.poly([[-2, 0], [C, -4 - 9 * k], [C + 12, 0], [C, 4 + 9 * k]], H[3]);
    p.poly([[4, 0], [C, -2 - 6 * k], [C + 8, 0], [C, 2 + 6 * k]], WHITE);
  }
  if (t < 0.24) star(p, C, 0, SPIKES, ease.outBack(clamp(t / 0.05)) * (1 - t / 0.24) + 0.15, 1 - 0.5 * (t / 0.24), WHITE, H[1], H[2], H[4]);
  flash(p, C, 0, 14, 1 - t / 0.12, H);
  fling(p, t, S, H, { n: 26, x: C, a0: -1.1, a1: 1.1, sp: [70, 260], life: [0.18, 0.45], t0: 0.02 });
}

/** A petal of flame from (x, y) along (ux, uy): a pointed leaf, a pale rib, a dark crimson rim. k: 0 new .. 1 gone. */
function petal(p, x, y, ux, uy, len, w, H, k) {
  if (len < 2) return;
  const nx = -uy, ny = ux, cool = Math.floor(k * 3.2), fade = k < 0.55 ? 1 : 1 - (k - 0.55) / 0.45;
  for (let d = 0; d <= len; d += 0.5) {
    const u = d / len, half = w * Math.pow(Math.sin(Math.PI * Math.pow(u, 0.75)), 0.85);
    for (let q = -half; q <= half; q += 0.5) {
      const e = Math.abs(q) / Math.max(0.5, half);
      const i = e > 0.78 ? 5 : e > 0.42 ? 3 : Math.abs(q) < 0.6 && u < 0.8 ? 0 : 1;
      p.dot(x + ux * d + nx * q, y + uy * d + ny * q, H[Math.min(8, i + cool)], fade);
    }
  }
}

// 20011 홍련 횡베기: a wide crimson cut, and a lotus of flame that opens petal by petal as the edge goes past.
function crimsonLotus(p, t0, o) {
  const t = t0 + LEAD, H = ramp("crimson"), PAL = rampUp("crimson"), R = o.reach, S = o.seed, G = glowOf(H), span = rad(160);
  const A = cut(R - 2, span, 17, 0, 1, PAL, { lens: true, hold: 0.08, trail: 0.28 });
  p.glow(R * 0.5, 0, R, G, 0.35 * (1 - smooth(0.2, 0.55, t)));
  for (let i = 0; i < 9; i++) {
    const r = rng(S, 61, i), a = A.a0 + (span * (i + 0.5)) / 9, age = t - 0.02 - 0.06 * (i / 9), life = r.range(0.3, 0.38);
    if (age < 0 || age > life) continue;
    const k = age / life, grow = ease.outBack(clamp(age / 0.08));
    petal(p, Math.cos(a) * R * 0.46, Math.sin(a) * R * 0.46, Math.cos(a), Math.sin(a), R * 0.36 * grow * (1 - 0.25 * k), 7 * grow * (1 - 0.45 * k), H, k);
  }
  slash(p, A, t);
  glint(p, t, A, H);
  fling(p, t, S, H, { n: 26, a0: -span / 2, a1: span / 2, rmin: R * 0.5, r0: R * 0.9, sp: [40, 170], life: [0.2, 0.45], t0: 0.03, spread: 0.08 });
}

/** Canvas for a fan `deg` wide and `reach` long with its apex on the origin. */
const fan = (name, reach, deg, duration, draw, pad = 18) => {
  const halfH = Math.ceil(reach * Math.sin(Math.min(Math.PI / 2, rad(deg) / 2))) + pad, left = pad + Math.ceil(Math.max(0, -Math.cos(rad(deg) / 2)) * reach);
  return { name, draw, reach, zoom: 1, meta: { name, size: [left + reach + pad, halfH * 2], origin: [left, halfH], duration, loop: false, fps: 30, options: { seed: 4, reach } } };
};

export const effects = [
  fan("SunBlade", 112, 140, 0.58, sunBlade),
  fan("FlameFan", 96, 80, 0.52, flameFan, 22),
  fan("FlameSlash", 80, 90, 0.52, flameSlash, 22),
  fan("SunSmash", 96, 80, 0.5, sunSmash, 22),
  fan("CrimsonLotus", 102, 160, 0.56, crimsonLotus),
];
