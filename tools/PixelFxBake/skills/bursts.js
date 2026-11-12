// Circle and ring skills for SBSAcademy_Team01. Each is centred on the origin and hits everything within `reach`
// px on the first frame (and again on each later hit of a multi-hit skill, at the times baked in here); the game
// does not turn these, and scales them so reach is the skill's range. Rings on the ground are drawn face-on so the
// art shows exactly what is hit; flames stand upright on them. Built on fire/_lib.js (flame tongues and blades,
// puffs, embers, sparks), adapted from Sprite Fusion's open-source fire
// (https://www.spritefusion.com/pixel-art-vfx/fire); the impact star is melee/_lib.js, after Sprite Fusion's
// impact. MIT License, Copyright (c) 2026 Hugo Duprez.
import { clamp, smooth, ease, rng, noise2, TAU, WHITE, sparkle } from "../../core/index.js";
import { flameBlade, flameTongue, firePuff, ember, spark, solid, LAYERS, BIG_LAYERS, BACK_LAYERS } from "../fire/_lib.js";
import { star } from "../melee/_lib.js";
import { ramp, glowOf, noiseAt, ONCE, flash, shockRing, fireRing, rays, fling } from "./_kit.js";

const EMBER_LAYERS = [[1, 1, 7], [0.7, 0.8, 6], [0.4, 0.55, 5]];
const cooling = (u) => (u < 0.35 ? LAYERS : u < 0.7 ? BACK_LAYERS : EMBER_LAYERS);
const spikes = (n, long, short, w = 3) => Array.from({ length: n }, (_, i) => ({ a: (i * TAU) / n + 0.2, len: i % 2 ? short : long, w }));
const SPIKES8 = spikes(8, 22, 13), SPIKES12 = spikes(12, 30, 17, 3.4);

/** How hard a multi-hit skill is pulsing: 1 on each hit, falling away after it. */
function pulseAt(t, hits, rate) {
  let k = 0;
  for (const h of hits) if (t >= h) k = Math.max(k, Math.exp(-(t - h) * rate));
  return k;
}

/** n flames scattered between rmin and rmax of the centre, nearest last so they overlap properly. */
function scatter(S, n, rmin, rmax, make) {
  const out = [];
  for (let i = 0; i < n; i++) {
    const r = rng(S, 201, i), a = ((i + r.range(0.1, 0.9)) / n) * TAU, d = r.range(rmin, rmax);
    out.push({ x: Math.cos(a) * d, y: Math.sin(a) * d, d, i, ...make(r, d) });
  }
  return out.sort((a, b) => a.y - b.y);
}

/** Upright flames [{ x, y, h, w, born, life, big }]: each pops up, burns and sinks, cooling as it goes. */
function stand(p, t, flames, N, S, H) {
  for (const f of flames) {
    const age = t - f.born;
    if (age < 0 || age > f.life) continue;
    const u = age / f.life, h = f.h * ease.outBack(clamp(age / 0.06), 2) * (1 - smooth(0.45, 1, u));
    flameTongue(p, f.x, f.y, h, f.w * (1 - 0.35 * u), N(3, S + f.i * 5) * 2.5, N, (S * 7 + f.i) * 13, H, f.big && u < 0.4 ? BIG_LAYERS : cooling(u), 0.5, 1 + h * 0.04);
  }
}

/** A flame tongue that holds its width most of the way up before it tapers: a pillar rather than a bonfire. */
function column(p, x, y, h, w, sx, N, n, H, layers) {
  if (h < 1) return;
  for (const [lw, lh, ri] of layers) {
    const L = h * lh, c = H[ri];
    for (let r = 0; r <= L; r++) {
      const u = r / Math.max(1, L);
      const half = w * lw * Math.pow(1 - Math.pow(u, 2.4), 0.8) * Math.min(1, (r + 1.5) / 3.5) + N(9, n, r * 0.3) * (0.6 + w * 0.05);
      if (half < 0.3) continue;
      const cx = x + sx * u * u * lh + N(4, n + 7, r * 0.12) * u * (1.5 + h * 0.03), Y = Math.round(y - r);
      for (let px = Math.round(cx - half), x1 = Math.round(cx + half); px <= x1; px++) p.px(px, Y, c);
    }
  }
}

/** Rubble knocked out of the ground: dark chunks that hop outward and settle. */
function rubble(p, t, S, H, R, t0, n) {
  for (let i = 0; i < n; i++) {
    const r = rng(S, 211, i), a = r.range(0, TAU), sp = R * r.range(0.9, 2), life = r.range(0.28, 0.42), age = t - t0;
    if (age < 0 || age > life) continue;
    const u = age / life, d = R * 0.12 + sp * life * (u - 0.45 * u * u), x = Math.cos(a) * d, y = Math.sin(a) * d - r.range(10, 22) * 4 * u * (1 - u);
    const fade = u < 0.7 ? 1 : 1 - (u - 0.7) / 0.3;
    p.dot(x, y, H[7], fade);
    p.dot(x + 1, y, H[8], fade);
    p.dot(x, y + 1, H[8], fade);
    p.dot(x + 1, y + 1, H[8], fade);
    if (u < 0.4) p.dot(x, y, H[4], 1);
  }
}

// 10003 화염 강타: a blow of flame brought straight down on what is close. A column for three frames, a star
// where it lands, a ring along the ground and flames left standing inside it.
function flameSmash(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H);
  p.glow(0, 0, R * 1.15, G, 0.35 * (1 - smooth(0.1, 0.5, t)));
  fireRing(p, 0, 0, R * ease.outCubic(clamp((t + 0.03) / 0.13)) - 5, H, { thick: 0.9, lick: 2.4, ph: t * 6, seed: S, alpha: 1 - smooth(0.16, 0.42, t), cool: t > 0.26 ? 2 : 0 });
  stand(p, t, scatter(S, 14, R * 0.18, R * 0.74, (r, d) => ({ h: r.range(19, 32), w: r.range(4.6, 6.4), born: (d / R) * 0.08 - 0.01, life: r.range(0.26, 0.4) })), N, S, H);
  if (t < 0.1) {
    const k = 1 - t / 0.1;
    column(p, 0, 3, R * 0.95 * (0.45 + 0.55 * k), 7 * k + 2, 0, N, S * 3, H, BIG_LAYERS);
  }
  if (t < 0.16) star(p, 0, 0, SPIKES8, ease.outBack(clamp((t + 0.02) / 0.05)) * (1 - t / 0.16) + 0.1, 0.8, WHITE, H[1], H[2], H[4]);
  flash(p, 0, 0, 17, 1 - t / 0.12, H);
  fling(p, t, S, H, { n: 28, sp: [R * 1, R * 2.6], life: [0.2, 0.45] });
}

// 20001 화염 망치: a hammer of fire dropped on the ground in front. It lands on the third frame: a star, cracks
// glowing out from under it, two rings shoving everything back, rubble.
const T_HAMMER = 0.066;
function flameHammer(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), hit = t - T_HAMMER;
  p.glow(0, 0, R * 1.15, G, 0.38 * (1 - smooth(0.15, 0.55, t)));
  if (hit >= 0) {
    // cracks: glowing as they open, then cooling to dark lines
    const heat = hit < 0.12 ? 0 : hit < 0.24 ? 1 : hit < 0.34 ? 2 : 3, fade = 1 - smooth(0.34, 0.48, hit), grow = ease.outCubic(clamp((hit + 0.02) / 0.1));
    for (let i = 0; i < 8; i++) {
      const r = rng(S, 71, i), a0 = (i / 8) * TAU + r.range(-0.25, 0.25), steps = Math.ceil((R * r.range(0.6, 0.92)) / 6), pts = [[0, 0]];
      for (let s = 0, x = 0, y = 0; s < steps; s++) {
        const a = a0 + r.range(-0.5, 0.5);
        x += Math.cos(a) * 6;
        y += Math.sin(a) * 6;
        pts.push([x, y]);
      }
      const n = Math.max(1, Math.round(steps * grow));
      for (let s = 1; s <= n; s++) {
        p.line(pts[s - 1][0], pts[s - 1][1] + 1, pts[s][0], pts[s][1] + 1, [H[4], H[6], H[7], H[8]][heat], fade);
        p.line(pts[s - 1][0], pts[s - 1][1], pts[s][0], pts[s][1], [H[1], H[3], H[5], H[7]][heat], fade);
        if (heat < 2) p.glow(pts[s][0], pts[s][1], 6, H[3], 0.2);
      }
    }
    fireRing(p, 0, 0, R * ease.outCubic(clamp((hit + 0.02) / 0.12)) - 5, H, { thick: 1.2, lick: 1.6, ph: t * 6, seed: S, alpha: 1 - smooth(0.14, 0.34, hit), cool: hit > 0.22 ? 2 : 0 });
    if (hit > 0.05) shockRing(p, 0, 0, R * 0.82 * ease.outCubic(clamp((hit - 0.05) / 0.14)), H, 1 - smooth(0.1, 0.24, hit), 2);
    rubble(p, t, S, H, R, T_HAMMER, 9);
    fling(p, t, S, H, { n: 26, sp: [R * 1.1, R * 3], life: [0.2, 0.42], t0: T_HAMMER });
  }
  // the hammer: a white-hot block on a shaft, dropping, then cooling where it landed
  const gone = smooth(0.12, 0.28, hit);
  if (gone < 1) {
    const hy = -46 * (1 - ease.inQuad(clamp(t / T_HAMMER))) - 1, cool = hit < 0.05 ? 0 : hit < 0.11 ? 1 : hit < 0.18 ? 2 : 3, a = 1 - gone;
    const c0 = [WHITE, H[1], H[3], H[5]][cool], c1 = [H[1], H[3], H[5], H[6]][cool], c2 = [H[3], H[5], H[6], H[7]][cool];
    if (hit < 0.05)
      for (const x of [-10, 0, 10]) flameBlade(p, x, hy - 18, 0, -1, (hit < 0 ? 30 : 14) * (0.8 + 0.3 * N(8, S + x)), 4.5, 0, N, S * 5 + x, H, LAYERS, 0.5, 1.2);
    p.beam(0, hy - 16, 17, hy - 54, 5, c2, a);
    p.beam(0, hy - 16, 17, hy - 54, 2.4, c1, a);
    p.rect(-17, hy - 21, 34, 21, c2, a);
    p.rect(-15, hy - 19, 30, 17, c1, a);
    p.rect(-12, hy - 16, 24, 10, c0, a);
    if (cool < 2) p.glow(0, hy - 10, 34, G, 0.5 * a);
  }
  if (hit >= 0) {
    if (hit < 0.18) star(p, 0, 0, SPIKES8, ease.outBack(clamp((hit + 0.02) / 0.05)) * (1 - hit / 0.18) + 0.1, 0.9, WHITE, H[1], H[2], H[4]);
    flash(p, 0, 0, 15, 1 - hit / 0.1, H);
  }
}

// 20003 대폭발 and the burst of 10004 화염구: a white flash, a ring racing to the edge of the reach, a ball of
// flame puffs rolling out behind it and cooling, sparks thrown past it. o.puffs sets how dense the ball is.
function explosion(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), D = o.dur, big = R > 80;
  p.glow(0, 0, R * 1.25, G, 0.45 * Math.pow(1 - clamp(t / D), 1.5));
  fireRing(p, 0, 0, R * ease.outCubic(clamp((t + 0.03) / 0.15)) - 4, H, { thick: big ? 1 : 0.7, lick: 1.8, ph: t * 6, seed: S, alpha: 1 - smooth(0.14, 0.36, t), cool: t > 0.22 ? 2 : 0 });
  rays(p, 0, 0, big ? 12 : 8, R * 0.15, R * 0.6 * ease.outBack(clamp((t + 0.02) / 0.05)) * (1 - smooth(0.04, 0.26, t)), big ? 6 : 4, N, S, H, t < 0.14 ? BIG_LAYERS : BACK_LAYERS);
  const list = [];
  for (let i = 0; i < o.puffs; i++) {
    const r = rng(S, 50, i), a = i * 2.39996 + r.range(-0.4, 0.4), q = Math.sqrt((i + r.next()) / o.puffs), life = r.range(0.45, 0.8) * D, age = t + 0.03 - r.range(0, 0.07) - q * 0.03;
    if (age < 0 || age > life) continue;
    const u = age / life, d = R * 0.74 * q * ease.outCubic(Math.min(1, age / 0.18)) + age * 8;
    list.push([u, Math.cos(a) * d, Math.sin(a) * d - (big ? 10 : 5) * u, r.range(0.12, 0.2) * R * (1.25 - 0.55 * q) * (big ? 0.8 : 1), r.next(), Math.cos(a) * 0.5, Math.sin(a) * 0.5 - 0.5]);
  }
  list.sort((a, b) => b[0] - a[0]);
  for (const [u, x, y, size, f, tx, ty] of list) firePuff(p, x, y, 1 - u, size, f, H, 0.08, tx, ty);
  flash(p, 0, 0, R * 0.4, 1 - t / 0.1, H);
  fling(p, t, S, H, { n: big ? 44 : 22, sp: [R * 1, R * 2.8], life: [0.22, 0.5], r0: R * 0.2 });
}

// 20004 화염 폭풍: a storm of fire turning over the same ground: three arms of flame winding into a white eye
// inside a wall of fire, flaring on each of the four hits.
const STORM_HITS = [0, 0.2, 0.4, 0.6];
function fireStorm(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), EMB = [H[1], H[3], H[5], H[6]];
  const env = Math.min(1, (t + 0.04) / 0.08) * (1 - smooth(0.7, 0.93, t)), spin = t * 7.5, pulse = pulseAt(t, STORM_HITS, 12);
  p.glow(0, 0, R * 1.2, G, (0.28 + 0.2 * pulse) * env);
  fireRing(p, 0, 0, R - 9, H, { thick: 0.9 + 0.5 * pulse, lick: 3.4, ph: t * 9, seed: S, alpha: env, cool: t > 0.8 ? 2 : 0 });
  for (const h of STORM_HITS) {
    const a = t - h;
    if (a >= 0 && a < 0.16) shockRing(p, 0, 0, R * (0.25 + 0.68 * ease.outCubic(a / 0.16)), H, 1 - a / 0.16, 2);
  }
  for (let j = 9; j >= 0; j--)
    for (let arm = 0; arm < 3; arm++) {
      const s = j / 9, rr = R * (0.14 + 0.7 * s), a = (arm * TAU) / 3 + spin - s * 2.4, ca = Math.cos(a), sa = Math.sin(a);
      // it turns clockwise, so each flame trails back along the turn and a little outward
      const dx = 0.85 * sa + 0.35 * ca, dy = -0.85 * ca + 0.35 * sa, l = Math.hypot(dx, dy);
      flameBlade(p, ca * rr, sa * rr, dx / l, dy / l, (10 + 17 * s) * env * (1 + 0.25 * pulse), (3 + 3.2 * s) * (0.6 + 0.4 * env), 0, N, (S * 5 + arm * 11 + j) * 17, H, s < 0.35 ? LAYERS : BIG_LAYERS, 0.6, 1.2);
    }
  for (let i = 0; i < 46; i++) {
    const r = rng(S, 77, i), age = t - r.range(-0.1, 0.72), life = r.range(0.2, 0.4);
    if (age < 0 || age > life) continue;
    const u = age / life, rr = R * r.range(0.2, 1) * (1 + 0.12 * u), a = r.range(0, TAU) + t * 7.5 * r.range(0.8, 1.2), x = Math.cos(a) * rr, y = Math.sin(a) * rr;
    if (i % 3) spark(p, x, y, -Math.sin(a) * 160, Math.cos(a) * 160, 1 - u, [H[1], H[2], H[4], H[5]]);
    else ember(p, x, y, 1 - u, r.chance(0.3) ? 2 : 1.2, EMB, age * 40 + i);
  }
  solid(p, 0, 0, 6 * env + 2 * pulse, H[3]);
  solid(p, 0, 0, 4 * env + 1.5 * pulse, H[1]);
  solid(p, 0, 0, 2.2 * env, WHITE);
  p.glow(0, 0, 30, H[1], 0.4 * env);
}

// 20005 운석검: a meteor in the shape of a sword. It comes down out of the upper right in two frames, stands in
// the ground white-hot while the blast goes out round it, and cools.
const T_SWORD = 0.066;
function meteorSword(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), hit = t - T_SWORD;
  p.glow(0, 0, R * 1.2, G, 0.4 * (1 - smooth(0.15, 0.62, t)));
  if (hit >= 0) {
    fireRing(p, 0, 0, R * ease.outCubic(clamp((hit + 0.02) / 0.14)) - 4, H, { thick: 1, lick: 2.4, ph: t * 6, seed: S, alpha: 1 - smooth(0.14, 0.38, hit), cool: hit > 0.24 ? 2 : 0 });
    rays(p, 0, 0, 12, R * 0.12, R * 0.55 * ease.outBack(clamp((hit + 0.02) / 0.05)) * (1 - smooth(0.05, 0.3, hit)), 5.5, N, S, H, hit < 0.14 ? BIG_LAYERS : BACK_LAYERS);
    stand(p, t, scatter(S, 9, R * 0.25, R * 0.66, (r, d) => ({ h: r.range(14, 24), w: r.range(4, 5.4), born: T_SWORD + (d / R) * 0.08, life: r.range(0.26, 0.42) })), N, S, H);
    rubble(p, t, S, H, R, T_SWORD, 10);
  }
  // the sword: its point at (tx, ty), the blade running back along (bx, by)
  const gone = smooth(0.24, 0.42, hit);
  if (gone < 1) {
    const k = 1 - clamp(t / T_SWORD), tx = 40 * k, ty = 6 - 112 * k, bx = 0.326, by = -0.945, nx = -by, ny = bx, a = 1 - gone;
    const cool = hit < 0.08 ? 0 : hit < 0.16 ? 1 : hit < 0.24 ? 2 : 3;
    const c0 = [WHITE, H[1], H[3], H[5]][cool], c1 = [H[1], H[3], H[5], H[6]][cool], c2 = [H[3], H[5], H[6], H[7]][cool];
    const at = (d, q) => [tx + bx * d + nx * q, ty + by * d + ny * q];
    if (hit < 0.14) {
      const len = hit < 0 ? 78 : 40 * (1 - hit / 0.14);
      [[-4, 0.8], [4, 0.8], [0, 1]].forEach(([q, share], i) => {
        const [x, y] = at(62, q);
        flameBlade(p, x, y, bx, by, len * share * (0.85 + 0.2 * N(8, S + i)), hit < 0 ? 9 : 6, 0, N, S * 5 + i, H, i === 2 ? BIG_LAYERS : LAYERS, 0.7, 1.6);
      });
    }
    p.poly([at(-2, 0), at(12, 6.5), at(63, 6.5), at(63, -6.5), at(12, -6.5)], c2, a);
    p.poly([at(1, 0), at(13, 4.6), at(62, 4.6), at(62, -4.6), at(13, -4.6)], c1, a);
    p.poly([at(6, 0), at(15, 1.8), at(60, 1.8), at(60, -1.8), at(15, -1.8)], c0, a);
    p.poly([at(62, 14), at(67, 14), at(67, -14), at(62, -14)], c2, a);
    p.poly([at(63, 12.5), at(66, 12.5), at(66, -12.5), at(63, -12.5)], c1, a);
    p.poly([at(67, 2.6), at(82, 2.6), at(82, -2.6), at(67, -2.6)], c2, a);
    const [qx, qy] = at(84, 0);
    solid(p, qx, qy, 3.6, c2);
    solid(p, qx, qy, 2.2, c1);
    if (cool < 2) p.glowLine(tx, ty, tx + bx * 62, ty + by * 62, 16, G, 0.5 * a);
  }
  if (hit >= 0) {
    if (hit < 0.2) star(p, 0, 4, SPIKES12, ease.outBack(clamp((hit + 0.02) / 0.05)) * (1 - hit / 0.2) + 0.1, 0.9, WHITE, H[1], H[2], H[4]);
    flash(p, 0, 4, R * 0.3, 1 - hit / 0.11, H);
    fling(p, t, S, H, { n: 40, sp: [R * 1, R * 2.8], life: [0.22, 0.5], t0: T_SWORD, r0: R * 0.15 });
  }
}

// 20007 용암 분출: lava bursting out from underfoot. A pool of it opens with a crust drifting on it, a ring
// shoves everything back, and blobs are thrown up to land round about as puddles that cool.
function lavaEruption(p, t, o) {
  const H = ramp("lava"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H);
  p.glow(0, 0, R * 1.1, G, 0.4 * (1 - smooth(0.25, 0.7, t)));
  fireRing(p, 0, 0, R * ease.outCubic(clamp((t + 0.03) / 0.14)) - 4, H, { thick: 0.9, lick: 1.6, ph: t * 6, seed: S, alpha: 1 - smooth(0.14, 0.36, t), cool: t > 0.22 ? 2 : 0 });
  const pr = R * 0.5 * ease.outCubic(clamp((t + 0.03) / 0.16)), cold = Math.floor(smooth(0.34, 0.66, t) * 3.5), thin = 1 - smooth(0.52, 0.72, t);
  if (thin > 0)
    p.shade(-pr - 8, -pr - 8, pr + 8, pr + 8, (x, y, out) => {
      const d = Math.hypot(x, y), a = Math.atan2(y, x), rim = pr * (1 + 0.12 * Math.sin(a * 5 + 1) + 0.08 * Math.sin(a * 9 + 2.2) + 0.04 * Math.sin(a * 13 + t * 6));
      if (d > rim) return;
      const e = rim - d, crust = noise2(x * 0.16 + 7, y * 0.16 - t * 1.2, S);
      const i = e < 1.5 ? 6 : e < 3.5 ? 4 : crust > 0.35 ? 6 : crust > 0.12 ? 4 : crust > -0.3 ? 2 : 1;
      out.c = H[Math.min(8, i + cold)];
      out.a = thin;
    });
  const drawn = [];
  for (let i = 0; i < 18; i++) {
    const r = rng(S, 91, i), a = r.range(0, TAU), d = R * r.range(0.32, 0.95), age = t - r.range(-0.03, 0.12), tf = r.range(0.22, 0.34), top = r.range(22, 48), size = r.range(2.6, 4.6);
    if (age < 0) continue;
    const gx = Math.cos(a) * d, gy = Math.sin(a) * d;
    if (age < tf) {
      const u = age / tf, x = gx * u, y = gy * u - top * 4 * u * (1 - u);
      drawn.push([gy * u, () => {
        solid(p, x, y, size + 1, H[5]);
        solid(p, x, y, size, H[3]);
        solid(p, x - 0.5, y - 0.5, size * 0.55, H[1]);
        p.glow(x, y, size * 3 + 4, G, 0.4);
      }]);
    } else {
      const land = age - tf, k = land / 0.3;
      if (k > 1) continue;
      const rx = size * 1.9 * (1 + 0.3 * k), cool = Math.floor(k * 3.5);
      drawn.push([gy - 100, () => {
        p.ellipse(gx, gy, rx, rx * 0.62, (dd) => H[Math.min(8, (dd > 0.7 ? 5 : dd > 0.35 ? 3 : 1) + cool)], 1 - smooth(0.6, 1, k));
        if (k < 0.5) p.glow(gx, gy, rx * 2.4, G, 0.3 * (1 - k * 2));
      }]);
      fling(p, t, S + 100 + i, H, { n: 4, x: gx, y: gy, a0: -2.6, a1: -0.5, sp: [40, 120], life: [0.1, 0.22], t0: t - land, spread: 0.01, g: 300 });
    }
  }
  drawn.sort((a, b) => a[0] - b[0]);
  for (const [, fn] of drawn) fn();
  const gk = 1 - smooth(0.06, 0.3, t);
  if (gk > 0.02) {
    flameTongue(p, -6, 2, R * 0.34 * gk, 5.5, -3, N, S * 7 + 1, H, LAYERS, 0.5, 1.4);
    flameTongue(p, 6, 2, R * 0.3 * gk, 5.5, 3, N, S * 7 + 2, H, LAYERS, 0.5, 1.4);
    flameTongue(p, 0, 3, R * 0.62 * gk, 9, N(3, S) * 3, N, S * 7, H, BIG_LAYERS, 0.6, 1.8);
  }
  flash(p, 0, 0, 14, 1 - t / 0.1, H);
  fling(p, t, S, H, { n: 20, sp: [R * 0.8, R * 2.2], life: [0.2, 0.42] });
}

// 20009 불기둥: a pillar of fire standing on the spot inside a ring burnt on the ground, surging on each of the
// three hits.
const PILLAR_HITS = [0, 0.18, 0.36], HOT_CORE = [[1, 1, 1], [0.55, 0.8, 0]];
function flamePillar(p, t, o) {
  const H = ramp("fire"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), EMB = [H[1], H[3], H[5], H[6]];
  const env = Math.min(1, (t + 0.05) / 0.08) * (1 - smooth(0.52, 0.76, t)), pulse = pulseAt(t, PILLAR_HITS, 11);
  p.glow(0, -R * 0.3, R * 1.2, G, (0.3 + 0.2 * pulse) * env);
  fireRing(p, 0, 0, R - 8, H, { thick: 0.8 + 0.5 * pulse, lick: 3, ph: t * 8, seed: S, alpha: env, cool: t > 0.62 ? 2 : 0 });
  for (const h of PILLAR_HITS) {
    const a = t - h;
    if (a >= 0 && a < 0.15) shockRing(p, 0, 0, R * (0.2 + 0.72 * ease.outCubic(a / 0.15)), H, 1 - a / 0.15, 2);
  }
  const top = R * 1.2 * env * (1 + 0.12 * pulse), W = R * 0.25 * (0.7 + 0.3 * env) * (1 + 0.15 * pulse);
  flameTongue(p, -W * 1.05, 0, top * 0.5, W * 0.6, -N(3, S + 1) * 4 - 3, N, S * 7 + 1, H, BACK_LAYERS, 0.7, 2);
  flameTongue(p, W * 1.05, 0, top * 0.56, W * 0.6, N(3, S + 2) * 4 + 3, N, S * 7 + 2, H, BACK_LAYERS, 0.7, 2);
  column(p, 0, 4, top, W, N(2, S + 3) * 5, N, S * 7 + 3, H, BIG_LAYERS);
  column(p, 0, 4, top * 0.74, W * 0.34, N(2, S + 3) * 3, N, S * 7 + 4, H, HOT_CORE);
  flameTongue(p, -W * 0.6, 7, top * 0.26, W * 0.5, -2, N, S * 7 + 5, H, LAYERS, 0.5, 1.4);
  flameTongue(p, W * 0.65, 7, top * 0.22, W * 0.5, 2, N, S * 7 + 6, H, LAYERS, 0.5, 1.4);
  for (let i = 0; i < 44; i++) {
    const r = rng(S, 87, i), age = t - r.range(-0.1, 0.6), life = r.range(0.25, 0.5);
    if (age < 0 || age > life) continue;
    ember(p, r.range(-1.3, 1.3) * W + Math.sin(age * 9 + i) * 3, -r.range(0.1, 0.8) * top - age * r.range(60, 140), 1 - age / life, r.chance(0.3) ? 2 : 1.2, EMB, age * 40 + i);
  }
  p.glow(0, -top * 0.4, W * 3.5, H[1], 0.35 * env + 0.2 * pulse);
}

// 20013 업화 강림: hellfire brought down on a far place. A dark comet lands on the third frame, the ground is
// scorched black, a wall of crimson flame stands round the blast and more fire rains into it.
const T_HELL = 0.066;
function hellfire(p, t, o) {
  const H = ramp("hell"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), hit = t - T_HELL;
  if (hit >= 0) p.circle(0, 0, R * 0.9 * ease.outCubic(clamp((hit + 0.02) / 0.12)), H[8], 0.5 * (1 - smooth(0.4, 0.7, hit)));
  p.glow(0, 0, R * 1.2, G, 0.4 * (1 - smooth(0.2, 0.75, t)));
  if (hit >= 0) {
    fireRing(p, 0, 0, R * ease.outCubic(clamp((hit + 0.02) / 0.16)) - 7, H, { thick: 1.4, lick: 3, ph: t * 6, seed: S, alpha: 1 - smooth(0.2, 0.5, hit), cool: hit > 0.32 ? 2 : 0 });
    rays(p, 0, 0, 14, R * 0.12, R * 0.5 * ease.outBack(clamp((hit + 0.02) / 0.05)) * (1 - smooth(0.05, 0.3, hit)), 7, N, S, H, hit < 0.15 ? BIG_LAYERS : BACK_LAYERS);
    const wall = [
      ...scatter(S, 16, R * 0.68, R * 0.76, (r) => ({ h: r.range(26, 44), w: r.range(5, 7.5), born: T_HELL + 0.03 + r.range(0, 0.05), life: r.range(0.42, 0.62), big: true })),
      ...scatter(S + 1, 8, R * 0.3, R * 0.42, (r) => ({ h: r.range(18, 30), w: r.range(4.5, 6.5), born: T_HELL + r.range(0, 0.04), life: r.range(0.3, 0.46) })).map((f) => ({ ...f, i: f.i + 16 })),
    ].sort((a, b) => a.y - b.y);
    stand(p, t, wall, N, S, H);
  }
  // more fire raining in: each streaks down out of the upper right, flashes and leaves a flame
  for (let i = 0; i < 7; i++) {
    const r = rng(S, 95, i), a = r.range(0, TAU), d = R * r.range(0.2, 0.82), x = Math.cos(a) * d, y = Math.sin(a) * d, age = t - T_HELL - r.range(0.03, 0.32);
    if (age < 0 && age > -0.07) {
      const k = -age / 0.07, sx = x + 16 * k, sy = y - 84 * k;
      flameBlade(p, sx, sy, 0.19, -0.98, 30, 3.6, 0, N, S * 9 + i, H, LAYERS, 0.5, 1);
      solid(p, sx, sy, 3.2, H[1]);
      p.glow(sx, sy, 12, G, 0.5);
    } else if (age >= 0 && age < 0.24) {
      flameTongue(p, x, y, 18 * ease.outBack(clamp(age / 0.05), 2) * (1 - smooth(0.4, 1, age / 0.24)), 4.2, N(3, S + i) * 2, N, S * 11 + i, H, cooling(age / 0.24), 0.5, 1.2);
      flash(p, x, y, 8, 1 - age / 0.08, H);
      fling(p, t, S + 40 + i, H, { n: 5, x, y, sp: [50, 160], life: [0.12, 0.28], t0: t - age, spread: 0.01 });
    }
  }
  if (hit < 0) {
    const k = 1 - t / T_HELL, cx = 34 * k, cy = -(R - 12) * k;
    flameBlade(p, cx, cy, 0.29, -0.957, 96, 15, 0, N, S * 3, H, BIG_LAYERS, 0.8, 2);
    solid(p, cx, cy, 15, H[4]);
    solid(p, cx, cy, 11, H[2]);
    solid(p, cx - 1, cy + 1, 7, WHITE);
    p.glow(cx, cy, 50, G, 0.6);
  } else {
    if (hit < 0.2) star(p, 0, 0, SPIKES12, ease.outBack(clamp((hit + 0.02) / 0.05)) * (1 - hit / 0.2) * 1.2 + 0.1, 1, WHITE, H[1], H[2], H[4]);
    flash(p, 0, 0, R * 0.38, 1 - hit / 0.12, H);
    fling(p, t, S, H, { n: 50, sp: [R * 0.9, R * 2.6], life: [0.25, 0.55], t0: T_HELL, r0: R * 0.2 });
  }
}

// 20015 태양 붕괴: a sun collapses. On the first hit it throws a ring of heat and starts to fall in on itself,
// its corona and streaks of light dragged into a point; on the second it goes off: a flash, a ring, long rays.
const T_NOVA = 0.25;
function sunCollapse(p, t, o) {
  const H = ramp("sun"), R = o.reach, S = o.seed, N = noiseAt(ONCE, t), G = glowOf(H), nova = t - T_NOVA;
  if (nova < 0) {
    const c = ease.inCubic(clamp(t / T_NOVA)), rs = R * 0.36 * (1 - 0.82 * c);
    p.glow(0, 0, R * (1.1 - 0.5 * c), G, 0.42);
    fireRing(p, 0, 0, R * ease.outCubic(clamp((t + 0.03) / 0.13)) - 4, H, { thick: 0.7, lick: 1.6, ph: t * 6, seed: S, alpha: 1 - smooth(0.12, 0.24, t) });
    for (let i = 0; i < 34; i++) {
      const r = rng(S, 97, i), a = r.range(0, TAU), age = t - r.range(0.02, 0.2), life = r.range(0.07, 0.12);
      if (age < 0 || age > life) continue;
      const u = age / life, d0 = R * r.range(0.55, 1), d = d0 + (rs - d0) * ease.inQuad(u), len = 6 + 14 * u, ca = Math.cos(a), sa = Math.sin(a);
      p.line(ca * (d + len), sa * (d + len), ca * d, sa * d, (q) => (q > 0.6 ? H[1] : q > 0.3 ? H[3] : H[5]));
      p.glow(ca * d, sa * d, 4, H[2], 0.3);
    }
    for (let i = 0; i < 12; i++) {
      const a = (i / 12) * TAU + t * 1.5, long = i % 2 === 0, ca = Math.cos(a), sa = Math.sin(a);
      flameBlade(p, ca * rs * 0.9, sa * rs * 0.9, ca, sa, (long ? 24 : 13) * (1 - c) * (1 + 0.1 * N(8, S + i)), (long ? 5 : 3.5) * (1 - 0.5 * c), N(3, S + i * 3) * 2, N, (S * 3 + i) * 7, H, LAYERS, 0.4, 0.8);
    }
    p.shade(-rs - 3, -rs - 3, rs + 3, rs + 3, (x, y) => {
      const d = Math.hypot(x, y), a = Math.atan2(y, x), rim = rs + 0.8 * Math.sin(a * 5 + t * 14) + 0.6 * Math.sin(a * 8 - t * 11 + 1);
      if (d > rim) return;
      const h = 1 - d / rim + 0.2 * noise2(x * 0.2 + t * 6, y * 0.2, S + 2);
      return rim - d < 1.2 ? H[4] : h > 0.62 ? WHITE : h > 0.42 ? H[1] : h > 0.24 ? H[2] : H[3];
    });
    if (c > 0.5) sparkle(p, 0, 0, 12 * ((c - 0.5) / 0.5), WHITE, H[2], 0.7);
    p.glow(0, 0, rs * 2.2, H[1], 0.5);
  } else {
    p.glow(0, 0, R * 1.25, G, 0.55 * Math.pow(1 - clamp(nova / 0.47), 1.5));
    fireRing(p, 0, 0, R * ease.outCubic(clamp((nova + 0.03) / 0.14)) - 7, H, { thick: 1.3, lick: 3, ph: t * 6, seed: S + 1, alpha: 1 - smooth(0.16, 0.4, nova), cool: nova > 0.26 ? 2 : 0 });
    if (nova > 0.04) shockRing(p, 0, 0, R * 0.7 * ease.outCubic(clamp((nova - 0.04) / 0.16)), H, 1 - smooth(0.1, 0.24, nova), 2);
    rays(p, 0, 0, 16, R * 0.1, R * 0.78 * ease.outBack(clamp((nova + 0.02) / 0.05)) * (1 - smooth(0.06, 0.36, nova)), 6, N, S, H, nova < 0.16 ? BIG_LAYERS : BACK_LAYERS);
    if (nova < 0.2) star(p, 0, 0, SPIKES12, ease.outBack(clamp((nova + 0.02) / 0.05)) * (1 - nova / 0.2) * 1.3 + 0.1, 1, WHITE, H[1], H[2], H[4]);
    flash(p, 0, 0, R * 0.45, 1 - nova / 0.11, H);
    const core = 1 - smooth(0.1, 0.4, nova);
    solid(p, 0, 0, 6 * core, H[2]);
    solid(p, 0, 0, 3.5 * core, WHITE);
    fling(p, t, S, H, { n: 50, sp: [R * 1, R * 2.8], life: [0.22, 0.5], t0: T_NOVA, r0: R * 0.15 });
  }
}

// 20008 열파 충격: a wave of heat thrown out as a ring. It hits only the band between three quarters of the
// reach and the reach, so that band is where it burns: it races out there, holds, and breaks up.
function heatWave(p, t, o) {
  const H = ramp("fire"), R = o.reach, band = R * 0.25, S = o.seed, G = glowOf(H);
  const front = R * (0.35 + 0.65 * ease.outCubic(clamp((t + 0.03) / 0.12)));
  p.glowRing(0, 0, front - band / 2, band * 1.6, G, 0.5 * (1 - smooth(0.2, 0.52, t)));
  const N = noiseAt(ONCE, t), burn = 1 - smooth(0.26, 0.5, t), inner = front - band;
  fireRing(p, 0, 0, inner + 4, H, { thick: 1.5, lick: 2.2, ph: t * 5, seed: S, alpha: 1 - smooth(0.24, 0.5, t), cool: t > 0.4 ? 3 : t > 0.3 ? 2 : 0 });
  for (let i = 0; i < 44; i++) {
    const r = rng(S, 98, i), a = ((i + r.range(0.2, 0.8)) / 44) * TAU, ca = Math.cos(a), sa = Math.sin(a), len = band * r.range(0.75, 1.15) * burn * (1 + 0.15 * N(6, S + i));
    flameBlade(p, ca * (inner + 3), sa * (inner + 3), ca, sa, len, r.range(3.4, 5), N(3, S + i * 3) * 3, N, (S * 3 + i) * 7, H, t < 0.3 ? (i % 3 ? LAYERS : BIG_LAYERS) : BACK_LAYERS, 0.6, 1.2);
  }
  for (let i = 0; i < 30; i++) {
    const r = rng(S, 99, i), a = r.range(0, TAU), age = t - r.range(0.02, 0.24), life = r.range(0.1, 0.2);
    if (age < 0 || age > life) continue;
    const u = age / life, d = front + r.range(-4, 6) + 20 * u, ca = Math.cos(a), sa = Math.sin(a);
    p.line(ca * d, sa * d, ca * (d + 5 + 4 * u), sa * (d + 5 + 4 * u), u < 0.5 ? H[1] : H[3], 1 - u);
  }
  shockRing(p, 0, 0, front + 3 + 8 * smooth(0.12, 0.4, t), H, 1 - smooth(0.1, 0.34, t), 1.4);
  fling(p, t, S, H, { n: 40, rmin: R * 0.7, r0: R * 0.95, sp: [30, 140], life: [0.2, 0.45], t0: 0.05, spread: 0.1 });
}

/** Canvas for a circle of `reach` px round the origin, with `up` extra px above it for what stands or falls. */
const burst = (name, reach, duration, draw, { up = 0, pad = 20, options = {} } = {}) => ({
  name, draw, reach, zoom: 1,
  meta: { name, size: [(reach + pad) * 2, (reach + pad) * 2 + up], origin: [reach + pad, reach + pad + up], duration, loop: false, fps: 30, options: { seed: 5, reach, ...options } },
});

export const effects = [
  burst("FlameSmash", 80, 0.5, flameSmash),
  burst("FlameHammer", 70, 0.56, flameHammer, { up: 24 }),
  burst("GreatExplosion", 104, 0.7, explosion, { options: { dur: 0.7, puffs: 90 } }),
  burst("FireballBurst", 58, 0.42, explosion, { pad: 16, options: { dur: 0.42, puffs: 40 } }),
  burst("FireStorm", 104, 0.95, fireStorm),
  burst("MeteorSword", 96, 0.68, meteorSword, { up: 44 }),
  burst("LavaEruption", 96, 0.72, lavaEruption),
  burst("FlamePillar", 112, 0.78, flamePillar, { up: 28 }),
  burst("Hellfire", 120, 0.8, hellfire, { up: 24 }),
  burst("SunCollapse", 104, 0.72, sunCollapse),
  burst("HeatWave", 96, 0.55, heatWave, { pad: 28 }),
];
