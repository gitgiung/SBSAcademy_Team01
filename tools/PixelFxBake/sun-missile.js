// A dart of white fire shaped like a jet fighter's missile, flying right: a needle nose, swept tail fins and small
// canards made of flame, a white-hot jet with shock diamonds that cools to gold, a thin heat streak rippling out
// behind it and sparks shed off the jet. No smoke. The game turns it to face where it is heading.
// The jet (a banded cone with shock diamonds) and the spark streaks follow projectiles/rocket.js, whose recipes are
// adapted from Sprite Fusion's open-source explosion (https://www.spritefusion.com/pixel-art-vfx/explosion),
// MIT License, Copyright (c) 2026 Hugo Duprez.
import { rand, fit, TAU, WHITE } from "../../core/index.js";
import { beat, wake, spark, pixelShape } from "./_lib.js";

export const meta = {
  name: "Sun missile",
  desc: "A dart of white fire shaped like a jet's missile: a needle nose, swept flame fins, a white-hot jet with shock diamonds cooling to gold, a rippling heat streak and sparks behind.",
  tags: ["projectile", "missile", "fire", "sun", "homing", "directional", "loop"],
  size: [80, 28],
  origin: [64, 14],
  duration: 0.4,
  loop: true,
  fps: 30,
  bg: "dark",
  options: {
    palette: ["#a8480c", "#e07a18", "#ffab2a", "#ffd24e", "#ffe98a", "#fff6c0", "#fffbe6", "#ffffff", "#ffffff"],
    speed: 200,
    seed: 2,
  },
};

const TAIL = -7; // where the jet leaves the body
const J0 = 17; // the jet's mean length
// nose right, its middle on the origin: lit along the top, white down the middle, gold underneath, swept tail fins
// and small canards, with a deep orange line round it so the white reads on any floor
const DART = pixelShape(-9, -5, 18, 11, (set) => {
  for (let x = -5; x <= 4; x++) set(x, -1, "c"), set(x, 0, "w"), set(x, 1, "d");
  set(5, 0, "w"), set(6, 0, "c");
  for (let y = -1; y <= 1; y++) set(-6, y, "a");
  for (const s of [-1, 1]) {
    set(-4, 2 * s, "b"), set(-5, 2 * s, "b"), set(-6, 2 * s, "a"), set(-7, 3 * s, "a"), set(-8, 3 * s, "a");
    set(1, 2 * s, "b");
  }
});

// the jet: a cone of flame in clean bands, white-hot at the tail with a chain of shock diamonds down its middle
function jet(p, F, x0, J, wob, fr) {
  const dia = [0, 1, 2].map((i) => 3.6 + i * 3.8 + (rand(fr, i, 41) - 0.5) * 0.6);
  p.shade(x0 - J - 2, -6, x0, 6, (x, y) => {
    const u = x0 - x;
    if (u < 0) return;
    const q = u / J;
    if (q > 1.05) return;
    const ady = Math.abs(y), qq = Math.min(q, 1), w = (1.9 + 2.3 * Math.pow(qq, 0.6)) * (1 - Math.pow(qq, 1.8)) * wob;
    const m = w - ady + (rand(Math.floor(x) * 7 + Math.floor(y) * 131, fr) - 0.5) * 0.8 * Math.min(1, 0.25 + q * 2);
    if (m < -0.5) return;
    if (m < 0.5) return F[q < 0.5 ? 1 : 0];
    for (let i = 0; i < dia.length; i++) {
      if (dia[i] > J * 0.75) break;
      const d = Math.abs(u - dia[i]) / (1.5 - i * 0.15) + ady / (1.3 - i * 0.2);
      if (d < 1) return WHITE;
    }
    const lvl = (1 - ady / Math.max(0.6, w)) * (1 - Math.pow(qq, 1.5));
    return lvl > 0.55 ? F[6] : lvl > 0.3 ? F[4] : lvl > 0.14 ? F[3] : F[2];
  });
}

export function draw(p, t, o) {
  const F = fit(o.palette, 9), S = o.seed, D = meta.duration, V = o.speed, w = (TAU * t) / D;
  const fr = Math.floor(t * 30 + 1e-6), left = -meta.origin[0] + 2;
  const J = J0 * (0.82 + 0.36 * rand(fr, S, 1)), wob = 0.9 + 0.2 * rand(fr, S, 2), fl = J / J0;
  const SP = [F[6], F[4], F[2], F[1]];

  // the heat streak left in the air: a thin line rippling back and cooling in steps, dithering out
  const T0 = TAIL - J0 + 2, LEN = 34;
  for (let x = T0; x > T0 - LEN; x -= 1) {
    const u = (T0 - x) / LEN, y = Math.sin(x * 0.28 + w * 2) * (0.25 + u * 1.5);
    p.dot(x, y, u < 0.22 ? F[6] : u < 0.5 ? F[4] : F[2], u < 0.4 ? 1 : (1 - u) / 0.6);
    if (u < 0.3) p.dot(x, y + (Math.sin(x * 0.28 + w * 2) > 0 ? -1 : 1), u < 0.14 ? F[5] : F[3], 0.8 - u * 2);
  }
  p.glowLine(T0, 0, T0 - LEN * 0.7, 0, 4, F[2], 0.16);

  // sparks shed off the jet, streaking back and fanning out
  beat(t, D, 8, S + 2, (r) => r.range(0.1, 0.22), (age, life, r) => {
    const [x, y, vx, vy] = wake(TAIL - r.range(2, J0 * 0.8), r.range(-1, 1), -r.range(20, 110), r.range(-50, 50), age, V, 2.6, 0);
    if (x > left + 6) spark(p, x, y, vx, vy, 1 - age / life, SP, 0.3, 0.012, 4);
  });

  // light under the body and the flame, so they sit on it
  p.glow(TAIL - 5, 0, 16, F[2], 0.22 * fl);
  p.glowLine(TAIL, 0, TAIL - J * 0.9, 0, 6, F[3], 0.2);
  p.glow(0, 0, 9, F[5], 0.12);

  jet(p, F, TAIL, J, wob, fr + S * 31);

  // the dart: its fins are flame, so their tips gutter from frame to frame
  p.sprite(DART.rows, { o: F[0], w: WHITE, c: F[6], d: F[4], b: F[3], a: F[2] }, DART.x0, DART.y0);
  const lit = rand(fr, S, 5) < 0.5;
  p.px(-8, -3, lit ? F[4] : F[1]), p.px(-8, 3, lit ? F[1] : F[3]);

  // the hot points: the tail where the jet starts, the nose
  p.glow(TAIL, 0, 5, F[6], 0.35);
  p.glow(6, 0, 4, F[6], 0.2);
}
