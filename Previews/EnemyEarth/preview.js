"use strict";

const $ = id => document.getElementById(id);
const canvas = $("stage");
const ctx = canvas.getContext("2d");
const W = canvas.width;
const H = canvas.height;
const FLOOR = 419;
const TAU = Math.PI * 2;
const assets = "../../Assets/Project/Sprite";
const names = ["Slime Bùn", "Lính Đất", "Pháp Sư Phong Ấn", "Bọ Cạp Đá", "Hạt Nham Thạch", "Phù Thủy Khóa Ngọc", "Boss Đất Nung"];
const ids = ["SlimeBun", "LinhDat", "PhapSuPhongAn", "BoCapDa", "HatNhamThach", "PhuThuyKhoaNgoc", "BossDatNung"];
const heights = [132, 164, 160, 144, 162, 165, 190];
const attacks = [
  ["Đập bùn quánh", .29, .53, 1.13, "Bùn nặng quật từ cao xuống, kéo sợi ở đầu vung rồi bắn thành giọt và vũng thấp sát đất."],
  ["Bọc Bùn Giảm Sát Thương", .23, .89, 2.48, "Bùn dâng từ chân, trùm lên thân. Cú đánh thử làm màng lõm xuống, nổi bọt và bật sáng. Trong game, giảm 35% sát thương trong 2 round."],
  ["Chém giáo đất nung", .25, .49, 1.12, "Vệt gốm nung bám theo mũi giáo, quét thành cung rồi vỡ thành mảnh sắc và bụi xô ngang."],
  ["Lá Chắn Đất Đá", .24, 1.08, 2.55, "Các phiến đá dựng từ chân, gài vào mặt trước của Lính Đất. Bản xem mô phỏng dựng, chịu đòn và vỡ khiên. Trong game, khiên bằng 25% HP và tối đa 2 round."],
  ["Bắn vụn đá ma thuật", .23, .58, 1.16, "Ba nhịp đá dăm tách nhau khi bay, dồn vào một điểm va chạm rồi văng bụi sáng và đá vụn."],
  ["Phong Ấn Ngọc Đá", .30, .73, 2.34, "Ấn vàng tự vẽ trên bàn cờ. Xích rút về đúng 2 ô, sau đó ổ khóa sáng lên. Đạn đá và va chạm có nhịp riêng. Khóa 1 round."],
  ["Chích đuôi đá nhọn", .31, .49, 1.04, "Ngọn châm dồn lực ở cao, xé xéo xuống sát chân Player. Đầu châm bật bụi và sạn theo quán tính."],
  ["Giáp Gai Độc Thạch", .27, .97, 2.33, "Gai trồi quanh Bọ Cạp. Cú đánh thử kích một nhịp giật lùi, gai phản đòn xuyên sang Player và lóe ở điểm phản 20%."],
  ["Lăn khối đá nóng đỏ", .27, .62, 1.33, "Đá lăn sát sàn, phần lõi nặng xoay nhanh và để lại khói, than hồng. Khi chạm, đá đỏ và bụi lan ra từ mặt đất."],
  ["Lá Chắn Dung Nham Bùng Nổ", .25, 1.31, 2.68, "Khiên 20% HP dựng trước. Cú đánh đầu làm màng rung và lóe, cú kế làm khiên vỡ, đẩy đá đỏ cùng sóng nhiệt về phía Player. Khiên tối đa 3 round trong game."],
  ["Bắn quả cầu địa chấn", .22, .63, 1.28, "Quả cầu quay theo hai lớp bụi xoắn. Va chạm dồn lực ở giữa rồi tỏa cát, đá vụn và khe nứt sát sàn."],
  ["Đại Phong Ấn Trận Địa", .29, .72, 2.48, "Đại ấn khóa đúng 4 ô bằng xích sáng. Cùng lúc, dấu kiếm hiện trên Player để báo giảm 20% ATK. Hai trạng thái kéo dài 2 round trong game."],
  ["Nện cột đá thần thánh", .30, .58, 1.33, "Cột đá rơi thẳng từ cao, găm vào đất rồi nứt. Bụi phình thấp, mảnh nặng văng lên và rơi xuống."],
  ["Đại Địa Chấn Hủy Diệt", .37, .91, 2.77, "Khiên đầu round lóe trên Boss. Sóng đá chạy sát đất đến Player, xích ghim 5 ô trong 2 round và Choáng Player 1 round. Khiên đầu round bằng 10% HP tối đa."]
].map(([name, release, hit, end, description]) => ({ name, release, hit, end, description }));

const common = [
  [31,39,260,243], [318,72,214,191], [557,59,302,225], [878,55,257,236], [1156,68,217,218],
  [26,299,276,237], [315,316,233,213], [550,311,307,225], [863,324,275,209], [1144,289,248,253],
  [31,544,264,237], [315,553,229,232], [568,542,272,257], [838,548,295,235], [1148,546,249,255],
  [29,816,280,231], [313,808,237,233], [554,815,303,229], [861,821,282,223], [1152,803,241,254]
];
const bosses = [
  [12,20,305,279], [339,19,294,274], [645,51,328,242], [981,37,291,255], [1285,21,322,282],
  [14,326,307,272], [337,338,321,254], [643,310,339,290], [982,337,304,249], [1320,328,280,249],
  [11,605,307,355], [324,654,317,308], [642,663,340,298], [982,595,319,376], [1327,696,258,264]
];

const art = { poses: [], gems: [] };
const state = { enemy: 0, mode: 0, time: 0, speed: .5, running: false, ready: false, previous: 0, frameId: 0 };
const clamp = value => Math.max(0, Math.min(1, value));
const mix = (a, b, t) => a + (b - a) * t;
const between = (a, b, t) => clamp((t - a) / (b - a));
const smooth = (a, b, t) => { const x = between(a, b, t); return x * x * (3 - 2 * x); };
const ease = t => 1 - (1 - t) ** 3;
const pulse = (t, a, b, c, d) => smooth(a, b, t) * (1 - smooth(c, d, t));
const seed = i => { const n = Math.sin(i * 127.1 + 37.6) * 43758.5453; return n - Math.floor(n); };
const mod = value => value - Math.floor(value);
const pos = (x, y) => ({ x, y });
const shift = (p, x, y) => pos(p.x + x, p.y + y);
const lerp = (a, b, t) => pos(mix(a.x, b.x, t), mix(a.y, b.y, t));
const angle = (a, b) => Math.atan2(b.y - a.y, b.x - a.x);

function image(path) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(path));
    img.src = path;
  });
}

const posePath = (index, mode, frame) => `${assets}/Enemies/Animations/Earth/${ids[index]}/Atack_${mode + 1}_${frame}.png`;

async function loadArt() {
  [art.player, art.common, art.bosses, art.poses, art.gems, art.rest, art.hit] = await Promise.all([
    image(`${assets}/Player.png`), image("EarthCommon.png"), image("EarthBosses.png"),
    Promise.all(ids.map((_, i) => Promise.all([0, 1].map(mode =>
      Promise.all([0, 1, 2].map(frame => image(posePath(i, mode, frame)))))))),
    Promise.all(["Water", "Ice", "Earth", "Fire", "Wood", "Wind"].map(gem => image(`${assets}/${gem}.png`))),
    Promise.all(ids.map(id => image(`${assets}/Enemies/Earth/Enemy_${id}.png`))),
    Promise.all(ids.map(id => image(`${assets}/Enemies/Animations/Earth/${id}/Hit_0.png`)))
  ]);
}

function sprite(id, point, width, options = {}) {
  const alpha = options.alpha ?? 1;
  if (alpha <= .004 || width < 1) return;
  const sheet = id < 20 ? art.common : art.bosses;
  const [sx, sy, sw, sh] = (id < 20 ? common : bosses)[id < 20 ? id : id - 20];
  const height = options.height ?? width * sh / sw;
  if (height < 1) return;
  ctx.save();
  ctx.translate(point.x, point.y);
  ctx.rotate(options.rotation ?? 0);
  if (options.flip) ctx.scale(-1, 1);
  ctx.globalAlpha *= clamp(alpha);
  const reveal = options.reveal ?? 1;
  if (reveal < .995) {
    ctx.beginPath();
    if (options.sweep) {
      const start = options.start ?? -Math.PI * .54;
      ctx.moveTo(0, 0);
      ctx.arc(0, 0, Math.hypot(width, height), start, start + TAU * clamp(reveal));
      ctx.closePath();
    } else if (options.rise) {
      ctx.rect(-width / 2, height / 2 - height * clamp(reveal), width, height * clamp(reveal));
    } else {
      ctx.rect(-width / 2, -height / 2, width * clamp(reveal), height);
    }
    ctx.clip();
  }
  const warp = options.warp ?? 0;
  if (warp > .1) {
    const step = height / 13;
    for (let row = 0; row < 13; row++) {
      const bent = Math.sin(row * .69 - (options.time ?? 0) * 17) * warp * Math.sin(row * Math.PI / 13);
      ctx.drawImage(sheet, sx, sy + row * sh / 13, sw, sh / 13, -width / 2 + bent, -height / 2 + row * step, width, step + .3);
    }
  } else {
    ctx.drawImage(sheet, sx, sy, sw, sh, -width / 2, -height / 2, width, height);
  }
  ctx.restore();
}

const tones = {
  rock: ["#796659", "#302626", "#d5be96"], clay: ["#b56136", "#4d2d23", "#f7bc78"],
  dust: ["#a88460", "#755744", "#ffe0a4"], mud: ["#725023", "#271c17", "#d0a452"],
  lava: ["#b73813", "#321b1a", "#ffaf50"], gold: ["#be751f", "#543218", "#ffda7c"]
};

function glow(point, radius, color, alpha) {
  if (alpha <= .003 || radius < 1) return;
  ctx.save();
  ctx.globalAlpha *= clamp(alpha);
  const light = ctx.createRadialGradient(point.x, point.y, 0, point.x, point.y, radius);
  light.addColorStop(0, color);
  light.addColorStop(1, "transparent");
  ctx.fillStyle = light;
  ctx.fillRect(point.x - radius, point.y - radius, radius * 2, radius * 2);
  ctx.restore();
}

function chip(point, size, rotation, opacity, tone, key = 0) {
  if (opacity <= .004 || size < .6) return;
  const [base, edge, light] = tones[tone];
  ctx.save();
  ctx.translate(point.x, point.y);
  ctx.rotate(rotation);
  ctx.globalAlpha *= clamp(opacity);
  ctx.beginPath();
  for (let k = 0; k < 5; k++) {
    const a = k * TAU / 5;
    const v = size * (.67 + seed(key * 5 + k) * .44);
    const x = Math.cos(a) * v;
    const y = Math.sin(a) * v * (tone === "mud" ? .66 : .76);
    if (k === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
  }
  ctx.closePath();
  ctx.fillStyle = base;
  ctx.fill();
  ctx.strokeStyle = edge;
  ctx.lineWidth = Math.max(.65, size * .14);
  ctx.stroke();
  ctx.beginPath();
  ctx.moveTo(-size * .25, -size * .47);
  ctx.lineTo(size * .34, -size * .32);
  ctx.strokeStyle = light;
  ctx.lineWidth = Math.max(.65, size * .1);
  ctx.stroke();
  ctx.restore();
}

function debris(point, age, tone, count, speed, options = {}) {
  const life = options.life ?? .60;
  if (age < 0 || age >= life) return;
  for (let i = 0; i < count; i++) {
    const f = seed(i + 42);
    const t = age / (life * (.55 + f * .45));
    if (t >= 1) continue;
    const direction = (options.heading ?? -Math.PI / 2) + (seed(i + 23) - .5) * (options.spread ?? 2.8);
    const vx = Math.cos(direction) * speed * (.21 + f * .79);
    const vy = Math.sin(direction) * speed * (.21 + f * .79);
    const py = Math.min(FLOOR + 6, point.y + vy * age + (options.gravity ?? 520) * age * age / 2);
    chip(pos(point.x + vx * age, py), (options.size ?? 11) * (.29 + f * .71) * (1 - t * .46),
      direction + t * (seed(i + 87) - .5) * 8, Math.sin(Math.PI * t) ** .4, tone, i);
  }
}

function dust(point, age, amount = 1, hot = false, drift = 0) {
  if (age < 0 || age > .98) return;
  for (let i = 0; i < 24; i++) {
    const f = seed(i + 190);
    const life = .43 + f * .55;
    const u = (age - i * .006) / life;
    if (u <= 0 || u >= 1) continue;
    const grow = ease(u);
    const x = (seed(47 + i) - .5) * (48 + grow * 174) * amount + drift * grow;
    const y = -(3 + 52 * grow * f) * amount;
    glow(shift(point, x, y), (8 + 28 * grow) * amount * (.6 + f * .4),
      hot ? (i % 3 === 0 ? "#c87b4e" : "#62504c") : "#be9e78", Math.sin(Math.PI * u) * .27);
  }
}

function ring(point, age, length, width, tone, height = .22) {
  if (age < 0 || age > length) return;
  const p = age / length;
  ctx.save();
  ctx.globalAlpha = Math.sin(Math.PI * p ** .46) * .78;
  ctx.strokeStyle = tone;
  ctx.shadowColor = tone;
  ctx.shadowBlur = 13;
  ctx.lineWidth = mix(5, 1, p);
  ctx.beginPath();
  const radius = width * mix(.15, 1, ease(p));
  ctx.ellipse(point.x, point.y, radius, radius * height, 0, 0, TAU);
  ctx.stroke();
  ctx.restore();
}

function cracks(point, age, radius, bright = false) {
  if (age < 0 || age > .95) return;
  const travel = ease(between(0, .27, age));
  ctx.save();
  ctx.globalAlpha = pulse(age, 0, .055, .27, .95);
  for (let i = 0; i < 9; i++) {
    const a = i * TAU / 9 + seed(i) * .45;
    let previous = point;
    for (let j = 1; j <= 6; j++) {
      const t = j / 6 * travel;
      const knot = shift(point, Math.cos(a) * radius * t + (seed(i * 9 + j) - .5) * 22 * t,
        Math.sin(a) * radius * t * .20 + (seed(i + j) - .5) * 6 * t);
      ctx.beginPath(); ctx.moveTo(previous.x, previous.y); ctx.lineTo(knot.x, knot.y);
      ctx.strokeStyle = "#2b1917"; ctx.lineWidth = 5 * (1 - j / 8); ctx.stroke();
      if (bright && j < 5) { ctx.strokeStyle = "#ed9b51"; ctx.lineWidth = 1; ctx.stroke(); }
      previous = knot;
    }
  }
  ctx.restore();
}

function impact(id, point, age, width, tone, config = {}) {
  if (age < 0) return;
  const duration = config.duration ?? .43;
  const rise = smooth(0, .17, age);
  const alpha = pulse(age, 0, .032, .12, duration);
  if (age < .21) glow(shift(point, 0, -10), width * .72, tones[tone][2], pulse(age, 0, .024, .054, .21) * .40);
  const drawWidth = mix(.30, 1, rise) * width;
  const ratio = config.ratio ?? .68;
  sprite(id, shift(point, 0, -drawWidth * ratio * .48), drawWidth,
    { height: drawWidth * ratio, alpha, warp: 3, time: age });
  debris(point, age, tone, config.count ?? 26, config.speed ?? 290, config);
  dust(point, age, config.dust ?? 1, tone === "lava", config.drift ?? 0);
}

function line(a, b, color, width, alpha) {
  if (alpha <= .004) return;
  ctx.save();
  ctx.globalAlpha *= clamp(alpha);
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.lineCap = "round";
  ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
  ctx.restore();
}

function groundSprite(id, floor, width, options = {}) {
  const crop = (id < 20 ? common : bosses)[id < 20 ? id : id - 20];
  const height = options.height ?? width * crop[3] / crop[2];
  sprite(id, shift(floor, 0, -height / 2), width, { ...options, height });
}

function label(text, point, tint = "#ffd699") {
  ctx.save();
  ctx.font = "600 14px Segoe UI, sans-serif";
  ctx.textBaseline = "middle";
  ctx.textAlign = "center";
  const width = ctx.measureText(text).width + 24;
  ctx.beginPath(); ctx.roundRect(point.x - width / 2, point.y - 12, width, 24, 6);
  ctx.fillStyle = "#261e1aeb"; ctx.fill();
  ctx.strokeStyle = "#826343"; ctx.lineWidth = 1; ctx.stroke();
  ctx.fillStyle = tint; ctx.fillText(text, point.x, point.y);
  ctx.restore();
}

function arena(camera) {
  const wall = ctx.createLinearGradient(0, 0, 0, H);
  wall.addColorStop(0, "#211b19"); wall.addColorStop(.62, "#514036"); wall.addColorStop(1, "#211915");
  ctx.fillStyle = wall; ctx.fillRect(0, 0, W, H);
  glow(pos(520, 298), 418, "#a57b43", .18);
  ctx.save(); ctx.translate(camera.x, camera.y);
  ctx.fillStyle = "#2b221f"; ctx.fillRect(-12, FLOOR, W + 24, H - FLOOR + 12);
  for (let i = 0; i < 24; i++) {
    const x = i * W / 23;
    line(pos(mix(W / 2, x, .57), FLOOR), pos(x - (W / 2 - x) * .43, H + 8), "#9a7b5b", 1, .17);
  }
  for (let i = 0; i < 7; i++) { const y = FLOOR + i * i * 4.75; line(pos(-8,y), pos(W+8,y), "#c6aa81", 1, .17); }
  ctx.restore();
}

function portrait(img, x, y, height) {
  const width = height * img.width / img.height;
  ctx.save();
  ctx.translate(x, y);
  ctx.scale(-1, 1);
  ctx.drawImage(img, -width / 2, -height, width, height);
  ctx.restore();
}

function shadow(x, size) {
  ctx.beginPath(); ctx.ellipse(x, FLOOR + 3, size, 13, 0, 0, TAU);
  ctx.fillStyle = "#130d0ba8"; ctx.fill();
}

function scene(index, mode, time) {
  const info = attacks[index * 2 + mode];
  const board = mode === 1 && (index === 2 || index === 5 || index === 6);
  const melee = mode === 0 && [0, 1, 3, 4, 6].includes(index);
  const player = board ? 244 : 320;
  const home = board ? 650 : 804;
  const dash = melee ? ease(between(.08, info.hit - .02, time)) * (1 - smooth(info.hit + .17, info.hit + .54, time)) : 0;
  const x = mix(home, player + (index === 6 ? 170 : 129), dash);
  const height = heights[index];
  const enemy = pos(x, FLOOR);
  const ground = pos(player + (melee ? 21 : 4), FLOOR - 3);
  return {
    index, mode, time, info, board, melee, enemy, height, player,
    weapon: pos(x - 45, FLOOR - height * .59), hitPoint: pos(player + 8, FLOOR - 74), ground,
    age: time - info.hit, cast: time - info.release, defending: mode === 1 && [0, 1, 3, 4].includes(index)
  };
}

function projectile(s, id, size, tone, options = {}) {
  if (s.cast < 0 || s.age >= 0) return;
  const t = between(s.info.release, s.info.hit, s.time);
  const p = lerp(s.weapon, s.hitPoint, t);
  p.y -= Math.sin(Math.PI * t) * (options.arc ?? 18);
  const rotation = angle(s.weapon, s.hitPoint) + (options.rotate ?? 0) * s.cast;
  for (let i = 0; i < 13; i++) {
    const tick = t - (i + 1) * .021;
    if (tick < 0) continue;
    const q = lerp(s.weapon, s.hitPoint, tick);
    q.y += (seed(i) - .5) * i * 4 - Math.sin(Math.PI * tick) * (options.arc ?? 18);
    if (options.dust) glow(q, 9 + i, "#c7a882", .25 - i * .015);
    chip(q, (5 + seed(i) * 4) * (1 - i * .05), rotation + i, .64 - i * .041, tone, i);
  }
  const alpha = smooth(0, .14, t);
  sprite(id, p, size * mix(.6, 1, smooth(0, .46, t)), { alpha, rotation, warp: options.warp ?? 2, time: s.time });
  return p;
}

function sweep(s, id, tone, shape) {
  const progress = smooth(s.info.release, s.info.hit, s.time);
  const alpha = pulse(s.time, s.info.release, s.info.release + .042, s.info.hit + .043, s.info.hit + .17);
  if (alpha <= .004) return;
  const start = shift(s.enemy, shape.x, -shape.y);
  const control = shift(s.enemy, -68, -shape.peak);
  const finish = shift(s.ground, shape.landing ?? 7, shape.floor ?? -13);
  const curve = t => lerp(lerp(start, control, t), lerp(control, finish, t), t);
  const tip = curve(progress);
  const tangent = angle(curve(Math.max(0, progress - .045)), tip);
  ctx.save();
  ctx.beginPath(); ctx.rect(-8, -16, W + 16, FLOOR + 22); ctx.clip();
  for (let i = 1; i <= 21; i++) {
    const f = i / 21;
    line(curve((i - 1) / 21 * progress), curve(f * progress), tones[tone][2], 1 + f * 6, alpha * f * .44);
    if (i % 2) chip(shift(curve(f * progress), seed(i) * 12, -4 - seed(i + 7) * 17), 3 + seed(i) * 7, tangent - i, alpha * .76, tone, i);
  }
  sprite(id, shift(tip, -Math.cos(tangent) * 39, -Math.sin(tangent) * 39), shape.size,
    { alpha, rotation: tangent + shape.angle, reveal: .18 + progress, sweep: true, warp: tone === "mud" ? 7 : 3, time: s.time });
  ctx.restore();
}

function shield(id, s, width, options = {}) {
  const progress = smooth(0, .37, s.cast);
  const strike = pulse(s.age, -.023, .043, .075, .24);
  const fade = 1 - smooth(options.fadeFrom ?? 1.42, options.fadeTo ?? 1.88, s.cast);
  const broad = width * mix(.38, 1, progress) * (1 + strike * .066);
  const high = (options.height ?? width * .79) * mix(.17, 1, progress) * (1 - strike * .14);
  const base = shift(s.enemy, options.offset ?? 0, 5);
  const ripple = Math.sin(s.time * 38) * strike * 3;
  glow(shift(base, 0, -high * .5), broad * .68, options.tint ?? "#ffc76b", progress * fade * (.21 + strike * .42));
  groundSprite(id, shift(base, ripple, 0), broad, { height: high, alpha: progress * fade * (options.alpha ?? .70),
    rise: true, reveal: progress, time: s.time, warp: (options.warp ?? 3) + strike * 9 });
  ring(base, s.cast - .01, .62, broad * .58, options.tint ?? "#ffc76b");
  return { progress, strike, fade, base, broad, high };
}

function bubbles(base, width, time, alpha, tone) {
  if (alpha <= .003) return;
  for (let i = 0; i < 16; i++) {
    const n = seed(i + 5);
    const f = mod(time * (.52 + n) + i * .15);
    const point = shift(base, (seed(i + 19) - .5) * width + Math.sin(f * 7 + i) * 10, -16 - f * width * .50);
    chip(point, (3 + n * 4) * mix(.62, 1.24, f), f * 2, Math.sin(f * Math.PI) * alpha, tone, i);
  }
}

function counterHit(s, age, tint = "#ffdfb7") {
  if (age < -.21 || age > .12) return;
  const progress = smooth(-.20, .012, age);
  const tip = lerp(shift(s.hitPoint, 15, -26), shift(s.enemy, -30, -61), progress);
  const opacity = pulse(age, -.20, -.16, .025, .12);
  for (let i = 0; i < 4; i++)
    line(shift(tip, -63 - i * 12, 15 + i * 9), shift(tip, 2 + i * 2, -i * 6), tint, 5 - i * .85, opacity);
  glow(tip, 70, tint, opacity * .31);
}

function drawSlime(s) {
  if (!s.mode) {
    sweep(s, 0, "mud", { x: 14, y: 135, peak: 208, size: 169, angle: -.98 });
    const start = pulse(s.cast, 0, .06, .18, .31);
    for (let i = 0; i < 6; i++) {
      const t = smooth(0, .29, s.cast - i * .015);
      line(shift(s.weapon, 39 - i * 8, -135 + t * 115), shift(s.ground, 82 - i * 9, -19 - t * 45), "#9d7237", 3 + i, start * .47);
    }
    impact(1, s.ground, s.age, 155, "mud", { duration: .34, speed: 240, size: 10, dust: .39 });
    groundSprite(1, s.ground, mix(80, 155, smooth(0, .18, s.age)),
      { height: 22, alpha: pulse(s.age, .07, .19, .36, .58), warp: 4, time: s.age });
    return;
  }
  const swirl = pulse(s.cast, -.05, .22, 1.45, 1.96);
  groundSprite(3, shift(s.enemy, 0, 8), 186 + 5 * Math.sin(s.time * 13),
    { alpha: swirl * .84, height: 55, rotation: Math.sin(s.time * 12) * .048, warp: 10, time: s.time });
  const dome = shield(2, s, 200, { height: 150, alpha: .53, warp: 7, tint: "#d79738" });
  bubbles(dome.base, 169, s.time, dome.progress * dome.fade * .74, "mud");
  counterHit(s, s.age);
  sprite(4, shift(s.enemy, 3, -177), 54 * (1 + dome.strike * .26), { alpha: dome.progress * dome.fade * (.56 + dome.strike * .44) });
  debris(shift(s.enemy, -41, -48), s.age, "mud", 18, 196, { life: .42, size: 8 });
}

function drawGuard(s) {
  if (!s.mode) {
    sweep(s, 5, "clay", { x: 45, y: 133, peak: 204, size: 186, angle: .69 });
    impact(6, shift(s.ground, 3, -23), s.age, 138, "clay", { speed: 316, heading: -2.01, drift: -49 });
    return;
  }
  const breaking = s.time - 1.72;
  const dome = shield(7, s, 223, { height: 143, alpha: .56, fadeFrom: 1.42, fadeTo: 1.59 });
  const raise = smooth(.05, .46, s.cast);
  groundSprite(8, shift(s.enemy, -52 - dome.strike * 5, 5), mix(115, 202, raise),
    { height: mix(51, 121, raise), alpha: raise * dome.fade * .96, rise: true, reveal: raise, warp: dome.strike * 2, time: s.time });
  counterHit(s, s.age);
  counterHit(s, breaking);
  sprite(9, shift(s.enemy, -13, -207), 55 + dome.strike * 12, { alpha: dome.progress * dome.fade });
  debris(shift(s.enemy, -58, -58), s.age, "clay", 12, 155, { life: .35, size: 9 });
  impact(6, shift(s.enemy, -52, 3), breaking, 144, "clay", { speed: 295, life: .59 });
}

const chosen = { 2: [9, 27], 5: [2, 11, 19, 32], 6: [3, 12, 17, 25, 34] };
const cellPoint = i => pos(825 + (i % 6) * 45, 165 + Math.floor(i / 6) * 43);

function board(s) {
  if (!s.board) return;
  const cells = chosen[s.index];
  const onset = s.cast;
  const progress = smooth(-.04, .32, onset);
  const rune = s.index === 2 ? 12 : 27;
  const chain = s.index === 2 ? 13 : 28;
  const width = 300;
  const center = pos(938, 273);
  ctx.fillStyle = "#221a16ed"; ctx.strokeStyle = "#8c66448c";
  ctx.beginPath(); ctx.roundRect(785, 112, width + 5, 306, 13); ctx.fill(); ctx.stroke();
  ctx.fillStyle = "#f1d6b6"; ctx.font = "600 14px Segoe UI, sans-serif"; ctx.fillText("Bàn Match-3", 801, 137);
  for (let i = 0; i < 36; i++) {
    const point = cellPoint(i);
    const ix = cells.indexOf(i);
    const locked = ix >= 0 && onset >= .21 + ix * .033;
    ctx.fillStyle = locked ? "#634020" : "#3c3029";
    ctx.strokeStyle = locked ? "#e3aa5c" : "#6b5146";
    ctx.beginPath(); ctx.roundRect(point.x - 18, point.y - 18, 36, 36, 6); ctx.fill(); ctx.stroke();
    ctx.globalAlpha = locked ? .52 : .87;
    ctx.drawImage(art.gems[(i * 13 + (i / 6 | 0) * 2) % 6], point.x - 15, point.y - 15, 30, 30);
    ctx.globalAlpha = 1;
  }
  const aura = pulse(onset, -.085, .26, .33, .69);
  if (aura > .002) {
    glow(center, 157, "#e99529", aura * .35);
    sprite(rune, center, s.index === 2 ? 194 : 261, { alpha: aura * .90, reveal: progress, sweep: true, rotation: (1 - progress) * -.51 });
    ring(center, onset + .01, .57, s.index === 2 ? 101 : 135, "#eaaa47", .50);
  }
  const linked = onset >= .19;
  if (linked && s.index !== 2) {
    for (let i = 1; i < cells.length; i++) {
      const a = cellPoint(cells[i - 1]);
      const p = smooth(.14 + i * .036, .42 + i * .047, onset);
      const b = lerp(a, cellPoint(cells[i]), p);
      line(a, b, "#3f2419", 5, .75 * p);
      line(a, b, "#f1b157", 1.5, .79 * p);
      for (let t = .08; t < .97; t += .12) chip(lerp(a, b, t), 2.8, t * 14, .72 * p, "gold", i + 3);
    }
  }
  cells.forEach((i, ix) => {
    const p = cellPoint(i);
    const tighten = smooth(.11 + ix * .039, .39 + ix * .047, onset);
    if (tighten < .004) return;
    const ping = pulse(onset, .15 + ix * .037, .24 + ix * .04, .33 + ix * .042, .67 + ix * .035);
    sprite(chain, p, mix(62, 38, tighten), { alpha: tighten * .87, reveal: tighten, rotation: (1 - tighten) * .49 });
    sprite(s.index === 6 ? 34 : 14, shift(p, 10, 9), 26, { alpha: smooth(.42, .56, tighten) * (.90 + Math.sin(s.time * 8 + ix) * .08) });
    glow(p, 37, "#ffcc67", ping * .64);
  });
  if (onset >= .39) {
    ctx.fillStyle = "#ffc581"; ctx.font = "600 13px Segoe UI, sans-serif";
    ctx.fillText(`${cells.length} ô bị khóa · ${s.index === 2 ? 1 : 2} round`, 803, 405);
  }
}

function drawSealer(s) {
  projectile(s, 10, s.mode ? 83 : 104, "rock", { arc: 23, rotate: s.mode ? .6 : 1.8 });
  if (s.cast >= 0 && s.age < 0 && !s.mode) {
    for (let i = 1; i <= 2; i++) {
      const u = between(s.info.release + i * .045, s.info.hit, s.time);
      if (u <= 0) continue;
      const p = shift(lerp(s.weapon, s.hitPoint, u), 13, -Math.sin(Math.PI * u) * (56 + i * 24));
      sprite(10, p, 50 - i * 9, { alpha: Math.sin(u * Math.PI) * .8, rotation: u * (4 - i) });
    }
  }
  impact(11, shift(s.hitPoint, 0, 27), s.age, s.mode ? 111 : 133, "rock", { count: 23, size: 9 });
}

function drawScorpion(s) {
  if (!s.mode) {
    sweep(s, 15, "rock", { x: 37, y: 158, peak: 217, size: 173, angle: .28, floor: -3 });
    impact(16, s.ground, s.age, 158, "dust", { speed: 312, gravity: 620, count: 27 });
    cracks(s.ground, s.age, 83);
    return;
  }
  const dome = shield(17, s, 215, { height: 134, alpha: .79, tint: "#c29e79" });
  debris(dome.base, s.cast, "rock", 18, 217, { size: 9 });
  counterHit(s, s.age);
  const kick = s.age - .04;
  const progress = smooth(0, .25, kick);
  if (kick >= 0) {
    const travel = lerp(shift(s.enemy, -29, -16), s.ground, progress);
    groundSprite(18, travel, mix(63, 190, progress), { height: mix(53, 102, progress),
      alpha: pulse(kick, 0, .045, .24, .45), rise: true, reveal: mix(.36, 1, progress) });
    sprite(19, s.hitPoint, mix(40, 120, smooth(.19, .36, kick)), { alpha: pulse(kick, .18, .26, .34, .53), rotation: kick * 2 });
    debris(shift(s.hitPoint, 0, 20), kick - .22, "rock", 27, 322, { heading: -1.91, size: 9 });
  }
}

function drawMagma(s) {
  if (!s.mode) {
    const t = between(s.info.release, s.info.hit, s.time);
    const roll = shift(lerp(shift(s.enemy, 3, 0), s.ground, t), 0, -53 - Math.sin(t * Math.PI) * 19);
    const alpha = pulse(s.cast, 0, .069, s.info.hit - s.info.release, s.info.hit - s.info.release + .076);
    for (let i = 1; i <= 13; i++) {
      const p = shift(roll, i * 12, -seed(i) * i * 4);
      glow(p, 13 + i * 3, i % 3 === 0 ? "#e66b2d" : "#80665d", alpha * (1 - i / 14) * .40);
      chip(shift(p, 0, seed(i + 13) * 15), 3 + seed(i) * 5, t * 5, alpha * (1 - i / 14), "lava", i);
    }
    sprite(20, roll, mix(116, 161, t), { alpha, flip: true, rotation: -.42 + t * 2.85, warp: 4, time: s.time });
    impact(21, s.ground, s.age, 208, "lava", { speed: 355, dust: 1.12, count: 32, life: .64 });
    ring(s.ground, s.age, .51, 130, "#f48a43"); cracks(s.ground, s.age, 132, true);
    return;
  }
  const first = s.time - .85;
  const dome = shield(22, s, 224, { height: 156, alpha: .66, warp: 8, tint: "#ff7b2f", fadeFrom: .99, fadeTo: 1.12 });
  bubbles(dome.base, 172, s.time * 1.7, dome.progress * dome.fade, "lava");
  counterHit(s, first); counterHit(s, s.age);
  impact(23, shift(s.enemy, -28, -33), first, 119, "lava", { duration: .26, count: 12, speed: 172, dust: .35, size: 7, life: .29 });
  impact(24, shift(s.enemy, -23, 5), s.age, 268, "lava", { speed: 441, life: .73, size: 17, dust: 1.31, count: 47 });
  ring(shift(s.enemy, -14, 6), s.age, .77, 244, "#ff9b4d", .19);
  ring(shift(s.enemy, -14, 6), s.age - .09, .63, 166, "#ffc266", .16);
  cracks(shift(s.enemy, -16, 4), s.age, 231, true);
  const blast = smooth(.07, .34, s.age);
  if (s.age >= .07) {
    const point = lerp(shift(s.enemy,-22,-24), s.ground, blast);
    sprite(23, shift(point, 5, -34), 125, { alpha: pulse(s.age, .08, .21, .28, .38), rotation: -.48, warp: 6, time: s.age });
    impact(21, s.ground, s.age - .29, 130, "lava", { speed: 264, life: .48, count: 20, dust: .69 });
  }
}

function drawWitch(s) {
  const p = projectile(s, 25, s.mode ? 106 : 147, "dust", { arc: 16, rotate: -9.2, dust: true, warp: 5 });
  if (p) {
    const a = pulse(s.cast, 0, .07, .28, .44);
    for (let i = 0; i < 16; i++) {
      const phi = s.time * 21 + i / 16 * TAU;
      chip(shift(p, Math.cos(phi) * (47 + i * .9), Math.sin(phi) * 23), 3 + seed(i) * 4, phi, a * .64, "dust", i);
    }
  }
  impact(26, s.ground, s.age, s.mode ? 132 : 170, "dust", { speed: 324, size: 12, count: 32 });
  cracks(s.ground, s.age, 128);
  if (s.mode && s.cast >= .06) {
    const badge = pos(s.player+4, FLOOR-200+Math.sin(s.time*5)*4);
    sprite(29, badge, 67, { alpha: pulse(s.cast, .06, .21, 1.72, 2.10) });
    if (s.cast >= .35) label("−20% ATK", shift(badge, 0, -45));
  }
}

function drawBoss(s) {
  if (!s.mode) {
    const drop = ease(between(s.info.release, s.info.hit, s.time));
    const opacity = pulse(s.cast, 0, .068, .32, .52);
    if (drop > 0) {
      const tip = shift(s.ground, mix(51, 0, drop), -mix(316, 0, drop));
      for (let i = 0; i < 7; i++) {
        const f = seed(i);
        line(shift(tip, (f - .5) * 136, -106 - f * 57), shift(tip, f * 63 - 24, -42), "#f1ab69", 2 + f * 3, opacity * .38);
      }
      groundSprite(30, tip, mix(150, 211, drop), { height: mix(210, 245, drop), alpha: opacity, rotation: mix(-.16, .04, drop) });
    }
    impact(31, s.ground, s.age, 225, "rock", { speed: 392, count: 38, size: 15, dust: 1.32, life: .67 });
    ring(s.ground, s.age, .56, 152, "#e7c08a"); cracks(s.ground, s.age, 168, true);
    return;
  }
  const passive = pulse(s.time, 0, .16, .32, .69);
  sprite(33, shift(s.enemy, 0, -100), mix(90, 181, smooth(0, .22, s.time)), { alpha: passive * .84, reveal: smooth(0,.29,s.time), sweep: true });
  ring(shift(s.enemy, 0, 2), s.time, .56, 123, "#ffb654");
  const progress = between(s.info.release, s.info.hit, s.time);
  const from = shift(s.enemy, -47, 0);
  if (progress > 0) {
    for (let i = 1; i <= 5; i++) {
      const reach = progress - (5 - i) * .15;
      if (reach < 0) continue;
      const point = lerp(from, s.ground, reach);
      const age = s.age + (5 - i) * .081;
      const alpha = pulse(s.cast, 0, .12, .52, .80) * mix(.37, 1, i / 5);
      groundSprite(32, point, mix(82, 235, reach), { height: mix(25, 118, reach), alpha, rise: true, reveal: smooth(0,.18,reach), warp: 3, time: s.time });
      if (i > 1) cracks(point, age + .23, 57 + i * 8, i === 5);
      dust(point, s.cast - i * .081, .52, false, 22);
    }
  }
  impact(32, s.ground, s.age, 322, "rock", { ratio: .51, speed: 500, size: 17, count: 43, life: .78, dust: 1.64 });
  ring(s.ground, s.age, .79, 220, "#e8ad69", .19); cracks(s.ground, s.age, 222, true);
  if (s.cast > .24) {
    const badge = pos(s.player, FLOOR - 184);
    sprite(34, badge, 59 * (1 + Math.sin(s.time * 7) * .06), { alpha: pulse(s.cast, .24, .42, 1.69, 2.28) });
    if (s.cast >= .42) label("Choáng · 1 round", shift(badge, 0, -43));
  }
}

const draw = [drawSlime, drawGuard, drawSealer, drawScorpion, drawMagma, drawWitch, drawBoss];

function paint(s) {
  const hitShake = s.defending ? 0 : pulse(s.age, 0, .035, .09, .24) * (s.index === 6 ? 5 : 2.3);
  const shiftCam = pos(Math.sin(s.age * 89) * hitShake, Math.sin(s.age * 70) * hitShake * .55);
  arena(shiftCam);
  ctx.save(); ctx.translate(shiftCam.x, shiftCam.y);
  shadow(s.player, 49); shadow(s.enemy.x, s.height * .36);
  portrait(art.player, s.player - hitShake * 2, FLOOR, 143);
  const early = s.time < .045;
  const follow = s.time > s.info.hit + .39;
  const frame = s.time < s.info.release ? 0 : s.time < s.info.hit + .035 ? 1 : 2;
  const firstMagmaHit = s.index === 4 && s.mode ? s.time - .85 : -9;
  const reaction = s.defending && ((s.age >= 0 && s.age < .14) || (firstMagmaHit >= 0 && firstMagmaHit < .14));
  const actor = reaction ? art.hit[s.index] : early || follow ? art.rest[s.index] : art.poses[s.index][s.mode][frame];
  const roll = s.index === 4 && !s.mode ? pulse(s.time, s.info.release, s.info.release + .085, s.info.hit + .025, s.info.hit + .20) : 0;
  ctx.globalAlpha = 1 - roll * .95;
  portrait(actor, s.enemy.x, FLOOR, s.height);
  ctx.globalAlpha = 1;
  draw[s.index](s);
  ctx.restore();
  board(s);
}

function markers(index, mode, info) {
  const shieldAction = mode === 1 && [0, 1, 3, 4].includes(index);
  const steps = [
    { label: index === 6 && mode ? "Khiên đầu round" : "Lấy đà", time: info.release * .55 },
    { label: shieldAction ? "Dựng khiên" : "Tung chiêu", time: info.release + (shieldAction ? .24 : .12) },
    { label: index === 4 && mode ? "Vỡ khiên" : "Va chạm", time: info.hit + .105 }
  ];
  if (shieldAction && index === 4) steps.splice(2, 0, { label: "Chịu đòn", time: .955 });
  if (shieldAction && index === 3) steps.push({ label: "Phản đòn", time: info.hit + .33 });
  if (shieldAction && index === 1) steps.push({ label: "Vỡ khiên", time: 1.89 });
  steps.push({ label: "Dư âm", time: Math.min(info.end - .12, info.hit + (shieldAction ? .86 : .34)) });
  return steps;
}

function caption(s) {
  if (s.time < s.info.release) return s.index === 6 && s.mode ? "Khiên đầu round · 10% HP tối đa" : "Lấy đà";
  if (s.defending) {
    if (s.index === 4 && s.time > .80 && s.age < 0) return "Đòn thử làm nứt khiên dung nham";
    if ((s.index === 4 && s.age >= 0) || (s.index === 1 && s.time >= 1.72)) return "Khiên vỡ";
    if (s.age >= 0) return s.index === 3 ? "Gai trả đòn · phản 20%" : s.index === 0 ? "Màng bùn giảm 35% sát thương" : "Khiên hấp thụ đòn đánh";
    return s.index === 3 ? "Gai đang dựng" : "Khiên đang dựng";
  }
  if (s.board && s.cast >= .26) return `${chosen[s.index].length} ô chịu phong ấn`;
  return s.age < 0 ? "Tung chiêu" : s.age < .22 ? "Va chạm" : "Dư âm";
}

function selectAttack(enemy, mode) {
  state.enemy = enemy; state.mode = mode;
  const info = attacks[enemy * 2 + mode];
  $("enemy-name").textContent = names[enemy]; $("attack-name").textContent = info.name;
  $("description").textContent = info.description;
  $("normal").setAttribute("aria-pressed", String(mode === 0)); $("active").setAttribute("aria-pressed", String(mode === 1));
  [...$("roster").children].forEach((b, i) => b.setAttribute("aria-current", String(i === enemy)));
  $("timeline").max = info.end;
  $("markers").replaceChildren(...markers(enemy, mode, info).map(marker => {
    const button = document.createElement("button");
    button.type = "button"; button.textContent = marker.label;
    button.dataset.time = marker.time;
    button.addEventListener("click", () => seek(marker.time));
    return button;
  }));
  seek(info.hit + .105);
}

function refresh() {
  const s = scene(state.enemy, state.mode, state.time);
  if (state.ready) paint(s);
  $("timeline").value = state.time;
  $("timecode").value = state.time.toFixed(2).replace(".", ",") + " s";
  $("event").textContent = caption(s);
  $("play").textContent = state.running ? "Dừng" : "Phát";
  [...$("markers").children].forEach(button => button.classList.toggle("current", Math.abs(+button.dataset.time - state.time) < .05));
}

function seek(time) {
  state.running = false;
  cancelAnimationFrame(state.frameId);
  state.time = Math.min(attacks[state.enemy * 2 + state.mode].end, Math.max(0, time));
  refresh();
}

function play(restart = false) {
  if (!state.ready) return;
  if (state.running && !restart) { seek(state.time); return; }
  if (restart || state.time >= attacks[state.enemy * 2 + state.mode].end) seek(0);
  state.running = true;
  state.previous = 0;
  function tick(now) {
    if (!state.running) return;
    const end = attacks[state.enemy * 2 + state.mode].end;
    if (state.previous) state.time += Math.min(.05, (now - state.previous) / 1000) * state.speed;
    state.previous = now;
    if (state.time >= end) { seek(end); return; }
    refresh();
    state.frameId = requestAnimationFrame(tick);
  }
  refresh();
  state.frameId = requestAnimationFrame(tick);
}

names.forEach((name, i) => {
  const button = document.createElement("button");
  const photo = document.createElement("img");
  button.type = "button"; button.className = "enemy-option";
  photo.src = `${assets}/Enemies/Earth/Enemy_${ids[i]}.png`; photo.alt = "";
  button.append(photo, document.createTextNode(name));
  button.addEventListener("click", () => selectAttack(i, state.mode));
  $("roster").append(button);
});
$("normal").addEventListener("click", () => selectAttack(state.enemy, 0));
$("active").addEventListener("click", () => selectAttack(state.enemy, 1));
$("play").addEventListener("click", () => play());
$("restart").addEventListener("click", () => play(true));
$("back").addEventListener("click", () => seek(state.time - 1 / 30));
$("forward").addEventListener("click", () => seek(state.time + 1 / 30));
$("speed").addEventListener("change", event => { state.speed = +event.target.value; });
$("timeline").addEventListener("input", event => seek(+event.target.value));
selectAttack(0, 0);

window.earthPreview = {
  get ready() { return state.ready; },
  metadata() {
    return JSON.stringify({ enemies: names, attacks: attacks.map((info, i) => ({ ...info, roundCount: 0,
      scenarios: [{ id: "standard", label: "Bình thường" }], markers: markers(i >> 1, i % 2, info) })) });
  },
  frame(enemy, mode, time) {
    const current = scene(enemy, mode, time);
    paint(current);
    return JSON.stringify({ image: canvas.toDataURL("image/jpeg", .94).split(",", 2)[1], caption: caption(current) });
  }
};

loadArt().then(() => {
  state.ready = true;
  $("load-message").hidden = true;
  refresh();
}).catch(error => {
  $("load-message").textContent = "Không tải được sprite " + error.message;
  console.error(error);
});
