"use strict";

const WIDTH = 1160;
const HEIGHT = 560;
const FLOOR = 419;
const TAU = Math.PI * 2;
const assetRoot = "../../Assets/Project/Sprite";
const waterFrames = assetRoot + "/Enemies/Animations/Water";
const canvas = document.getElementById("stage");
const g = canvas.getContext("2d");
const $ = id => document.getElementById(id);

const enemies = [
  { id: "SlimeNuoc", name: "Slime Nước", height: 134, tint: "#53d9ed",
    normal: ["Đập bọt nước cơ bản", .20, .59, 1.17, "Nén thân, nhả giọt nước từ miệng. Vòng nước và các giọt nhỏ tách ra khi đạn chạm."],
    active: ["Tia Nước Làm Chậm", .24, .59, 1.53, "Tia nước cuộn tiến từ miệng Slime trong 0,35 giây. Đồng hồ báo chi phí skill tăng 2 Energy trong 1 round."] },
  { id: "ThuyQuaiNho", name: "Thủy Quái Nhỏ", height: 149, tint: "#63e2f0",
    normal: ["Quật vây nước", .34, .50, 1.03, "Lưỡi liềm hiện theo đầu vây. Bọt rời mép vệt chém và tạt theo hướng quật."],
    active: ["Cắn Xoáy Thủy Triều", .18, .50, 1.26, "Nước cuộn theo cú lao, siết quanh mục tiêu rồi đẩy một vòng bọt thấp sát mặt sàn."] },
  { id: "KeTromKyNang", name: "Kẻ Trộm Kỹ Năng", height: 147, tint: "#54dfdc",
    normal: ["Phi dao nước", .21, .48, .92, "Dao rời tay và hướng theo đường bay. Vệt nước ngắn giữ cho mũi dao sắc và dễ đọc."],
    active: ["Cướp Đoạt Nội Năng", .26, .65, 1.47, "Dòng Energy chạy từ Player về lọ thuốc. Hồi máu và va chạm đạn có nhịp hiển thị riêng."] },
  { id: "PhapSuBang", name: "Pháp Sư Băng", height: 149, tint: "#85c8ff",
    normal: ["Bắn vụn băng lạnh giá", .23, .55, 1.08, "Một mảnh chính dẫn chùm băng. Khi chạm, bông tuyết chớp rồi nhả vụn băng và bụi lạnh."],
    active: ["Mũi Băng Hàn Giá", .34, .68, 1.53, "Mũi băng xuất phát từ tay. Xung khóa bật trên tối đa 2 viên hợp lệ ngay khi tung chiêu, trước khi đạn chạm Player."] },
  { id: "QuaiVatDamLay", name: "Quái Vật Đầm Lầy", height: 173, tint: "#b4cb70",
    normal: ["Quật xúc tu đầm lầy", .36, .55, 1.18, "Cung roi theo xúc tu. Bùn và rêu văng thành cục nặng về hướng quất rồi rơi."],
    active: ["Đầm Lầy Ăn Mòn", .30, .73, 1.73, "Luồng bùn lan từ miệng. HUD hiện nhịp Energy, còn khiên rạn báo giảm DEF. Vũng bùn nổi bọt tại điểm chạm."] },
  { id: "TinhLinhBangGia", name: "Tinh Linh Băng Giá", height: 163, tint: "#a6e3ff",
    normal: ["Phóng lốc băng giá", .24, .68, 1.25, "Lõi lốc bay theo đạn. Tuyết xoay quanh lõi rồi bung thành tinh thể ở điểm chạm."],
    active: ["Đóng Băng Hàng Ngũ", .39, .77, 1.71, "Vòng phép tự vẽ trên đầu. Cột tuyết giáng xuống và gai băng lan trên đúng các ô có đá ở hàng cuối."] },
  { id: "ThuyThanSaNga", name: "Thủy Thần Sa Ngã", height: 191, tint: "#51c6e3",
    normal: ["Chém trảm đao nước", .36, .51, 1.18, "Vệt nước theo đầu đinh ba. Vòng hút chỉ bật khi Player thực sự mất 5 Energy."],
    active: ["Sóng Thần Diệt Tuyệt Tối Thượng", .38, .93, 1.88, "Chân sóng đi trước, đỉnh dựng lên sau. Xoáy nước bung khi chạm. HUD mô phỏng Energy về 0 và lượt bị Choáng."] }
];

// Atlas rectangles use pixel coordinates from the two 5-column reference layouts.
const commonRects = [
  [12,64,269,196], [303,65,253,200], [571,22,279,258], [850,79,266,184], [1152,24,231,253],
  [12,299,274,251], [299,313,250,238], [566,304,272,253], [840,318,272,232], [1121,330,269,205],
  [9,625,284,150], [302,578,247,234], [595,574,224,244], [840,581,267,230], [1120,590,270,195],
  [7,825,281,262], [296,834,260,248], [565,899,276,125], [866,850,223,222], [1115,863,277,204]
];
const bossRects = [
  [7,10,315,288], [328,61,293,227], [624,35,357,245], [981,10,336,275], [1335,29,242,260],
  [8,311,287,297], [315,310,335,310], [658,309,295,300], [955,379,355,209], [1317,302,296,298],
  [7,649,312,278], [321,644,296,290], [609,628,373,304], [959,640,363,289], [1312,636,300,285]
];

const photos = { player: null, common: null, boss: null, poses: [], gems: [] };
const state = { enemy: 0, mode: 1, scenario: "standard", round: 0, roundAge: 1, time: 0, playing: false, speed: .5, ready: false };
const scenesWithBoard = new Set([3, 5]);
const closeCombat = (id, active) => id === 1 || (!active && (id === 4 || id === 6));
const unit = value => Math.max(0, Math.min(1, value));
const mix = (a, b, t) => a + (b - a) * t;
const fraction = (a, b, t) => unit((t - a) / (b - a));
const smooth = (a, b, t) => { const x = fraction(a, b, t); return x * x * (3 - 2 * x); };
const easeOut = t => 1 - (1 - t) ** 3;
const pulse = (t, start, peak, hold, end) => smooth(start, peak, t) * (1 - smooth(hold, end, t));
const seed = i => { const x = Math.sin(i * 127.1 + 17.9) * 43758.5453; return x - Math.floor(x); };
const pt = (x, y) => ({ x, y });
const lerpPoint = (a, b, t) => pt(mix(a.x, b.x, t), mix(a.y, b.y, t));
const angleTo = (a, b) => Math.atan2(b.y - a.y, b.x - a.x);
const held = (t, duration) => t >= 0 && t < duration;
const formatTime = new Intl.NumberFormat("vi-VN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

function pathToFrame(index, mode, frame) {
  return `${waterFrames}/${enemies[index].id}/Atack_${mode + 1}_${frame}.png`;
}

function loadImage(url) {
  return new Promise((resolve, reject) => {
    const photo = new Image();
    photo.onload = () => resolve(photo);
    photo.onerror = () => reject(new Error(url));
    photo.src = url;
  });
}

async function loadArtwork() {
  [photos.player, photos.common, photos.boss, photos.poses, photos.gems] = await Promise.all([
    loadImage(assetRoot + "/Player.png"),
    loadImage("WaterCommon.png"),
    loadImage("WaterBosses.png"),
    Promise.all(enemies.map((_, index) => Promise.all([0, 1].map(mode =>
      Promise.all([0, 1, 2].map(frame => loadImage(pathToFrame(index, mode, frame)))))))),
    Promise.all(["Water", "Ice", "Earth", "Fire", "Wood", "Wind"].map(name => loadImage(assetRoot + "/" + name + ".png")))
  ]);
}

function attack(index, mode) {
  const [name, release, hit, end, description] = enemies[index][mode ? "active" : "normal"];
  return { name, release, hit, end, description };
}

function sceneAt(index, mode, time, scenario, round, roundAge = 1) {
  const info = attack(index, mode);
  const melee = closeCombat(index, !!mode);
  const board = !!mode && scenesWithBoard.has(index);
  const missed = scenario === "miss" && !melee;
  const cast = time >= info.release;
  const contact = time >= info.hit && !missed;
  const playerX = board ? 258 : 305;
  const homeX = board ? 670 : 790;
  const engage = easeOut(fraction(.055, info.release + .025, time));
  const retreat = smooth(info.hit + .13, info.hit + .43, time);
  const x = melee ? mix(homeX, playerX + (index === 6 ? 132 : 114), engage * (1 - retreat)) : homeX;
  const approach = melee ? -21 * Math.sin(Math.PI * engage) : 0;
  const poseStart = melee ? info.release - .13 : info.release - .12;
  const pose = time < poseStart || time > info.hit + .35 ? 0 : time < info.release ? 0 : time < info.hit + .035 ? 1 : 2;
  const from = scenario === "low" ? 3 : scenario === "empty" ? 0 : 12;
  let energy = from;
  if (index === 2 && mode && cast && from >= 5) energy -= 5;
  if (index === 4 && mode && cast && from >= 10) energy -= 10;
  if (index === 6 && !mode && contact && from >= 5) energy -= 5;
  if (index === 6 && mode && cast) energy = 0;
  const cost = index === 0 && mode && cast && round === 0 ? 2 : 0;
  const defense = index === 4 && mode && cast && round < 2 ? 30 : 0;
  const stun = index === 6 && mode && cast && scenario !== "immune" && round === 0;
  const energyUI = index === 2 && mode || index === 4 && mode || index === 6;
  const height = enemies[index].height;
  const hit = pt(playerX + 5, FLOOR - 74);
  return {
    index, mode, time, info, scenario, round, roundAge, melee, board, missed, cast, contact, x, height, pose, hit,
    enemyFloor: FLOOR + approach, playerX, from, energy, cost, defense, stun, energyUI, tint: enemies[index].tint,
    floorPoint: pt(playerX + (melee ? 26 : 13), FLOOR - 4),
    target: missed ? pt(52, FLOOR - 193) : hit
  };
}

function cell(id) {
  return id < 20 ? [photos.common, commonRects[id]] : [photos.boss, bossRects[id - 20]];
}

function atlas(id, x, y, width, options = {}) {
  const [image, rect] = cell(id);
  const [sx, sy, sw, sh] = rect;
  const height = options.height ?? width * sh / sw;
  const alpha = options.alpha ?? 1;
  if (alpha <= .002 || width < 1 || height < 1) return;
  g.save();
  g.translate(x, y);
  g.rotate(options.rotation ?? 0);
  if (options.flip) g.scale(-1, 1);
  g.globalAlpha *= unit(alpha);
  if (options.reveal !== undefined && options.reveal < .995) {
    const progress = unit(options.reveal);
    g.beginPath();
    if (options.radial) {
      const start = options.start ?? -Math.PI * .8;
      g.moveTo(0, 0);
      g.arc(0, 0, Math.max(width, height), start, start + TAU * progress);
      g.closePath();
    } else {
      g.rect(-width / 2, -height / 2, width * progress, height);
    }
    g.clip();
  }
  if (!options.warp) {
    g.drawImage(image, sx, sy, sw, sh, -width / 2, -height / 2, width, height);
  } else {
    const bands = 20;
    for (let i = 0; i < bands; i++) {
      const u = (i + .5) / bands;
      const bend = Math.sin(u * 15 - options.time * 23) * options.warp
        + Math.sin(u * 29 - options.time * 35) * options.warp * .25;
      const drift = bend * Math.sin(u * Math.PI);
      g.drawImage(image, sx + (i * sw / bands), sy, sw / bands + (i < bands - 1 ? .5 : 0), sh,
        -width / 2 + (i * width / bands), -height / 2 + drift, width / bands + (i < bands - 1 ? .55 : 0), height);
    }
  }
  g.restore();
}

function anchoredJet(id, from, to, thickness, time, alpha, origin = [.055, .88], tip = [.91, .12]) {
  if (alpha <= .002) return;
  const [image, rect] = cell(id);
  const [sx, sy, sw, sh] = rect;
  const a = pt(sw * origin[0], sh * origin[1]);
  const b = pt(sw * tip[0], sh * tip[1]);
  const length = Math.hypot(to.x - from.x, to.y - from.y);
  if (length < 4) return;
  g.save();
  g.globalAlpha *= alpha;
  g.translate(from.x, from.y);
  g.rotate(angleTo(from, to));
  g.scale(length / Math.hypot(b.x - a.x, b.y - a.y), thickness / sh);
  g.rotate(-angleTo(a, b));
  g.translate(-a.x, -a.y);
  const bands = 18;
  for (let i = 0; i < bands; i++) {
    const u = i / bands;
    const offset = Math.sin(u * 16 - time * 37) * Math.sin(u * Math.PI) * 13;
    g.drawImage(image, sx + u * sw, sy, sw / bands + (i < bands - 1 ? .6 : 0), sh,
      u * sw, offset, sw / bands + (i < bands - 1 ? .6 : 0), sh);
  }
  g.restore();
}

const colors = {
  water: ["#4ccdeb", "#a6f6ff", "#245e7a"], foam: ["#96e7f4", "#f2ffff", "#3d849a"],
  ice: ["#79c3f2", "#eafaFF", "#427cb1"], mud: ["#817843", "#b7bb65", "#44402b"],
  deep: ["#288fa9", "#9cf9f9", "#174a63"], heal: ["#6ef6c8", "#e1ffe1", "#367f73"]
};

function particle(x, y, size, angle, alpha, kind) {
  if (alpha <= .005) return;
  const [fill, shine, stroke] = colors[kind];
  g.save();
  g.translate(x, y);
  g.rotate(angle);
  g.globalAlpha *= alpha;
  g.beginPath();
  if (kind === "ice") {
    g.moveTo(-size * .52, -size * .21);
    g.lineTo(size * .82, 0);
    g.lineTo(size * .03, size * .24);
    g.lineTo(-size * .53, size * .10);
    g.closePath();
  } else if (kind === "heal") {
    g.moveTo(-size * .6, 0); g.lineTo(0, -size * .7); g.lineTo(size * .6, 0); g.lineTo(0, size * .7); g.closePath();
  } else {
    g.ellipse(0, 0, size * .62, size * (kind === "foam" ? .41 : .30), 0, 0, TAU);
  }
  g.lineWidth = Math.max(.65, size * .085);
  g.strokeStyle = stroke;
  g.fillStyle = fill;
  g.fill();
  g.stroke();
  g.fillStyle = shine;
  g.beginPath();
  g.ellipse(-size * .10, -size * .12, size * .23, size * .085, 0, 0, TAU);
  g.fill();
  g.restore();
}

function burst(origin, age, kind, count = 21, config = {}) {
  if (age < 0 || age > .95) return;
  for (let i = 0; i < count; i++) {
    const noise = seed(i + (config.seed ?? 91));
    const offset = seed(i + 281);
    const delay = offset * (config.stagger ?? .044);
    const life = .26 + noise * (config.life ?? .43);
    const t = age - delay;
    if (!held(t, life)) continue;
    const heading = (config.heading ?? -Math.PI / 2) + (offset - .5) * (config.spread ?? Math.PI * 1.45);
    const velocity = (config.speed ?? 272) * (.38 + noise * .74);
    const drift = velocity * (1 - Math.exp(-t * 2.1)) / 2.1;
    const gravity = config.gravity ?? (kind === "mud" ? 560 : kind === "ice" ? 130 : 370);
    const x = origin.x + Math.cos(heading) * drift;
    const y = origin.y + Math.sin(heading) * drift + gravity * t * t / 2;
    if (y > FLOOR + 13) continue;
    const size = (config.size ?? 11) * (.24 + noise * .76) * (1 - .45 * t / life);
    particle(x, y, size, heading + (kind === "ice" ? t * 5.1 : t * 1.9),
      smooth(0, .025, t) * (1 - smooth(life * .5, life, t)), kind);
  }
}

function halo(x, y, radius, color, opacity) {
  if (opacity <= .002) return;
  g.save();
  g.globalAlpha = unit(opacity);
  const light = g.createRadialGradient(x, y, radius * .06, x, y, radius);
  light.addColorStop(0, color);
  light.addColorStop(1, "transparent");
  g.fillStyle = light;
  g.fillRect(x - radius, y - radius, radius * 2, radius * 2);
  g.restore();
}

function waterRing(p, age, color, width = 120, lifetime = .58, squash = .28) {
  if (!held(age, lifetime)) return;
  const radius = mix(13, width, easeOut(age / lifetime));
  g.save();
  g.globalAlpha = pulse(age, 0, .035, lifetime * .34, lifetime) * .69;
  g.strokeStyle = color;
  g.lineWidth = mix(4, .75, age / lifetime);
  g.beginPath();
  g.ellipse(p.x, p.y, radius, radius * squash, 0, 0, TAU);
  g.stroke();
  g.restore();
}

function portrait(image, x, y, height, flip, squash = 1, stretch = 1) {
  const width = height * image.width / image.height;
  g.save();
  g.translate(x, y);
  g.scale(flip ? -squash : squash, stretch);
  g.drawImage(image, -width / 2, -height * .95, width, height);
  g.restore();
}

function pivot(s, side = .29, level = .56) {
  const image = photos.poses[s.index][s.mode][s.pose];
  const width = s.height * image.width / image.height;
  return pt(s.x - side * width, s.enemyFloor - s.height * level);
}

function traceCurve(start, control, end, t) {
  return lerpPoint(lerpPoint(start, control, t), lerpPoint(control, end, t), t);
}

function weapon(s, id, shape, color) {
  const age = s.time - s.info.release;
  const length = (s.info.hit - s.info.release);
  const start = pt(s.x + shape[0], s.enemyFloor + shape[1]);
  const control = pt(s.x + shape[2], s.enemyFloor + shape[3]);
  const end = pt(s.hit.x - 11, s.hit.y + shape[4]);
  const amount = smooth(0, length, age);
  const opacity = pulse(age, 0, .035, length + .020, length + .165);
  if (opacity > .002) {
    const head = traceCurve(start, control, end, amount);
    const behind = traceCurve(start, control, end, Math.max(0, amount - .055));
    g.save();
    g.globalAlpha = opacity;
    g.strokeStyle = color;
    g.shadowColor = color;
    g.shadowBlur = 12;
    g.lineCap = "round";
    for (let i = 1; i < 24; i++) {
      const p = traceCurve(start, control, end, (i - 1) / 24 * amount);
      const q = traceCurve(start, control, end, i / 24 * amount);
      g.lineWidth = mix(1, 6.5, i / 24);
      g.beginPath(); g.moveTo(p.x, p.y); g.lineTo(q.x, q.y); g.stroke();
    }
    g.restore();
    const tangent = angleTo(behind, head);
    const blade = pt(head.x - 35 * Math.cos(tangent), head.y - 35 * Math.sin(tangent));
    atlas(id, blade.x, blade.y, shape[5], { alpha: opacity, rotation: tangent + .66, reveal: .15 + amount, radial: true, flip: false, warp: 2, time: s.time });
  }
}

function flight(s, id, options = {}) {
  const target = s.target;
  const end = s.info.hit + (s.missed ? .12 : 0);
  if (s.time < s.info.release || s.time >= end) return;
  const at = fraction(s.info.release, end, s.time);
  const from = options.origin ?? pivot(s);
  const current = lerpPoint(from, target, at);
  current.y -= Math.sin(at * Math.PI) * (options.arc ?? 0);
  const angle = angleTo(from, target);
  const size = options.size ?? 100;
  const tail = options.tail ?? "water";
  if (tail) {
    for (let i = 8; i > 0; i--) {
      const progress = at - i * .028;
      if (progress < 0) continue;
      const p = lerpPoint(from, target, progress);
      const lift = Math.sin(i * 2.2 + at * 15) * i * .95;
      p.y += lift - Math.sin(progress * Math.PI) * (options.arc ?? 0);
      particle(p.x, p.y, (9 - .72 * i) * (options.trailSize ?? 1), angle, .67 - .061 * i, tail);
    }
  }
  const grow = (options.squash ? mix(.79, 1, smooth(0, .24, at)) : 1) * (1 + Math.sin(s.time * 25) * (options.flutter ?? .034));
  halo(current.x, current.y, size * .72, options.glow ?? s.tint, .115);
  atlas(id, current.x, current.y, size * grow, { rotation: angle - (options.authoredAngle ?? 0) + (options.spin ?? 0) * s.time,
    height: options.height ? options.height / grow : undefined, warp: options.warp, time: s.time, alpha: smooth(0, .10, at) });
  return current;
}

function splash(id, p, age, kind, width, config = {}) {
  const opacity = pulse(age, 0, .035, config.hold ?? .12, config.end ?? .32);
  if (opacity > .002) {
    const grow = mix(.26, 1, smooth(0, .19, age));
    const y = p.y - (config.lift ?? 14) * grow;
    halo(p.x, y, width * .67, colors[kind][1], opacity * .25);
    atlas(id, p.x, y, width * grow, { alpha: opacity, rotation: config.rotation ?? 0, warp: config.warp,
      time: age, height: config.height ? config.height * grow : undefined });
  }
  burst(p, age, kind, config.count ?? 22, config);
}

function orbit(p, time, alpha, tone, rx, ry, arms = 2) {
  if (alpha <= .003) return;
  g.save();
  g.globalAlpha *= alpha;
  g.translate(p.x, p.y);
  g.strokeStyle = tone;
  g.lineWidth = 1.8;
  g.lineCap = "round";
  for (let i = 0; i < arms; i++) {
    g.beginPath();
    for (let j = 0; j < 34; j++) {
      const step = j / 33;
      const a = time + i * TAU / arms + step * 4.72;
      const x = Math.cos(a) * step * rx;
      const y = Math.sin(a) * step * ry;
      if (!j) g.moveTo(x, y); else g.lineTo(x, y);
    }
    g.stroke();
  }
  g.restore();
}

function drawSlime(s) {
  const age = s.time - s.info.hit;
  if (!s.mode) {
    flight(s, 0, { origin: pivot(s, .30, .39), size: 113, squash: true, warp: 4, arc: 12 });
    if (s.contact) {
      splash(1, pt(s.hit.x, s.hit.y + 16), age, "water", 140, { lift: 13, size: 12 });
      waterRing(s.floorPoint, age - .02, "#7fd4e8", 105);
    }
    return;
  }

  const source = pivot(s, .30, .39);
  const wind = pulse(s.time, .04, .15, s.info.release, s.info.release + .10);
  halo(source.x, source.y, 38 + wind * 21, "#b0fcff", wind * .72);
  if (wind > .02) atlas(0, source.x - 6, source.y, 17 + wind * 24,
    { rotation: Math.PI, alpha: wind * .84 });
  const end = s.info.hit + (s.missed ? .13 : 0);
  const progress = fraction(s.info.release, end, s.time);
  const alpha = pulse(s.time, s.info.release, s.info.release + .045, end, end + .11);
  anchoredJet(2, source, lerpPoint(source, s.target, easeOut(progress)), 69 + Math.sin(s.time * 33) * 6, s.time, alpha);
  if (s.contact) {
    burst(s.hit, age, "water", 23, { speed: 230, heading: Math.PI * 1.16, spread: 2.9 });
    const puddle = pulse(age, 0, .14, .57, .91);
    atlas(3, s.floorPoint.x, s.floorPoint.y - 6, mix(35, 204, smooth(0, .22, age)),
      { height: 74, rotation: Math.sin(age * 4) * .038, alpha: puddle, warp: 5, time: -age });
    orbit(s.floorPoint, -age * 8, puddle * .53, "#adfbff", 76, 17);
    waterRing(s.floorPoint, age, "#a0f4f4", 109);
  }
  if (s.cost) {
    const badge = pt(s.playerX, FLOOR - 174 - Math.sin(s.time * 5) * 3);
    atlas(4, badge.x, badge.y, 48, { alpha: .94 });
    textBadge("+2 Energy · 1 round", badge.x, badge.y - 38, "#abedf4");
  }
}

function drawBeast(s) {
  const time = s.time;
  const age = time - s.info.hit;
  if (!s.mode) {
    weapon(s, 5, [49, -77, -46, -166, 13, 145], "#95f7fa");
    const early = time - (s.info.release + .035);
    const sweep = fraction(s.info.release, s.info.hit, time);
    if (!s.contact) burst(lerpPoint(pt(s.x + 30, s.enemyFloor - 72), s.hit, sweep), early,
      "foam", 10, { speed: 115, size: 9, heading: 3.15, spread: 2.3, life: .12 });
    if (s.contact) splash(6, pt(s.hit.x, s.hit.y + 22), age, "foam", 135,
      { count: 27, heading: 2.95, spread: 2.5, gravity: 480, size: 13 });
    return;
  }

  const motion = pulse(time, .13, .24, s.info.hit - .045, s.info.hit + .09);
  const travel = pt(s.x + 23, s.enemyFloor - 62);
  atlas(8, travel.x, travel.y, 211, { alpha: motion, rotation: -.18, flip: true, warp: 8, time });
  orbit(travel, time * -12, motion * .60, "#c8ffff", 77, 38, 3);
  for (let i = 0; i < 12; i++) {
    const drift = seed(i + 29);
    const lag = time - .15 - i * .019;
    if (lag <= 0) continue;
    const wake = pt(travel.x + (i * 10 + drift * 15) * (1 + lag * .27), travel.y + Math.sin(lag * 18 + i) * 16);
    particle(wake.x, wake.y, 6 + drift * 9, .3, motion * (.85 - i * .056), "foam");
  }
  const jaws = pulse(age, -.15, -.074, .025, .17);
  if (jaws > .002) {
    const size = mix(172, 112, smooth(-.09, .022, age)) + smooth(.035, .13, age) * 24;
    atlas(7, s.hit.x, s.hit.y + 3, size, { rotation: age * -7.5, alpha: jaws, warp: 3, time });
    orbit(s.hit, -time * 17, jaws * .76, "#c0ffff", size * .46, size * .35, 3);
  }
  if (s.contact) {
    atlas(9, s.floorPoint.x, s.floorPoint.y, mix(47, 265, smooth(0, .20, age)),
      { height: mix(18, 68, smooth(0, .20, age)), alpha: pulse(age, 0, .036, .16, .46), warp: 5, time: age });
    waterRing(s.floorPoint, age, "#c7ffff", 162);
    burst(s.floorPoint, age, "foam", 37, { speed: 350, size: 13 });
  }
}

function drawEnergyTransfer(from, to, time, alpha, kind) {
  if (alpha <= .004) return;
  const cp = pt(mix(from.x, to.x, .51), Math.min(from.y, to.y) - 82);
  g.save();
  g.globalAlpha *= alpha * .65;
  g.lineCap = "round";
  g.strokeStyle = "#47cce3";
  g.shadowColor = "#44c9f4";
  g.shadowBlur = 13;
  g.lineWidth = 2.2;
  g.beginPath(); g.moveTo(from.x, from.y); g.quadraticCurveTo(cp.x, cp.y, to.x, to.y); g.stroke();
  g.restore();
  for (let i = 0; i < 16; i++) {
    const p = tmod(time * 2.4 - i * .064);
    const pos = traceCurve(from, cp, to, p);
    particle(pos.x, pos.y + Math.sin(p * 18 - time * 18) * 8 * Math.sin(p * Math.PI),
      4 + seed(i + 998) * 5, .1, alpha * Math.sin(Math.PI * p), kind);
  }
}

function tmod(value) { return value - Math.floor(value); }

function drawThief(s) {
  const age = s.time - s.info.hit;
  const hand = pivot(s, .37, .51);
  if (!s.mode) {
    flight(s, 10, { origin: hand, size: 117, trailSize: .74, warp: 3, authoredAngle: -.18, flutter: .012 });
    if (s.contact) splash(11, s.hit, age, "water", 104, { lift: 7, end: .22, count: 15, speed: 235, spread: 2 });
    return;
  }

  const castAge = s.time - s.info.release;
  const bottle = pivot(s, .08, .41);
  const pull = pulse(castAge, -.06, .06, .42, .65);
  const paid = s.from >= 5;
  const rune = pt(s.hit.x + 47, s.hit.y - 11);
  atlas(12, rune.x, rune.y, mix(35, 105, smooth(-.04, .19, castAge)),
    { alpha: pull * (paid ? .91 : .48), rotation: -.23 + castAge * 1.8, warp: 3, time: s.time });
  orbit(rune, castAge * 7, pull * .63, "#a4ebfd", 41, 36);
  if (paid) drawEnergyTransfer(rune, bottle, castAge, pulse(castAge, .025, .14, .39, .62), "deep");
  atlas(13, bottle.x, bottle.y, 67, { alpha: pulse(castAge, .075, .21, .53, .77), rotation: Math.sin(castAge * 8) * .12 });
  if (s.scenario !== "full") {
    const heal = castAge - .43;
    const healAlpha = pulse(heal, 0, .095, .22, .65);
    atlas(14, s.x + 8, FLOOR - 57 - Math.max(0, heal) * 83, 113,
      { alpha: healAlpha, rotation: Math.sin(heal * 10) * .16, warp: 4, time: heal });
    for (let i = 0; i < 15 && heal > 0; i++) {
      const progress = tmod(heal * 1.77 + i * .083);
      particle(s.x + Math.cos(heal * 9 + i) * (45 + seed(i + 23) * 24), FLOOR - 14 - progress * 142,
        5 + seed(i + 6) * 7, heal * 2, healAlpha * Math.sin(progress * Math.PI), "heal");
    }
  }
  flight(s, 10, { origin: hand, size: 84, trailSize: .73, warp: 2, authoredAngle: -.18 });
  if (s.contact) splash(11, s.hit, age, "water", 85, { end: .24, count: 12, speed: 202, spread: 2 });
}

function drawMage(s) {
  const hand = pivot(s, .38, .55);
  const age = s.time - s.info.hit;
  const charge = pulse(s.time, .07, s.info.release - .064, s.info.release, s.info.release + .04);
  halo(hand.x, hand.y, 39 + charge * 19, "#b5e6ff", charge * .67);
  if (charge > .02) orbit(hand, -s.time * 8, charge, "#dbf6ff", 16 + charge * 7, 13, 2);
  if (!s.mode) {
    flight(s, 15, { origin: hand, size: 128, tail: "ice", trailSize: .93, authoredAngle: -.69, flutter: .018, warp: 1.75 });
    if (s.contact) splash(16, s.hit, age, "ice", 141, { size: 14, count: 23, speed: 279, spread: TAU, life: .38 });
  } else {
    flight(s, 17, { origin: hand, size: 168, tail: "ice", trailSize: .88, warp: 4, flutter: .014 });
    if (s.contact) {
      splash(16, s.hit, age, "ice", 110, { size: 12, count: 25, speed: 255, spread: TAU, end: .27 });
      waterRing(s.floorPoint, age, "#b4e7fc", 97, .48, .29);
    }
  }
}

function swampBubbles(origin, age, opacity) {
  if (opacity < .02) return;
  for (let i = 0; i < 15; i++) {
    const s = seed(i + 65);
    const progress = tmod(age * (1.25 + s * .8) + i * .13);
    const x = origin.x + (seed(i + 310) - .5) * 170 + Math.sin(progress * 5 + i) * 6;
    const y = origin.y - 5 - progress * (20 + 21 * s);
    const alpha = opacity * (1 - smooth(.7, 1, progress)) * smooth(0, .16, progress);
    particle(x, y, (9 + 7 * s) * (.44 + progress * .56), -.1, alpha, "mud");
    if (progress > .77) waterRing(pt(x, y), (progress - .77) * .5, "#b3cb74", 9, .12, .45);
  }
}

function drawSwamp(s) {
  const age = s.time - s.info.hit;
  if (!s.mode) {
    weapon(s, 20, [25, -104, -62, -169, 25, 172], "#a3ce8e");
    if (s.contact) splash(21, pt(s.hit.x + 4, s.hit.y + 25), age, "mud", 155,
      { size: 16, count: 23, speed: 311, gravity: 590, heading: 2.94, spread: 2.7 });
    return;
  }
  const mouth = pivot(s, .31, .59);
  const charge = pulse(s.time, .075, .22, s.info.release, s.info.release + .065);
  halo(mouth.x, mouth.y, 46, "#849851", charge * .68);
  const end = s.info.hit + (s.missed ? .10 : 0);
  const progress = fraction(s.info.release, end, s.time);
  anchoredJet(22, mouth, lerpPoint(mouth, s.target, easeOut(progress)), mix(57, 101, progress),
    s.time * .60, pulse(s.time, s.info.release, s.info.release + .063, end, end + .16), [.08,.90], [.89,.20]);
  if (s.contact) {
    const mud = pulse(age, 0, .19, .59, 1);
    atlas(23, s.floorPoint.x, s.floorPoint.y - 34, mix(46, 244, smooth(0, .50, age)),
      { height: mix(44, 114, smooth(0, .36, age)), alpha: mud, warp: 4.5, time: age * .49 });
    swampBubbles(s.floorPoint, age, mud);
    burst(s.floorPoint, age, "mud", 27, { size: 16, gravity: 520, speed: 320 });
  }
  if (s.defense) {
    atlas(24, s.playerX, FLOOR - 169 - Math.sin(s.time * 5) * 4, 53, { alpha: .95 });
    textBadge(`−30% DEF · ${2 - s.round} round`, s.playerX, FLOOR - 212, "#dadfa2");
  }
  if (s.scenario === "wounded" && s.round > 0 && s.round <= 2 && s.roundAge < .82) {
    const light = pulse(s.roundAge, 0, .09, .45, .82);
    const rise = s.roundAge * 1.37;
    orbit(pt(s.x, FLOOR - 41), s.roundAge * -7.5, light * .7, "#b3d974", 61, 26);
    for (let i = 0; i < 10; i++)
      particle(s.x + Math.sin(i * 2.4 + s.roundAge * 2) * 48,
        FLOOR - tmod(rise + i * .097) * 137, 8, .35, light * .83, "heal");
    if (light > .22) textBadge("+5% HP · nội tại", s.x, FLOOR - s.height - 25, "#b7ecc0");
  }
}

function drawSpirit(s) {
  const age = s.time - s.info.hit;
  const launch = pivot(s, .36, .59);
  if (!s.mode) {
    const core = flight(s, 25, { origin: launch, size: 123, height: 116, tail: "ice", arc: 17, warp: 6.0,
      spin: -1.1, authoredAngle: Math.PI, flutter: .028 });
    if (core) orbit(core, s.time * -23, .82, "#d8f7ff", 44, 31, 3);
    if (s.contact) splash(26, s.hit, age, "ice", 148, { size: 14, count: 28, speed: 287, spread: TAU });
    return;
  }
  const cast = s.time - s.info.release;
  const rune = pt(s.x, s.enemyFloor - s.height - 32);
  const opacity = pulse(cast, -.34, -.24, .14, .41);
  const sketch = smooth(-.32, -.021, cast);
  if (opacity > .002) {
    g.save();
    g.globalAlpha *= opacity * .57;
    g.strokeStyle = "#a6e4ff"; g.shadowColor = "#a6e4ff"; g.shadowBlur = 9;
    g.lineWidth = 2;
    g.beginPath(); g.ellipse(rune.x, rune.y, 73, 26, 0, -Math.PI / 2, -Math.PI / 2 + TAU * sketch); g.stroke();
    g.restore();
    atlas(27, rune.x, rune.y, 148, { height: 64, reveal: sketch, radial: true,
      rotation: Math.sin(s.time * 4) * .13, alpha: opacity });
  }
  flight(s, 17, { origin: launch, size: 130, tail: "ice", warp: 3, trailSize: .85 });
  if (s.contact) splash(26, s.hit, age, "ice", 106, { size: 9, count: 18, speed: 225, spread: TAU });
}

function drawSeaGod(s) {
  const age = s.time - s.info.hit;
  if (!s.mode) {
    weapon(s, 30, [67, -162, -28, -181, 22, 194], "#6ce8fa");
    if (s.contact) {
      burst(s.hit, age, "deep", 22, { size: 10, speed: 253, heading: 3.05, spread: 2.65 });
      if (s.from > s.energy) {
        const alpha = pulse(age, .018, .10, .20, .50);
        atlas(31, s.hit.x, s.hit.y + 30, mix(57, 129, smooth(0, .19, age)),
          { rotation: -age * 6.6, alpha, warp: 4, time: age });
        orbit(pt(s.hit.x, s.hit.y + 27), -age * 18, alpha * .72, "#78eff6", 59, 22, 3);
      }
    }
    return;
  }
  const castAge = s.time - s.info.release;
  const start = pt(s.x - 45, FLOOR - 8);
  const finish = s.missed ? pt(68, FLOOR - 174) : s.floorPoint;
  const end = s.info.hit + (s.missed ? .11 : 0);
  const reach = fraction(s.info.release, end, s.time);
  const run = lerpPoint(start, finish, reach);
  const lift = mix(24, 169, smooth(0, .72, reach));
  const swell = pulse(s.time, s.info.release - .08, s.info.release + .13, end, end + .25);
  if (swell > .004) {
    const rake = pt(run.x - 34 * smooth(0, .23, reach), run.y + 4);
    atlas(33, rake.x, rake.y, mix(87, 273, smooth(0, .78, reach)),
      { height: mix(28, 71, reach), alpha: swell * .65, rotation: .018, warp: 7, time: s.time });
    const roll = pt(run.x + 34, run.y - lift * .50);
    atlas(32, roll.x, roll.y, mix(115, 278, reach) * (1 + .024 * Math.sin(s.time * 24)),
      { height: lift * 1.39, alpha: swell, flip: true, rotation: -.066, warp: 10, time: castAge });
    for (let i = 0; i < 33; i++) {
      const r = seed(i + 87);
      const drift = tmod(castAge * (.96 + r * .46) + i * .131);
      particle(roll.x + (r - .6) * 171 - drift * (27 + r * 24),
        roll.y - lift * (.23 + .32 * r) - Math.sin(Math.PI * drift) * 24 + drift * drift * 41,
        4 + seed(i + 191) * 11, -.32, swell * Math.sin(Math.PI * drift) * .94, "foam");
    }
  }
  if (s.contact) {
    const impact = pulse(age, 0, .065, .36, .76);
    atlas(33, s.floorPoint.x, s.floorPoint.y - 23, mix(77, 351, smooth(0, .27, age)),
      { height: mix(50, 155, smooth(0, .27, age)), alpha: impact, warp: 7, time: -age });
    orbit(s.floorPoint, -age * 13, impact * .74, "#b4fafa", 111, 36, 4);
    waterRing(s.floorPoint, age - .03, "#aaf9ff", 232, .72);
    waterRing(s.floorPoint, age - .10, "#5bbbbf", 176, .61);
    burst(s.floorPoint, age, "foam", 46, { speed: 497, size: 13, life: .46, gravity: 482 });
  }
  if (s.stun) {
    const p = pt(s.playerX, FLOOR - 168);
    orbit(p, s.time * 8, .85, "#affdff", 24, 8, 2);
    for (let i = 0; i < 3; i++)
      particle(p.x + Math.cos(s.time * 8 + TAU * i / 3) * 25, p.y + Math.sin(s.time * 8 + TAU * i / 3) * 8,
        6.2, s.time, .92, "heal");
    textBadge("Choáng · 1 round", p.x, p.y - 33, "#a2f4f5");
  }
}

const drawAttack = [drawSlime, drawBeast, drawThief, drawMage, drawSwamp, drawSpirit, drawSeaGod];

const boardFrame = { x: 799, y: 106, step: 49, side: 43 };

function boardSnapshot(s) {
  const prelocked = new Set(s.scenario === "locked" ? [7, 20, 30, 33] : []);
  const vacancies = new Set(s.scenario === "gaps" ? [31, 34] : []);
  const targets = [];
  if (!s.board || !s.cast) return { prelocked, vacancies, targets, remaining: 0 };
  if (s.index === 3) {
    const choices = Array.from({ length: 36 }, (_, i) => i)
      .filter(i => !prelocked.has(i) && !vacancies.has(i))
      .sort((a, b) => seed(a + 164) - seed(b + 164));
    targets.push(...choices.slice(0, 2));
  } else {
    for (let slot = 30; slot < 36; slot++)
      if (!vacancies.has(slot)) targets.push(slot);
  }
  return { prelocked, vacancies, targets, remaining: Math.max(0, 2 - s.round) };
}

function box(x, y, w, h, fill, border, radius = 10) {
  g.beginPath();
  g.roundRect(x, y, w, h, radius);
  g.fillStyle = fill;
  g.fill();
  if (border) {
    g.strokeStyle = border;
    g.lineWidth = 1;
    g.stroke();
  }
}

function textBadge(label, x, y, color) {
  g.save();
  g.font = "600 15px 'Segoe UI', sans-serif";
  g.textAlign = "center";
  g.textBaseline = "middle";
  const width = g.measureText(label).width + 20;
  box(x - width / 2, y - 13, width, 26, "#081c2a", "#407180", 6);
  g.fillStyle = color;
  g.fillText(label, x, y - 1);
  g.restore();
}

function boardPoint(i) {
  const col = i % 6, row = Math.floor(i / 6);
  return pt(boardFrame.x + col * boardFrame.step + boardFrame.side / 2,
    boardFrame.y + row * boardFrame.step + boardFrame.side / 2);
}

function thawCracks(p, age) {
  if (!held(age, .47)) return;
  g.save();
  g.globalAlpha = pulse(age, 0, .055, .14, .31);
  g.beginPath();
  g.roundRect(p.x - 21, p.y - 21, 42, 42, 6);
  g.clip();
  g.strokeStyle = "#e3f8ff";
  g.lineWidth = 1.1;
  g.beginPath();
  g.moveTo(p.x - 18, p.y - 12);
  g.lineTo(p.x - 9, p.y - 5);
  g.lineTo(p.x - 3, p.y - 10);
  g.lineTo(p.x + 3, p.y + 6);
  g.lineTo(p.x + 13, p.y + 4);
  g.lineTo(p.x + 20, p.y + 18);
  g.moveTo(p.x + 3, p.y + 6);
  g.lineTo(p.x - 5, p.y + 11);
  g.lineTo(p.x - 11, p.y + 18);
  g.stroke();
  g.restore();
  burst(p, age, "ice", 9, { size: 6, speed: 91, gravity: 85, spread: 4.6, life: .13 });
}

function boardOverlay(s) {
  const data = boardSnapshot(s);
  const age = s.time - s.info.release;
  box(780, 57, 329, 385, "#111f2d", "#345c71", 15);
  g.font = "600 17px 'Segoe UI', sans-serif";
  g.fillStyle = "#e4f3f9";
  g.fillText("Bàn Match-3 mô phỏng", 799, 89);
  for (let i = 0; i < 36; i++) {
    const p = boardPoint(i);
    const old = data.prelocked.has(i) && s.round < 1;
    const affected = data.targets.includes(i);
    const freeze = affected && data.remaining > 0;
    const thaw = affected && !data.remaining && s.round >= 2 && s.roundAge < .51;
    box(p.x - 22, p.y - 22, 44, 44, i % 2 ? "#162b3d" : "#1b3142",
      freeze ? "#78c8fa" : old ? "#7a8d98" : "#284559", 6);
    if (data.vacancies.has(i)) continue;
    const gem = photos.gems[(i * 7 + Math.floor(i / 6) * 3) % photos.gems.length];
    const scale = 31 / Math.max(gem.width, gem.height);
    const w = gem.width * scale, h = gem.height * scale;
    g.save();
    g.globalAlpha = old ? .34 : 1;
    g.drawImage(gem, p.x - w / 2, p.y - h / 2, w, h);
    g.restore();
    if (old && !freeze) textBadge("×", p.x + 13, p.y - 12, "#b0bec7");
    if (freeze || thaw) {
      const delay = s.index === 5 ? (i % 6) * .027 : 0;
      const lock = smooth(delay + .024, delay + .23, age);
      const flare = pulse(age - delay, 0, .088, .15, .40);
      const ice = thaw ? 1 - smooth(.05, .43, s.roundAge) : 1;
      if (s.index === 3) atlas(19, p.x, p.y + 7, (47 + flare * 13) * lock,
        { height: 24, alpha: flare, rotation: .10 * Math.sin(age * 7) });
      g.save();
      g.beginPath(); g.roundRect(p.x - 21, p.y - 21, 42, 42, 6); g.clip();
      if (s.index === 3) atlas(18, p.x, p.y, 49 * lock, { alpha: ice * (.52 * lock + flare * .21) });
      if (s.index === 5) atlas(28, p.x + 2, p.y + 15, 58 * lock,
        { alpha: ice * .82 * lock, height: 34, flip: i % 2 === 1 });
      g.restore();
      if (flare > 0) burst(p, age - delay, "ice", 8, { size: 5, speed: 88, gravity: 80, spread: 4.2, life: .2, seed: i + 99 });
      if (thaw) thawCracks(p, s.roundAge);
    }
  }
  if (s.index === 5 && s.cast && s.round === 0) {
    const opacity = pulse(age, 0, .12, .27, .53);
    const descent = smooth(.01, .25, age);
    const last = boardPoint(33);
    g.save();
    g.beginPath(); g.rect(790, 103, 309, 294); g.clip();
    atlas(29, last.x - 17, mix(181, 300, descent), 87,
      { height: 170 + descent * 32, alpha: opacity * .85, warp: 2.5, time: age });
    g.restore();
  }
  g.font = "13px 'Segoe UI', sans-serif";
  g.fillStyle = "#9fb8ce";
  g.fillText(!s.cast ? "Chờ tung chiêu" : !data.remaining ? "Đã mở khóa"
    : `${data.targets.length} ô · còn ${data.remaining} round`, 799, 424);
}

function battleBackground(s) {
  const palette = s.index === 4 ? ["#15292b", "#28443b", "#111e21"]
    : s.index === 3 || s.index === 5 ? ["#142738", "#284460", "#111f2e"]
    : ["#132735", "#214553", "#101f2a"];
  const sky = g.createLinearGradient(0, 0, 0, HEIGHT);
  sky.addColorStop(0, palette[0]); sky.addColorStop(.65, palette[1]); sky.addColorStop(1, palette[2]);
  g.fillStyle = sky;
  g.fillRect(0, 0, WIDTH, HEIGHT);
  halo(s.board ? 369 : 535, 269, 460, s.tint, .095);
  g.save();
  g.fillStyle = "#0b17244a";
  g.fillRect(0, 450, WIDTH, 112);
  g.strokeStyle = "#98c7d02a";
  g.lineWidth = 1;
  for (let i = 0; i < 5; i++) {
    g.beginPath(); g.moveTo(0, 426 + 13 * i * (1 + i * .19)); g.lineTo(WIDTH, 426 + 13 * i * (1 + i * .19)); g.stroke();
  }
  for (let i = -3; i < 9; i++) {
    g.beginPath(); g.moveTo(75 + i * 154, 425); g.lineTo(-81 + i * 217, HEIGHT); g.stroke();
  }
  g.restore();
  for (let i = 0; i < 19; i++) {
    const x = 28 + seed(i + 102) * 1104;
    const base = 97 + seed(i + 75) * 245;
    g.globalAlpha = .09 + seed(i + 96) * .15;
    g.fillStyle = s.tint;
    g.beginPath(); g.arc(x, base + Math.sin(s.time * .48 + i) * 13, .9 + seed(i + 146) * 1.65, 0, TAU); g.fill();
  }
  g.globalAlpha = 1;
}

function footShadow(x, y, width) {
  g.save();
  g.fillStyle = "#050c177a";
  g.beginPath(); g.ellipse(x, y + 2, width, 14, 0, 0, TAU); g.fill();
  g.restore();
}

function statusHud(s) {
  if (s.energyUI) {
    box(30, 34, 190, 62, "#0c1b28d4", "#34576c");
    g.font = "600 13px 'Segoe UI', sans-serif";
    g.fillStyle = "#b5d4e7"; g.fillText("ENERGY PLAYER", 44, 55);
    g.font = "700 23px 'Segoe UI', sans-serif";
    g.fillStyle = "#e7ffff"; g.fillText(String(s.energy), 43, 82);
    for (let i = 0; i < 12; i++) {
      const x = 79 + 10.65 * i;
      box(x, 66, 6.8, 12, i < s.energy ? "#75e9ef" : "#2b4456", null, 2);
    }
    const signal = s.index === 6 && !s.mode ? s.info.hit : s.info.release;
    const age = s.time - signal;
    if (s.from > s.energy && held(age, .60)) {
      textBadge(`−${s.from - s.energy} Energy`, 122, 113, "#a6efff");
      if (s.index === 6 && s.mode) {
        atlas(34, 201, 67, mix(26, 71, smooth(0, .15, age)),
          { alpha: pulse(age, 0, .031, .086, .27), rotation: age * 2.1 });
        burst(pt(201, 67), age, "deep", 16, { size: 7, speed: 93, spread: TAU, gravity: 22, life: .13 });
      }
    }
  }
  if (s.index === 4 && s.scenario === "wounded") {
    const hp = Math.min(.54, .44 + s.round * .05);
    box(914, 35, 190, 61, "#182727db", "#496952");
    g.font = "600 13px 'Segoe UI', sans-serif";
    g.fillStyle = "#c1e4cd";
    g.fillText(`HP boss · ${Math.round(hp * 100)}%`, 929, 56);
    box(928, 70, 158, 8, "#283c3a", null, 4);
    box(928, 70, 158 * hp, 8, "#8ec88d", null, 4);
  }
}

function paint(s) {
  g.clearRect(0, 0, WIDTH, HEIGHT);
  battleBackground(s);
  footShadow(s.playerX, FLOOR, 53);
  footShadow(s.x, s.enemyFloor, 52 + s.height * .12);
  const age = s.time - s.info.hit;
  const hitKick = s.contact && s.round === 0 ? Math.sin(age * 39) * 6 * (1 - smooth(.065, .18, age)) : 0;
  portrait(photos.player, s.playerX + hitKick, FLOOR, 142, true);
  const gather = s.index === 0 ? pulse(s.time, .045, s.info.release - .065, s.info.release - .025, s.info.release + .065) : 0;
  const pose = photos.poses[s.index][s.mode][s.pose];
  portrait(pose, s.x, s.enemyFloor, s.height, true, 1 + gather * .20, 1 - gather * .15);
  drawAttack[s.index](s);
  if (s.board) boardOverlay(s);
  statusHud(s);
  if (s.missed && s.time >= s.info.hit) textBadge("Đạn trượt", s.playerX, FLOOR + 51, "#d0d9ea");
  if (s.index === 6 && s.mode && s.cast && s.scenario === "immune")
    textBadge("Miễn Choáng", s.playerX, FLOOR - 171, "#c5e9fa");
  if (s.index === 6 && s.mode && s.cast && s.round === 1 && s.scenario !== "immune")
    textBadge("Đã bỏ qua lượt do Choáng", s.playerX, FLOOR - 171, "#b5daec");
}

function scenariosFor(index, mode) {
  const items = [["standard", "Bình thường"]];
  if ((index === 2 || index === 4) && mode || index === 6 && !mode) items.push(["low", "Player chỉ có 3 Energy"]);
  if (index === 2 && mode) items.push(["full", "Kẻ Trộm đầy HP"]);
  if (index === 4 && mode) items.push(["wounded", "Boss dưới 50% HP"]);
  if (index === 6 && mode) items.push(["immune", "Player miễn Choáng"], ["empty", "Player có 0 Energy"]);
  if (mode && scenesWithBoard.has(index)) items.push(["locked", "Bàn có ô đã khóa"]);
  if (index === 5 && mode) items.push(["gaps", "Hàng dưới có ô trống"]);
  if (!closeCombat(index, !!mode)) items.push(["miss", "Đạn trượt"]);
  return items;
}

function roundCount(index, mode) {
  if (!mode) return 0;
  return index === 0 || index === 6 ? 1 : index === 3 || index === 4 || index === 5 ? 2 : 0;
}

function eventText(s) {
  if (!s.cast && s.time < s.info.hit) return "Lấy đà";
  if (s.index === 0 && s.mode) return s.cost ? "Chi phí skill +2 Energy" : "Chi phí skill đã về mức thường";
  if (s.board) {
    const board = boardSnapshot(s);
    return board.remaining ? `${board.targets.length} ô chịu khóa · còn ${board.remaining} round` : "Các ô đã mở khóa";
  }
  if (s.index === 2 && s.mode) {
    const energy = s.from > s.energy ? "Đã trừ 5 Energy" : "Thiếu Energy để hút";
    const heal = s.scenario === "full" ? "HP đã đầy" : "Hồi máu có nhịp riêng";
    return `${energy} · ${heal}`;
  }
  if (s.index === 4 && s.mode)
    return s.defense ? `−30% DEF · còn ${2 - s.round} round` : "Đã hết giảm DEF";
  if (s.index === 6 && s.mode)
    return `Energy ${s.energy} · ${s.stun ? "Choáng 1 round" : s.scenario === "immune" ? "Miễn Choáng" : "Đã xử lý lượt Choáng"}`;
  if (s.index === 6 && !s.mode && s.contact)
    return s.from > s.energy ? "Đã trừ 5 Energy" : "Thiếu Energy để hút";
  if (s.time >= s.info.hit) return s.missed ? "Đạn trượt" : s.time > s.info.hit + .22 ? "Dư âm" : "Va chạm";
  return s.melee ? "Áp sát" : "Đang di chuyển tới mục tiêu";
}

function options(select, entries, current) {
  select.replaceChildren(...entries.map(([value, name]) => new Option(name, value, false, String(current) === String(value))));
}

function timelineMarkers(info) {
  return [
    ["Lấy đà", info.release * .53], ["Tung chiêu", info.release + .039],
    ["Va chạm", info.hit + .052], ["Dư âm", Math.min(info.end - .07, info.hit + .33)]
  ];
}

function configure() {
  const info = attack(state.enemy, state.mode);
  $("enemy-name").textContent = enemies[state.enemy].name;
  $("attack-name").textContent = info.name;
  $("description").textContent = info.description;
  $("normal").setAttribute("aria-pressed", String(state.mode === 0));
  $("active").setAttribute("aria-pressed", String(state.mode === 1));
  const scenarios = scenariosFor(state.enemy, state.mode);
  if (!scenarios.some(([value]) => value === state.scenario)) state.scenario = "standard";
  options($("scenario"), scenarios, state.scenario);
  $("scenario-label").hidden = scenarios.length < 2;
  const count = roundCount(state.enemy, state.mode);
  state.round = Math.min(state.round, count);
  options($("round"), [[0, "Lúc áp dụng"], ...Array.from({ length: count }, (_, i) => [i + 1, `Sau round ${i + 1}`])], state.round);
  $("round-label").hidden = count < 1;
  $("timeline").max = info.end;
  $("markers").replaceChildren(...timelineMarkers(info).map(([label, time]) => {
    const b = document.createElement("button");
    b.type = "button";
    b.textContent = label;
    b.dataset.time = time;
    b.addEventListener("click", () => seek(time));
    return b;
  }));
  [...$("roster").children].forEach((button, i) => button.setAttribute("aria-current", String(state.enemy === i)));
}

function refresh() {
  const current = sceneAt(state.enemy, state.mode, state.time, state.scenario, state.round, state.roundAge);
  if (state.ready) paint(current);
  $("timeline").value = state.time;
  $("timecode").value = `${formatTime.format(state.time)} s`;
  $("event").textContent = eventText(current);
  $("play").textContent = state.playing ? "Dừng" : state.time >= current.info.end ? "Phát lại" : "Phát";
  [...$("markers").children].forEach(button =>
    button.classList.toggle("current", Math.abs(+button.dataset.time - state.time) < .045));
}

function seek(time) {
  state.playing = false;
  state.time = Math.min(attack(state.enemy, state.mode).end, Math.max(0, time));
  refresh();
}

function pick(enemy, mode) {
  cancelAnimationFrame(roundTickId);
  state.enemy = enemy;
  state.mode = mode;
  state.scenario = "standard";
  state.round = 0;
  state.roundAge = 1;
  configure();
  const info = attack(enemy, mode);
  seek(mode && scenesWithBoard.has(enemy) ? info.release + .22 : info.hit + .08);
}

let tickId = 0;
let roundTickId = 0;
let previousTick = 0;
function tick(now) {
  if (!state.playing) { tickId = 0; return; }
  if (previousTick) state.time += Math.min((now - previousTick) / 1000, .05) * state.speed;
  previousTick = now;
  const duration = attack(state.enemy, state.mode).end;
  if (state.time >= duration) {
    seek(duration);
    tickId = 0;
    return;
  }
  refresh();
  tickId = requestAnimationFrame(tick);
}

function playPause() {
  if (state.playing) {
    seek(state.time);
    return;
  }
  if (!state.ready) return;
  cancelAnimationFrame(roundTickId);
  if (state.time >= attack(state.enemy, state.mode).end || state.round !== 0) {
    state.time = 0;
    state.round = 0;
    state.roundAge = 1;
    $("round").value = "0";
  }
  state.playing = true;
  previousTick = 0;
  if (!tickId) tickId = requestAnimationFrame(tick);
  refresh();
}

function selectRound(round) {
  state.round = round;
  state.roundAge = round ? 0 : 1;
  const info = attack(state.enemy, state.mode);
  seek(round ? info.end : info.hit + .075);
  cancelAnimationFrame(roundTickId);
  if (!round) return;
  const start = performance.now();
  function drawRound(now) {
    state.roundAge = Math.min((now - start) / 1000, 1);
    refresh();
    if (state.roundAge < 1) roundTickId = requestAnimationFrame(drawRound);
  }
  roundTickId = requestAnimationFrame(drawRound);
}

function setup() {
  enemies.forEach((enemy, i) => {
    const b = document.createElement("button");
    const photo = document.createElement("img");
    photo.src = pathToFrame(i, 0, 0);
    photo.alt = "";
    b.type = "button";
    b.className = "enemy-option";
    b.append(photo, document.createTextNode(enemy.name));
    b.addEventListener("click", () => pick(i, state.mode));
    $("roster").append(b);
  });
  $("normal").addEventListener("click", () => pick(state.enemy, 0));
  $("active").addEventListener("click", () => pick(state.enemy, 1));
  $("play").addEventListener("click", playPause);
  $("restart").addEventListener("click", () => {
    seek(0);
    state.round = 0;
    state.roundAge = 1;
    $("round").value = "0";
    playPause();
  });
  $("back").addEventListener("click", () => seek(state.time - 1 / 30));
  $("forward").addEventListener("click", () => seek(state.time + 1 / 30));
  $("speed").addEventListener("change", event => { state.speed = +event.target.value; });
  $("timeline").addEventListener("input", event => seek(+event.target.value));
  $("scenario").addEventListener("change", event => {
    state.scenario = event.target.value;
    seek(state.time);
  });
  $("round").addEventListener("change", event => selectRound(+event.target.value));
  window.addEventListener("keydown", event => {
    if (event.target instanceof Element && event.target.matches("button, input, select")) return;
    if (event.code === "Space") {
      event.preventDefault();
      playPause();
    }
    if (event.code === "ArrowLeft") seek(state.time - 1 / 30);
    if (event.code === "ArrowRight") seek(state.time + 1 / 30);
  });
}

setup();

// The Unity Editor window shares this renderer to keep the visual preview in sync.
window.waterPreview = {
  get ready() { return state.ready; },
  metadata() {
    return JSON.stringify({ enemies: enemies.map(enemy => enemy.name), attacks: enemies.flatMap((_, index) =>
      [0, 1].map(mode => ({
        ...attack(index, mode), roundCount: roundCount(index, mode),
        scenarios: scenariosFor(index, mode).map(([id, label]) => ({ id, label })),
        markers: timelineMarkers(attack(index, mode)).map(([label, time]) => ({ label, time }))
      }))) });
  },
  frame(enemy, mode, time, scenario, round, roundAge) {
    const current = sceneAt(enemy, mode, time, scenario, round, roundAge);
    paint(current);
    return JSON.stringify({ image: canvas.toDataURL("image/jpeg", .94).split(",", 2)[1], caption: eventText(current) });
  }
};

loadArtwork().then(() => {
  state.ready = true;
  $("load-message").hidden = true;
  pick(0, 1);
  seek(.52);
}).catch(error => {
  $("load-message").textContent = "Không tải được sprite " + error.message;
  console.error(error);
});
