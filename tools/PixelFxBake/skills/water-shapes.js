// Water skills that are not a piercing line. The game already aims and scales them:
// a projectile loops with its head on the origin and is not stretched,
// a circle or ring stays unrotated and `reach` px becomes the skill range,
// a fan or sweep turns toward the aim and `reach` px becomes the skill range.
// These are water: foam, beads and sheets. They do not reuse the fire tongues.
// Droplets are pixelfx's water bead (src/fx/water/_lib.js), adapted from Sprite Fusion's
// open-source blood, MIT License, Copyright (c) 2026 Hugo Duprez.
import { clamp, ease, rng, TAU, WHITE } from "../../core/index.js";
import { fit } from "../../core/color.js";
import { WATER, bead } from "../water/_lib.js";

const PALS = {
  water: fit(WATER, 8),
  deep: fit(["#040810", "#081220", "#0c243c", "#123456", "#1a527c", "#3a86b8", "#8ec8ee", "#f2fbff"], 8),
  abyss: fit(["#02060c", "#061018", "#0a1c30", "#0e2c48", "#124068", "#226898", "#62a8d8", "#e4f4ff"], 8),
  foam: fit(["#0c2848", "#145888", "#1c78b4", "#38a4e4", "#78d0ff", "#c8eeff", "#ffffff", "#ffffff"], 8),
};

const REACH = 80;

function front(R, age) {
  return R * (0.8 + 0.2 * ease.outQuad(clamp(Math.max(0, age) / 0.06)));
}

function pulse(t, hits, gap) {
  let best = 0;
  let age = -1;
  for (let i = 0; i < hits; i++) {
    const a = t + 0.02 - i * gap;
    if (a < 0) continue;
    const k = a < 0.07 ? 1 : Math.max(0, 1 - (a - 0.07) / 0.34);
    if (k > best) {
      best = k;
      age = a;
    }
  }
  return { k: best, age };
}

function crest(p, C, radius, count, spin, k) {
  for (let i = 0; i < count; i++) {
    const a = (i / count) * TAU + spin;
    const x = Math.cos(a) * radius;
    const y = Math.sin(a) * radius;
    p.px(x, y, i % 3 === 0 ? WHITE : C[6]);
    if (i % 4 === 0) p.px(x + Math.cos(a), y + Math.sin(a), C[5]);
  }
  if (k > 0) p.glow(0, 0, Math.max(8, radius * 0.35), C[5], 0.16 * k);
}

function drops(p, C, S, radius, age, k, count, arc) {
  for (let i = 0; i < count; i++) {
    const r = rng(S, 40, i);
    const life = r.range(0.14, 0.3);
    const local = age - r.range(0, 0.04);
    if (local < 0 || local > life) continue;
    const a = arc == null ? r.range(0, TAU) : r.range(-arc, arc);
    const d = radius * r.range(0.45, 1);
    const fade = k * (1 - local / life);
    bead(p, Math.cos(a) * d, Math.sin(a) * d, Math.cos(a) * r.range(30, 110), Math.sin(a) * r.range(30, 110), r.chance(0.28) ? 2 : 1, C, fade);
  }
}

function rings(p, C, radius, k, bands) {
  for (let b = 0; b < bands; b++) {
    const rr = radius - b * 7;
    if (rr < 5) break;
    p.ring(0, 0, rr, b === 0 ? 2.1 : 1.3, (v) => (v < 0.32 ? WHITE : v < 0.68 ? C[6] : C[3]), k * (1 - b * 0.24));
  }
}

function ripple(p, t, o) {
  const C = PALS[o.pal] || PALS.water;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const radius = front(o.reach, hit.age);
  rings(p, C, radius, hit.k, o.bands || 3);
  crest(p, C, radius, o.crests || 20, hit.age * 2, hit.k);
  drops(p, C, o.seed || 1, radius, hit.age, hit.k, o.drops || 16);
}

function slam(p, t, o) {
  const C = PALS[o.pal] || PALS.water;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const radius = front(o.reach, hit.age);
  p.circle(0, 0, radius * 0.9, (d) => (d < 0.12 ? WHITE : d < 0.4 ? C[6] : d < 0.72 ? C[4] : C[2]), hit.k * 0.72);
  p.ring(0, 0, radius, 2.6, (v) => (v < 0.4 ? WHITE : C[5]), hit.k);
  crest(p, C, radius, 24, 0.4, hit.k);
  drops(p, C, o.seed || 2, radius, hit.age, hit.k, 22);
}

function whirl(p, t, o) {
  const C = PALS.abyss;
  const hit = pulse(t, 1, 0);
  if (hit.k <= 0) return;
  const radius = front(o.reach, hit.age);
  p.circle(0, 0, radius * 0.55, (d) => (d < 0.2 ? C[6] : C[2]), hit.k * 0.45);
  for (let arm = 0; arm < 3; arm++) {
    for (let s = 0; s < 18; s++) {
      const u = s / 17;
      const a = arm * TAU / 3 + u * 5.2 - t * 8;
      const d = u * radius;
      p.circle(Math.cos(a) * d, Math.sin(a) * d, 2.4 - u * 1.2, (q) => (q < 0.35 ? WHITE : C[u < 0.4 ? 6 : 4]), hit.k);
    }
  }
  p.ring(0, 0, radius, 1.8, (v) => (v < 0.4 ? WHITE : C[5]), hit.k);
  drops(p, C, o.seed || 3, radius, hit.age, hit.k, 14);
}

function spout(p, t, o) {
  const C = PALS.water;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const radius = front(o.reach, hit.age) * 0.72;
  const height = 18 + 34 * hit.k;
  for (let y = 0; y < height; y += 3) {
    const w = 3.4 * (1 - y / (height + 10));
    p.circle(0, -y, w, (d) => (d < 0.28 ? WHITE : d < 0.6 ? C[6] : C[4]), hit.k);
  }
  rings(p, C, radius, hit.k, 2);
  crest(p, C, radius, 14, hit.age, hit.k);
  drops(p, C, o.seed || 4, radius, hit.age, hit.k, 12);
}

function band(p, t, o) {
  const C = PALS[o.pal] || PALS.foam;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const outer = front(o.reach, hit.age);
  const inner = Math.max(6, outer - o.band);
  p.glowRing(0, 0, (inner + outer) * 0.5, o.band, C[5], 0.32 * hit.k);
  p.ring(0, 0, outer - 1, 2.3, (v) => (v < 0.36 ? WHITE : C[6]), hit.k);
  p.ring(0, 0, inner, 1.5, C[3], hit.k * 0.85);
  const steps = 28;
  for (let i = 0; i < steps; i++) {
    const a = (i / steps) * TAU + hit.age;
    const d = inner + (outer - inner) * (0.35 + 0.4 * ((i * 3) % 5) / 5);
    if (i % 2 === 0) bead(p, Math.cos(a) * d, Math.sin(a) * d, Math.cos(a) * 40, Math.sin(a) * 40, 1, C, hit.k * 0.8);
    else p.px(Math.cos(a) * outer, Math.sin(a) * outer, WHITE);
  }
}

function fan(p, t, o) {
  const C = PALS[o.pal] || PALS.water;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const len = front(o.reach, hit.age);
  const n = o.streams;
  const half = o.half;
  for (let i = 0; i < n; i++) {
    const a = n === 1 ? 0 : -half + (i / (n - 1)) * half * 2;
    const ca = Math.cos(a);
    const sa = Math.sin(a);
    const tip = len * (0.78 + 0.22 * ((i % 3) / 2));
    const body = o.body * hit.k;
    p.beam(0, 0, ca * tip, sa * tip, body + 2.2, C[2]);
    p.beam(0, 0, ca * tip, sa * tip, body, (u, v) => (v < 0.28 ? WHITE : C[6]));
    bead(p, ca * tip, sa * tip, ca * 70, sa * 70, 2, C, hit.k);
  }
  p.circle(2, 0, 3.2 * hit.k + 1, WHITE, hit.k);
  drops(p, C, o.seed || 5, len, hit.age, hit.k, 10, half);
}

function sheet(p, t, o) {
  const C = PALS[o.pal] || PALS.foam;
  const hit = pulse(t, o.hits || 1, o.gap || 0);
  if (hit.k <= 0) return;
  const outer = front(o.reach, hit.age);
  const thick = o.thick;
  const inner = Math.max(4, outer - thick);
  const half = o.half;
  const pad = 6;
  p.shade(-outer - pad, -outer - pad, outer + pad, outer + pad, (x, y, out) => {
    const d = Math.hypot(x, y);
    if (d < inner || d > outer + 1) return;
    const a = Math.atan2(y, x);
    if (Math.abs(a) > half) return;
    const q = (d - inner) / Math.max(1, outer - inner);
    out.a = hit.k;
    return q < 0.2 ? WHITE : q < 0.55 ? C[6] : C[3];
  });
  p.circle(1, 0, 2.4 * hit.k + 1, WHITE, hit.k);
  drops(p, C, o.seed || 6, outer, hit.age, hit.k, 12, half);
}

function streak(p, t, o) {
  const C = PALS[o.pal] || PALS.water;
  const len = o.len;
  const body = o.body;
  const wob = Math.sin(t * 28) * 0.6;
  p.beam(-len, wob * 0.3, -1, 0, body + 2, C[2]);
  p.beam(-len * 0.85, wob, 1, 0, body, (u, v) => (v < 0.26 ? WHITE : u > 0.72 ? C[6] : C[4]));
  if (o.head === "arrow") {
    p.poly([[7, 0], [-5, -body * 1.5], [-1, 0], [-5, body * 1.5]], C[5]);
    p.poly([[4, 0], [-2, -body * 0.55], [1, 0], [-2, body * 0.55]], WHITE);
  } else if (o.head === "spear") {
    p.poly([[9, 0], [-12, -body * 0.8], [-3, 0], [-12, body * 0.8]], C[4]);
    p.poly([[6, 0], [-7, -body * 0.35], [0, 0], [-7, body * 0.35]], WHITE);
  } else if (o.head === "column") {
    for (let x = -len + 6; x < -2; x += 9) p.circle(x, wob, body * 0.85, (d) => (d < 0.35 ? WHITE : C[5]), 1);
    p.circle(2, 0, body, (d) => (d < 0.3 ? WHITE : C[6]), 1);
  } else {
    p.line(-len * 0.2, 0, 8, 0, WHITE);
    p.circle(6, 0, 1.3, WHITE, 1);
  }
  const n = o.beads || 5;
  for (let i = 0; i < n; i++) {
    const r = rng(o.seed || 1, 9, i);
    const u = (i / n + t * 1.7) % 1;
    const x = -4 - u * len;
    bead(p, x, r.range(-body, body) + wob, -90, r.range(-20, 20), 1, C, 1 - u);
  }
  p.glow(0, 0, 10 + body, C[5], 0.28);
}

const area = (name, duration, draw, options) => ({
  name,
  draw,
  reach: REACH,
  zoom: 1,
  meta: {
    name,
    size: [(REACH + 18) * 2, (REACH + 18) * 2],
    origin: [REACH + 18, REACH + 18],
    duration,
    loop: false,
    fps: 30,
    options: { seed: 4, reach: REACH, ...options },
  },
});

const shot = (name, size, origin, options) => ({
  name,
  draw: streak,
  reach: 0,
  zoom: 3,
  meta: {
    name,
    size,
    origin,
    duration: 0.4,
    loop: true,
    fps: 30,
    options,
  },
});

const deg = (value) => (value * Math.PI) / 180;

export const effects = [
  shot("WaterArrow", [84, 28], [68, 14], { len: 46, body: 2.2, head: "arrow", pal: "water", beads: 5, seed: 11 }),
  shot("RapidCurrent", [64, 18], [50, 9], { len: 34, body: 1.35, head: "needle", pal: "water", beads: 4, seed: 12 }),
  shot("QuadrupleCurrent", [52, 16], [40, 8], { len: 26, body: 1.05, head: "needle", pal: "foam", beads: 3, seed: 13 }),
  shot("TwinWaterSpear", [80, 28], [62, 14], { len: 40, body: 2.7, head: "spear", pal: "water", beads: 4, seed: 14 }),
  shot("LongWaterspout", [118, 26], [98, 13], { len: 78, body: 2.5, head: "column", pal: "deep", beads: 7, seed: 15 }),

  area("RipplePierce", 0.46, ripple, { pal: "water", bands: 3, drops: 12, crests: 16 }),
  area("PressureLance", 0.48, slam, { pal: "water" }),
  area("RepeatingWaterspout", 0.78, spout, { hits: 4, gap: 0.12 }),
  area("AbyssLance", 0.55, whirl, {}),
  area("PressureBarrage", 0.74, slam, { pal: "deep", hits: 3, gap: 0.16 }),
  area("DeepSeaLance", 0.52, ripple, { pal: "abyss", bands: 2, drops: 10, crests: 12 }),
  area("OceanPierce", 0.68, slam, { pal: "foam", hits: 2, gap: 0.25 }),

  area("OceanLine", 0.5, band, { pal: "foam", band: 26 }),
  area("TidalLine", 1.02, band, { pal: "deep", band: 20, hits: 4, gap: 0.2 }),

  area("RapidsPierce", 0.58, fan, { half: deg(35), streams: 5, body: 2.4, hits: 2, gap: 0.16, pal: "water" }),
  area("RapidsBreakthrough", 0.5, fan, { half: deg(45), streams: 7, body: 3.3, pal: "deep" }),
  area("DeepSeaFlash", 0.7, sheet, { half: deg(70), thick: 16, hits: 3, gap: 0.14, pal: "foam" }),
  area("PressureFlash", 0.5, sheet, { half: deg(80), thick: 20, pal: "water" }),
  area("CrossingCurrent", 0.52, sheet, { half: deg(90), thick: 22, pal: "deep" }),
  area("SeveringTide", 0.54, sheet, { half: deg(100), thick: 18, pal: "foam" }),
];
