// Neutral basic attack: a small white core and a short pale-gold tail.
// Uses the same pixelfx primitives and exporter as the other combat effects.
import { TAU, WHITE, hex } from "../../core/index.js";

const GOLD = hex("#ffe7a3"), PALE = hex("#fff5d5"), RIM = hex("#e6c77a");

function flight(p, t) {
  const pulse = .5 + .5 * Math.sin(t * TAU / .4);
  p.glowLine(-2, 0, -16, 0, 4.5, GOLD, .20);
  for (let x = -16; x <= -3; x++) {
    const strength = (x + 17) / 15;
    p.dot(x, Math.sin(x * .55 + t * TAU / .4) * .6, x < -9 ? RIM : GOLD, strength * .85);
    if (x > -10) p.px(x, 0, PALE, strength * .9);
  }
  p.disc(0, 0, 5.5 + pulse * .3, RIM, 1);
  p.disc(.4, 0, 4.5, GOLD, 1);
  p.disc(.8, 0, 3.3, PALE, 1);
  p.disc(1.1, -.3, 2.5, WHITE, 1);
  p.glow(0, 0, 9, GOLD, .24);
}

function impact(p, t) {
  const u = t / .2, k = Math.max(0, 1 - u);
  if (k <= .01) return;
  p.glow(0, 0, 10, GOLD, .3 * k);
  p.disc(0, 0, 4 * k, PALE, 1);
  p.disc(0, 0, 2.5 * k, WHITE, 1);
  for (let i = 0; i < 5; i++) {
    const a = i * TAU / 5 + .25, distance = 2 + u * 8;
    const x = Math.cos(a) * distance, y = Math.sin(a) * distance;
    p.dot(x, y, u < .45 ? PALE : GOLD, k);
  }
}

export const effects = [
  { name: "BasicLightBolt", draw: flight, zoom: 5,
    meta: { name: "BasicLightBolt", size: [48, 28], origin: [30, 14], duration: .4, loop: true, fps: 30 } },
  { name: "BasicLightHit", draw: impact, zoom: 5,
    meta: { name: "BasicLightHit", size: [32, 32], origin: [16, 16], duration: .2, loop: false, fps: 30 } },
];
