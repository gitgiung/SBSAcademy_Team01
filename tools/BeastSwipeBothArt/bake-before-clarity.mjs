import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const art='Assets/2.Resources/Art/Beast_Zombie_Boss',root=art+'/Idle/animations/swipe-both';
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];const names=['North','NorthEast','East','SouthEast','South','SouthWest','West','NorthWest'];
const staged=[];
const selected=process.argv[2];
if(selected&&!dirs.includes(selected))throw Error('Unknown direction: '+selected);
function bounds(p,w,h){let minX=w,minY=h,maxX=-1,maxY=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(p[(y*w+x)*4+3]){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);}if(maxX<0)throw Error('Empty sprite');let footMin=w,footMax=-1;for(let y=Math.max(minY,maxY-12);y<=maxY;y++)for(let x=0;x<w;x++)if(p[(y*w+x)*4+3]){footMin=Math.min(footMin,x);footMax=Math.max(footMax,x);}return {minX,minY,maxX,maxY,footX:(footMin+footMax)/2};}
for(let r=0;r<8;r++){
 if(selected&&dirs[r]!==selected){for(let i=0;i<16;i++)staged.push({r,i,raw:await sharp(`${root}/${dirs[r]}/${i}.png`).ensureAlpha().raw().toBuffer()});continue;}
 // Generated SW faced the camera; mirror the matching SE sequence for a stable SW heading.
 const mirrored=dirs[r]==='south-west',sourceDir=mirrored?'south-east':dirs[r];const source=`tools/BeastSwipeBothArt/smooth-sources/${sourceDir}.png`,m=await sharp(source).metadata(),cells=[];
 for(let i=0;i<16;i++){
  const x=i%4,y=Math.floor(i/4),left=Math.round(x*m.width/4),top=Math.round(y*m.height/4),w=Math.round((x+1)*m.width/4)-left,h=Math.round((y+1)*m.height/4)-top;
  let pipeline=sharp(source).extract({left,top,width:w,height:h});if(mirrored)pipeline=pipeline.flop();const p=await pipeline.ensureAlpha().raw().toBuffer();
  for(let k=0;k<p.length;k+=4){p[k+3]=p[k+3]>=200?255:0;if(!p[k+3])p.fill(0,k,k+4);}
  const b=bounds(p,w,h);if(b.minX===0||b.minY===0||b.maxX===w-1||b.maxY===h-1)throw Error(`Source clipping ${dirs[r]}/${i}`);cells.push({p,w,h,b});
 }
 const scale=Math.min(104/(cells[0].b.maxY-cells[0].b.minY+1), ...cells.map(c=>72/Math.max(c.b.footX-c.b.minX,c.b.maxX-c.b.footX)), ...cells.map(c=>132/(c.b.maxY-c.b.minY+1)));
 for(let i=0;i<16;i++){
  const c=cells[i];const w=Math.round(c.w*scale),h=Math.round(c.h*scale);const p=await sharp(c.p,{raw:{width:c.w,height:c.h,channels:4}}).resize(w,h,{kernel:'nearest'}).raw().toBuffer();const b=bounds(p,w,h);
  const x=Math.round(88-b.footX),y=148-b.maxY;
  if(x+b.minX<12||x+b.maxX>163||y+b.minY<12)throw Error(`Output margin ${dirs[r]}/${i}`);
  const crop=await sharp(p,{raw:{width:w,height:h,channels:4}}).extract({left:b.minX,top:b.minY,width:b.maxX-b.minX+1,height:b.maxY-b.minY+1}).png().toBuffer();
  const raw=await sharp({create:{width:176,height:176,channels:4,background:'#00000000'}}).composite([{input:crop,left:x+b.minX,top:y+b.minY}]).raw().toBuffer();staged.push({r,i,raw});
 }
}
for(const {r,i,raw} of staged){const file=`${root}/${dirs[r]}/${i}.png`,png=await sharp(raw,{raw:{width:176,height:176,channels:4}}).png().toBuffer();
 if(selected&&dirs[r]!==selected)continue;
 for(let attempt=0;;attempt++){try{fs.writeFileSync(file,png);break;}catch(e){if(attempt>=9)throw e;await new Promise(resolve=>setTimeout(resolve,150));}}
 if(!fs.existsSync(file+'.meta')){let meta=fs.readFileSync(`${root}/${dirs[r]}/0.png.meta`,'utf8');const id=meta.match(/internalID: (-?\d+)/)[1];meta=meta.replace(/^guid: \w+/m,'guid: '+crypto.randomBytes(16).toString('hex')).replaceAll(id,String(21300000+i)).replaceAll('0_0',`${i}_0`).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+crypto.randomBytes(16).toString('hex'));fs.writeFileSync(file+'.meta',meta);}
}
for(let r=0;r<8;r++){
 if(selected&&dirs[r]!==selected)continue;
 const file=`${art}/Animator/Clips/BossSwipeBoth_${names[r]}.anim`;let clip=fs.readFileSync(file,'utf8');const refs=[];
 for(let i=0;i<16;i++){const m=fs.readFileSync(`${root}/${dirs[r]}/${i}.png.meta`,'utf8');refs.push(`{fileID: ${m.match(/internalID: (-?\d+)/)[1]}, guid: ${m.match(/^guid: (\w+)/m)[1]}, type: 3}`);}
 const time=i=>i===14?'0.5833333':i===16?'0.6666667':String(Number((i/24).toFixed(9)));
 let keys='';for(let i=0;i<=16;i++)keys+=`    - time: ${time(i)}\n      value: ${refs[Math.min(i,15)]}\n`;
 clip=clip.replace(/(    curve:\r?\n)[\s\S]*?(    attribute: m_Sprite)/,'$1'+keys+'$2').replace(/m_SampleRate: \d+/,'m_SampleRate: 24');
 clip=clip.replace(/(    pptrCurveMapping:\r?\n)[\s\S]*?(  m_AnimationClipSettings:)/,'$1'+[...refs,refs[15]].map(v=>'    - '+v+'\n').join('')+'$2');fs.writeFileSync(file,clip);
}
const output=[];
for(let i=0;i<16;i++)output.push(await sharp({create:{width:704,height:352,channels:4,background:'#353a45'}}).composite(staged.filter(f=>f.i===i).map(f=>({input:f.raw,raw:{width:176,height:176,channels:4},left:f.r%4*176,top:Math.floor(f.r/4)*176}))).raw().toBuffer());
for(const [name,delay] of [['preview.gif',42],['preview-slow.gif',100]])await sharp(Buffer.concat(output),{raw:{width:704,height:352*16,channels:4,pageHeight:352}}).gif({loop:0,delay:Array(16).fill(delay)}).toFile('tools/BeastSwipeBothArt/'+name);
console.log(`${selected||'All directions'} baked; feet aligned, events and duration preserved.`);

