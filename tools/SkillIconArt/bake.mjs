// Rebuild every combat skill icon from editable vector artwork. Existing metas are never written.
// npm install --prefix .utmp/skill-icons-runtime --no-audit --no-fund sharp@0.35.5
// node tools/SkillIconArt/bake.mjs [--check]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import crypto from 'node:crypto';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const require = createRequire(path.join(root, '.utmp/skill-icons-runtime/package.json'));
const sharp = require('sharp');
const check = process.argv.includes('--check');
const skills = JSON.parse(fs.readFileSync(path.join(root, 'Assets/RemoteContent/SkillPilot/CombatSkillTable.json'), 'utf8')).skills;
const palettes = {
  ELEM_FIRE: ['#ff482c','#ffb52e','#fff5bf'], ELEM_WATER: ['#136ee8','#37d9fa','#e7ffff'],
  ELEM_ICE: ['#557fe8','#96eaff','#ffffff'], ELEM_WIND: ['#179c8e','#72f8c4','#eaffed'],
  ELEM_EARTH: ['#926044','#dfae66','#fff1c2'], ELEM_LIGHTNING: ['#7b46f2','#cf8bff','#fff4ff'],
  ELEM_PHYSICAL: ['#269b69','#83e4a5','#f0ffe5'], ELEM_ENERGY: ['#a56ad8','#f0c76b','#fff9da']
};
const rarities = {Common:'#aebbc9',Uncommon:'#65d596',Rare:'#5eaeff',Epic:'#c185ff',Legendary:'#ffb947',Mythic:'#ff738b',Fusion:'#ffe6a1'};
// Explicit silhouettes: the table's name, element and geometry remain the authority for gameplay.
const motifs = {
  10001:'bolt',10002:'volley',10003:'impact',10004:'orb',10005:'spear',10006:'flare',10007:'blade',
  10008:'gust',10009:'arrow',10010:'scatter',10011:'fan',10012:'shards',10013:'breath',10014:'ring',
  10015:'sun',10016:'barrage',10017:'beam',10018:'crystal',10019:'arrow',10020:'rock',10021:'lightning',
  10022:'stream',10023:'ripple',10024:'trident',10025:'arrow',10026:'volley',10027:'spear',10028:'fan',
  10029:'pressure',10030:'lance',10031:'pillar',10032:'ring',10033:'vortex',10034:'scatter',10035:'blade',10036:'trident',
  20001:'hammer',20002:'dash',20003:'explosion',20004:'vortex',20005:'meteor',20006:'slash',20007:'eruption',
  20008:'shockring',20009:'pillar',20010:'sunstrike',20011:'doubleblade',20012:'cross',20013:'rain',20014:'lance',20015:'collapse',
  20016:'thrust',20017:'slash',20018:'breaker',20019:'twinspear',20020:'trident',20021:'pressure',20022:'stream',
  20023:'shockring',20024:'abysslance',20025:'doubleblade',20026:'beam',20027:'ripple',20028:'flood',20029:'crescent',20030:'wave',
  30001:'regeneration',30002:'heal',30003:'mana',30004:'shield',30005:'swords',30006:'wing',30007:'prayer',
  30008:'blessing',30009:'watershield',30010:'armor'
};
const P = (d,fill='url(#body)',stroke='',width=2) => `<path d="${d}" fill="${fill}"${stroke?` stroke="${stroke}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"`:''}/>`;
const line = (d,c='url(#light)',w=4) => P(d,'none',c,w);
const ellipse = (x,y,rx,ry,c='url(#body)',s='',w=2) => `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${c}" stroke="${s||'none'}" stroke-width="${w}"/>`;
const group = (s,t) => `<g transform="${t}">${s}</g>`;
const star = (x,y,r=10) => P(`M${x-r} ${y}Q${x} ${y-2} ${x} ${y-r}Q${x+2} ${y} ${x+r} ${y}Q${x} ${y+2} ${x} ${y+r}Q${x-2} ${y} ${x-r} ${y}`,'url(#light)');
function bolt(water=false) {
  return P(water?'M36 203C82 179 69 124 122 86Q151 60 199 51Q182 101 162 129C118 169 83 153 36 203Z':'M35 207Q73 159 64 124L102 145Q95 92 151 80L144 108Q180 56 202 50Q191 100 163 138L178 135Q130 183 92 170Z')
    + P('M58 186Q107 145 124 120Q146 87 188 65Q157 122 119 142Z','url(#light)')
    + line('M48 199Q105 155 145 115','#ffffff',2);
}
function spear() {
  return line('M45 211L178 78','url(#body)',15)+line('M47 207L177 77','url(#light)',4)
    +P('M146 83L204 43L177 111L173 84Z','url(#body)','#d5faff',2)+P('M173 84L204 43L177 94Z','url(#light)');
}
function wave() {
  return P('M29 187Q62 189 85 146Q114 51 181 68Q230 81 208 126Q186 156 160 132Q190 139 190 114Q181 84 157 111Q146 137 159 157Q177 183 227 169Q205 214 127 213Q65 213 29 187Z')
    +P('M49 189Q100 185 115 119Q140 67 181 82Q211 94 196 117Q198 88 172 94Q138 102 137 143Q123 185 87 194Z','url(#light)')
    +line('M85 201Q151 179 196 187','url(#light)',4)+ellipse(201,148,4,6,'url(#light)');
}
function ring(count=1) {
  let out='';
  for(let i=0;i<count;i++)out+=ellipse(128,147-i*19,85-i*13,43-i*6,'none','url(#body)',14-i*2)+ellipse(128,143-i*19,85-i*13,43-i*6,'none','url(#light)',3);
  return out;
}
function blade() {
  return P('M34 195Q173 183 212 49Q219 174 96 209L135 182Q75 206 34 195Z')
    +P('M48 194Q177 172 209 66Q193 157 133 184Q97 197 48 194Z','url(#light)');
}
function shield() {
  return P('M128 40Q167 64 202 65L194 141Q180 193 128 218Q76 193 62 141L54 65Q92 64 128 40Z','url(#body)','url(#light)',5)
    +P('M128 59L128 198Q174 167 178 132L185 81Z','#061f3c')
    +line('M75 81L82 137Q89 163 109 180','url(#light)',5);
}
function sword() {
  return P('M115 151L114 71L128 35L142 71L141 151Z','url(#light)')+P('M128 40L142 71L141 151L128 145Z','url(#body)')
    +P('M91 147L165 147L160 163L136 158L136 195L145 207L128 219L111 207L120 195L120 158L96 163Z','url(#body)','url(#light)',2);
}
function art(s) {
  const m=motifs[s.Id],water=s.Element==='ELEM_WATER';
  let out='';
  switch(m) {
    case 'bolt': case 'gust': case 'flare': out=bolt(water); if(m==='gust')out+=line('M39 142Q71 93 136 64','url(#light)',3);if(m==='flare')out+=star(168,86,29);break;
    case 'volley': case 'scatter': case 'barrage': {
      const n=s.Projectiles; for(let i=0;i<n;i++)out+=group(bolt(water),`translate(${18+i*23} ${87-i*10}) scale(${n>4?0.44:0.54})`);break;
    }
    case 'orb': case 'sun': case 'collapse': case 'sunstrike': {
      out=ellipse(128,119,63,63,'url(#body)')+ellipse(119,110,43,43,'url(#light)');
      for(let i=0;i<10;i++){const a=i*36;out+=group(P('M121 45L128 20L138 47Z'),`rotate(${a} 128 119)`);}
      out+=line('M77 105Q95 66 133 75','#fffceb',5);
      if(m==='collapse')out+=ellipse(128,138,96,25,'none','url(#light)',5)+P('M137 57L113 108L140 132L116 180L150 128L128 104Z','#331332');
      if(m==='sunstrike')out+=P('M110 165L110 187L85 187L128 224L171 187L146 187L146 165Z','url(#light)');break;
    }
    case 'arrow':out=bolt(water)+P('M148 65L207 43L186 102L174 77Z','url(#light)');break;
    case 'spear':case 'lance':case 'abysslance':case 'thrust': out=spear();if(m!=='spear')out+=line('M37 164Q99 173 129 116Q143 91 170 91','url(#body)',7);break;
    case 'trident':out=spear()+line('M94 145L92 99L133 60','url(#body)',10)+line('M105 157L149 163L190 118','url(#body)',10)+P('M117 58L145 46L136 76Z','url(#light)')+P('M174 113L202 101L193 131Z','url(#light)');break;
    case 'twinspear':out=group(spear(),'translate(-8 4) scale(.85)')+group(spear(),'translate(43 34) scale(.85)');break;
    case 'beam': case 'flood': case 'stream':out=group(bolt(water),m==='flood'?'translate(-21 -21) scale(1.16)':'translate(0 0)')+line('M42 206L207 41','url(#light)',m==='beam'?5:12)+star(200,50,m==='beam'?18:9);break;
    case 'blade':case 'slash':case 'crescent':out=blade();if(m==='slash')out+=group(sword(),'translate(39 26) rotate(40 128 128) scale(.7)');if(m==='crescent')out+=group(blade(),'translate(25 19) scale(.8)');break;
    case 'doubleblade':out=blade()+group(blade(),'translate(-4 -22) scale(.85)');break;
    case 'fan':case 'breath':case 'breaker':for(let i=0;i<5;i++)out+=group(bolt(water),`translate(128 195) rotate(${-48+i*24}) scale(.64) translate(-65 -197)`);if(m==='breaker')out+=group(wave(),'translate(40 -6) scale(.7)');break;
    case 'ring':case 'shockring':case 'ripple':case 'pressure':out=ring(m==='ring'?1:m==='shockring'?2:3);if(m==='pressure')out+=P('M113 36L143 36L143 80L163 80L128 115L93 80L113 80Z','url(#light)');if(m==='shockring')out+=star(128,89,24);break;
    case 'wave':out=wave();break;
    case 'vortex':for(let i=0;i<5;i++)out+=group(blade(),`rotate(${i*72} 128 128) translate(21 21) scale(.83)`);out+=ellipse(128,128,20,20,'#090e25')+ellipse(124,123,8,8,'url(#light)');break;
    case 'pillar':case 'eruption':out=ring(1)+P('M78 151Q99 120 93 67L114 85L128 29L145 72L166 49Q153 117 180 152Q130 177 78 151Z')+P('M108 149L128 60L144 147Z','url(#light)');if(m==='eruption')out+=P('M38 190L81 147L111 182L143 167L179 145L221 191Z','#5e3340','url(#body)',4);break;
    case 'impact':case 'explosion':out=ring()+P('M128 42L145 101L188 67L169 120L218 132L166 147L188 184L143 165L128 206L109 166L64 187L86 147L35 132L90 115L68 69L110 99Z')+star(128,128,m==='explosion'?54:37);break;
    case 'dash':out=bolt()+P('M96 135L140 135L126 113L198 128L155 177L159 151L87 151Z','url(#light)');break;
    case 'hammer':out=group(P('M117 111L139 111L139 214L117 214Z','url(#body)','url(#light)',3)+P('M66 56L170 56L189 77L180 111L65 111L54 89Z','url(#body)','url(#light)',4)+P('M68 63L162 63L169 79L62 79Z','url(#light)'), 'rotate(35 128 128)');break;
    case 'meteor':out=bolt()+group(sword(),'rotate(40 128 128)')+group(ring(),'translate(42 89) scale(.65)');break;
    case 'rain':for(let i=0;i<3;i++)out+=group(bolt(),`translate(${i*51-5} ${i===1?68:6}) scale(.62)`);out+=group(ring(),'translate(19 79) scale(.85)');break;
    case 'cross':out=group(P('M108 35L148 35L148 108L221 108L221 148L148 148L148 221L108 221L108 148L35 148L35 108L108 108Z','url(#body)','url(#light)',3),'rotate(12 128 128)')+star(128,128,43);break;
    case 'shards':case 'crystal':for(let i=0;i<(m==='shards'?3:1);i++)out+=group(P('M128 34L174 89L151 173L103 206L80 127Z','url(#body)','url(#light)',3)+P('M128 34L131 113L103 206L102 119Z','url(#light)')+line('M131 113L174 89M131 113L151 173','#fffaff',2),`translate(${m==='shards'?i*49-12:0} ${m==='shards'?(i%2)*29:0}) scale(${m==='shards'?.67:1})`);break;
    case 'rock':out=bolt()+P('M94 75L156 57L197 91L201 145L166 180L108 171L76 132Z','url(#body)','#ffdfaa',3)+P('M94 75L133 102L156 57L197 91L160 118L166 180L128 147L108 171L112 115L76 132Z','#775249')+line('M94 75L133 102L160 118L197 91','url(#light)',4);break;
    case 'lightning':out=P('M145 28L62 141L115 132L88 224L205 101L145 112L181 28Z','url(#body)','url(#light)',3)+P('M145 41L91 124L135 117L109 192L178 117L133 126Z','url(#light)');break;
    case 'shield':case 'watershield':out=shield();out+=m==='watershield'?group(wave(),'translate(49 64) scale(.6)'):P('M118 85L138 85L138 116L159 116L159 136L138 136L138 168L118 168L118 136L97 136L97 116L118 116Z','url(#light)');break;
    case 'swords':out=group(sword(),'rotate(-35 128 128)')+group(sword(),'rotate(35 128 128)');break;
    case 'heal':case 'regeneration':out=ellipse(128,127,75,75,'none','url(#body)',8)+P('M109 65L147 65L147 108L190 108L190 146L147 146L147 189L109 189L109 146L66 146L66 108L109 108Z','url(#body)','url(#light)',3);if(m==='regeneration')out+=P('M37 156Q19 100 65 66L65 96L83 67L53 44L56 61Q1 103 37 156Z','url(#light)')+P('M219 98Q238 155 190 192L190 163L173 191L204 212L200 195Q252 150 219 98Z','url(#light)');break;
    case 'mana':out=P('M128 32Q116 76 80 117Q38 178 95 207Q146 231 180 191Q208 153 169 111Q140 76 128 32Z','url(#body)','url(#light)',3)+line('M105 122Q68 165 100 185','url(#light)',10)+star(146,149,24);break;
    case 'wing':out=P('M39 201Q62 157 74 98Q89 41 217 44L179 82L150 86L187 91L153 121L124 120L153 132L121 160L95 154L113 170Q75 184 39 201Z','url(#body)','url(#light)',3)+line('M52 190Q119 100 195 59','url(#light)',5);break;
    case 'prayer':out=shield()+ellipse(128,44,49,13,'none','url(#light)',6)+star(128,125,39);break;
    case 'blessing':out=star(128,119,78)+ellipse(128,119,53,53,'none','url(#body)',7)+star(63,179,17)+star(202,67,13);break;
    case 'armor':out=P('M77 63L108 49Q128 77 148 49L179 63L209 104L174 123L167 200Q128 216 89 200L82 123L47 104Z','url(#body)','url(#light)',4)+P('M128 91L157 119L146 173L128 194L109 173L99 119Z','#53273b')+group(bolt(),'translate(73 78) scale(.44)');break;
    default:throw new Error(`Missing artwork ${s.Id}`);
  }
  return out;
}
function svg(s,card=true) {
  let colors=palettes[s.Element];
  if(s.Id===30003)colors=['#4558df','#9ca8ff','#eff5ff'];
  if(s.Id===30005)colors=['#d5424f','#ff9982','#fff4ca'];
  if(s.Id===30004||s.Id===30007)colors=['#ae874b','#f5d69a','#fffbe8'];
  const [dark,mid,light]=colors, frame=rarities[s.Rarity] || '#aebbc9';
  const seed=s.Id%17;
  let particles='';
  for(let i=0;i<10;i++){const a=(i*137+seed*19)*Math.PI/180,r=71+(i%3)*9,x=128+Math.cos(a)*r,y=128+Math.sin(a)*r;particles+=i%3===0?star(x,y,3+i%4):ellipse(x,y,1.5+i%2,1.5+i%2,mid);}
  const defs=`<defs><linearGradient id="body" x1="0" y1="1" x2=".85" y2="0"><stop stop-color="${dark}"/><stop offset=".52" stop-color="${mid}"/><stop offset="1" stop-color="${light}"/></linearGradient><linearGradient id="light" x2=".4" y2="1"><stop stop-color="#ffffff"/><stop offset="1" stop-color="${light}"/></linearGradient><radialGradient id="bg"><stop stop-color="${dark}" stop-opacity=".4"/><stop offset="1" stop-color="#080f20"/></radialGradient><filter id="glow" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="7"/></filter></defs>`;
  let artwork=art(s);
  // Keep the fan inside the frame, and distinguish sustained attacks from single casts.
  if(['fan','breath','breaker'].includes(motifs[s.Id]))artwork=group(artwork,'translate(13 13) scale(.9)');
  if(s.Hits>1 && !['volley','scatter','barrage'].includes(motifs[s.Id])) {
    for(let i=0;i<Math.min(4,s.Hits);i++)artwork+=line(`M${76+i*30} 221L${83+i*30} 213`,'url(#light)',3);
  }
  if(s.Id>=20000&&s.Id<30000)artwork+=P('M117 27L128 18L139 27L128 36Z','url(#light)');
  return `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">${defs}${card?`<rect x="3" y="3" width="250" height="250" rx="20" fill="#080d18" stroke="${frame}" stroke-width="3"/><rect x="10" y="10" width="236" height="236" rx="14" fill="url(#bg)" stroke="${frame}" stroke-opacity=".25"/>${ellipse(128,128,98,98,'none',mid,1)}`:''}<g opacity=".35" filter="url(#glow)">${artwork}</g>${particles}<g stroke-linejoin="round">${artwork}</g>${card?`<path d="M17 48V17H48M208 17H239V48M17 208V239H48M208 239H239V208" fill="none" stroke="${frame}" stroke-width="3"/>`:''}</svg>`;
}
const preview=path.join(root,'.utmp/skill-icons');fs.mkdirSync(preview,{recursive:true});
let verified=0;const thumbs=[];const seen=new Set();
for (const [i,s] of skills.entries()) {
  for(const card of [true,false]) {
    const rel = card
      ? s.Id === 10025 ? 'Assets/RemoteContent/SkillPilot/Skill_10025.png' : `Assets/Resources/SkillIcons/Skill_${s.Id}.png`
      : `Assets/2.Resources/Art/SkillIcons/SkillIcon_${s.Id}.png`;
    const dest=path.join(root,rel), source=svg(s,card), png=await sharp(Buffer.from(source)).png().toBuffer();
    if(!fs.existsSync(dest+'.meta'))throw new Error('Missing existing meta: '+rel);
    if(check){if(!fs.readFileSync(dest).equals(png))throw new Error('Art differs: '+rel);verified++;}
    else {
      // Unity's importer may briefly hold a Windows file handle while the editor is open.
      for(let attempt=0;;attempt++) {
        try { fs.writeFileSync(dest,png); break; }
        catch(error) { if(attempt>=30 || !['EBUSY','EACCES','EPERM','UNKNOWN'].includes(error.code))throw error; await new Promise(r=>setTimeout(r,200)); }
      }
    }
    if(card){
      const hash=crypto.createHash('sha256').update(png).digest('hex');if(seen.has(hash))throw new Error('Duplicate icon '+s.Id);seen.add(hash);
      const thumb=await sharp(png).resize(112,112).toBuffer();
      thumbs.push({input:thumb,left:(i%8)*132+10,top:Math.floor(i/8)*144+8});
      const label=Buffer.from(`<svg width="132" height="24"><text x="66" y="17" text-anchor="middle" fill="#dbe5ef" font-family="sans-serif" font-size="13">${s.Id}</text></svg>`);
      thumbs.push({input:label,left:(i%8)*132,top:Math.floor(i/8)*144+121});
    }
  }
}
await sharp({create:{width:1056,height:Math.ceil(skills.length/8)*144+8,channels:4,background:'#101625'}}).composite(thumbs).png().toFile(path.join(preview,'all-skills.png'));
fs.writeFileSync(path.join(preview,'manifest.json'),JSON.stringify(skills.map(s=>({id:s.Id,name:s.Name,motif:motifs[s.Id],element:s.Element})),null,2));
console.log(`${check?'Verified':'Wrote'} ${skills.length*2} PNGs (${skills.length} unique skills), 256px. Existing metas preserved. ${verified} byte comparisons.`);
