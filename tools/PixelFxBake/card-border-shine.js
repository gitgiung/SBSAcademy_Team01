// 카드 테두리를 타고 도는 밝은 빛 알갱이. 꼬리·잔상·점선은 없고, 속도만 불규칙하게 바뀝니다.
// 구운 프레임에 카드 등급 색을 곱해 씁니다.
import { fit, mix, clamp, TAU, WHITE } from "../../core/index.js";

export const meta = {
  name: "Card border shine",
  desc: "A bright white bead rides the card rim at an uneven pace. No trail, no sparks — just the moving highlight.",
  tags: ["ui", "card", "border", "shine", "upgrade", "loop"],
  // 레벨업 카드 275×350과 같은 비율(11:14). Unity Border(width=3) 중심에 맞춰 inset을 둡니다.
  size: [208, 264],
  origin: [104, 132],
  duration: 2.0,
  loop: true,
  fps: 30,
  bg: "dark",
  options: {
    palette: ["#1a2840", "#2e4a78", "#4a78b8", "#7ab0e8", "#b8dcff", "#e0f0ff", "#ffffff"],
    insetX: 2,
    insetY: 2,
    // 카드 테두리 막대(가로 약 9/1000, 세로 약 6/500)를 덮을 두께입니다.
    thickness: 3.2,
    seed: 4,
  },
};

/** Raw time 0..1 → path 0..1. Speed wobbles but the ends match, so the loop has no seam. */
function pathU(u, S) {
  const bump = (n, ph, amp) => (amp * (Math.cos(ph) - Math.cos(n * TAU * u + ph))) / (n * TAU);
  return clamp(u + bump(1, 0.4 + S * 0.02, 0.48) + bump(2, 1.9, 0.32) + bump(3, 0.7, 0.2));
}

/** Point on the inset rectangle, clockwise from the top-left corner. Returns [x, y, tx, ty]. */
function onRim(u, W, H, insetX, insetY) {
  const L = 2 * (W - 2 * insetX + H - 2 * insetY), s = ((u % 1) + 1) % 1 * L;
  const w = W - 2 * insetX, h = H - 2 * insetY;
  if (s < w) return [insetX + s, insetY, 1, 0];
  if (s < w + h) return [W - insetX, insetY + (s - w), 0, 1];
  if (s < 2 * w + h) return [W - insetX - (s - w - h), H - insetY, -1, 0];
  return [insetX, H - insetY - (s - 2 * w - h), 0, -1];
}

export function draw(p, t, o) {
  const C = fit(o.palette, 7), S = o.seed, D = meta.duration;
  const [W, H] = meta.size, ix = o.insetX, iy = o.insetY, th = o.thickness;
  const u0 = pathU(t / D, S);

  // 불규칙 주기는 속도로만 표현합니다. 밝기는 거의 유지해 안 보이거나 점처럼 끊기지 않게 합니다.
  const pulse = 0.82 + 0.18 * Math.sin(TAU * (t / D) * 2 + 0.3);
  const [x, y, tx, ty] = onRim(u0, W, H, ix, iy);

  // 짧은 연속 하이라이트(점선·잔상 아님). 경로의 약 2.5%만 꽉 채워 그립니다.
  const head = 0.025;
  const steps = 14;
  for (let k = 0; k <= steps; k++) {
    const along = (k / steps) * head;
    let u = u0 - along;
    if (u < 0) u += 1;
    const [px, py, ptx, pty] = onRim(u, W, H, ix, iy);
    const fall = 1 - k / steps;
    const hot = fall * fall;
    const c = hot > 0.55 ? WHITE : hot > 0.25 ? C[6] : C[5];
    for (let q = -th; q <= th; q += 0.55) {
      const wx = px + -pty * q, wy = py + ptx * q;
      const edge = 1 - Math.abs(q) / (th + 0.15);
      if (hot > 0.35 && Math.abs(q) < 1.4) p.px(wx - W / 2, wy - H / 2, WHITE);
      else p.dot(wx - W / 2, wy - H / 2, c, clamp(pulse * edge * (0.55 + 0.45 * hot)));
    }
  }

  // 머리 쪽만 살짝 번져 테두리 위에서 눈에 띄게 합니다. 긴 꼬리·스파크는 없습니다.
  p.glow(x - W / 2, y - H / 2, 5, mix(C[5], WHITE, 0.5), 0.55 * pulse);
  p.glow(x - W / 2, y - H / 2, 2.5, WHITE, 0.85 * pulse);
}
