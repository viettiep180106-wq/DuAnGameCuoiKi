"use strict";

const $ = id => document.getElementById(id);
const canvas = $("stage");
const g = canvas.getContext("2d");
const W = canvas.width, H = canvas.height, FLOOR = 422, TAU = Math.PI * 2;
const assets = "../../Assets/Project/Sprite";
const names = ["Quỷ Rừng", "Bọ Rác", "Cung Thủ Ám", "Tinh Linh Hồi Máu", "Rễ Cây Ăn Thịt", "Pháp Sư Bùa Chú", "Cổ Thụ Ngàn Năm"];
const ids = ["QuyRung", "BoRac", "CungThuAm", "TinhLinhHoiMau", "ReCayAnThit", "PhapSuBuaChu", "CoThuNganNam"];
const sizes = [155, 123, 154, 144, 170, 160, 191];
const moves = [
  ["Cào móng gỗ", .29, .51, 1.12, "Ba vết cào hiện theo đường vung tay, kéo lá theo sau. Dăm gỗ tách ra ở điểm chạm rồi rơi xuống."],
  ["Tiếng Gầm Tự Nhiên", .28, .68, 1.86, "Vòng âm bật từ miệng, rồi gió cuộn dưới chân đồng minh còn sống. Kiếm xanh hướng lên báo +25% ATK trong 2 round."],
  ["Cắn lao tới", .26, .48, .98, "Hai mép hàm khép vào mục tiêu ngay sau cú lao. Bụi rêu và sạn bật sát chân rồi tan trong khoảng 0,25 giây."],
  ["Vỏ Bọc Gai Nhọn", .25, 1.04, 2.32, "Vòng gai mọc từ gốc rồi vỏ cây khóa lại. Một đòn đánh thử làm vỏ bật sáng. Khiên trong game bằng 40% Max HP và kéo dài tối đa 2 round."],
  ["Bắn tên gỗ mục", .24, .58, 1.25, "Tên đi ngang tầm mắt với đuôi lá mỏng. Chọn tình huống trúng độc để xem riêng nội tại có 15% tỉ lệ kích hoạt."],
  ["Mưa Tên Độc Mộc", .31, .91, 1.97, "Tên mồi vút lên, sau đó 7 luồng rơi dồn quanh Player. Làn độc tím xanh bung từ các điểm rơi. Combat hiện dùng một projectile gây sát thương."],
  ["Phun phấn hoa", .24, .66, 1.26, "Đám phấn lơ lửng theo cung nhẹ. Hạt vàng xanh nở ra ở điểm chạm và tản mỏng dần."],
  ["Mưa Phước Lành", .25, .73, 1.98, "Ấn lá hiện dưới chân đồng minh còn sống. Mưa sáng kéo khoảng 0,5 giây, rồi dấu cộng nổi lên ở nơi HP thực sự tăng. Hồi 15% Max HP."],
  ["Quất rễ gai góc", .25, .55, 1.66, "Rễ quất ngang và bắn dăm gỗ. Xoáy đỏ–xanh quay ngược từ Player về Rễ Cây Ăn Thịt khi nhịp hồi máu xảy ra. Code hiện hồi theo 10% ATK của đòn."],
  ["Rễ Mẹ Tái Sinh", .29, .87, 2.26, "Nôi rễ đón một đồng minh Mộc đã gục và hạt mầm nhập vào ở nhịp hồi sinh 30% HP. Sau đó đồng minh đang sống nhận thêm nhịp hồi 15%."],
  ["Phóng bùa chú tầm xa", .23, .57, 1.71, "Lá bùa giữ hướng và bay thẳng. Luồng sáng xanh ôm đầu bùa, cuộn dọc hai bên rồi thuôn thành đuôi. Khi chạm, bùa vỡ thành giấy cháy."],
  ["Bùa Chú Phục Sinh & Cương Giáp", .28, .87, 2.24, "Một đồng minh Mộc hồi sinh ở 40% HP. Màng cương giáp hiện theo trạng thái DEF +30% trong 2 round. Code hiện gán buff, chưa cộng hệ số này vào DEF tính sát thương."],
  ["Rễ ngầm đâm nát", .30, .61, 1.40, "Bụi bật lên dưới chân Player. Ngay sau đó, đất đá tách ra ở gốc khi rễ trồi từ tâm đám bụi."],
  ["Rễ Cây Phục Sinh Tối Thượng", .34, .96, 2.61, "Tán cây rọi lên đội hình. Đồng minh đã gục hồi sinh ở 50% HP, rồi cả đội nhận hồi 20% và khiên 10% Max HP Boss trong 2 round. Có mẫu riêng cho nội tại triệu hồi."]
].map(([name, release, hit, end, description]) => ({name, release, hit, end, description}));

const common = [
  [32,53,253,239],[300,50,260,253],[552,31,303,288],[868,30,220,289],[1095,48,283,266],
  [31,313,255,228],[307,330,247,198],[565,315,272,210],[839,336,278,186],[1121,315,253,253],
  [21,598,306,179],[317,575,226,207],[518,573,324,202],[840,549,270,245],[1114,565,258,206],
  [17,796,280,267],[296,796,267,248],[557,791,269,269],[817,838,300,205],[1104,790,271,267]
];
const bosses = [
  [20,22,328,302],[360,25,301,296],[675,24,320,298],[1002,32,295,280],[1305,30,285,293],
  [27,327,321,280],[349,322,306,292],[650,335,331,276],[976,336,344,277],[1314,336,276,283],
  [14,614,334,317],[346,673,332,263],[673,609,314,350],[990,646,331,289],[1333,619,269,307]
];
const ancientRootCrack = 36;
bosses[ancientRootCrack-20] = [14,810,334,121];

const art = {};
const state = {enemy: 0, mode: 1, scenario: "standard", time: 0, speed: .5, ready: false, playing: false, frame: 0};
const clamp = n => Math.min(1, Math.max(0, n));
const lerp = (a,b,t) => a + (b-a)*t;
const frac = (a,b,t) => clamp((t-a)/(b-a));
const smooth = (a,b,t) => { const u = frac(a,b,t); return u*u*(3-2*u); };
const pulse = (t,a,b,c,d) => smooth(a,b,t)*(1-smooth(c,d,t));
const ease = t => 1 - (1-t)**3;
const seed = i => { const n = Math.sin(i*117.71+22.15)*43758.5; return n-Math.floor(n); };
const repeat = t => t-Math.floor(t);
const p = (x,y) => ({x,y});
const at = (a,x,y) => p(a.x+x,a.y+y);
const mix = (a,b,t) => p(lerp(a.x,b.x,t),lerp(a.y,b.y,t));
const angle = (a,b) => Math.atan2(b.y-a.y,b.x-a.x);
const active = (t,end) => t >= 0 && t < end;

function load(path) {
  return new Promise((resolve,reject) => {
    const img = new Image(); img.onload = () => resolve(img); img.onerror = () => reject(Error(path)); img.src = path;
  });
}
const portraitPath = i => `${assets}/Enemies/Wood/Enemy_${ids[i]}.png`;
const posePath = (i, name) => `${assets}/Enemies/Animations/Wood/${ids[i]}/${name}.png`;

async function artwork() {
  [art.player,art.common,art.bosses,art.rest,art.poses,art.hit] = await Promise.all([
    load(`${assets}/Player.png`), load("WoodCommon.png"), load("WoodBosses.png"), Promise.all(ids.map((_,i) => load(portraitPath(i)))),
    Promise.all(ids.map((_,i) => Promise.all([1,2].map(mode =>
      Promise.all([0,1,2].map(frame => load(posePath(i,`Atack_${mode}_${frame}`)))))))),
    load(posePath(1,"Hit_0"))
  ]);
}

function sprite(id,point,width,o={}) {
  const opacity = o.alpha ?? 1;
  if (opacity <= .003 || width < 1) return;
  const image = id < 20 ? art.common : art.bosses;
  const [sx,sy,sw,sh] = id < 20 ? common[id] : bosses[id-20];
  const h = o.height ?? width*sh/sw;
  if (h < 1) return;
  g.save(); g.translate(point.x,point.y); g.rotate(o.rotation ?? 0); g.globalAlpha *= clamp(opacity);
  if (o.flip) g.scale(-1,1);
  const reveal = o.reveal ?? 1;
  if (reveal < .996 || o.half) {
    g.beginPath();
    if (o.half) g.rect(-width*.55, o.half < 0 ? -h*.55 : 0, width*1.1, h*.55);
    else if (o.radial) { g.moveTo(0,0); g.arc(0,0,Math.hypot(width,h),-1.8,-1.8+TAU*reveal); g.closePath(); }
    else if (o.rise) g.rect(-width*.56,h/2-h*reveal,width*1.12,h*reveal);
    else g.rect(-width/2,-h/2,width*reveal,h);
    g.clip();
  }
  if (o.warp) {
    for (let i=0;i<14;i++) {
      const bend = Math.sin(i*.69-(o.time ?? 0)*18)*Math.sin(i*Math.PI/14)*o.warp;
      g.drawImage(image,sx,sy+i*sh/14,sw,sh/14,-width/2+bend,-h/2+i*h/14,width,h/14+.3);
    }
  } else g.drawImage(image,sx,sy,sw,sh,-width/2,-h/2,width,h);
  g.restore();
}

function ground(id,point,width,o={}) {
  const rect = id < 20 ? common[id] : bosses[id-20];
  const height = o.height ?? width*rect[3]/rect[2];
  sprite(id,at(point,0,-height*.5),width,{...o,height});
}

function glow(point,r,color,alpha) {
  if (alpha <= .003 || r < 1) return;
  const grad=g.createRadialGradient(point.x,point.y,0,point.x,point.y,r);
  grad.addColorStop(0,color); grad.addColorStop(1,"transparent");
  g.save(); g.globalAlpha*=clamp(alpha); g.fillStyle=grad;
  g.fillRect(point.x-r,point.y-r,r*2,r*2); g.restore();
}

function line(a,b,color,width,alpha) {
  if (alpha <= .003) return;
  g.save(); g.globalAlpha *= clamp(alpha); g.strokeStyle=color; g.lineWidth=width; g.lineCap="round";
  g.beginPath(); g.moveTo(a.x,a.y); g.lineTo(b.x,b.y); g.stroke(); g.restore();
}

function spark(point,size,alpha,color) {
  if (alpha <= .003 || size < .8) return;
  g.save(); g.globalAlpha *= clamp(alpha); g.fillStyle=color;
  g.beginPath(); g.arc(point.x,point.y,size,0,TAU); g.fill(); g.restore();
}

function leaf(point,size,angle,alpha,color) {
  if (alpha <= .003 || size < .8) return;
  g.save(); g.translate(point.x,point.y); g.rotate(angle); g.globalAlpha*=clamp(alpha); g.fillStyle=color;
  g.beginPath(); g.moveTo(-size,0); g.quadraticCurveTo(0,-size*.83,size,0);
  g.quadraticCurveTo(0,size*.85,-size,0); g.fill(); g.restore();
}

function scatter(center,age,count,color,mode="leaf",speed=224,life=.66) {
  if (!active(age,life)) return;
  for (let i=0;i<count;i++) {
    const u=seed(i+23), t=age/(life*(.54+u*.46));
    if (t >= 1) continue;
    const a=-Math.PI*.5+(seed(i+81)-.5)*3.6;
    const x=Math.cos(a)*age*speed*(.24+u*.76);
    const y=Math.sin(a)*age*speed*(.31+u*.69) + (mode==="leaf" ? 220 : 495)*age*age/2;
    const point=p(center.x+x,Math.min(FLOOR+17,center.y+y));
    const fade=Math.sin(Math.PI*t)**.53;
    if (mode==="light") spark(point,(1+u*3.8)*(1-t*.6),fade,color);
    else leaf(point,(2.7+u*7)*(1-t*.37),a+t*(seed(i)-.5)*9,fade,color);
  }
}

function ripple(center,age,duration,radius,color,height=.31) {
  if (!active(age,duration)) return;
  const t=age/duration, r=radius*lerp(.12,1,ease(t));
  g.save(); g.globalAlpha=Math.sin(Math.PI*t**.51)*.76;
  g.strokeStyle=color; g.shadowColor=color; g.shadowBlur=13; g.lineWidth=lerp(5,1.1,t);
  g.beginPath(); g.ellipse(center.x,center.y,r,r*height,0,0,TAU); g.stroke(); g.restore();
}

function puff(center,age,color,count=17,spread=1) {
  if (!active(age,.87)) return;
  for (let i=0;i<count;i++) {
    const key=seed(11+i), t=frac(i*.006,.48+key*.36,age);
    if (t <= 0 || t >= 1) continue;
    const x=(seed(i+74)-.5)*(22+151*ease(t))*spread;
    const y=-(5+key*33*ease(t))*spread;
    glow(at(center,x,y), (8+(19+key*12)*t)*spread, color, Math.sin(Math.PI*t)*.29);
  }
}

function burst(id,center,age,width,tone="#c4e773",life=.42) {
  if (age < 0) return;
  const a=pulse(age,0,.036,.13,life), amount=lerp(.24,1,smooth(0,.18,age));
  glow(center,width*.52,tone,pulse(age,0,.037,.06,.23)*.45);
  sprite(id,at(center,0,-8*amount),width*amount,{alpha:a,warp:4,time:age});
  scatter(center,age,22,tone,id===16 || id===26 ? "light" : "chip",245,life+.16);
}

function options(index,mode) {
  const variants=[{id:"standard",label:"Mặc định"}];
  if (mode && [4,5,6].includes(index)) variants.push({id:"living",label:"Không có đồng minh đã gục"});
  if (mode && index===6) variants.push({id:"summon",label:"Nội tại · triệu hồi Bọ Rác"});
  if (!mode && index===2) variants.push({id:"poison",label:"Kích hoạt độc từ nội tại"});
  if (!mode && index===4) variants.push({id:"full",label:"Rễ Cây đang đầy HP"});
  return variants;
}

function scene(index,mode,time,scenario) {
  const move=moves[index*2+mode];
  const team=mode && [0,3,4,5,6].includes(index);
  const melee=!mode && [0,1,4,6].includes(index);
  const playerX=team ? 218 : 305;
  const home=team ? 775 : 811;
  const advance=melee ? ease(frac(.065,move.hit-.027,time))*(1-smooth(move.hit+.16,move.hit+.56,time)) : 0;
  const enemy=p(lerp(home,playerX+(index===6?165:128),advance),FLOOR);
  const cast=time-move.release, age=time-move.hit;
  const actors=[];
  if (team) {
    const slots=[{id:index===0?4:0,base:p(602,FLOOR+8),hp:.60}, {id:1,base:p(945,FLOOR+13),hp:.47},
      {id:2,base:p(1067,FLOOR-20),hp:1}];
    if ([4,5,6].includes(index) && scenario!=="living") {
      slots[0].hp=0; slots[1].hp=0;
    }
    if (index===6 && scenario==="summon") {
      slots[0].id=1;
      slots[1].hp=.47;
      slots[1].id=0;
      slots[0].summon=true;
    }
    slots.forEach((unit,i) => {
      const revive=scenario!=="summon" && unit.hp===0 && (index===6 || i===0);
      actors.push({...unit,revive,delay:i*.105});
    });
    actors.push({id:index,base:enemy,hp:index===0?.82:.74,revive:false,delay:.15,caster:true});
  }
  const muzzle=at(enemy,-42,-sizes[index]*(index===0?.65:index===2?.54:.60));
  return {index,mode,time,scenario,move,team,enemy,muzzle,actors,cast,age,playerX,
    hit:p(playerX+10,FLOOR-77), feet:p(playerX+7,FLOOR+2)};
}

function unitHp(unit,s) {
  let hp=unit.hp;
  if (unit.summon) return s.age >= .10 ? 1 : 0;
  if (unit.revive && s.age >= unit.delay+.10) hp=s.index===4?.30:s.index===5?.40:.50;
  if (hp>0 && s.age>=unit.delay+.29) {
    if (s.index===3 || s.index===4) hp+=.15;
    if (s.index===6 && s.scenario!=="summon") hp+=.20;
  }
  return clamp(hp);
}

function arena() {
  const bg=g.createLinearGradient(0,0,0,H);
  bg.addColorStop(0,"#16251d"); bg.addColorStop(.70,"#344d31"); bg.addColorStop(1,"#17281d");
  g.fillStyle=bg; g.fillRect(0,0,W,H);
  glow(p(782,305),437,"#6f9e44",.2);
  for (let i=0;i<17;i++) spark(p(48+seed(i)*1064,85+seed(i+28)*290),1+seed(i+40),.10,"#b9d68b");
  g.fillStyle="#202d22"; g.fillRect(0,FLOOR,W,H-FLOOR);
  for (let i=0;i<23;i++) {
    const x=i*W/22; line(p(lerp(W/2,x,.60),FLOOR),p(x+(x-W/2)*.48,H),"#a4b085",1,.15);
  }
  for (let i=0;i<7;i++) line(p(0,FLOOR+i*i*4.65),p(W,FLOOR+i*i*4.65),"#a4b085",1,.16);
}

function actor(image,base,height,opacity=1,stretch=1) {
  const w=height*image.width/image.height;
  g.save(); g.translate(base.x,base.y); g.scale(-stretch,1); g.globalAlpha*=clamp(opacity);
  g.drawImage(image,-w/2,-height,w,height); g.restore();
}

function footShadow(point,size,opacity=1) {
  g.save(); g.globalAlpha*=opacity; g.fillStyle="#0e1812a3";
  g.beginPath(); g.ellipse(point.x,point.y+3,size,12,0,0,TAU); g.fill(); g.restore();
}

function health(unit,s) {
  const hp=unitHp(unit,s);
  if ((s.index===0 || s.scenario==="summon") && !unit.summon) return;
  const center=at(unit.base,0,20);
  g.fillStyle="#132018"; g.beginPath(); g.roundRect(center.x-29,center.y-3,58,7,3); g.fill();
  if (hp<=0) return;
  g.fillStyle="#9ade79";
  g.beginPath(); g.roundRect(center.x-28,center.y-2,56*hp,5,2); g.fill();
}

function label(text,point,tint="#d7fbaf") {
  g.save(); g.font="600 13px Segoe UI, sans-serif"; g.textAlign="center"; g.textBaseline="middle";
  const width=g.measureText(text).width+18;
  g.fillStyle="#152418e8"; g.strokeStyle="#607c4e";
  g.beginPath(); g.roundRect(point.x-width/2,point.y-11,width,22,5); g.fill(); g.stroke();
  g.fillStyle=tint; g.fillText(text,point.x,point.y); g.restore();
}

function swing(s,id,width,twist) {
  const a=pulse(s.time,s.move.release,s.move.release+.046,s.move.hit+.029,s.move.hit+.19);
  if (a<=.003) return;
  const t=smooth(s.move.release,s.move.hit,s.time);
  const start=at(s.enemy,55,-123), control=at(s.enemy,-61,s.index===4?-178:-153), end=at(s.hit,0,19);
  const curve=f=>mix(mix(start,control,f),mix(control,end,f),f);
  const point=curve(t), theta=angle(curve(Math.max(0,t-.06)),point);
  for (let i=1;i<=19;i++) {
    const u=i/19; line(curve((i-1)*t/19),curve(u*t),"#d4c086",1+u*5,a*.54*u);
    if (i%2) leaf(at(curve(u*t),seed(i)*13,-seed(i+7)*19),2+seed(i)*6,i+t*8,a*.75,"#91c557");
  }
  sprite(id,at(point,-Math.cos(theta)*25,-Math.sin(theta)*25),width,
    {alpha:a,rotation:theta+twist,reveal:Math.min(1,.17+t),radial:true,warp:3,time:s.time});
}

function bolt(s,id,width,{arc=12,authored=0,spin=0,green="#b6df76"}={}) {
  if (s.cast<0 || s.age>=0) return;
  const t=frac(s.move.release,s.move.hit,s.time), opacity=smooth(0,.15,t);
  const curve=u=>at(mix(s.muzzle,s.hit,u),0,-Math.sin(u*Math.PI)*arc);
  for (let i=1;i<=13;i++) {
    const u=t-i*.031;
    if (u<0) continue;
    const point=at(curve(u),0,Math.sin(i*2+t*13)*i*.67);
    leaf(point,2.2+seed(i)*4,t*7+i,opacity*(1-i/14)*.91,green);
  }
  const point=curve(t);
  sprite(id,point,width*(1+Math.sin(s.time*28)*.044),
    {alpha:opacity,rotation:angle(s.muzzle,s.hit)-authored+s.cast*spin,warp:3,time:s.time});
  return point;
}

function healAt(unit,s,age,{rain=false}={}) {
  if (rain) {
    const rainAge=age+.43;
    const a=pulse(rainAge,0,.07,.36,.50);
    if (a>.003) {
      for (let i=0;i<12;i++) {
        const u=repeat(rainAge*2.55+i*.139);
        const point=at(unit.base,(seed(i+83)-.5)*109,-205+u*187);
        line(at(point,0,-27-seed(i)*14),point,"#a6ffd6",2.1,a*Math.sin(u*Math.PI)*.79);
        spark(point,2.2+seed(i)*2.3,a*Math.sin(u*Math.PI),"#c4ffe0");
      }
      sprite(17,at(unit.base,0,-128+rainAge*75),101,{alpha:a*.75,warp:3,time:rainAge});
    }
  }
  if (unit.hp>=1 || unitHp(unit,s)<=0 || !active(age,.80)) return;
  const alpha=pulse(age,0,.08,.27,.78), lift=lerp(27,153,ease(frac(0,.72,age)));
  glow(at(unit.base,0,-56),85,"#97ffa0",.31*alpha);
  sprite(19,at(unit.base,Math.sin(age*8)*10,-lift),87,{alpha,warp:5,time:age});
  for (let i=0;i<8;i++) {
    const t=repeat(age*1.6+i*.179);
    const pt=at(unit.base,Math.cos(age*8+i)*39,-28-t*127), a=alpha*Math.sin(Math.PI*t);
    line(at(pt,-4,0),at(pt,4,0),"#d2ffa7",2,a);
    line(at(pt,0,-4),at(pt,0,4),"#d2ffa7",2,a);
  }
}

function floorGlyph(unit,age,width=130) {
  const alpha=pulse(age,0,.16,.79,1.20);
  sprite(18,at(unit.base,0,2),width*lerp(.64,1,ease(frac(0,.29,age))),
    {alpha,height:49,reveal:smooth(0,.30,age),radial:true,rotation:Math.sin(age*4)*.08});
  ripple(unit.base,age-.09,.73,width*.56,"#b7eb73");
}

function drawOgre(s) {
  if (!s.mode) {
    swing(s,0,158,.91); burst(1,at(s.hit,4,13),s.age,143,"#d2b878"); return;
  }
  const wave=pulse(s.cast,0,.064,.19,.36);
  const front=at(s.muzzle,-ease(frac(0,.35,s.cast))*128,5);
  sprite(2,front,lerp(34,228,ease(frac(0,.35,s.cast))),{alpha:wave,rotation:Math.sin(s.time*7)*.13});
  ripple(front,s.cast,.36,119,"#aaff91",.87);
  for (const unit of s.actors) {
    const a=s.age-unit.delay;
    const wind=pulse(a,-.08,.11,.47,1.03);
    sprite(4,at(unit.base,0,-17),123,{height:62,alpha:wind*.91,rotation:Math.sin(s.time*14+unit.delay)*.13,warp:5,time:s.time});
    for (let i=0;i<11;i++) {
      const turn=s.time*13+i*.71;
      leaf(at(unit.base,Math.cos(turn)*(57+seed(i)*8),-16+Math.sin(turn)*17),3+seed(i)*4,turn,wind*.83,"#a3e973");
    }
    const icon=pulse(a,.025,.16,.53,.89);
    sprite(3,at(unit.base,0,-lerp(41,147,ease(frac(.02,.56,a)))),57,{alpha:icon});
  }
}

function drawBeetle(s) {
  if (!s.mode) {
    const bite=pulse(s.age,-.16,-.083,.02,.20);
    const open=1-smooth(-.135,.013,s.age);
    sprite(5,at(s.hit,14,-17-open*29),159,{half:-1,rotation:-.19-open*.34,alpha:bite});
    sprite(5,at(s.hit,5,13+open*25),159,{half:1,rotation:.16+open*.27,alpha:bite});
    burst(6,at(s.feet,6,-37),s.age,141,"#b19a68",.28);
    puff(s.feet,s.age,"#a39c62",13,.72); return;
  }
  const growth=smooth(0,.37,s.cast), hit=pulse(s.age,-.021,.055,.084,.25), fade=1-smooth(1.61,2.00,s.cast);
  const body=at(s.enemy,0,-68);
  floorGlyph({base:s.enemy},s.cast,147);
  ground(8,at(s.enemy,0,6),lerp(95,188,growth),{height:lerp(29,79,growth),alpha:growth*fade,rise:true,reveal:growth});
  ground(7,at(s.enemy,Math.sin(s.time*38)*4*hit,2),lerp(76,190,growth)*(1+hit*.064),
    {height:lerp(64,144,growth)*(1-hit*.09),alpha:growth*fade*(.68+hit*.21),rise:true,reveal:growth,warp:hit*6,time:s.time});
  sprite(9,at(body,0,-77),55*(1+hit*.28),{alpha:growth*fade});
  const strike=frac(-.18,.025,s.age);
  const tip=mix(s.hit,at(body,-38,-3),strike);
  line(at(tip,-78,18),tip,"#fce7a7",6,pulse(s.age,-.18,-.11,.03,.12));
  scatter(at(body,-47,9),s.age,20,"#9bc85d","chip",170,.46);
}

function drawArcher(s) {
  if (!s.mode) {
    bolt(s,10,151,{arc:4,authored:-.26});
    burst(11,s.hit,s.age,97,"#d194f4",.27);
    if (s.scenario==="poison") sprite(14,at(s.hit,1,-4),83,{alpha:pulse(s.age,.08,.24,.49,.91)*.75,warp:4,time:s.time});
    return;
  }
  const opening=frac(0,.31,s.cast);
  if (active(s.cast,.34)) {
    const high=at(s.muzzle,-141*opening,-239*opening);
    sprite(12,high,134,{alpha:pulse(s.cast,0,.045,.22,.34),rotation:-2.06,warp:4,time:s.time});
    line(s.muzzle,high,"#aad46b",3,pulse(s.cast,0,.06,.18,.34)*.53);
  }
  for (let i=0;i<7;i++) {
    const age=s.age+.27-i*.044;
    const t=frac(0,.26,age), x=s.playerX+(seed(i+7)-.5)*148;
    const to=p(x,FLOOR-8+seed(i)*13), from=p(x+110+seed(i+22)*86,40+seed(i+33)*60);
    if (active(age,.26)) {
      const point=mix(from,to,t*t);
      line(at(point,28,-53),point,"#8ce86e",2.5,smooth(0,.05,age));
      sprite(12,point,77+seed(i)*13,{alpha:smooth(0,.055,age),rotation:angle(from,to)+.28});
    }
    scatter(to,age-.26,10,"#c7a5f2","light",145,.30);
    puff(to,age-.26,i%2?"#9872bd":"#94c267",10,.65);
  }
  sprite(13,at(s.feet,27,-144),195,{alpha:pulse(s.age,-.25,-.13,.04,.22)*.74,warp:6,time:s.time});
  const cloud=pulse(s.age,.02,.21,.47,.96);
  sprite(14,at(s.hit,1,28-31*ease(frac(0,.8,s.age))),lerp(66,208,ease(frac(0,.53,s.age))),
    {alpha:cloud,warp:8,time:s.time});
  ripple(s.feet,s.age,.55,115,"#b8e86d");
}

function drawHealer(s) {
  if (!s.mode) {
    const center=bolt(s,15,136,{arc:27,spin:.76,green:"#bdec84"});
    if (center) glow(center,85,"#d5fa84",.31);
    burst(16,s.hit,s.age,150,"#f0f9a0",.48); return;
  }
  for (const unit of s.actors) {
    if (unit.hp<=0) continue;
    healAt(unit,s,s.age-unit.delay-.29,{rain:true});
  }
}

function revival(unit,s,boss=false) {
  const a=s.age-unit.delay;
  if (!unit.revive && !unit.summon) return;
  const alpha=pulse(a,-.35,-.12,.31,.71), rise=smooth(-.34,.08,a);
  const groundBase=at(unit.base,0,8);
  ground(boss?34:22,groundBase,lerp(72,boss?126:162,rise),{height:lerp(42,151,rise),alpha,rise:true,reveal:rise,warp:5,time:s.time});
  ripple(groundBase,a+.27,.53,91,"#daf58c");
  const core=pulse(a,-.14,.055,.19,.46);
  sprite(23,at(unit.base,0,-lerp(29,92,smooth(-.12,.33,a))),lerp(42,91,smooth(-.15,.16,a)),
    {alpha:core,rotation:a*2.1});
  glow(at(unit.base,0,-63),99,"#c4ff84",pulse(a,.02,.13,.20,.56)*.66);
  scatter(at(unit.base,0,-39),a-.04,18,"#d7ff95","light",201,.58);
}

function drawRoots(s) {
  if (!s.mode) {
    swing(s,20,195,.45); burst(21,at(s.feet,4,-23),s.age,148,"#c1a467");
    if (s.scenario==="full") return;
    const a=s.age-.09, t=ease(frac(0,.45,a));
    if (active(a,.56)) {
      const end=at(s.enemy,-12,-80), start=at(s.hit,7,8), q=at(mix(start,end,t),0,-Math.sin(t*Math.PI)*75);
      for (let i=1;i<=13;i++) {
        const u=Math.max(0,t-i*.040), point=at(mix(start,end,u),0,-Math.sin(u*Math.PI)*75+Math.sin(u*18+i)*10);
        spark(point,3.5-i*.15,(1-i/14)*pulse(a,0,.055,.33,.56),i%2?"#7aff86":"#fc747b");
      }
      sprite(24,q,lerp(68,91,t),{alpha:pulse(a,0,.065,.40,.56),rotation:-a*9,warp:3,time:a});
    }
    glow(at(s.enemy,0,-58),89,"#9ef482",pulse(a,.31,.49,.55,.75)*.58);
    return;
  }
  for (const unit of s.actors) {
    revival(unit,s);
    if (s.scenario==="living") {
      const a=s.age-unit.delay;
      const open=smooth(-.23,.14,a);
      ground(22,at(unit.base,0,9),lerp(74,118,open),{height:lerp(27,52,open),
        alpha:pulse(a,-.25,-.08,.17,.50)*.72,rise:true,reveal:open});
    }
    healAt(unit,s,s.age-unit.delay-.29);
  }
}

function talismanFlight(s) {
  if (s.cast < 0 || s.age >= 0) return;
  const progress = frac(s.move.release, s.move.hit, s.time);
  const center = mix(s.muzzle, s.hit, progress);
  const alpha = smooth(0, .10, progress);
  const length = lerp(65, 149, ease(frac(0, .53, progress)));
  const phase = s.cast * 32;

  g.save();
  g.translate(center.x, center.y);
  g.rotate(angle(s.muzzle, s.hit));
  g.globalAlpha *= alpha;
  glow(p(13, 0), 64, "#6bff80", .38);

  const wake = g.createLinearGradient(-length, 0, 48, 0);
  wake.addColorStop(0, "#63e07800");
  wake.addColorStop(.37, "#48ea6c57");
  wake.addColorStop(.75, "#68ff89bd");
  wake.addColorStop(1, "#d6ff9ceb");
  const drift = Math.sin(phase * 1.18) * 8;
  g.beginPath();
  g.moveTo(-length, drift);
  g.bezierCurveTo(-98, -3 + drift, -38, -36, 12, -33);
  g.bezierCurveTo(55, -36, 63, -6, 44, 13);
  g.bezierCurveTo(17, 44, -55, 29 - drift, -length, drift);
  g.fillStyle = wake;
  g.fill();

  // Push highlights from the leading edge toward the narrowing wake.
  for (let ribbon = 0; ribbon < 4; ribbon++) {
    const side = ribbon % 2 ? 1 : -1;
    const ribbonPoint = u => {
      const envelope = Math.sin(Math.PI * u) ** .63;
      const wave = Math.sin(u * 17 - phase + ribbon * 1.7);
      return p(34 - (length + 27) * u,
        (side * (22 - ribbon * 2.9) + wave * (6 + ribbon)) * envelope + u * drift);
    };
    for (let i = 0; i < 24; i++) {
      const t = i / 24;
      const shine = .42 + .58 * (.5 + .5 * Math.sin(t * 22 - phase + ribbon * 1.7));
      line(ribbonPoint(t), ribbonPoint((i + 1) / 24), ribbon < 2 ? "#a7ff90" : "#6df7bc",
        (ribbon < 2 ? 5 : 2.7) * (1 - t * .82), shine * (1 - t * .88) * .84);
    }
  }

  for (let i = 0; i < 14; i++) {
    const trail = repeat(s.cast * (2.7 + seed(i) * .76) + i * .137);
    const spread = Math.sin(trail * Math.PI) * (21 + seed(i + 3) * 26);
    const point = p(21 - trail * (length + 12), Math.sin(phase * .8 + i * 1.9) * spread);
    spark(point, (1.3 + seed(i + 11) * 2.1) * (1 - trail * .59),
      Math.sin(trail * Math.PI) * .77, i % 3 === 0 ? "#d7ffb5" : "#80f8ac");
  }
  g.restore();

  // Keep the authored paper sharp while the light moves independently around it.
  g.save();
  g.translate(center.x, center.y);
  g.scale(.47, .47);
  g.translate(-243, -430);
  g.globalAlpha *= alpha;
  g.beginPath();
  g.moveTo(244, 361);
  g.lineTo(318, 382);
  g.quadraticCurveTo(295, 449, 234, 498);
  g.lineTo(167, 453);
  g.quadraticCurveTo(221, 425, 244, 361);
  g.closePath();
  g.clip();
  g.drawImage(art.bosses, 166, 360, 153, 139, 166, 360, 153, 139);
  g.restore();
}

function drawShaman(s) {
  if (!s.mode) {
    talismanFlight(s);
    burst(26,s.hit,s.age,148,"#e2fa89",.47);
    const curse=pulse(s.cast,.04,.20,1.22,1.47);
    const mark=at(s.hit,0,-113+Math.sin(s.time*7)*4);
    sprite(29,mark,55*(1+pulse(s.age,.39,.49,.54,.67)*.25),{alpha:curse,rotation:s.time*.58});
    if (s.age>.26) label("Độc Mộc · 2 round",at(mark,0,-41),"#dfb8ff");
    return;
  }
  for (const unit of s.actors) {
    const a=s.age-unit.delay;
    if (unit.revive) {
      const spin=pulse(a,-.32,-.13,.34,.66);
      sprite(27,at(unit.base,-5,-62),lerp(155,109,smooth(-.19,.22,a)),{alpha:spin,height:70,rotation:a*3.8});
      glow(at(unit.base,0,-67),94,"#e7ff91",pulse(a,.035,.131,.22,.47)*.69);
      scatter(at(unit.base,0,-41),a-.06,21,"#e2ff92","light");
    }
    if (unitHp(unit,s)<=0) continue;
    const grow=smooth(-.02,.37,a);
    ground(28,at(unit.base,0,9),lerp(79,173,grow),{height:lerp(60,137,grow),alpha:grow*.64*(1-smooth(1.05,1.38,a)),rise:true,reveal:grow,warp:2,time:s.time});
  }
}

function drawAncient(s) {
  if (!s.mode) {
    const dustLead=.06, rootAge=s.age-dustLead;
    ground(31,at(s.feet,0,11),lerp(81,259,ease(frac(0,.30,s.age))),{height:105,alpha:pulse(s.age,0,.048,.20,.53),warp:6,time:s.time});
    scatter(s.feet,s.age,39,"#cbab76","chip",381,.68);
    puff(s.feet,s.age,"#af9265",23,1.35);
    ripple(s.feet,s.age,.67,165,"#d1b684",.23);
    const crack=smooth(0,.12,rootAge);
    ground(ancientRootCrack,s.feet,lerp(128,269,crack),{height:lerp(27,80,crack),
      alpha:pulse(rootAge,0,.028,.105,.25)*.88,rise:true,reveal:.91});
    const lift=smooth(0,.15,rootAge), stay=1-smooth(.26,.49,rootAge);
    ground(30,s.feet,lerp(131,236,lift),{height:244*lift*stay,alpha:lift*stay,rise:true,reveal:lift,warp:4,time:s.time});
    return;
  }
  const ritual=s.scenario!=="summon";
  const aura=pulse(s.cast,.025,.26,.79,1.37);
  if (ritual) {
    sprite(32,p(845,238),454,{height:370,alpha:aura*.92,warp:4,time:s.time});
    const columns=[607,773,947,1060];
    for (let i=0;i<columns.length;i++) {
      glow(p(columns[i],337),101,"#ccf17e",aura*(.28+Math.sin(s.time*10+i)*.095));
      for (let j=0;j<8;j++) {
        const drop=repeat(s.cast*.84+j*.179+i*.13);
        leaf(p(columns[i]+(seed(i*9+j)-.5)*107,168+drop*220),4.2+seed(j)*5,drop*TAU,
          aura*.78*Math.sin(Math.PI*drop),"#c5ed79");
      }
    }
  }
  for (const unit of s.actors) {
    revival(unit,s,true);
    if (ritual) healAt(unit,s,s.age-unit.delay-.29);
  }
  if (ritual) {
    const cover=pulse(s.age,.12,.46,.97,1.54);
    const pulseWidth=lerp(115,596,smooth(.08,.53,s.age));
    ground(33,p(828,FLOOR+27),pulseWidth,{height:205*smooth(.08,.50,s.age),alpha:cover*.56,rise:true,
      reveal:smooth(.10,.49,s.age),warp:5,time:s.time});
    for (const unit of s.actors) if (unitHp(unit,s)>0) ripple(unit.base,s.age-.16,.75,75,"#b6f680",.22);
  }
}

const painters=[drawOgre,drawBeetle,drawArcher,drawHealer,drawRoots,drawShaman,drawAncient];

function paint(s) {
  arena();
  const knock=s.mode?0:pulse(s.age,0,.047,.08,.21)*6;
  const player=p(s.playerX-knock,FLOOR);
  footShadow(player,47); actor(art.player,player,143);
  if (s.team) {
    for (const unit of s.actors) {
      if (unit.caster) continue;
      if (s.index!==0 && unitHp(unit,s)>0 && s.scenario!=="summon") floorGlyph(unit,s.cast-unit.delay,131);
      const hp=unitHp(unit,s);
      if (hp>0) {
        footShadow(unit.base,37);
        const born=unit.revive || unit.summon ? smooth(.10,.27,s.age-unit.delay) : 1;
        actor(art.rest[unit.id],at(unit.base,0,-8*(1-born)),118,.94*born);
      } else {
        footShadow(unit.base,41,.69);
        if (!unit.summon) label("Đã gục",at(unit.base,0,-23),"#c0c9b6");
      }
    }
    if (s.index!==0 && s.scenario!=="summon") floorGlyph({base:s.enemy},s.cast-.10,157);
  }
  footShadow(s.enemy,sizes[s.index]*.34);
  const pose=s.time<s.move.release?0:s.time<s.move.hit+.048?1:2;
  const beetle=s.mode && s.index===1 && active(s.age,.135);
  const body=beetle?art.hit:s.time>.055&&s.age<.43?art.poses[s.index][s.mode][pose]:art.rest[s.index];
  actor(body,s.enemy,sizes[s.index]);
  painters[s.index](s);
  if (s.team) s.actors.forEach(unit=>health(unit,s));
}

function markers(index,mode,move) {
  const heal = mode && index===3;
  const revive = mode && [4,5,6].includes(index);
  const result=[{label:"Lấy đà",time:move.release*.56},{label:"Tung chiêu",time:move.release+.10},
    {label:heal?"Hồi máu":revive?(index===6?"Kén nở":"Hồi sinh"):mode&&index===1?"Thử khiên":mode&&index===0?"Buff đội hình":"Va chạm",
      time:move.hit+(heal?.43:revive?.225:.12)}];
  if (mode && index===2) result.splice(2,0,{label:"Mưa tên",time:move.hit-.035});
  if (!mode && index===4) result.push({label:"Hút máu",time:move.hit+.36});
  result.push({label:"Dư âm",time:Math.min(move.end-.11,move.hit+(mode?.71:.31))});
  return result;
}

function caption(s) {
  if (s.cast<0) return "Lấy đà";
  if (s.mode) {
    if (s.index===0 && s.age>=0) return "+25% ATK cho đồng minh còn sống · 2 round";
    if (s.index===1) return s.age<0?"Gai dựng quanh Bọ Rác · Khiên 40% Max HP":"Khiên nhận đòn thử";
    if (s.index===2) return s.age<0?"Tên mồi và 7 luồng tên rơi":"Mây độc lan từ các điểm chạm";
    if (s.index===3) return s.age<.14?"Mưa sáng trên đồng minh còn sống":"Hồi 15% Max HP";
    if (s.index===4 && s.age>.13) return s.scenario==="living"?"Hồi máu cả đội · không có đồng minh đã gục":"Hồi sinh 1 đồng minh · tiếp sau là hồi 15%";
    if (s.index===5 && s.age>.13) return s.scenario==="living"?"Cương giáp cho đội hình đang sống":"Hồi sinh 1 đồng minh · cương giáp theo đội hình";
    if (s.index===6 && s.scenario==="summon") return s.age<.10?"Nội tại · kén triệu hồi":"1 Bọ Rác theo lệnh Spawn hiện tại";
    if (s.index===6 && s.age>.18) return s.scenario==="living"?"Hồi 20% và cấp khiên cho đội hình":"Hồi sinh tại từng vị trí đã gục · hồi 20% và cấp khiên";
  }
  if (!s.mode && s.index===4 && s.age>.18 && s.scenario!=="full") return "Xoáy hút máu về Rễ Cây";
  if (!s.mode && s.index===2 && s.age>.1 && s.scenario==="poison") return "Đã kích hoạt độc từ nội tại";
  return s.age<0?"Tung chiêu":s.age<.30?"Va chạm":"Dư âm";
}

const current=()=>scene(state.enemy,state.mode,state.time,state.scenario);

function refresh() {
  const s=current(); if (state.ready) paint(s);
  $("timeline").value=state.time; $("timecode").value=state.time.toFixed(2).replace(".",",")+" s";
  $("event").textContent=caption(s); $("play").textContent=state.playing?"Dừng":"Phát";
  [...$("markers").children].forEach(button=>button.classList.toggle("current",Math.abs(+button.dataset.time-state.time)<.05));
}

function seek(time) {
  state.playing=false; cancelAnimationFrame(state.frame);
  state.time=Math.max(0,Math.min(current().move.end,time)); refresh();
}

function play(restart=false) {
  if (!state.ready) return;
  if (state.playing && !restart) { seek(state.time); return; }
  if (restart || state.time>=current().move.end) seek(0);
  state.playing=true;
  let before=0;
  function tick(now) {
    if (!state.playing) return;
    if (before) state.time+=Math.min(.05,(now-before)/1000)*state.speed;
    before=now;
    const end=current().move.end;
    if (state.time>=end) { seek(end); return; }
    refresh(); state.frame=requestAnimationFrame(tick);
  }
  refresh(); state.frame=requestAnimationFrame(tick);
}

function select(enemy,mode) {
  state.enemy=enemy; state.mode=mode; state.scenario="standard";
  const move=current().move, variants=options(enemy,mode);
  $("enemy-name").textContent=names[enemy]; $("attack-name").textContent=move.name;
  $("description").textContent=move.description;
  $("normal").setAttribute("aria-pressed",String(!mode)); $("active").setAttribute("aria-pressed",String(!!mode));
  [...$("roster").children].forEach((button,i)=>button.setAttribute("aria-current",String(i===enemy)));
  $("scenario-label").hidden=variants.length<2;
  $("scenario").replaceChildren(...variants.map(item=>new Option(item.label,item.id)));
  $("timeline").max=move.end;
  $("markers").replaceChildren(...markers(enemy,mode,move).map(step=>{
    const button=document.createElement("button"); button.type="button"; button.dataset.time=step.time; button.textContent=step.label;
    button.addEventListener("click",()=>seek(step.time)); return button;
  }));
  seek(move.hit+.12);
}

names.forEach((name,i)=>{
  const button=document.createElement("button"), thumb=document.createElement("img");
  button.type="button"; button.className="enemy-option"; thumb.src=portraitPath(i); thumb.alt="";
  button.append(thumb,document.createTextNode(name)); button.addEventListener("click",()=>select(i,state.mode)); $("roster").append(button);
});
$("normal").addEventListener("click",()=>select(state.enemy,0)); $("active").addEventListener("click",()=>select(state.enemy,1));
$("play").addEventListener("click",()=>play()); $("restart").addEventListener("click",()=>play(true));
$("back").addEventListener("click",()=>seek(state.time-1/30)); $("forward").addEventListener("click",()=>seek(state.time+1/30));
$("speed").addEventListener("change",event=>{state.speed=+event.target.value;});
$("timeline").addEventListener("input",event=>seek(+event.target.value));
$("scenario").addEventListener("change",event=>{state.scenario=event.target.value; seek(current().move.hit+.24);});
select(0,1);

window.woodPreview={
  get ready(){return state.ready;},
  metadata(){return JSON.stringify({enemies:names,attacks:moves.map((move,i)=>({...move,roundCount:0,
    markers:markers(i>>1,i%2,move),scenarios:options(i>>1,i%2)}))});},
  frame(enemy,mode,time,scenario){
    const s=scene(enemy,mode,time,scenario); paint(s);
    return JSON.stringify({image:canvas.toDataURL("image/jpeg",.94).split(",",2)[1],caption:caption(s)});
  }
};

artwork().then(()=>{state.ready=true; $("load-message").hidden=true; refresh();}).catch(error=>{
  $("load-message").textContent="Không tải được sprite "+error.message; console.error(error);
});
