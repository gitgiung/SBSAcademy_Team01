import fs from 'node:fs';import path from 'node:path';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const art='Assets/2.Resources/Art/Beast_Zombie_Boss',out='tools/BeastClarity/motion-audit';fs.mkdirSync(out,{recursive:true});
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
const modes=['idle','move','run','body-charge','swipe','swipe-both','sprint','swipe-left'];const stats=[];
for(const mode of modes){const layers=[];let maxCount=0;for(let r=0;r<8;r++){
 const dir=dirs[r],folder=`${art}/Idle/animations/${mode}/${dir}`;if(!fs.existsSync(folder))continue;const files=fs.readdirSync(folder).filter(f=>f.endsWith('.png')).sort((a,b)=>parseInt(a)-parseInt(b));maxCount=Math.max(maxCount,files.length);
 const rows=[];
 for(let i=0;i<files.length;i++){const file=`${folder}/${files[i]}`,meta=fs.readFileSync(file+'.meta','utf8'),ppu=Number(meta.match(/spritePixelsToUnits: ([\d.]+)/)[1]),m=await sharp(file).metadata(),w=Math.round(m.width*100/ppu),h=Math.round(m.height*100/ppu);const raw=await sharp(file).resize(w,h,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();let x0=w,y0=h,x1=0,y1=0,n=0;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(raw[(y*w+x)*4+3]>127){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);n++;}
  rows.push({i,width:x1-x0+1,height:y1-y0+1,foot:y1-h/2,area:n});
  layers.push({input:await sharp(raw,{raw:{width:w,height:h,channels:4}}).png().toBuffer(),left:100+i*176+Math.round((176-w)/2),top:24+r*176+Math.round((176-h)/2)});
 }
 stats.push({mode,dir,frames:rows});}
 const width=100+maxCount*176,height=24+8*176;const labels=`<svg width="${width}" height="${height}"><g fill="white" font-family="Arial" font-size="16">${dirs.map((d,r)=>`<text x="2" y="${100+r*176}">${d}</text>`).join('')}${Array.from({length:maxCount},(_,i)=>`<text x="${180+i*176}" y="18">${i}</text>`).join('')}</g></svg>`;
 await sharp({create:{width,height,channels:4,background:'#353a45'}}).composite([...layers,{input:Buffer.from(labels),left:0,top:0}]).png().toFile(`${out}/${mode}.png`);
}
const clips=[];for(const f of fs.readdirSync(`${art}/Animator/Clips`).filter(f=>f.endsWith('.anim'))){const s=fs.readFileSync(`${art}/Animator/Clips/${f}`,'utf8'),curve=s.split('    curve:')[1]?.split('    attribute:')[0]||'',keys=[...curve.matchAll(/- time: ([\d.]+)\s+value: \{fileID: (-?\d+), guid: (\w+)/g)].map(m=>({time:+m[1],guid:m[3]}));clips.push({name:f,stop:+s.match(/m_StopTime: ([\d.]+)/)?.[1],loop:s.match(/m_LoopTime: (\d)/)?.[1],keys});}
fs.writeFileSync(`${out}/measurements.json`,JSON.stringify({stats,clips},null,2));console.log('8 contact sheets and 73 clip timing records written.');
