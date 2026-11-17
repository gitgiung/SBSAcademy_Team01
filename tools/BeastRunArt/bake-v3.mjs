import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));
const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/run';
const out='tools/BeastRunArt';
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
function bounds(data,w,h){let x0=w,y0=h,x1=0,y1=0;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]>=200){x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);}return {left:x0,top:y0,width:x1-x0+1,height:y1-y0+1};}
const frames=[];
for(const dir of dirs){
 if(dir==='west'){for(let i=0;i<8;i++)frames.push({dir,i,png:fs.readFileSync(`${root}/${dir}/${i}.png`)});continue;}
 // Retain the approved SE/SW shared motion; SW mirrors the corrected SE identity.
 const source=`${out}/${['south-east','south-west'].includes(dir)?'south-east':dir}-identity-v3.png`;
 const {width,height}=await sharp(source).metadata();const cells=[];
 for(let i=0;i<8;i++){
  const left=Math.round(i%4*width/4),top=Math.round(Math.floor(i/4)*height/2),w=Math.round((i%4+1)*width/4)-left,h=Math.round((Math.floor(i/4)+1)*height/2)-top;
  const raw=await sharp(source).extract({left,top,width:w,height:h}).ensureAlpha().raw().toBuffer();
  for(let p=0;p<raw.length;p+=4){raw[p+3]=raw[p+3]>=200?255:0;if(!raw[p+3])raw.fill(0,p,p+4);}
  const box=bounds(raw,w,h);cells.push({raw,w,h,box});
 }
 // One scale per direction; never stretch individual poses. Keep existing 132px canvas.
 const scale=88/(cells.reduce((s,c)=>s+c.box.height,0)/8);
 for(let i=0;i<8;i++){
  const c=cells[i],w=Math.round(c.box.width*scale),h=Math.round(c.box.height*scale);
  let sprite=sharp(c.raw,{raw:{width:c.w,height:c.h,channels:4}}).extract(c.box).resize(w,h,{kernel:'nearest'});
  if(dir==='south-west')sprite=sprite.flop();
  const raw=await sprite.raw().toBuffer();
  // Align the pelvis rather than full silhouette, so swinging limbs cannot move the body.
  let sum=0,count=0;for(let y=Math.floor(h*.53);y<Math.ceil(h*.63);y++)for(let x=0;x<w;x++)if(raw[(y*w+x)*4+3]){sum+=x;count++;}
  const left=Math.round(66-sum/count),top=116-h-([3,7].includes(i)?2:0);
  if(left<8||top<8||left+w>124||top+h>124)throw Error(`Unsafe margin ${dir}/${i}`);
  const png=await sharp({create:{width:132,height:132,channels:4,background:'#00000000'}}).composite([{input:raw,raw:{width:w,height:h,channels:4},left,top}]).png().toBuffer();
  for(let attempt=0;;attempt++){try{fs.writeFileSync(`${root}/${dir}/${i}.png`,png);break;}catch(error){if(attempt>=9)throw error;await new Promise(resolve=>setTimeout(resolve,150));}}
  frames.push({dir,i,png});
 }
}
for(const [name,delay] of [['preview.gif',83],['preview-slow.gif',200]]){
 const pages=[];for(let i=0;i<8;i++)pages.push(await sharp({create:{width:528,height:264,channels:4,background:'#353a45'}}).composite(dirs.map((dir,r)=>({input:frames.find(f=>f.dir===dir&&f.i===i).png,left:r%4*132,top:Math.floor(r/4)*132}))).raw().toBuffer());
 await sharp(Buffer.concat(pages),{raw:{width:528,height:264*8,channels:4,pageHeight:264}}).gif({loop:0,delay:Array(8).fill(delay)}).toFile(`${out}/${name}`);
}
await sharp({create:{width:1056,height:1056,channels:4,background:'#353a45'}}).composite(frames.map(f=>({input:f.png,left:f.i*132,top:dirs.indexOf(f.dir)*132}))).png().toFile(`${out}/contact-v3.png`);
console.log('56 Run PNGs written, West and all metadata/clips untouched.');
