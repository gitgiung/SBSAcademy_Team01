// Projectiles for SBSAcademy_Team01's light skills. Each one flies right, loops, and has its head on the origin;
// the game turns it to the aim and moves it at 12 units a second (384 px/s at 32 px a unit), so what it sheds is
// left behind at about that speed. Built on fire/_lib.js (flame blades, puffs, embers, sparks) and fire/fireball.js,
// adapted from Sprite Fusion's open-source fire (https://www.spritefusion.com/pixel-art-vfx/fire),
// MIT License, Copyright (c) 2026 Hugo Duprez.
import { rng, TAU, WHITE, sparkle, bolt, forks } from "../../core/index.js";
import { noiseFor, flameBlade, firePuff, ember, spark, solid, soft, looped, LAYERS } from "../fire/_lib.js";
import { draw as fireball } from "../fire/fireball.js";
import { PALS, ramp, glowOf, noiseAt, frameOf } from "./_kit.js";

const D = 0.4, NL = noiseFor(D), V = 384;
const DIAG = [[1, 1], [1, -1], [-1, 1], [-1, -1]], CROSS = [[1, 0], [-1, 0], [0, 1], [0, -1]];

/** Sparks and embers the head leaves behind. */
function shed(p, t, S, H, n, o = {}) {
  const SPK = [H[1], H[2], H[4], H[5]], EMB = [H[1], H[3], H[5], H[6]];
  looped(t, D, n, S + 300, (r) => ({ x: -r.range(o.x0 ?? 2, o.x1 ?? 12), y: r.range(-1, 1) * (o.y ?? 2.5), v: V * r.range(0.25, 0.55), vy: r.range(-22, 22), life: r.range(0.1, 0.24), big: r.chance(0.25), ph: r.range(0, 6.3) }), (q, age) => {
    const x = q.x - q.v * age, y = q.y + q.vy * age, k = 1 - age / q.life;
    if (q.big) ember(p, x, y, k, 1.2, EMB, age * 40 + q.ph);
    else spark(p, x, y, -q.v, q.vy, k, SPK);
  });
}

/** Flame tongues streaming back from (x, y): [[length, half-width, y offset], ...]. */
function tail(p, x, y, list, N, S, H, jit = 0.45) {
  list.forEach(([len, w, dy], i) => {
    const n = (S * 5 + i) * 17, L = len * (0.85 + 0.2 * N(5, n));
    flameBlade(p, x, y + dy * 0.5, -1, dy * 0.02, L, w, N(2.5, n + 5) * 2 + dy, N, n, H, LAYERS, jit, 1 + L * 0.04);
  });
}

// 10001 불꽃 탄환: the plain fire bullet, a small white-hot slug with a short flame tail.
function fireBullet(p, t, o) {
  const H = ramp("fire"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H), hy = N(2.5, S + 51) * 0.4;
  tail(p, 1, hy, [[27, 3.1, 0], [18, 3.2, -1.2], [17, 3.2, 1.2]], N, S, H);
  solid(p, 0, hy, 4.3, H[4]);
  solid(p, 0.5, hy, 3.3, H[2]);
  solid(p, 1, hy, 2.2, WHITE);
  shed(p, t, S, H, 9);
  p.glowLine(-4, hy, -28, 0, 8, G, 0.22);
  p.glow(0, hy, 22, G, 0.42);
  p.glow(0, hy, 8, H[1], 0.3);
}

// 10002 불꽃 난사: three quick shots, so each is a thin hot needle.
function fireDart(p, t, o) {
  const H = ramp("fire"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H);
  flameBlade(p, -3, 0, -1, 0, 16 + 3 * N(5, S + 3), 2.3, N(2.5, S + 5) * 1.5, N, S * 17, H, LAYERS, 0.35, 0.8);
  for (let x = -10; x <= 2; x++) {
    p.px(x, -1, x > -4 ? H[3] : H[5]);
    p.px(x, 1, x > -4 ? H[3] : H[5]);
  }
  for (let x = -14; x <= 6; x++) p.px(x, 0, x > 1 ? WHITE : x > -6 ? H[1] : H[3]);
  p.px(7, 0, H[2]);
  shed(p, t, S, H, 5, { y: 1.5 });
  p.glowLine(4, 0, -20, 0, 6, G, 0.3);
  p.glow(2, 0, 12, H[1], 0.3);
}

// 10004 화염구: pixelfx's own fireball, a churning head with a torn tail that cools into smoke.
function fireOrb(p, t) {
  fireball(p, t, { palette: PALS.fire, radius: 7, speed: 120, seed: 5 });
}

// 10006 플레어 탄환: a strong flare, a white star that leaves a line of light and twinkles.
function flareShot(p, t, o) {
  const H = ramp("sun"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H);
  for (let x = -4; x >= -42; x--) {
    const u = -x / 42;
    p.dot(x, 0, u < 0.3 ? H[1] : u < 0.6 ? H[3] : H[5], 1.15 - u);
    if (u < 0.45) {
      p.dot(x, -1, H[4], 0.85 - u);
      p.dot(x, 1, H[4], 0.85 - u);
    }
  }
  looped(t, D, 10, S + 200, (r) => ({ x: -r.range(4, 14), y: r.range(-6, 6), v: V * r.range(0.2, 0.35), life: r.range(0.12, 0.26), big: r.chance(0.45) }), (q, age) => {
    const x = q.x - q.v * age, k = 1 - age / q.life;
    if (k < 0.3 && Math.sin(age * 90) < 0) return;
    if (q.big && k > 0.4) sparkle(p, x, q.y, 2, H[1], H[3], 0.3 * k);
    else {
      p.px(x, q.y, k > 0.5 ? H[1] : H[3]);
      p.glow(x, q.y, 4, H[2], 0.3 * k);
    }
  });
  const arm = Math.round(8.5 + 1.5 * N(5, S + 1)), d = Math.round(4.5 + N(7.5, S + 2));
  for (let i = 1; i <= arm; i++) for (const [a, b] of CROSS) p.px(a * i, b * i, i > arm * 0.66 ? H[3] : i > arm * 0.33 ? H[1] : WHITE);
  for (let i = 1; i <= d; i++) for (const [a, b] of DIAG) p.px(a * i, b * i, i > d * 0.6 ? H[4] : H[2]);
  solid(p, 0, 0, 4.4, H[3]);
  solid(p, 0, 0, 3.4, H[1]);
  solid(p, 0, 0, 2.4, WHITE);
  p.glowLine(-2, 0, -36, 0, 9, G, 0.3);
  p.glow(0, 0, 30, G, 0.55);
  p.glow(0, 0, 12, H[0], 0.4);
}

// 10008 열풍 탄: a light shot of hot wind: a white-hot crescent of air with wings of flame swept back off its tips.
function heatGust(p, t, o) {
  const H = ramp("fire"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H);
  for (const side of [-1, 1]) {
    flameBlade(p, -1, side * 7, -0.99, side * 0.14, 21 + 4 * N(5, S + side), 2.6, -side * 3, N, S * 7 + side * 3, H, LAYERS, 0.4, 1.2);
    flameBlade(p, -6, side * 3.4, -1, 0, 13 + 3 * N(5, S + 4 + side), 1.9, -side * 1.5, N, S * 7 + 20 + side * 3, H, LAYERS, 0.35, 0.9);
  }
  flameBlade(p, -8, 0, -1, 0, 20 + 4 * N(5, S + 9), 2.2, N(2.5, S + 8) * 2, N, S * 7 + 40, H, LAYERS, 0.4, 1.1);
  p.arc(-5, 0, 9.5 + 0.5 * N(5, S + 9), (u) => 4.8 * Math.pow(Math.sin(Math.PI * u), 0.7), -1.05, 1.05, (u, v) => (v > 0.72 ? H[0] : v > 0.45 ? H[2] : v > 0.2 ? H[4] : H[5]));
  shed(p, t, S, H, 7, { y: 6, x0: 6, x1: 16 });
  p.glowLine(0, 0, -26, 0, 12, G, 0.22);
  p.glow(0, 0, 20, G, 0.4);
}

// 10009 홍염 화살: a crimson fire arrow, a glowing shaft and head wrapped in flame.
function flameArrow(p, t, o) {
  const H = ramp("crimson"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H);
  tail(p, -2, 0, [[31, 3.4, 0], [20, 3, -1.5], [20, 3, 1.5]], N, S, H);
  flameBlade(p, -25, -1, -0.85, -0.52, 7 + N(5, S + 30), 1.5, 0, N, S * 3 + 40, H, LAYERS, 0.3, 0.5);
  flameBlade(p, -25, 1, -0.85, 0.52, 7 + N(5, S + 31), 1.5, 0, N, S * 3 + 41, H, LAYERS, 0.3, 0.5);
  for (let x = -28; x <= -2; x++) p.px(x, 0, x > -12 ? H[1] : H[3]);
  for (let x = -3; x <= 8; x++) {
    const hw = (8 - x) * 0.42;
    for (let y = -Math.ceil(hw); y <= Math.ceil(hw); y++) {
      if (Math.abs(y) > hw + 0.3) continue;
      p.px(x, y, Math.abs(y) > hw - 0.7 ? H[3] : x > 3 ? WHITE : H[1]);
    }
  }
  shed(p, t, S, H, 8, { x0: 4, x1: 22 });
  p.glowLine(4, 0, -30, 0, 8, G, 0.25);
  p.glow(2, 0, 18, G, 0.42);
}

// 10010 불씨 산탄: four of these scatter at once, so each is one small ember.
function emberPellet(p, t, o) {
  const H = ramp("fire"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H), ph = t / D, f = 0.5 + 0.5 * N(7.5, S + 1);
  for (let x = -3; x >= -16; x--) {
    const u = -x / 16;
    p.dot(x, Math.sin(x * 0.7 + ph * TAU) * 0.7 * u, u < 0.3 ? H[3] : u < 0.6 ? H[5] : H[6], 1.05 - u);
  }
  for (const [a, b] of CROSS) p.px(a * 2, b * 2, H[5]);
  for (const [a, b] of DIAG) p.px(a, b, H[4]);
  for (const [a, b] of CROSS) p.px(a, b, H[2]);
  p.px(0, 0, f > 0.35 ? WHITE : H[1]);
  shed(p, t, S, H, 3, { y: 1 });
  p.glow(0, 0, 11, G, 0.4 + 0.1 * f);
  p.glow(0, 0, 4, H[1], 0.4);
}

// 10013 화염 연사: four straight shots in quick succession, each a tracer of fire.
function fireTracer(p, t, o) {
  const H = ramp("fire"), S = o.seed, N = noiseAt(NL, t), G = glowOf(H);
  for (let x = 2; x >= -46; x--) {
    const u = (2 - x) / 48;
    p.dot(x, 0, u < 0.2 ? WHITE : u < 0.4 ? H[1] : u < 0.62 ? H[3] : u < 0.82 ? H[5] : H[6], u < 0.6 ? 1 : 1.5 - u * 1.4 + 0.2 * N(7.5, S, x * 0.3));
    if (u < 0.3) {
      p.px(x, -1, u < 0.12 ? H[1] : H[3]);
      p.px(x, 1, u < 0.12 ? H[1] : H[3]);
    }
  }
  p.px(3, 0, WHITE);
  p.px(4, 0, H[2]);
  shed(p, t, S, H, 5, { y: 1.2 });
  p.glowLine(2, 0, -30, 0, 6, G, 0.32);
  p.glow(1, 0, 10, H[1], 0.35);
}

// 10018 얼음 탄환: a faceted shard of ice trailing frost and cold mist.
function iceShard(p, t, o) {
  const H = ramp("ice"), S = o.seed, N = noiseAt(NL, t);
  looped(t, D, 10, S + 100, (r) => ({ x: -r.range(6, 16), y: r.range(-3, 3), v: V * r.range(0.15, 0.3), life: r.range(0.15, 0.3), s: r.range(2, 3.6) }), (q, age) =>
    soft(p, q.x - q.v * age, q.y, q.s * (0.7 + age * 3), H[3], 0.24 * (1 - age / q.life)));
  looped(t, D, 12, S + 200, (r) => ({ x: -r.range(2, 14), y: r.range(-5, 5), v: V * r.range(0.25, 0.5), vy: r.range(-14, 14), life: r.range(0.12, 0.26), big: r.chance(0.3) }), (q, age) => {
    const x = q.x - q.v * age, y = q.y + q.vy * age, k = 1 - age / q.life;
    if (k < 0.3 && Math.sin(age * 80) < 0) return;
    if (q.big) for (const [a, b] of CROSS) p.px(x + a, y + b, H[2]);
    p.px(x, y, q.big ? WHITE : k > 0.5 ? H[1] : H[3]);
    p.glow(x, y, 4, H[3], 0.25 * k);
  });
  const half = (x) => (x > 1 ? (4.6 * (10 - x)) / 9 : (4.6 * (x + 15)) / 16);
  for (let x = -15; x <= 10; x++) {
    const hw = half(x);
    for (let y = -Math.ceil(hw); y <= Math.ceil(hw); y++) {
      const ay = Math.abs(y);
      if (ay > hw + 0.35) continue;
      p.px(x, y, ay > hw - 0.75 ? (y < 0 ? H[1] : H[6]) : y === 0 ? WHITE : y < 0 ? (x > 1 ? H[2] : H[3]) : x > 1 ? H[4] : H[5]);
    }
  }
  if (N(5, S + 4) > 0) sparkle(p, 4, -2, 2, WHITE, H[2], 0.3);
  p.glow(0, 0, 20, H[3], 0.35);
  p.glow(4, 0, 8, H[1], 0.3);
}

// 10019 바람 화살: an arrow of wind, two curved blades of air meeting at a point, a helix of air twisting behind.
function windArrow(p, t, o) {
  const H = ramp("wind"), S = o.seed, ph = t / D;
  for (let s = 0; s < 2; s++)
    for (let x = -4; x >= -44; x--) {
      const u = (-x - 4) / 40, a = x * 0.3 + ph * TAU * 2 + s * Math.PI, y = Math.sin(a) * (2 + 4.5 * u), front = Math.cos(a) > 0;
      p.dot(x, y, front ? (u < 0.4 ? H[1] : H[3]) : H[5], (front ? 1.1 : 0.7) - u * 0.9);
    }
  for (let x = 4; x >= -30; x--) {
    const u = (4 - x) / 34;
    p.dot(x, 0, u < 0.3 ? WHITE : u < 0.6 ? H[2] : H[4], 1.1 - u);
  }
  for (let i = 0; i <= 28; i++) {
    const u = i / 28, x = 8 - u * 16, y = 5.4 * Math.pow(u, 0.7), c = u < 0.35 ? WHITE : u < 0.7 ? H[2] : H[3];
    p.px(x, y, c);
    p.px(x, -y, c);
    if (u > 0.2 && u < 0.85) {
      p.px(x - 1, y - 1, H[4]);
      p.px(x - 1, -y + 1, H[4]);
    }
  }
  p.px(9, 0, WHITE);
  looped(t, D, 8, S + 200, (r) => ({ x: -r.range(2, 14), y: r.range(-7, 7), v: V * r.range(0.3, 0.5), life: r.range(0.1, 0.2) }), (q, age) => {
    const x = q.x - q.v * age;
    p.line(x, q.y, x + 3, q.y, age / q.life < 0.5 ? H[2] : H[4]);
  });
  p.glowLine(4, 0, -30, 0, 9, H[3], 0.22);
  p.glow(2, 0, 16, H[2], 0.3);
}

// 10020 암석포: a tumbling boulder trailing dust and pebbles.
function rockShot(p, t, o) {
  const E = ramp("earth"), S = o.seed, rot = (t / D) * TAU, R0 = 8;
  looped(t, D, 14, S + 100, (r) => ({ x: -r.range(4, 14), y: r.range(-5, 5), v: V * r.range(0.15, 0.3), life: r.range(0.16, 0.32), s: r.range(2.4, 4.2) }), (q, age) =>
    soft(p, q.x - q.v * age, q.y, q.s * (0.7 + age * 3.5), E[3], 0.32 * (1 - age / q.life)));
  looped(t, D, 6, S + 200, (r) => ({ x: -r.range(4, 12), y: r.range(-7, 7), v: V * r.range(0.3, 0.5), vy: r.range(-20, 20), life: r.range(0.14, 0.26) }), (q, age) => {
    const x = q.x - q.v * age, y = q.y + q.vy * age;
    p.px(x, y, E[4]);
    p.px(x + 1, y, E[2]);
    p.px(x, y + 1, E[6]);
  });
  const bump = (a) => 1 + 0.16 * Math.sin(a * 3 + 1.3) + 0.1 * Math.sin(a * 5 + 4.1) + 0.06 * Math.sin(a * 7 + 0.4);
  p.shade(-R0 - 4, -R0 - 4, R0 + 4, R0 + 4, (x, y) => {
    const d = Math.hypot(x, y), a = Math.atan2(y, x) - rot, rim = R0 * bump(a);
    if (d > rim + 0.9) return;
    if (d > rim - 0.3) return E[8];
    const v = (x / R0) * 0.75 + Math.sin(a * 3 + d * 0.55) * 0.3;
    return v > 0.5 ? E[1] : v > 0.12 ? E[3] : v > -0.3 ? E[4] : E[6];
  });
  for (let i = 0; i < 3; i++) {
    const a = rot + i * 2.2 + 0.6, d = 3 + i;
    p.px(Math.cos(a) * d, Math.sin(a) * d, E[7]);
  }
  p.glow(0, 0, 14, E[3], 0.12);
}

// 10021 전기 화살: a bolt of lightning with an arrow's point, re-struck every other frame.
function voltArrow(p, t, o) {
  const H = ramp("volt"), S = o.seed, f = frameOf(t) % 12, r = rng(S, 71, f >> 1);
  const path = bolt(r, -36, 0, 2, 0, { jag: 0.2, depth: 4 });
  for (const fork of forks(r, path, { count: 3, len: 12, jag: 0.35, spread: 1 })) p.polyline(fork, H[4]);
  p.polyline(path.map(([x, y]) => [x, y - 1]), H[4]);
  p.polyline(path.map(([x, y]) => [x, y + 1]), H[4]);
  p.polyline(path, f % 2 ? WHITE : H[1]);
  for (let x = 0; x <= 9; x++) {
    const hw = (9 - x) * 0.5;
    for (let y = -Math.ceil(hw); y <= Math.ceil(hw); y++) {
      if (Math.abs(y) > hw + 0.3) continue;
      p.px(x, y, Math.abs(y) > hw - 0.8 ? H[3] : WHITE);
    }
  }
  for (let i = 0; i < 4; i++) {
    const q = rng(S, 91, f, i), x = q.range(-34, 6), y = q.range(-8, 8);
    if (q.chance(0.5)) for (const [a, b] of CROSS) p.px(x + a, y + b, H[3]);
    p.px(x, y, WHITE);
    p.glow(x, y, 4, H[2], 0.3);
  }
  p.glowLine(-34, 0, 4, 0, 9, H[3], 0.3);
  p.glow(4, 0, 14, H[1], 0.4);
}

const shot = (name, size, origin, draw, duration = D) => ({ name, draw, zoom: 3, meta: { name, size, origin, duration, loop: true, fps: 30, options: { seed: 3 } } });

export const effects = [
  shot("FireBullet", [84, 32], [66, 16], fireBullet),
  shot("FireDart", [64, 22], [50, 11], fireDart),
  shot("FireOrb", [150, 64], [122, 32], fireOrb, 1.2),
  shot("FlareShot", [96, 40], [74, 20], flareShot),
  shot("HeatGust", [84, 40], [66, 20], heatGust),
  shot("FlameArrow", [96, 32], [78, 16], flameArrow),
  shot("EmberPellet", [48, 20], [36, 10], emberPellet),
  shot("FireTracer", [84, 20], [70, 10], fireTracer),
  shot("IceShard", [72, 32], [54, 16], iceShard),
  shot("WindArrow", [96, 36], [78, 18], windArrow),
  shot("RockShot", [84, 44], [62, 22], rockShot),
  shot("VoltArrow", [96, 36], [78, 18], voltArrow),
];
