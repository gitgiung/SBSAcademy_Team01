// Straight-line skills for SBSAcademy_Team01. Each runs right from its root (the origin, the caster) to `reach`
// px; the game turns it to the aim and scales it so reach is the skill's range. The hit lands on the first frame,
// so nothing here winds up for long. Built on fire/_lib.js (flame blades, embers, sparks), adapted from Sprite
// Fusion's open-source fire (https://www.spritefusion.com/pixel-art-vfx/fire), MIT License, Copyright (c) 2026 Hugo Duprez.
import { clamp, smooth, ease, rng, TAU, WHITE, sparkle } from "../../core/index.js";
import { flameBlade, ember, LAYERS, BIG_LAYERS, BACK_LAYERS } from "../fire/_lib.js";
import { ramp, glowOf, noiseAt, ONCE, flash, fling } from "./_kit.js";

// dying flames: deep orange and red, no white heart
const EMBER_LAYERS = [[1, 1, 7], [0.7, 0.8, 6], [0.4, 0.55, 5]];
const cooling = (u) => (u < 0.35 ? LAYERS : u < 0.7 ? BACK_LAYERS : EMBER_LAYERS);

// 10005 화염창: a spear of fire thrown down the line. It crosses in three frames, burns a wake behind it and
// bursts where it stops.
function flameSpear(p, t, o) {
  const H = ramp("fire"), L = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), EMB = [H[1], H[3], H[5], H[6]];
  // it is already a third of the way out on the first frame: the hit lands then
  const T_FLY = 0.1, T_LEAD = 0.033, T_HOLD = 0.15;
  const tip = L * ease.outQuad(clamp((t + T_LEAD) / T_FLY));
  const passed = (x) => T_FLY * (1 - Math.sqrt(1 - clamp(x / L))) - T_LEAD; // when the head went through x
  // the wake: flames standing off the path where it has passed, leaning back, burning down
  for (let i = 0; i < 30; i++) {
    const r = rng(S, 21, i), x = r.range(0.02, 0.97) * (L - 10), age = t - passed(x), life = r.range(0.18, 0.4);
    if (x > tip || age < 0 || age > life) continue;
    const u = age / life, side = r.sign(), a = r.range(0.4, 1.15), len = r.range(7, 19) * (1 - u * 0.8) * Math.min(1, age / 0.03);
    flameBlade(p, x, side * 1.5, -Math.cos(a), side * Math.sin(a), len, r.range(2.4, 3.4) * (1 - 0.4 * u), 0, N, (S * 7 + i) * 13, H, cooling(u), 0.45, 0.9);
  }
  // the streak it burns into the air
  const sw = 1 - smooth(0.1, 0.42, t);
  if (sw > 0.02 && tip > 4)
    p.beam(0, 0, tip, 0, 2 + 5 * sw, (u, v) => (v < 0.3 ? (sw > 0.5 ? WHITE : H[1]) : v < 0.65 ? H[2] : H[4]), (u) => clamp(0.35 + u * 1.3) * Math.min(1, sw / 0.3));
  // the spear: a long white-hot head, a shaft of light, flame streaming back off it
  if (t < T_HOLD) {
    [[58, 5.5, 0], [40, 5, -2.2], [40, 5, 2.2]].forEach(([len, w, dy], i) => {
      const n = (S * 5 + i) * 17;
      flameBlade(p, tip - 10, dy * 0.5, -1, dy * 0.02, Math.min(len, tip) * (0.85 + 0.2 * N(8, n)), w, N(5, n + 5) * 3 + dy, N, n, H, i ? LAYERS : BIG_LAYERS, 0.5, 1.6);
    });
    if (tip > 20) p.beam(Math.max(0, tip - 78), 0, tip - 16, 0, 3, (u, v) => (v < 0.5 ? WHITE : H[2]));
    p.poly([[tip + 2, 0], [tip - 9, -5.2], [tip - 31, 0], [tip - 9, 5.2]], H[3]);
    p.poly([[tip, 0], [tip - 9, -3.5], [tip - 26, 0], [tip - 9, 3.5]], H[1]);
    p.poly([[tip - 3, 0], [tip - 9, -1.8], [tip - 20, 0], [tip - 9, 1.8]], WHITE);
    p.glow(tip - 8, 0, 30, G, 0.6);
  }
  // where it stops: a flash, a glint, sparks thrown on ahead
  const hit = t - (T_FLY - T_LEAD);
  if (hit >= 0) {
    flash(p, L - 4, 0, 11, 1 - hit / 0.12, H);
    if (hit < 0.14) sparkle(p, L - 4, 0, 10 * (1 - hit / 0.14), WHITE, H[2], 0.6);
    fling(p, t, S + 5, H, { n: 18, x: L - 6, a0: -0.9, a1: 0.9, sp: [60, 240], life: [0.15, 0.4], t0: T_FLY - T_LEAD });
  }
  // embers shaken off along the path
  for (let i = 0; i < 22; i++) {
    const r = rng(S, 33, i), x = r.range(0.05, 0.95) * L, age = t - passed(x) - r.range(0, 0.08), life = r.range(0.2, 0.4);
    if (age < 0 || age > life) continue;
    ember(p, x - 20 * age, r.range(-4, 4) + r.range(-30, 30) * age, 1 - age / life, r.chance(0.3) ? 2 : 1.2, EMB, age * 40 + i);
  }
  p.glowLine(0, 0, tip, 0, 16, G, 0.4 * (1 - smooth(0.15, 0.5, t)));
}

// 10012 용암 파편: three volleys of molten shards, one for each hit, each crossing in four frames and
// splashing where it stops.
const VOLLEYS = [0, 0.15, 0.3], T_CROSS = 0.13;
function lavaShards(p, t, o) {
  const H = ramp("lava"), L = o.reach, S = o.seed, G = glowOf(H);
  VOLLEYS.forEach((t0, v) => {
    for (let i = 0; i < 5; i++) {
      const r = rng(S, 41, v, i), lane = ((i - 2) / 2) * 8 + r.range(-1.5, 1.5), fly = T_CROSS * r.range(0.85, 1.1), size = r.range(5.2, 7.6), spin = r.range(-9, 9), rot0 = r.range(0, TAU), stop = L * r.range(0.9, 1);
      const age = t - t0 - r.range(0, 0.035) + 0.04; // the first shards are already out on the first frame
      if (age < 0) continue;
      const u = clamp(age / fly), x = stop * u, after = age - fly, fade = after > 0 ? 1 - after / 0.12 : 1;
      if (fade <= 0) continue;
      // the molten trail: hot at the shard, cooling and breaking up behind
      const from = after > 0 ? stop - 48 * fade : Math.max(0, x - 48);
      if (x - from > 2)
        p.beam(from, lane, x, lane, 2.2, (q, w) => (q > 0.7 ? (w < 0.5 ? H[1] : H[3]) : q > 0.35 ? H[4] : H[6]), (q) => (0.15 + q * 1.1) * fade);
      p.glowLine(from, lane, x, lane, 7, G, 0.25 * fade);
      if (after <= 0) {
        // the shard: a dark crust with a glowing rim, white-hot where it cuts the air
        const rot = rot0 + spin * age, pts = (k) => [[0, 1.6], [1.7, 0.8], [3.1, 1.05], [4.6, 0.75]].map(([a, rad]) => [x + Math.cos(a + rot) * rad * size * k, lane + Math.sin(a + rot) * rad * size * k * 0.8]);
        p.poly(pts(1.3), H[3]);
        p.poly(pts(1), H[7]);
        p.poly(pts(0.45), H[4]);
        p.px(x + size * 1.2, lane, WHITE);
        p.px(x + size * 1.2 - 1, lane, H[1]);
        p.glow(x, lane, 14, G, 0.5);
      } else {
        // the splash where it stops
        flash(p, stop, lane, 5.5, 1 - after / 0.09, H);
        fling(p, t, S + v * 10 + i, H, { n: 5, x: stop, y: lane, a0: -1.3, a1: 1.3, sp: [50, 170], life: [0.12, 0.3], t0: t - after, spread: 0.01 });
      }
    }
  });
}

// 20014 멸화 일섬: one stroke. A white hairline the whole length for a frame, then it opens into a lens of
// dark-edged crimson fire that flames along both lips and closes to a dotted scar.
function annihilation(p, t, o) {
  const H = ramp("hell"), L = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), EMB = [H[1], H[3], H[5], H[6]];
  const lens = (x) => Math.pow(Math.sin(Math.PI * clamp(x / L)), 0.45); // 0 at both ends, 1 through the middle
  const open = ease.outCubic(clamp((t - 0.04) / 0.12)), close = smooth(0.2, 0.5, t);
  // flames erupting from both lips, a wave running out from the root
  for (let i = 0; i < 30; i++) {
    const r = rng(S, 51, i), x = ((i + r.range(0.15, 0.85)) / 30) * L, age = t - (0.04 + (x / L) * 0.05 + r.range(0, 0.03)), life = r.range(0.22, 0.4);
    if (age < 0 || age > life) continue;
    const u = age / life, side = i % 2 ? 1 : -1, lean = r.range(-0.35, 0.35);
    const len = r.range(12, 21) * lens(x) * ease.outBack(clamp(age / 0.05)) * (1 - 0.75 * u);
    flameBlade(p, x, side * 1.5, Math.sin(lean), side * Math.cos(lean), len, r.range(3.4, 5) * (1 - 0.4 * u), 0, N, (S * 7 + i) * 13, H, cooling(u), 0.5, 1);
  }
  // the cut itself
  if (close < 1) {
    const wide = t < 0.04 ? 1.6 : (1.6 + 4.6 * open) * (1 - close);
    p.shade(0, -9, L, 9, (x, y) => {
      const hw = lens(x) * wide, ay = Math.abs(y);
      if (hw < 0.3 || ay > hw + 1) return;
      if (ay > hw) return t < 0.26 ? H[8] : 0;
      const q = ay / hw;
      return q < 0.5 - 0.35 * close ? WHITE : q < 0.78 ? H[2] : H[4];
    });
  }
  if (t < 0.07) {
    p.line(-6, 0, L + 8, 0, WHITE);
    sparkle(p, L * 0.5, 0, 12 * (1 - t / 0.07), WHITE, H[2], 0.8);
  }
  // the scar it leaves
  if (t > 0.3) for (let x = 2; x < L; x++) p.dot(x, 0, H[4], 0.75 * (1 - smooth(0.36, 0.53, t)) * lens(x));
  // sparks off the far end, embers lifting off the lips
  fling(p, t, S + 3, H, { n: 16, x: L, a0: -0.7, a1: 0.7, sp: [80, 260], life: [0.15, 0.35], t0: 0.03 });
  for (let i = 0; i < 24; i++) {
    const r = rng(S, 63, i), x = r.range(0.05, 0.95) * L, age = t - 0.08 - r.range(0, 0.14), life = r.range(0.2, 0.34), side = i % 2 ? 1 : -1;
    if (age < 0 || age > life) continue;
    ember(p, x + r.range(-10, 10) * age, side * (3 + r.range(20, 60) * age), 1 - age / life, r.chance(0.3) ? 2 : 1.2, EMB, age * 40 + i);
  }
  p.glowLine(0, 0, L, 0, 22, G, 0.5 * (1 - smooth(0.1, 0.5, t)) + (t < 0.07 ? 0.3 : 0));
}

const line = (name, reach, height, duration, draw) => ({ name, draw, reach, zoom: 1, meta: { name, size: [reach + 44, height], origin: [16, height >> 1], duration, loop: false, fps: 30, options: { seed: 6, reach } } });

export const effects = [
  line("FlameSpear", 256, 76, 0.5, flameSpear),
  line("LavaShards", 224, 64, 0.58, lavaShards),
  line("Annihilation", 256, 84, 0.55, annihilation),
];
