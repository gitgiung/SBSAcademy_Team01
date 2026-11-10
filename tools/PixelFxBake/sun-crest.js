// The sun a caster draws round themself before a barrage, seen face-on and centred on the caster. A white point
// races round a circle and leaves a burning ring, flame spikes snap out of it as the point passes (six long, six
// short), the sun holds and flickers, then the spikes pull in and the ring breaks up into embers.
// The game plays it in three parts: frames 0..IN-1 once, IN..IN+HOLD-1 on repeat (a seamless stretch) while the
// missiles leave the long spikes, then the rest once. Built from fire/_lib.js (flame blades, embers, sparks), which
// is adapted from Sprite Fusion's open-source fire (https://www.spritefusion.com/pixel-art-vfx/fire),
// MIT License, Copyright (c) 2026 Hugo Duprez.
import { fit, mix, clamp, smooth, ease, rng, tileFbm, sparkle, TAU, WHITE } from "../../core/index.js";
import { noiseFor, hotFirst, flameBlade, ember, spark, looped, solid } from "./_lib.js";

const FPS = 30;
/** Frames of the draw-in, of the repeating hold, and of the break-up (the last one empty). */
export const IN = 7, HOLD = 12, OUT = 10;
const T_IN = IN / FPS, T_HOLD = HOLD / FPS, T_OUT = T_IN + T_HOLD, T_END = (IN + HOLD + OUT - 1) / FPS;
const T_DRAW = 0.12; // how long the point takes to go round
const SPIKES = 12; // even ones long, odd ones short; spike 0 points up, the rest follow clockwise

export const meta = {
  name: "Sun crest",
  desc: "A sun drawn round the caster: a white point races round a ring of fire, flame spikes snap out behind it, the sun burns, then breaks into embers.",
  tags: ["fire", "sun", "ring", "aura", "cast", "one-shot"],
  size: [176, 176],
  origin: [88, 88],
  duration: T_END,
  loop: false,
  fps: FPS,
  bg: "dark",
  options: {
    palette: ["#3a1408", "#7a2a0c", "#c2500f", "#f08a1c", "#ffb92e", "#ffdf5e", "#fff3a8", "#fffbe0", "#ffffff"],
    radius: 38,
    long: 30,
    short: 16,
    seed: 7,
  },
};

const NL = noiseFor(T_HOLD);
// spike layers [width share, length share, step of the hot-first ramp]: orange rim, gold, pale, a white heart
const SPIKE = [[1, 1, 6], [0.76, 0.88, 4], [0.5, 0.7, 2], [0.24, 0.46, 0]];
const COOL = [[1, 1, 7], [0.7, 0.85, 5], [0.42, 0.62, 3]];

export function draw(p, t, o) {
  const H = hotFirst(fit(o.palette, 9)), R = o.radius, S = o.seed;
  const tt = t - T_IN, ph = tt / T_HOLD; // the hold repeats every whole number of ph
  const N = (rate, seed = 0, off = 0) => NL(tt, rate, seed, off);
  const k = clamp((t - T_OUT) / (T_END - T_OUT)); // 0 until the break-up, 1 when it's gone
  if (k > 0.985) return;
  const GLOW = mix(mix(H[3], H[4], 0.5), WHITE, 0.2);
  const head = t < T_DRAW ? (TAU * t) / T_DRAW : TAU + 1; // how far round the point has drawn (clockwise from the top)
  const passed = (a) => T_DRAW * (a / TAU); // when the point went through angle a
  const dir = (a) => [Math.sin(a), -Math.cos(a)];
  const vis = Math.pow(1 - k, 1.5);

  // light first, so it sits under everything: a soft pool, the ring's own glow
  const lit = clamp(t / T_DRAW) * vis;
  p.glow(0, 0, R * 2.1, H[4], 0.12 * lit);
  p.glowRing(0, 0, R + 5 * ease.outQuad(k), 8, GLOW, 0.3 * lit);

  // the ring: a white-hot wire with gold either side and an orange rim, its outer edge licking outwards
  const grow = 5 * ease.outQuad(k), thin = 1 - 0.55 * k, fadeR = 1 - smooth(0.2, 0.85, k), cool = Math.floor(k * 4.5);
  const RO = R + grow + 6;
  if (fadeR > 0)
    p.shade(-RO, -RO, RO, RO, (x, y, out) => {
      const d = Math.hypot(x, y), s = d - (R + grow);
      if (s < -4 || s > 6) return;
      let a = Math.atan2(x, -y);
      if (a < 0) a += TAU;
      if (a > head) return;
      const cell = (a / TAU) * 56;
      const lick = 0.5 + 0.5 * tileFbm(cell, ph * 4, 56, 4, S, 2), nick = tileFbm(cell + 9, ph * 4, 56, 4, S + 3, 2);
      const inner = -2.9 * thin + nick * 0.4, outer = (2.7 + lick * 2.6) * thin;
      const dep = Math.min(s - inner, outer - s);
      if (dep < 0) return;
      const fresh = clamp(1 - (t - passed(a)) / 0.1); // just drawn: still white
      let i = dep < 0.9 ? 6 : dep < 1.7 ? 4 : dep < 2.5 ? 2 : 0;
      if (fresh > 0.5) i = Math.max(0, i - 4);
      else if (fresh > 0) i = Math.max(0, i - 2);
      out.c = H[Math.min(7, i + cool + (cool && i === 0 ? 1 : 0))];
      // breaking up: it goes in patches, not all at once
      out.a = fadeR >= 1 ? 1 : fadeR * (1.15 + 0.6 * tileFbm(cell * 0.5, 2.5, 28, 0, S + 8, 2));
    });

  // the spikes: flame blades standing on the ring, popping out as the point passes, breathing while they burn
  const tips = [];
  for (let i = 0; i < SPIKES; i++) {
    const a = (i * TAU) / SPIKES, big = i % 2 === 0, [ux, uy] = dir(a), n = (S * 5 + i) * 19;
    const pop = ease.outBack(clamp((t - passed(a)) / 0.09), 2.4);
    const pull = 1 - ease.inQuad(clamp(k / 0.42));
    const breath = 1 + 0.07 * Math.sin(TAU * ph + i * 2.1) + 0.09 * N(7, n + 3);
    const len = (big ? o.long : o.short) * pop * pull * breath, w = (big ? 8.2 : 5.6) * Math.min(1, pop) * (0.55 + 0.45 * pull);
    const r0 = R + grow + 0.5;
    tips.push({ a, big, ux, uy, x: ux * (r0 + len), y: uy * (r0 + len), len, on: pop * pull });
    if (len < 1.5) continue;
    const flash = t - passed(a) < 0.06; // the frame it pops, it's all white and gold
    flameBlade(p, ux * r0, uy * r0, ux, uy, len, w, N(3, n + 5) * (big ? 1.6 : 1), N, n, H, flash ? [[1, 1, 3], [0.7, 0.9, 1], [0.45, 0.75, 0]] : k > 0.18 ? COOL : SPIKE, 0.4, 0.7);
    if (big) p.glowLine(ux * r0, uy * r0, ux * (r0 + len * 0.8), uy * (r0 + len * 0.8), 6, H[4], 0.2 * vis);
    if (flash) p.glow(ux * (r0 + len * 0.4), uy * (r0 + len * 0.4), big ? 12 : 8, H[1], 0.5);
  }

  // the point that draws it: a white bead with a star on it, sparks thrown off behind
  if (t < T_DRAW + 1e-6) {
    const [ux, uy] = dir(head), hx = ux * R, hy = uy * R;
    solid(p, hx, hy, 3.2, H[2]);
    solid(p, hx, hy, 2.1, WHITE);
    sparkle(p, hx, hy, 6, WHITE, H[2], 0.9);
    for (let j = 0; j < 9; j++) {
      const r = rng(S, 61, j, Math.floor(t * FPS + 1e-6)), back = head - r.range(0.05, 0.5), [bx, by] = dir(back);
      const out = r.range(-0.6, 1), sp = r.range(60, 150);
      spark(p, bx * (R + out * 3), by * (R + out * 3), (bx * out - by * 0.6) * sp, (by * out + bx * 0.6) * sp, r.range(0.4, 1), [WHITE, H[1], H[3], H[5]]);
    }
  } else if (t < T_DRAW + 0.07) {
    // it closes at the top: a glint where the ends meet
    sparkle(p, 0, -R, 7 * (1 - (t - T_DRAW) / 0.07) + 2, WHITE, H[2], 0.9);
  }

  // two glints running round the wire opposite each other, half a turn every repeat (so the repeat has no seam)
  if (t >= T_DRAW + 0.07 && k < 0.15)
    for (let j = 0; j < 2; j++) {
      const [gx, gy] = dir(Math.PI * (ph + j));
      sparkle(p, gx * R, gy * R, 3, WHITE, H[2], 0.5);
    }

  // embers shed from the spike tips while it burns, carried outward and cooling
  const EMB = [H[1], H[3], H[5], H[6]];
  looped(tt, T_HOLD, 32, S + 500, (r, j) => {
    const tip = j % SPIKES;
    return { tip, side: r.range(-1, 1), sp: r.range(22, 52), turn: r.range(-0.5, 0.5), life: r.range(0.22, 0.42), s: r.chance(0.2) ? 2 : 1.2, f: r.range(0, 6.3) };
  }, (q, age) => {
    const T = tips[q.tip];
    if (T.on < 0.85) return;
    const a = T.a + q.turn, d = q.sp * age * (1 - age * 0.7);
    const x = T.x - T.ux * 3 + Math.sin(a) * d - T.uy * q.side * 1.5, y = T.y - T.uy * 3 - Math.cos(a) * d + T.ux * q.side * 1.5;
    const left = (1 - age / q.life) * (1 - clamp(k * 2.5));
    if (left > 0.03) ember(p, x, y, left, q.s, EMB, age * 40 + q.f);
  });

  // the break-up: embers thrown off the ring and the spike roots, flying out and winking off
  if (k > 0) {
    const age = t - T_OUT;
    for (let j = 0; j < 46; j++) {
      const r = rng(S, 701, j), a = r.range(0, TAU), life = r.range(0.12, 0.22), born = r.range(0, 0.05), g = age - born;
      if (g < 0 || g >= life) continue;
      const sp = r.range(35, 110), d = R + r.range(-1, 3) + sp * g * (1 - g * 1.2), [ux, uy] = dir(a);
      if (j % 3) ember(p, ux * d, uy * d, 1 - g / life, r.chance(0.25) ? 2 : 1.2, EMB, g * 40 + j);
      else spark(p, ux * d, uy * d, ux * sp, uy * sp, 1 - g / life, [H[1], H[2], H[4], H[5]]);
    }
  }
}
