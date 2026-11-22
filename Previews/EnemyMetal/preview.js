"use strict";

const $ = id => document.getElementById(id);
const canvas = $("stage"), g = canvas.getContext("2d");
const W = canvas.width, H = canvas.height, FLOOR = 437, TAU = Math.PI * 2;
const assets = "../../Assets/Project/Sprite";
const names = ["Golem Thép", "Sát Thủ Bóng Tối", "Kỵ Sĩ Sắt", "Cung Thủ Xuyên Giáp", "Cỗ Máy Xay Thịt", "Bậc Thầy Sát Thủ", "Cỗ Máy Vĩnh Cửu"];
const ids = ["GolemThep", "SatThuBongToi", "KySiSat", "CungThuXuyenGiap", "CoMayXayThit", "BacThaySatThu", "CoMayVinhCuu"];
const bodyHeight = [170,150,158,153,181,167,228];
const moves = [
  ["Đấm thép nặng nề",.23,.38,1.04,"Sóng đấm xoay −90° theo nắm tay. Bụi bật dưới chân Golem và Player rồi tạt ra hai bên."],
  ["Pháo Đài Kim Loại",.19,.59,1.72,"Khóa vòng thép xuống sàn, nâng huy hiệu rồi khép vòm lục giác. Giảm 40% sát thương trong 2 round. Chọn tình huống chịu đòn để thử gợn lan."],
  ["Chém song kiếm",.24,.40,1.03,"Hai đường thép quét chéo lệch nhịp. Tia sáng chỉ bật tại giao điểm khi nhát thứ hai tới."],
  ["Ám Sát Bất Ngờ",.23,.55,1.35,"Bóng tím chạy dọc đường lao. Hai nhát cắt bật lên khi áp sát, rồi mảnh giáp tách khỏi Player. Game bỏ qua 20% DEF."],
  ["Đâm thương sắt",.26,.43,1.07,"Luồng khí đâm thẳng ngang tầm mũi giáo. Tia lóe nằm trên cùng đường đâm, bụi tan sát sàn."],
  ["Lá Chắn Máu Ảo",.22,.66,1.82,"Huy hiệu nổi trước ngực, 13 hạt vàng dâng từ chân. Màng hổ phách bám Kỵ Sĩ trong 2 round hoặc tới khi cạn khiên 20% Max HP."],
  ["Bắn tên thép",.23,.57,1.15,"Tách nhịp giương, buông và va chạm. Mũi tên giữ trục bay, vệt gió rút sau thân và tia lửa nhỏ lóe đúng điểm găm."],
  ["Mũi Tên Xuyên Thấu",.27,.69,1.40,"Nón khí bật ở cung. Tên và vòng gió bay qua Player một đoạn rồi nổ hình thoi sau lưng. Xuyên 30% DEF, một lần tính sát thương."],
  ["Cưa xích càn quét",.25,.45,1.21,"Lưỡi cưa tăng tốc và chạy theo quỹ đạo vung. Tia mài bắn tiếp tuyến lúc chạm, sau đó rơi theo trọng lực."],
  ["Giáp Gai Phản Pháo",.20,.60,2.03,"Ba vòng gai khép quanh Cỗ Máy. Khi chịu đòn, gai lao ngược về Player rồi bùng sóng đỏ. Giảm 30% trong 2 round, phản 25% lượng HP thực mất."],
  ["Phi dao găm độc",.23,.55,1.20,"Mũi dao chĩa thẳng vào Player. Độc lỏng chảy về mũi dao, tách giọt trong lúc bay rồi bắn tóe khi chạm."],
  ["Trảm Huyết Tử Thần",.23,.73,1.50,"Các nhát đỏ bạc lần lượt quét qua Player, chốt bằng lưỡi liềm lớn. Chảy máu 2% Max HP mỗi round, kéo dài 3 round."],
  ["Pháo nện titan",.29,.96,1.76,"Luồng sáng lớn bắn chếch lên khỏi màn hình. Piston vàng rơi thẳng từ trên đầu Player, làm nứt sàn và bắn đất đá ra ngoài."],
  ["Pháo Laser Xuyên Thấu Tối Thượng",.23,.66,2.00,"Vòng xanh tụ ở nòng, laser kéo qua sàn và cuộn vân dọc tia. Các điểm đất nổ lệch nhịp theo đường quét. Game tính một đòn, bỏ qua DEF và khiên."]
].map(([name,release,hit,end,description])=>({name,release,hit,end,description}));

const cells = [
  [56,22,361,215],[458,63,184,154],[675,12,362,250],[1066,28,181,215],[1322,26,265,215],
  [110,267,247,193],[432,251,237,214],[689,278,328,183],[1041,252,241,211],[1338,263,242,191],
  [55,530,365,123],[437,478,226,219],[705,486,283,209],[1046,472,228,236],[1346,475,245,227],
  [70,745,337,128],[476,734,158,147],[680,718,347,208],[1030,722,269,186],[1349,706,251,227],
  [48,15,294,286],[389,24,280,259],[710,24,309,272],[1043,14,279,276],[1354,54,308,220],
  [56,339,265,251],[396,339,262,249],[701,333,310,263],[1040,327,296,279],[1395,345,217,262],
  [24,619,330,290],[368,639,319,265],[701,643,331,259],[1053,635,268,269],[1326,634,349,266]
];
const art = {};
const state = {enemy:0, mode:1, scenario:"standard", time:0, round:0, roundAge:1, speed:.5, ready:false, playing:false, frame:0};
const blue = "#7deaff", silver = "#ddeeff", gold = "#ffca6d", purple = "#b79aef", red = "#ff646b", green = "#aaf951";
const clamp = u=>Math.max(0,Math.min(1,u));
const lerp = (a,b,u)=>a+(b-a)*u;
const frac = (a,b,t)=>clamp((t-a)/(b-a));
const smooth = (a,b,t)=>{const u=frac(a,b,t);return u*u*(3-2*u);};
const ease = u=>1-(1-u)**3;
const pulse = (t,a,b,c,d)=>smooth(a,b,t)*(1-smooth(c,d,t));
const p = (x,y)=>({x,y});
const at = (q,x,y)=>p(q.x+x,q.y+y);
const mix = (a,b,t)=>p(lerp(a.x,b.x,t),lerp(a.y,b.y,t));
const angle = (a,b)=>Math.atan2(b.y-a.y,b.x-a.x);
const seed = n=>{const x=Math.sin(n*127.1+17.9)*43758.54;return x-Math.floor(x);};
const repeat = u=>u-Math.floor(u);
const titanDropLead = .28;
const options = (i,m)=>!m ? [{id:"standard",label:"Tiêu chuẩn"}] : ({
  0:[{id:"standard",label:"Kích hoạt vòm"},{id:"hit",label:"Vòm chịu đòn"}],
  2:[{id:"standard",label:"Kích hoạt khiên"},{id:"hit",label:"Khiên chịu đòn"},{id:"break",label:"Khiên vỡ"}],
  4:[{id:"standard",label:"Khép giáp gai"},{id:"counter",label:"Phản đòn"}],
  6:[{id:"standard",label:"Player trúng tia"},{id:"guard",label:"Xuyên khiên Player"}]
}[i] ?? [{id:"standard",label:"Tiêu chuẩn"}]);
const rounds = (i,m)=>m ? (i===5 ? 3 : [0,2,4].includes(i) ? 2 : 0) : 0;
const load = path=>new Promise((resolve,reject)=>{
  const img = new Image();
  img.onload=()=>resolve(img); img.onerror=()=>reject(Error(path)); img.src=path;
});
const portrait = i=>`${assets}/Enemies/Metal/Enemy_${ids[i]}.png`;
const pose = (id,file)=>`${assets}/Enemies/Animations/Metal/${id}/${file}.png`;

function trim(image,removeGround=false) {
  const target=document.createElement("canvas");
  target.width=image.width; target.height=image.height;
  const ctx=target.getContext("2d",{willReadFrequently:true});
  ctx.drawImage(image,0,0);
  const pixels=ctx.getImageData(0,0,target.width,target.height);
  const rgba=pixels.data;
  let x0=target.width,y0=target.height,x1=0,y1=0;
  for(let y=0;y<target.height;y++) for(let x=0;x<target.width;x++) {
    const i=(y*target.width+x)*4;
    if(removeGround && y>target.height*.79){
      const [r,g,b]=[rgba[i],rgba[i+1],rgba[i+2]];
      // These poses include a flat, desaturated floor painted under the feet.
      if(r>=118&&r<=134&&g>=120&&g<=137&&b>=106&&b<=125&&Math.abs(r-g)<7&&g-b>4) rgba[i+3]=0;
    }
    if(rgba[i+3]<84) continue;
    x0=Math.min(x0,x); y0=Math.min(y0,y); x1=Math.max(x1,x+1); y1=Math.max(y1,y+1);
  }
  if(removeGround) ctx.putImageData(pixels,0,0);
  const source=removeGround?target:image;
  return x1>x0 && y1>y0 ? [source,x0,y0,x1-x0,y1-y0] : [source,0,0,image.width,image.height];
}

async function artwork() {
  const attackImages=ids.map(id=>Promise.all([1,2].map(mode=>{
    const frames=[0,1,2].map(n=>load(pose(id,`Atack_${mode}_${n}`)));
    return Promise.all(frames);
  })));
  const [common,bosses,player,attacks,hurt]=await Promise.all([
    load("MetalCommon.png"),load("MetalBosses.png"),load(`${assets}/Player.png`),
    Promise.all(attackImages),
    Promise.all(ids.map(id=>load(pose(id,"Hit_0"))))
  ]);
  art.atlases=[common,bosses]; art.player=trim(player);
  const hasPaintedFloor=i=>i>=5;
  art.attacks=attacks.map((modes,i)=>modes.map(frames=>frames.map(image=>trim(image,hasPaintedFloor(i)))));
  art.hurt=hurt.map((image,i)=>trim(image,hasPaintedFloor(i)));
  art.stand=art.attacks.map(modes=>modes[0][0]);
}

function sprite(id,center,width,{height,alpha=1,turn=0,reveal=1,ground=false,warp=0,t=0}={}) {
  if(alpha<.005 || width<.6 || reveal<.005) return;
  const [sx,sy,w,h]=cells[id], image=art.atlases[id<20?0:1];
  const tall=height ?? width*h/w;
  if(tall<.6) return;
  g.save(); g.translate(center.x,center.y); g.rotate(turn); g.globalAlpha*=clamp(alpha);
  const top=ground?-tall:-tall/2;
  if(reveal<.998) {
    g.beginPath(); g.rect(-width*.52,top-tall*.06,width*(reveal+.02),tall*1.12); g.clip();
  }
  if(warp>0) {
    for(let i=0;i<10;i++) {
      const sway=Math.sin(i*.8-t*33)*warp*Math.sin(i*Math.PI/9);
      g.drawImage(image,sx,sy+i*h/10,w,h/10,-width*.5+sway,top+i*tall/10,width,tall/10+.36);
    }
  } else g.drawImage(image,sx,sy,w,h,-width*.5,top,width,tall);
  g.restore();
}

function line(a,b,color,width,alpha) {
  if(alpha<.005) return;
  g.save(); g.strokeStyle=color; g.lineWidth=width; g.lineCap="round"; g.globalAlpha*=clamp(alpha);
  g.beginPath(); g.moveTo(a.x,a.y); g.lineTo(b.x,b.y); g.stroke(); g.restore();
}
function glow(q,r,color,alpha) {
  if(alpha<.005 || r<1) return;
  const fill=g.createRadialGradient(q.x,q.y,0,q.x,q.y,r);
  fill.addColorStop(0,color); fill.addColorStop(1,color+"00");
  g.save(); g.globalAlpha*=clamp(alpha); g.fillStyle=fill; g.fillRect(q.x-r,q.y-r,r*2,r*2); g.restore();
}
function ring(q,r,ratio,color,alpha,width=2,turn=0) {
  if(alpha<.005 || r<1) return;
  g.save(); g.strokeStyle=color; g.globalAlpha*=clamp(alpha); g.lineWidth=width;
  g.beginPath(); g.ellipse(q.x,q.y,r,r*ratio,turn,0,TAU); g.stroke(); g.restore();
}
function wave(q,age,span,r,color,flat=.22) {
  const u=frac(0,span,age);
  ring(q,r*lerp(.13,1,ease(u)),flat,color,pulse(age,0,.026,.09,span)*.73,lerp(3.5,.8,u));
}
function flash(id,q,age,size,color,span=.29,turn=0) {
  const a=pulse(age,0,.024,.077,span);
  glow(q,size*.64,color,a*.5);
  sprite(id,q,size*lerp(.29,1,ease(frac(0,.075,age)))*(1-.3*smooth(.095,span,age)),{alpha:a,turn:turn+age*.65});
}
function sparks(q,age,{count=16,color=gold,heading=-Math.PI/2,spread=2.6,speed=325,gravity=590,life=.46}={}) {
  if(age<=0||age>=life) return;
  for(let i=0;i<count;i++){
    const ttl=life*lerp(.47,1,seed(i+17)), u=age/ttl;
    if(u>=1) continue;
    const a=heading+spread*(seed(i+5)-.5), v=speed*lerp(.36,1,seed(i+41));
    const head=at(q,Math.cos(a)*v*age,Math.sin(a)*v*age+gravity*age*age*.5);
    const tail=at(head,-Math.cos(a)*v*.033,(-Math.sin(a)*v-gravity*age)*.033);
    const light=Math.min(1,age*62)*(1-u);
    line(tail,head,color,lerp(1,3.2,seed(i)),light);
    glow(head,lerp(2,6,seed(i+9)),color,light*.64);
  }
}
function dustCloud(q,age,size,heading=1) {
  if(age<=0||age>.53) return;
  for(let i=0;i<7;i++){
    const t=age-i*.015;
    const u=frac(0,.42,t);
    sprite(1,at(q,heading*(i*6+52*seed(i+20)*u)-(i%2)*14,-(6+i*2)*u),size*lerp(.21,.53,u),{
      alpha:pulse(t,0,.032,.10,.40)*(.33+.17*seed(i)),ground:true,turn:heading*(seed(i)-.5)*.20});
  }
}
function seekPath(a,b,u,id,width,opacity,{turn=0,warp=0,t=0}={}) {
  sprite(id,mix(a,b,u),width,{alpha:opacity,turn:angle(a,b)+turn,warp,t});
}
function poseBody(image,base,height) {
  const [atlas,x,y,w,h]=image, width=height*w/h;
  g.save(); g.translate(base.x,base.y);
  g.drawImage(atlas,x,y,w,h,-width*.5,-height,width,height); g.restore();
}
function shadow(q,r) {
  g.fillStyle="#0c122290";g.beginPath();g.ellipse(q.x,q.y+1,r,12,0,0,TAU);g.fill();
}
function arena() {
  const bg=g.createLinearGradient(0,0,0,H);
  bg.addColorStop(0,"#17212f");bg.addColorStop(.74,"#26384a");bg.addColorStop(1,"#162436");
  g.fillStyle=bg;g.fillRect(0,0,W,H);
  glow(p(620,287),506,"#7195b5",.15); glow(p(129,365),226,"#4c75ad",.14);
  g.fillStyle="#172333";g.fillRect(0,FLOOR,W,H-FLOOR);
  for(let i=0;i<17;i++) {
    const x=(i-1)*W/14;
    line(p(lerp(W*.50,x,.65),FLOOR),p(x,H),"#7d9bb5",1,.16);
  }
  for(let i=0;i<7;i++) {
    const y=FLOOR+i*i*3.7;
    line(p(0,y),p(W,y),"#7d9bb5",1,.14);
  }
}

function scene(index,mode,time,scenario="standard",round=0,roundAge=1) {
  const move=moves[index*2+mode], age=time-move.hit, cast=time-move.release;
  const guard=mode && [0,2,4].includes(index);
  const melee=!guard && ([0,1,2,4].includes(index) || index===5&&mode);
  const player=p(817,FLOOR);
  const home=p(269,FLOOR);
  const gap=[114,93,152,0,151,100,147][index];
  const close=p(player.x-gap,FLOOR);
  const dashEnd=move.hit-(index===5&&mode?.29:.01);
  const dashStart=move.release-(mode&&index===1?0:.13);
  const approach=melee ? ease(frac(dashStart,dashEnd,time))*(1-smooth(move.hit+.14,move.end-.06,time)) : 0;
  const enemy=mix(home,close,approach), h=bodyHeight[index];
  const hit=p(player.x-14,FLOOR-83);
  const source=at(enemy,[60,52,96,53,73,46,71][index],-h*[.53,.58,.44,.55,.44,.58,.43][index]);
  const trigger=move.hit+(index===4?.33:.44);
  const duration=rounds(index,mode);
  const guardHit=guard && scenario!=="standard" ? (round===1 ? roundAge-.09 : round===0 ? time-trigger : -5) : -5;
  const broken=index===2&&scenario==="break" && (round>=1 ? round>=2 || roundAge>.14 : guardHit>.16);
  const keep=round===0 || round<duration;
  return {index,mode,time,scenario,round,roundAge,move,age,cast,guard,guardHit,broken,keep,home,close,enemy,player,hit,source,h};
}

function body(s) {
  const {index,mode,age,time,guardHit,round,move}=s;
  if(guardHit>=0 && guardHit<.13) return art.hurt[index];
  if(round || time<.06 || age>.37) return art.stand[index];
  if(index===6&&!mode) {
    const frame=s.cast<.085 ? 0 : s.cast<.25 ? 1 : 2;
    return art.attacks[index][mode][frame];
  }
  const striking=index===6&&mode ? s.age>=-.015 : time>=move.release+(index===1&&mode?.16:.03);
  const frame=!striking ? 0 : age<.10?1:2;
  return art.attacks[index][mode][frame];
}

function golem(s) {
  if (!s.mode) {
    const u=ease(frac(-.065,.095,s.age));
    const contact=mix(s.source,s.hit,u*.82);
    const force=pulse(s.age,-.065,.025,.058,.24);
    const golemDust=at(s.close,-6,6);
    const playerDust=at(s.player,8,6);
    sprite(0,contact,lerp(51,138,u),{
      height:lerp(31,115,u),alpha:force,turn:-Math.PI/2});
    wave(contact,s.age,.29,76,silver,.32);
    dustCloud(golemDust,s.age+.022,145,-1.15);
    dustCloud(playerDust,s.age+.012,150,1.16);
    sparks(at(contact,0,13),s.age+.016,{count:15,color:"#c2bcae",heading:-.32,spread:1.3,speed:217,life:.33});
    return;
  }
  const seal=pulse(s.cast,0,.18,.64,1.15);
  const lock=smooth(-.30,.11,s.age);
  sprite(4,at(s.enemy,0,5),lerp(94,246,lock),{height:lerp(28,86,lock),alpha:seal,ground:true,turn:Math.sin(s.cast*4)*.048});
  ring(s.enemy,116*lock,.23,blue,seal*.55);
  const raise=smooth(-.34,.11,s.age);
  const badgeAt=at(s.enemy,0,lerp(-71,-197,raise));
  sprite(3,badgeAt,lerp(37,77,raise)+14*pulse(s.age,-.09,.055,.14,.33),{alpha:pulse(s.age,-.35,-.14,.27,.61)});
  const dome=lock*(s.keep?1:1-smooth(0,.33,s.roundAge));
  const flash=pulse(s.guardHit,0,.038,.10,.45);
  sprite(2,at(s.enemy,0,9),lerp(124,322,dome)*(1+flash*.055),{
    height:lerp(63,204,dome),alpha:dome*(.56+flash*.37),ground:true,warp:flash*3,t:s.time});
  if (s.guardHit>0) {
    flashAtShield(at(s.enemy,100,-86),s.guardHit,blue,46);
    wave(s.enemy,s.guardHit,.45,159,blue);
  }
}

function flashAtShield(q,age,color,size) {
  flash(6,q,age,size,color,.25);
  const radius=lerp(14,95,ease(frac(0,.37,age)));
  ring(q,radius,.85,color,pulse(age,0,.035,.09,.37)*.82,lerp(3,1,frac(0,.37,age)));
  sparks(q,age,{count:13,color,heading:-.5,gravity:300,speed:260});
}

function doubleCut(q,age,id,width,color,turn) {
  const u=smooth(-.075,.068,age);
  const a=pulse(age,-.077,-.027,.085,.235);
  sprite(id,at(q,lerp(-53,8,u),0),width,{alpha:a,reveal:u,turn,warp:1.6,t:age});
  for(let i=0;i<2;i++) {
    const t=age-i*.041;
    const x=smooth(-.065,.06,t), light=pulse(t,-.067,-.017,.06,.139);
    const start=at(q,-width*.61,i?-width*.35:width*.35);
    const end=at(q,width*.60,i?width*.34:-width*.34);
    line(mix(start,end,Math.max(0,x-.49)),mix(start,end,x),color,lerp(6,1,x),light*.84);
  }
}

function shadowAssassin(s) {
  if (!s.mode) {
    doubleCut(s.hit,s.age,5,150,silver,-.08);
    flash(6,s.hit,s.age-.025,86,silver,.23);
    sparks(s.hit,s.age-.026,{count:9,color:silver,heading:-1,life:.28});
    return;
  }
  const dash=pulse(s.cast,0,.063,.25,.43);
  const trailStart=at(s.home,3,-86), tip=at(s.enemy,49,-87);
  if(dash>.005 && tip.x>trailStart.x+18) {
    const width=tip.x-trailStart.x;
    sprite(7,mix(trailStart,tip,.50),width,{height:lerp(54,96,width/500),alpha:dash*.82,warp:8,t:s.time});
    for(let i=0;i<10;i++) {
      const along=repeat(seed(i)+s.cast*1.75);
      const q=at(mix(trailStart,tip,along),0,Math.sin(i*7+s.cast*15)*31);
      line(at(q,-42*seed(i+4),0),q,purple,1+seed(i+8)*2,dash*.37*(1-along));
    }
  }
  doubleCut(s.hit,s.age,8,184,purple,-.12);
  flash(6,s.hit,s.age-.012,94,purple,.22);
  const split=at(s.hit,16,-4);
  sprite(9,split,lerp(47,187,ease(frac(.022,.33,s.age))),{
    alpha:pulse(s.age,.018,.065,.16,.43),turn:.18+s.age*.6});
  sparks(split,s.age-.03,{count:22,color:gold,heading:-.73,speed:420,gravity:730});
}

function knight(s) {
  if(!s.mode) {
    const u=smooth(-.14,.02,s.age), a=pulse(s.age,-.14,-.05,.056,.27);
    const contact=p(s.hit.x,s.source.y);
    const tip=at(contact,-Math.max(0,contact.x-s.source.x)*(1-u),0);
    const w=lerp(46,176,u);
    sprite(10,at(tip,-w*.47,0),w,{height:lerp(19,45,u),alpha:a});
    line(at(tip,-w*.79,0),tip,silver,3.2,a*.70);
    flash(11,contact,s.age,82,"#fff0d4",.27);
    dustCloud(at(s.player,-16,0),s.age-.04,96,1);
    return;
  }
  const build=smooth(-.29,.135,s.age);
  const hit=pulse(s.guardHit,0,.04,.09,.31);
  const lifetime=s.keep ? 1 : 1-smooth(0,.29,s.roundAge);
  const integrity=s.broken ? (s.guardHit<.16 ? 0 : 1-smooth(.16,.30,s.guardHit)) : 1;
  const shield=build*lifetime*integrity;
  const emblem=pulse(s.age,-.38,-.14,.19,.59);
  sprite(13,at(s.enemy,0,lerp(-62,-153,smooth(-.33,.24,s.age))),lerp(38,84,build),{alpha:emblem});
  const rise=pulse(s.cast,.02,.16,.60,.90);
  sprite(14,at(s.enemy,0,3),149,{height:152,alpha:rise*.45,ground:true,warp:5,t:s.cast});
  for(let i=0;i<13;i++) {
    const u=repeat(s.cast*1.7+seed(i+11));
    const q=at(s.enemy,(seed(i)-.5)*132+Math.sin(u*5+i)*10,4-u*189);
    glow(q,3.5+seed(i+1)*6,gold,rise*Math.sin(u*Math.PI)*.79);
    line(at(q,0,9+seed(i+4)*11),q,gold,1.5,rise*Math.sin(u*Math.PI)*.63);
  }
  sprite(12,at(s.enemy,0,-79),lerp(101,216,shield)*(1+hit*.062),{
    height:lerp(96,180,shield),alpha:shield*(.57+hit*.36),warp:hit*3,t:s.time});
  if(s.guardHit>0) flashAtShield(at(s.enemy,79,-87),s.guardHit,gold,51);
  if(s.broken) {
    const t=s.guardHit-.16;
    sprite(9,at(s.enemy,32,-74),lerp(83,224,ease(frac(0,.25,t))),{alpha:pulse(t,0,.034,.11,.43),turn:.4});
    sparks(at(s.enemy,25,-75),t,{count:26,color:gold,heading:0,spread:4,gravity:420,life:.54});
  }
}

function archer(s) {
  if(!s.mode) {
    const u=frac(s.move.release,s.move.hit,s.time);
    const tail=pulse(s.cast,0,.043,.31,.37)*(1-smooth(-.027,.044,s.age));
    const fly=mix(s.source,at(s.hit,-50,0),u);
    line(at(fly,-118*Math.min(1,u*4),8),fly,"#a8d6e7",2.5,tail*.67);
    seekPath(s.source,at(s.hit,-50,0),u,15,118,tail,{warp:2.5,t:s.time});
    flash(16,s.hit,s.age,62,"#ffe1b1",.21);
    sparks(s.hit,s.age,{count:9,speed:245,life:.28,color:"#ffd8a1"});
    return;
  }
  const rear=at(s.hit,105,-6), muzzle=at(s.source,23,0);
  glow(s.source,60,blue,pulse(s.cast,-.21,-.065,.02,.11)*.71);
  const push=smooth(0,.17,s.cast);
  sprite(18,at(muzzle,72*push,-3),lerp(49,163,push),{height:lerp(36,108,push),alpha:pulse(s.cast,0,.022,.077,.28),warp:3,t:s.cast});
  wave(at(s.source,19,0),s.cast,.31,68,blue,.67);
  const u=frac(s.move.release,s.move.hit+.12,s.time);
  const flight=pulse(s.cast,0,.044,s.move.hit-s.move.release+.084,s.move.hit-s.move.release+.172);
  const center=mix(s.source,at(rear,-62,0),u);
  line(at(center,-163*u,0),at(center,47,0),blue,lerp(3,9,u),flight*.53);
  seekPath(s.source,at(rear,-62,0),u,17,162,flight,{warp:4,t:s.time});
  for(let i=0;i<3;i++) {
    const t=repeat(s.cast*4+i/3);
    ring(at(center,57-t*121,0),20+t*12,.45,blue,flight*Math.sin(t*Math.PI)*.76,2.5,Math.PI/2);
  }
  sparks(s.hit,s.age,{count:10,color:blue,heading:0,speed:290,life:.28});
  flash(19,rear,s.age-.12,158,blue,.32);
  sparks(rear,s.age-.12,{count:24,color:blue,heading:0,speed:430,life:.43});
}

function grinder(s) {
  if(!s.mode) {
    const u=smooth(-.17,.075,s.age);
    const q=mix(at(s.source,-1,15),at(s.hit,-26,16),u);
    q.y-=Math.sin(Math.PI*u)*48;
    const a=pulse(s.age,-.19,-.112,.10,.315), rotation=s.age*25+Math.max(0,s.cast)**2*36;
    sprite(20,q,161,{alpha:a*.23,turn:rotation-.52});
    sprite(20,q,135,{alpha:a,turn:rotation});
    sprite(21,at(s.hit,18,-18),lerp(61,186,ease(frac(0,.24,s.age))),{
      alpha:pulse(s.age,0,.031,.077,.27),turn:.57});
    sparks(at(s.hit,-9,1),s.age,{count:36,heading:-.91,spread:1.64,speed:568,gravity:1050,life:.54});
    return;
  }
  const growth=smooth(-.36,.095,s.age);
  const a=growth*(s.keep?1:1-smooth(0,.31,s.roundAge));
  const shock=pulse(s.guardHit,0,.032,.105,.41);
  wave(s.enemy,s.cast,.62,144,silver);
  sprite(22,at(s.enemy,-9,5),lerp(116,273,growth)*(1+shock*.048),{
    height:lerp(89,211,growth),alpha:a*(.81+.18*shock),ground:true,turn:shock*.028});
  if(s.guardHit<=0 || !s.keep) return;
  const from=at(s.enemy,83,-92), to=at(s.hit,4,3);
  const u=frac(.06,.31,s.guardHit);
  const shoot=pulse(s.guardHit,.04,.083,.29,.367);
  flashAtShield(from,s.guardHit,"#fba67b",59);
  for(let i=0;i<5;i++){
    const k=Math.max(0,u-i*.096);
    const tip=at(mix(from,to,k),0,Math.sin(k*Math.PI)*(i-2)*28);
    line(at(tip,-86,0),tip,"#e78879",3,shoot*.49);
    sprite(23,tip,lerp(43,68,k)*(1-i*.12),{alpha:shoot*(1-i*.16),turn:.11+i*.44});
  }
  const age=s.guardHit-.31;
  sprite(24,at(s.hit,5,18),lerp(70,227,ease(frac(0,.24,age))),{alpha:pulse(age,0,.032,.11,.34),ground:true});
  wave(s.player,age,.44,153,red);
  sparks(s.hit,age,{count:22,color:red,heading:-.53,life:.44});
}

const daggerTip=p(201,59);
const daggerAngle=angle(p(82,183),daggerTip);
const daggerOutline=[
  [201,59],[179,62],[168,76],[155,73],[133,84],[132,105],[121,114],[119,128],
  [91,155],[70,161],[61,179],[64,192],[77,203],[93,200],[104,188],[108,170],
  [131,149],[143,143],[159,137],[168,133],[180,105]
];

function maskedSprite(image,rect,outline,pivot,center,turn,scale,opacity,outside=false) {
  if(opacity<.005) return;
  const [x,y,width,height]=rect;
  g.save();
  g.translate(center.x,center.y); g.rotate(turn); g.scale(scale,scale); g.translate(-pivot.x,-pivot.y);
  g.globalAlpha*=clamp(opacity);
  g.beginPath();
  if(outside) g.rect(0,0,width,height);
  outline.forEach(([px,py],i)=>i ? g.lineTo(px,py) : g.moveTo(px,py));
  g.closePath(); g.clip("evenodd");
  g.drawImage(image,x,y,width,height,0,0,width,height);
  g.restore();
}

function flyingDagger(source,target,cast,progress,opacity) {
  if(opacity<.005) return;
  const tip=mix(source,target,progress);
  const heading=angle(source,target);
  g.save();
  g.translate(tip.x,tip.y); g.rotate(heading);
  glow(p(-49,0),55,"#7bef36",opacity*.25);
  for(let i=0;i<12;i++) {
    const age=repeat(cast*7.8+seed(i+31));
    const reach=progress*123*age;
    const origin=-51-37*seed(i+13);
    const x=origin-reach,y=(seed(i)-.5)*(10+reach*.53)+Math.sin(cast*38+i)*3;
    const wet=opacity*Math.sin(age*Math.PI)*(1-age);
    line(p(x-8-reach*.12,y*1.1),p(x,y),green,lerp(2.5,.7,age),wet*.87);
    glow(p(x,y),lerp(5,2.2,age),green,wet*.56);
  }
  g.restore();

  maskedSprite(art.atlases[1],cells[25],daggerOutline,daggerTip,tip,heading-daggerAngle,.79,opacity);

  g.save();
  g.translate(tip.x,tip.y); g.rotate(heading);
  for(let lane=0;lane<3;lane++) {
    const flow=s=>p(-78+s*80,(lane-1)*Math.sin(Math.PI*s)*13+
      Math.sin(s*8-cast*39+lane*1.8)*Math.sin(Math.PI*s)*2.7);
    for(let j=0;j<15;j++) {
      const u=j/15;
      const slick=.38+.44*(.5+.5*Math.sin(u*15-cast*40+lane));
      const width=Math.sin(Math.PI*(u*.88+.075))*(lane===1?3.2:5.1);
      line(flow(u),flow((j+1)/15),lane===1?"#e1ff9c":"#89ed38",width,opacity*slick);
    }
    const t=repeat(cast*6.9+lane*.31);
    const bead=flow(t);
    line(at(bead,-8,0),bead,"#dbff8e",3,opacity*Math.sin(t*Math.PI));
    glow(bead,8,green,opacity*.62*Math.sin(t*Math.PI));
  }
  glow(p(1,0),14,green,opacity*.45);
  g.restore();
}

function masterAssassin(s) {
  if(!s.mode) {
    const u=frac(s.move.release,s.move.hit,s.time);
    const a=pulse(s.cast,0,.026,.31,.38)*(1-smooth(0,.038,s.age));
    flyingDagger(s.source,s.hit,s.cast,u,a);
    sprite(26,s.hit,lerp(47,125,ease(frac(0,.23,s.age))),{
      alpha:pulse(s.age,0,.04,.13,.40),turn:s.age*.63,warp:3,t:s.age});
    sparks(s.hit,s.age,{count:17,color:green,heading:-.74,spread:2.8,gravity:680,speed:280});
    return;
  }
  const sequence=[[-.41,-.86],[-.32,.55],[-.23,-.24],[-.125,.85]];
  sequence.forEach(([start,turn],i)=>{
    const cut=s.age-start;
    const q=at(s.hit,(i-1.5)*19,-11+i*8);
    const a=pulse(cut,0,.037,.10,.22), u=smooth(0,.105,cut);
    sprite(27,q,186+i*15,{alpha:a,turn,reveal:u});
    const a1=at(q,-106,Math.sin(turn)*-93), b1=at(q,106,Math.sin(turn)*93);
    line(mix(a1,b1,Math.max(0,u-.49)),mix(a1,b1,u),"#ffe0dd",lerp(5,1,u),a*.78);
  });
  const finish=smooth(-.035,.13,s.age);
  sprite(28,at(s.hit,lerp(-56,44,finish),-6),lerp(216,332,finish),{
    height:lerp(144,205,finish),alpha:pulse(s.age,-.042,.04,.13,.39),turn:lerp(-.45,.21,finish),reveal:finish});
  flash(6,s.hit,s.age,87,red,.21,.23);
  sparks(s.hit,s.age,{count:28,color:"#e88983",heading:-.72,speed:480,gravity:740});
  const on=s.round ? (s.round<3?1:1-smooth(.27,.64,s.roundAge)) : smooth(.11,.25,s.age);
  const tick=s.round?pulse(s.roundAge,0,.11,.25,.50):0;
  const mark=at(s.player,6,-174+Math.sin((s.round?s.roundAge:s.time)*9)*3);
  glow(mark,36+tick*17,"#d84156",on*(.28+tick*.45));
  sprite(29,mark,42+tick*13,{alpha:on*.9,warp:tick*1.7,t:s.roundAge});
  if (s.round) sparks(at(mark,0,9),s.roundAge-.05,{color:red,count:6,heading:1.57,gravity:230,speed:100,life:.42});
}

const titanPistonBase=p(166,235);
const titanPistonOutline=[
  [119,116],[120,100],[129,87],[139,78],[153,73],[166,73],[177,78],[187,88],[191,97],
  [204,97],[214,102],[217,151],[229,158],[239,169],[242,215],[231,226],[205,232],
  [169,237],[128,231],[105,222],[90,209],[89,172],[97,160],[113,151],[114,123]
];

function titanWake(path,progress,age,width,opacity,reach) {
  const first=Math.max(0,progress-reach);
  if(progress-first<.008 || opacity<.005) return;
  const tail=path(first), tip=path(progress);
  const left=[], right=[];
  for(let i=0;i<=16;i++) {
    const u=i/16,x=lerp(first,progress,u);
    const q=path(x);
    const turn=angle(path(Math.max(0,x-.008)),path(Math.min(1,x+.008)))+Math.PI/2;
    const radius=width*Math.sin(u*Math.PI*.69)*(1+.10*Math.sin(u*22-age*40));
    const dx=Math.cos(turn)*radius, dy=Math.sin(turn)*radius;
    left.push(at(q,dx,dy)); right.push(at(q,-dx,-dy));
  }
  const fill=g.createLinearGradient(tail.x,tail.y,tip.x,tip.y);
  fill.addColorStop(0,"#2ac9ff00"); fill.addColorStop(.36,"#2abfff48");
  fill.addColorStop(.78,"#7deaffad"); fill.addColorStop(1,"#e1ffffd9");
  g.save();
  g.fillStyle=fill; g.globalAlpha*=opacity;
  g.beginPath(); g.moveTo(left[0].x,left[0].y);
  for(const q of left.slice(1)) g.lineTo(q.x,q.y);
  for(const q of right.reverse()) g.lineTo(q.x,q.y);
  g.closePath(); g.fill();
  g.restore();

  for(let i=0;i<15;i++) {
    const u=repeat(age*5.2+seed(i+18));
    const ahead=lerp(first,progress,u), behind=lerp(first,progress,Math.max(0,u-.29));
    const a=path(behind), b=path(ahead);
    const turn=angle(a,b)+Math.PI/2, spread=(seed(i+47)-.5)*width*1.76;
    const d=Math.cos(turn)*spread, e=Math.sin(turn)*spread;
    line(at(a,d*.42,e*.42),at(b,d,e),i%3===0?"#f0ffff":blue,
      lerp(1.2,5.4,seed(i+5)),opacity*Math.sin(u*Math.PI)*.79);
  }
}

function titanSlam(s) {
  const impact=at(s.player,0,4);
  const muzzle=at(s.enemy,-53,-s.h*.87);
  const launch=frac(0,.32,s.cast);
  const launchPath=t=>at(muzzle,66*t+75*t*t,-(muzzle.y+78)*(.55*t+.45*t*t));
  const head=launchPath(launch);
  const power=pulse(s.cast,-.21,-.055,.07,.21);
  glow(muzzle,76,blue,power*.78);
  const recoil=pulse(s.cast,0,.027,.055,.19);
  ring(muzzle,lerp(32,81,ease(frac(0,.19,s.cast))),.43,blue,recoil*.87,3.2,.30);
  const charge=pulse(s.cast,-.23,-.10,-.012,.052);
  sprite(33,muzzle,lerp(113,68,smooth(-.24,0,s.cast)),{alpha:charge*.85,turn:s.cast*6});
  const ascent=pulse(s.cast,0,.040,.22,.40);
  titanWake(launchPath,launch,s.cast,42,ascent,.69);
  glow(head,80,blue,ascent*.69);
  const direction=angle(launchPath(Math.max(0,launch-.045)),head);
  sprite(33,head,104,{height:78,alpha:ascent,turn:direction,warp:2.8,t:s.cast});
  sparks(muzzle,s.cast,{count:19,color:blue,heading:-1.29,spread:1.45,speed:475,gravity:140,life:.34});

  const warning=pulse(s.age,-.36,-.235,-.076,.012);
  ring(at(impact,0,-4),lerp(117,58,smooth(-.36,-.02,s.age)),.24,blue,warning*.69,2.8);
  const fall=frac(-titanDropLead,0,s.age);
  const fallPath=t=>at(impact,0,-(impact.y+57)*(1-(.28*t+.72*t*t)));
  const piston=fallPath(fall);
  const falling=pulse(s.age,-titanDropLead,-.18,-.011,.065);
  titanWake(fallPath,fall,s.age+titanDropLead,47,falling*.82,.59);
  glow(at(piston,0,-62),110,blue,falling*.71);
  const weight=pulse(s.age,0,.038,.075,.21);
  const pistonScale=lerp(.90,1.08,fall)*(1-weight*.055);

  const burst=ease(frac(0,.21,s.age));
  sprite(31,at(impact,0,11),lerp(104,307,burst),{
    height:lerp(39,102,burst),alpha:pulse(s.age,0,.045,.14,.48)});
  // Split the same atlas cell into the moving piston and its ground splash.
  maskedSprite(art.atlases[1],cells[30],titanPistonOutline,titanPistonBase,impact,0,pistonScale,
    pulse(s.age,0,.027,.095,.33),true);
  maskedSprite(art.atlases[1],cells[30],titanPistonOutline,titanPistonBase,piston,0,pistonScale,
    pulse(s.age,-titanDropLead,-.20,.080,.30));
  wave(impact,s.age,.45,192,gold);
  wave(impact,s.age-.077,.44,242,blue);
  dustCloud(impact,s.age-.034,183,1);
  sparks(impact,s.age,{count:31,color:"#ead3a0",heading:-1.57,spread:2.6,speed:458,life:.58});
}

function finalMachine(s) {
  if(!s.mode) {
    titanSlam(s);
    return;
  }
  const muzzle=at(s.enemy,87,-91);
  const wind=smooth(-.35,-.012,s.age);
  const casting=pulse(s.age,-.445,-.31,-.015,.096);
  sprite(33,muzzle,lerp(142,78,wind),{alpha:casting,turn:wind*1.45});
  glow(muzzle,lerp(58,108,wind),blue,casting*(.25+.49*wind));
  for(let i=0;i<21;i++) {
    const t=repeat(s.cast*2.84+i/21), a=i*2.4+s.time*3.3;
    const q=at(muzzle,Math.cos(a)*lerp(124,3,ease(t)),Math.sin(a)*lerp(73,2,ease(t)));
    line(at(q,Math.cos(a)*19,Math.sin(a)*13),q,blue,lerp(1.1,3.2,t),casting*Math.sin(t*Math.PI)*.75);
  }
  const extend=ease(frac(0,.18,s.age)), thin=1-smooth(.46,.69,s.age);
  const beam=pulse(s.age,0,.035,.48,.70);
  if(beam>.004) drawLaser(muzzle,extend,thin,beam,s.age);
  if(s.scenario==="guard" && s.age<.31) {
    const guard=pulse(s.age+.45,0,.12,.57,.74);
    sprite(12,at(s.player,-7,-79),170,{alpha:guard*.57,height:167});
    flash(19,s.hit,s.age-.086,83,blue,.21);
  }
  for(let i=0;i<5;i++) {
    const q=p(470+i*151,FLOOR+12+(i%2)*10), t=s.age-.064-i*.052;
    const spread=ease(frac(0,.25,t));
    sprite(34,q,lerp(65,210,spread),{height:lerp(36,122,spread),
      alpha:pulse(t,0,.038,.11,.49),ground:true,warp:5,t});
    wave(q,t,.47,128,blue);
    sparks(q,t,{count:17,color:i%2?gold:blue,speed:390,life:.51});
  }
}

function drawLaser(muzzle,extend,thin,opacity,time) {
  const tip=at(muzzle,(W-muzzle.x+7)*extend,0), width=tip.x-muzzle.x;
  if(width<2 || thin<.005) return;
  const center=at(muzzle,width*.46,0), thick=lerp(45,124,extend)*thin;
  const light=g.createLinearGradient(0,muzzle.y-thick,0,muzzle.y+thick);
  light.addColorStop(0,"#67e6ff00"); light.addColorStop(.43,"#6ceaff83");
  light.addColorStop(.5,"#e2ffffd9"); light.addColorStop(.56,"#6ceaff83"); light.addColorStop(1,"#67e6ff00");
  g.save(); g.globalAlpha*=opacity; g.fillStyle=light; g.fillRect(muzzle.x,muzzle.y-thick,width,thick*2);
  g.beginPath(); g.rect(muzzle.x-60,muzzle.y-thick*1.6,width+60,thick*3.2); g.clip();
  sprite(32,center,width*1.19,{height:thick*1.29,alpha:.72,warp:10,t:time});
  line(muzzle,tip,"#a3faff",thick*.26,1);
  line(muzzle,tip,"#f6ffff",thick*.10,1);
  for(let i=0;i<15;i++) {
    const u=repeat(time*3.3+seed(i+7)), x=muzzle.x+width*u;
    const dis=Math.sin(time*22+i*.9)*thick*.43;
    line(p(x-width*.11,muzzle.y+dis*.72),p(x,muzzle.y+dis),i%3?"#97f6ff":"#f5ffff",lerp(1.4,4,seed(i)),.68);
  }
  for(let i=0;i<7;i++) {
    const u=repeat(time*2.18+i/7);
    ring(at(muzzle,u*width,0),thick*lerp(.65,.22,u),lerp(.44,.26,u),blue,Math.sin(u*Math.PI)*.73,2.4,Math.PI/2);
  }
  g.restore();
  glow(muzzle,thick*.95,blue,opacity*.65);
  glow(tip,thick*.52,blue,opacity*.56);
}

const painters=[golem,shadowAssassin,knight,archer,grinder,masterAssassin,finalMachine];

function paint(s) {
  arena();
  const past=s.age-(s.index===6 && s.mode?.12:0);
  const knock=s.guard?0:pulse(past,0,.052,.085,.22)*8;
  const player=at(s.player,knock,0);
  shadow(player,47);
  poseBody(art.player,player,147);
  shadow(s.enemy,s.h*.35);
  poseBody(body(s),s.enemy,s.h);
  painters[s.index](s);
}

function markers(index,mode,move) {
  const points=[{label:"Lấy đà",time:move.release*.58},{label:"Tung chiêu",time:move.release+.06}];
  if(index===6&&!mode) {
    points[1]={label:"Bắn lên",time:move.release+.155};
    points.push({label:"Piston rơi",time:move.hit-.11});
  }
  const label=mode && [0,2,4].includes(index) ? "Khóa khiên" : index===6 && mode ? "Tia quét" : "Va chạm";
  const extra=mode&&index===1 ? .055 : mode&&index===6 ? .21 : .085;
  points.push({label,time:move.hit+extra});
  if(mode&&index===3) points.push({label:"Nổ sau lưng",time:move.hit+.19});
  if(mode&&[0,2,4].includes(index)) points.push({label:index===4?"Thử phản đòn":"Thử chịu đòn",time:move.hit+(index===4?.73:.57)});
  if(mode&&index===5) points.push({label:"Chảy máu",time:move.hit+.29});
  if(mode&&index===6) points.push({label:"Nổ mặt sàn",time:move.hit+.45});
  points.push({label:"Dư âm",time:Math.min(move.end-.055,move.hit+(mode&&index===6?.81:mode?.57:.38))});
  return points;
}

function caption(s) {
  if(s.index===6&&!s.mode) {
    if(s.cast<0) return "Tụ lực ở nắm tay máy";
    if(s.age<-titanDropLead) return "Luồng sáng bắn lên cao";
    if(s.age<0) return "Piston rơi thẳng xuống Player";
    return s.age<.32 ? "Giáng xuống mặt đất" : "Vết nứt và bụi tan dần";
  }
  if (s.guard) {
    const status={0:"Giảm sát thương 40%",2:"Khiên hấp thụ 20% Max HP",4:"Giảm sát thương 30% · Phản 25%"}[s.index];
    if(s.round===2) return "Hết hiệu lực sau round 2";
    if(s.broken) return "Khiên vỡ";
    if(s.guardHit>.31&&s.index===4) return "Sóng phản đòn trên Player";
    if(s.guardHit>.05&&s.index===4) return "Gai phản đòn bay về Player";
    if(s.guardHit>=0&&s.guardHit<.43) return "Giáp chịu đòn";
    return s.age<0 ? "Dựng lớp phòng hộ" : status + " · 2 round";
  }
  if(s.index===5&&s.mode&&s.round)
    return s.round===3&&s.roundAge>.55 ? "Kết thúc chảy máu" : "Round "+s.round+" · Chảy máu 2% Max HP";
  if(s.index===6&&s.mode) {
    if(s.age<0) return "Năng lượng tụ về nòng";
    if(s.age<.39) return s.scenario==="guard" ? "Tia xuyên qua khiên Player" : "Tia chạy ngang sàn";
    return "Vụ nổ lan theo đường quét";
  }
  if(s.index===3&&s.mode&&s.age>=0)
    return s.age<.12 ? "Tên đi xuyên Player" : "Nổ hình thoi phía sau";
  if(s.index===5&&s.mode) {
    if(s.age<-.40) return "Áp sát";
    if(s.age<0) return "Chuỗi nhát chém";
    if(s.age>.24) return "Chảy máu · 3 round";
    return "Nhát chốt";
  }
  return s.cast<0 ? "Lấy đà" : s.age<0 ? "Tung chiêu" : s.age<.24 ? "Va chạm" : "Dư âm";
}

function current() {
  const {enemy,mode,time,scenario,round,roundAge}=state;
  return scene(enemy,mode,time,scenario,round,roundAge);
}
function refresh() {
  const s=current();
  if(state.ready) paint(s);
  $("timeline").value=state.time;
  $("timecode").value=state.time.toFixed(2).replace(".",",")+" s";
  $("event").textContent=caption(s);
  $("play").textContent=state.playing?"Dừng":"Phát";
  for(const button of $("markers").children)
    button.classList.toggle("current",Math.abs(+button.dataset.time-state.time)<.04&&!state.round);
}
function stop() {
  state.playing=false;
  cancelAnimationFrame(state.frame);
}
function seek(time) {
  stop(); state.round=0; state.roundAge=1; $("round").value=0;
  state.time=Math.max(0,Math.min(current().move.end,time));
  refresh();
}
function play(restart=false) {
  if(!state.ready) return;
  if(state.playing&&!restart) { stop(); refresh(); return; }
  if(restart||state.round||state.time>=current().move.end) seek(0);
  stop();
  state.playing=true;
  let before=0;
  function tick(now) {
    if(!state.playing) return;
    if(before) state.time=Math.min(current().move.end,state.time+Math.min(.05,(now-before)/1000)*state.speed);
    before=now;
    if(state.time>=current().move.end) stop();
    refresh();
    if(state.playing) state.frame=requestAnimationFrame(tick);
  }
  refresh(); state.frame=requestAnimationFrame(tick);
}
function chooseRound(round) {
  if(!round) { seek(current().move.hit+.11); return; }
  stop(); state.round=round; state.roundAge=0;
  state.time=current().move.end;
  const start=performance.now();
  function tick(now) {
    if(state.round!==round) return;
    state.roundAge=Math.max(0,(now-start)/1000);
    refresh();
    if(state.roundAge<1.25) state.frame=requestAnimationFrame(tick);
  }
  state.frame=requestAnimationFrame(tick);
}
function select(enemy,mode) {
  state.enemy=enemy;state.mode=mode;state.scenario="standard";
  const move=current().move, variants=options(enemy,mode), count=rounds(enemy,mode);
  $("enemy-name").textContent=names[enemy]; $("attack-name").textContent=move.name;
  $("description").textContent=move.description;
  $("normal").setAttribute("aria-pressed",String(!mode)); $("active").setAttribute("aria-pressed",String(!!mode));
  [...$("roster").children].forEach((button,i)=>button.setAttribute("aria-current",String(i===enemy)));
  $("scenario-label").hidden=variants.length<2;
  $("scenario").replaceChildren(...variants.map(item=>new Option(item.label,item.id)));
  $("round-label").hidden=!count;
  $("round").replaceChildren(new Option("Khi thi triển",0),...Array.from({length:count},(_,i)=>new Option("Sau round "+(i+1),i+1)));
  $("timeline").max=move.end;
  $("markers").replaceChildren(...markers(enemy,mode,move).map(step=>{
    const button=document.createElement("button");button.type="button";button.dataset.time=step.time;button.textContent=step.label;
    button.addEventListener("click",()=>seek(step.time));return button;
  }));
  seek(move.hit+(enemy===6&&mode?.24:.11));
}

names.forEach((name,i)=>{
  const button=document.createElement("button"),thumb=document.createElement("img");
  button.type="button";button.className="enemy-option";thumb.src=portrait(i);thumb.alt="";
  button.append(thumb,document.createTextNode(name));
  button.addEventListener("click",()=>select(i,state.mode));$("roster").append(button);
});
$("normal").addEventListener("click",()=>select(state.enemy,0));
$("active").addEventListener("click",()=>select(state.enemy,1));
$("play").addEventListener("click",()=>play());
$("restart").addEventListener("click",()=>play(true));
$("back").addEventListener("click",()=>seek(state.time-1/30));
$("forward").addEventListener("click",()=>seek(state.time+1/30));
$("speed").addEventListener("change",event=>{state.speed=+event.target.value;});
$("timeline").addEventListener("input",event=>seek(+event.target.value));
$("scenario").addEventListener("change",event=>{
  state.scenario=event.target.value;
  const s=current(), delay={hit:.57,break:.72,counter:.72,guard:.19}[state.scenario] ?? .11;
  if(state.round) chooseRound(state.round); else seek(s.move.hit+delay);
});
$("round").addEventListener("change",event=>chooseRound(+event.target.value));
select(0,1);

window.metalPreview={
  get ready(){return state.ready;},
  metadata(){return JSON.stringify({enemies:names,attacks:moves.map((move,i)=>({...move,roundCount:rounds(i>>1,i%2),
    markers:markers(i>>1,i%2,move),scenarios:options(i>>1,i%2)}))});},
  frame(enemy,mode,time,scenario,round=0,roundAge=1){
    const s=scene(enemy,mode,time,scenario,round,roundAge);
    paint(s);
    return JSON.stringify({image:canvas.toDataURL("image/jpeg",.94).split(",")[1],caption:caption(s)});
  }
};
artwork().then(()=>{state.ready=true;$("load-message").hidden=true;refresh();}).catch(error=>{
  $("load-message").textContent="Không tải được sprite "+error.message;
  console.error(error);
});
