// Piercing water lines for SBSAcademy_Team01. Each runs right from its root (the caster) to `reach` px.
// Circles, rings, fans, sweeps and flying shots are in water-shapes.js.
// The game turns it to the aim and scales it so reach is the skill's range. The hit lands on the first
// frame, so the stream is already partway out. Droplets use pixelfx's water bead
// (src/fx/water/_lib.js), adapted from Sprite Fusion's open-source blood, MIT License,
// Copyright (c) 2026 Hugo Duprez. Repeat pulses follow the skill table's hit count and delay.
import { clamp, smooth, ease, rng, WHITE } from "../../core/index.js";
import { fit } from "../../core/color.js";
import { WATER, bead } from "../water/_lib.js";

const REACH = 240;
const FLY = 0.1;
const LEAD = 0.04;
const HOLD = 0.28;

const PALS = {
  water: fit(WATER, 8),
  deep: fit(["#040810", "#081220", "#0c243c", "#123456", "#1a527c", "#3a86b8", "#8ec8ee", "#f2fbff"], 8),
  abyss: fit(["#02060c", "#061018", "#0a1c30", "#0e2c48", "#124068", "#226898", "#62a8d8", "#e4f4ff"], 8),
  foam: fit(["#0c2848", "#145888", "#1c78b4", "#38a4e4", "#78d0ff", "#c8eeff", "#ffffff", "#ffffff"], 8),
};

function laneY(spread, index, count) {
  if (spread <= 0 || count <= 1) return 0;
  return ((index / (count - 1)) * 2 - 1) * spread;
}

function pierce(p, t, o) {
  const C = PALS[o.pal] || PALS.water;
  const L = o.reach;
  const S = o.seed || 1;
  const hits = o.hits || 1;
  const gap = o.gap || 0;
  const body = o.body || 2.4;
  const lanes = o.spread > 0 ? 2 : 1;
  for (let h = 0; h < hits; h++) {
    const age = t - h * gap + LEAD;
    if (age <= 0) continue;
    const fade = age <= FLY ? 1 : 1 - (age - FLY) / HOLD;
    if (fade <= 0) continue;
    const tip = L * ease.outQuad(clamp(age / FLY));
    for (let lane = 0; lane < lanes; lane++) {
      const y0 = laneY(o.spread || 0, lane, lanes);
      const amp = (o.amp || 0) * (0.65 + 0.35 * Math.sin(h + lane));
      drawStream(p, C, S + h * 17 + lane * 3, y0, tip, L, body, amp, fade, o.head, age);
      spray(p, C, t - h * gap, S, h, lane, y0, tip, L, body, fade);
      const landed = age - FLY;
      if (landed >= 0 && landed < 0.18) splash(p, C, S, h, lane, L, y0, body, 1 - landed / 0.18);
    }
  }
}

function drawStream(p, C, S, y0, tip, L, body, amp, fade, head, age) {
  const yAt = (x) => y0 + (amp > 0.2 ? Math.sin(x * 0.085 + S) * amp * Math.sin(Math.PI * clamp(x / L)) : 0);
  const step = amp > 0.2 ? 10 : Math.max(8, tip);
  const w = body * 2 * (fade > 0.45 ? 1 : 0.55);
  if (fade > 0.2) {
    for (let x = 0; x < tip - 0.5; x += step) {
      const x1 = Math.min(tip, x + step);
      p.beam(x, yAt(x), x1, yAt(x1), w + 2, C[1]);
      p.beam(x, yAt(x), x1, yAt(x1), w, (u, v) => (v < 0.22 ? WHITE : v < 0.5 ? C[6] : v < 0.78 ? C[4] : C[2]));
    }
    p.line(0, y0, tip, y0, WHITE);
  } else {
    for (let x = 0; x < tip; x += 2) if (rng(S, 4, x).next() > 0.35) p.px(x, Math.round(yAt(x)), C[6]);
  }
  if (head === "column") {
    for (let x = 10; x < tip - 6; x += 14) {
      p.circle(x, yAt(x), body * 0.85, (d) => (d < 0.35 ? WHITE : C[5]), fade);
      p.px(x, yAt(x) - 1, WHITE);
    }
  }
  if (head === "slash") {
    const open = ease.outCubic(clamp(age / 0.08)) * (1 - smooth(0.16, 0.34, age));
    const wide = 1.1 + body * 1.35 * open;
    p.shade(-2, y0 - wide - 3, tip + 4, y0 + wide + 3, (x, y, out) => {
      if (x < 0 || x > tip) return;
      const hw = Math.max(0.7, wide * Math.sin(Math.PI * clamp(x / L)));
      const ay = Math.abs(y - y0);
      if (ay > hw) return;
      const q = ay / hw;
      out.a = fade;
      return q < 0.28 ? WHITE : q < 0.62 ? C[6] : C[3];
    });
  }
  const hy = yAt(tip);
  if (head === "arrow" || head === "breakthrough") {
    const s = head === "breakthrough" ? 1.45 : 1;
    p.poly([[tip + 4 * s, hy], [tip - 12 * s, hy - body * 1.5 * s], [tip - 5 * s, hy], [tip - 12 * s, hy + body * 1.5 * s]], C[5]);
    p.poly([[tip + 2 * s, hy], [tip - 8 * s, hy - body * 0.75 * s], [tip - 3 * s, hy], [tip - 8 * s, hy + body * 0.75 * s]], WHITE);
  } else if (head === "spear") {
    p.poly([[tip + 6, hy], [tip - 22, hy - body * 0.9], [tip - 8, hy], [tip - 22, hy + body * 0.9]], C[5]);
    p.poly([[tip + 3, hy], [tip - 16, hy - body * 0.4], [tip - 6, hy], [tip - 16, hy + body * 0.4]], WHITE);
  } else if (head === "bulb") {
    p.circle(tip - 1, hy, body * 1.55, (d) => (d < 0.3 ? WHITE : d < 0.65 ? C[6] : C[3]), fade);
  } else if (head === "needle") {
    p.line(tip - 16, hy, tip + 8, hy, WHITE, fade);
    for (let i = 0; i < 5; i++) {
      const r = rng(S, 9, i);
      p.px(tip + r.range(-2, 8), hy + r.range(-3, 3), r.chance(0.5) ? WHITE : C[6]);
    }
  } else if (head !== "slash" && head !== "column") {
    p.poly([[tip + 2, hy], [tip - 7, hy - body * 0.8], [tip - 3, hy], [tip - 7, hy + body * 0.8]], WHITE);
  }
  if (head === "ripple" && tip > 12) bead(p, 6, y0 + body + 2, -30, 12, 3, C, fade);
  p.glowLine(0, y0, tip, y0, 8 + body, C[5], 0.28 * fade);
}

function spray(p, C, t, S, h, lane, y0, tip, L, body, fade) {
  const n = 8;
  for (let i = 0; i < n; i++) {
    const r = rng(S, 40 + h * 5 + lane, i);
    const x = r.range(0.08, 0.92) * tip;
    const passed = FLY * (1 - Math.sqrt(1 - clamp(x / Math.max(1, L)))) - LEAD;
    const local = t - passed;
    const life = r.range(0.12, 0.24);
    if (local < 0 || local > life) continue;
    const side = r.sign();
    const y = y0 + side * (body + 1 + local * r.range(16, 36));
    bead(p, x - 10 * local, y, r.range(-20, 40), side * r.range(20, 70), r.chance(0.25) ? 2 : 1, C, fade * (1 - local / life));
  }
}

function splash(p, C, S, h, lane, x, y, body, k) {
  p.circle(x, y, (2.5 + body) * (1.4 - 0.4 * k), (d) => (d < 0.35 ? WHITE : C[5]), k);
  p.ring(x, y, 4 + (1 - k) * (8 + body), 1.4, (v) => (v < 0.5 ? WHITE : C[4]), k);
  for (let i = 0; i < 6; i++) {
    const r = rng(S, 80 + h + lane, i);
    const a = r.range(-0.9, 0.9);
    bead(p, x + Math.cos(a) * (4 + (1 - k) * 10), y + Math.sin(a) * (3 + (1 - k) * 6), Math.cos(a) * 80, Math.sin(a) * 40, 1, C, k);
  }
}

const line = (name, height, hits, gap, opt) => ({
  name,
  draw: pierce,
  reach: REACH,
  zoom: 1,
  meta: {
    name,
    size: [REACH + 44, height],
    origin: [16, height >> 1],
    duration: +((hits - 1) * gap + FLY + HOLD).toFixed(3),
    loop: false,
    fps: 30,
    options: { seed: 6, reach: REACH, hits, gap, ...opt },
  },
});

export const effects = [
  // 관통 직선만 여기 둔다. 탄환·원·고리·부채·호는 water-shapes.js가 같은 파일 이름으로 다시 굽는다.
  line("WaterJet", 72, 1, 0, { body: 2.3, head: "jet", pal: "water", amp: 1.4 }),
  line("TripleWaterLance", 80, 3, 0.15, { body: 2.2, head: "spear", pal: "water" }),
  line("WaterSpear", 80, 1, 0, { body: 2.8, head: "spear", pal: "water" }),
  line("LanceCurrent", 72, 1, 0, { body: 2.2, head: "spear", pal: "foam", amp: 0.8 }),
  line("SpearOfTheSea", 88, 1, 0, { body: 3.4, head: "spear", pal: "foam" }),
  line("WaveThrust", 80, 1, 0, { body: 3.8, head: "bulb", pal: "water", amp: 2.4 }),
  line("OceanThrust", 92, 1, 0, { body: 4.4, head: "spear", pal: "deep", amp: 1.8 }),
  line("AbyssPierce", 92, 1, 0, { body: 4.2, head: "spear", pal: "abyss" }),
  line("HeavenstreamPierce", 80, 1, 0, { body: 1.6, head: "needle", pal: "foam" }),
  line("FinalCurrent", 96, 1, 0, { body: 4.4, head: "jet", pal: "foam", amp: 1.6 }),
];
