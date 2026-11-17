import fs from 'node:fs';import path from 'node:path';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const art='Assets/2.Resources/Art/Beast_Zombie_Boss',root=art+'/Idle/animations';
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
const report=[];
function clean(p){for(let k=0;k<p.length;k+=4){p[k+3]=p[k+3]>=200?255:0;if(!p[k+3])p.fill(0,k,k+4);}return p;}
function bounds(p,w,h){let minX=w,minY=h,maxX=-1,maxY=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(p[(y*w+x)*4+3]){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);}if(maxX<0)throw Error('Empty');let lo=w,hi=-1;for(let y=Math.max(minY,maxY-12);y<=maxY;y++)for(let x=0;x<w;x++)if(p[(y*w+x)*4+3]){lo=Math.min(lo,x);hi=Math.max(hi,x);}return {minX,minY,maxX,maxY,footX:(lo+hi)/2};}
async function cell(source,cols,rows,i,r,flip=false){const m=await sharp(source).metadata(),left=Math.round(i*m.width/cols),top=Math.round(r*m.height/rows),w=Math.round((i+1)*m.width/cols)-left,h=Math.round((r+1)*m.height/rows)-top;let q=sharp(source).extract({left,top,width:w,height:h});if(flip)q=q.flop();const p=clean(await q.ensureAlpha().raw().toBuffer());return {p,w,h,b:bounds(p,w,h)};}
async function write(file,png,size){for(let a=0;;a++){try{fs.writeFileSync(file,png);break;}catch(e){if(a===9)throw e;await new Promise(r=>setTimeout(r,150));}}
 let meta=fs.readFileSync(file+'.meta','utf8').replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: 200').replace(/textureCompression: \d+/g,'textureCompression: 0');
 meta=meta.replace(/(      rect:\r?\n        serializedVersion: 2\r?\n)[\s\S]*?(      alignment:)/,`$1        x: 0\n        y: 0\n        width: ${size}\n        height: ${size}\n$2`);fs.writeFileSync(file+'.meta',meta);report.push(file);}
async function padded(c,inner,size,pad){const p=await sharp(c.p,{raw:{width:c.w,height:c.h,channels:4}}).resize(inner,inner,{kernel:'nearest'}).png().toBuffer();return sharp({create:{width:size,height:size,channels:4,background:'#00000000'}}).composite([{input:p,left:pad,top:pad}]).png().toBuffer();}
// Grid-based approved sources: retain the same relative cell placement.
for(let r=0;r<8;r++){
 for(let i=0;i<11;i++)await write(`${root}/body-charge/${dirs[r]}/${i}.png`,await padded(await cell('tools/BeastChargeArt/charge-source.png',11,8,i,r),220,264,22),264);
 for(let i=0;i<8;i++){
  let png=await padded(await cell('tools/BeastSwipeArt/swipe-source.png',8,8,i,r),288,352,32);
  if(r===0&&i===6){const patch=await sharp('tools/BeastSwipeArt/north-6-left-cleanup.png').resize(64,352,{kernel:'nearest'}).raw().toBuffer(),p=await sharp(png).raw().toBuffer();for(let y=0;y<352;y++)patch.copy(p,y*352*4,y*64*4,(y+1)*64*4);png=await sharp(p,{raw:{width:352,height:352,channels:4}}).png().toBuffer();}
  await write(`${root}/swipe/${dirs[r]}/${i}.png`,png,352);
  const group=['east','west'].includes(dirs[r])?'SwipeEastWestFullFrame':dirs[r]==='north-east'?'SwipeNorthEastFullFrame':null;
  if(group)await write(`${art}/Animator/FrameSources/${group}/${dirs[r]}-${i}.png`,png,352);
 }
}
// West retained its original Run sheet. Other directions use the approved identity edit.
for(const dir of dirs){
 if(dir==='west'){for(let i=0;i<8;i++)await write(`${root}/run/west/${i}.png`,await padded(await cell('tools/BeastRunArt/run-source.png',8,8,i,6),220,264,22),264);continue;}
 const cells=[];for(let i=0;i<8;i++)cells.push(await cell(`tools/BeastRunArt/${dir==='south-west'?'south-east':dir}-identity-v3.png`,4,2,i%4,Math.floor(i/4)));
 const scale=176/(cells.reduce((s,c)=>s+c.b.maxY-c.b.minY+1,0)/8);
 for(let i=0;i<8;i++){const c=cells[i],cw=c.b.maxX-c.b.minX+1,ch=c.b.maxY-c.b.minY+1,w=Math.round(cw*scale),h=Math.round(ch*scale);let q=sharp(c.p,{raw:{width:c.w,height:c.h,channels:4}}).extract({left:c.b.minX,top:c.b.minY,width:cw,height:ch}).resize(w,h,{kernel:'nearest'});if(dir==='south-west')q=q.flop();const p=await q.raw().toBuffer();let sum=0,n=0;for(let y=Math.floor(h*.53);y<Math.ceil(h*.63);y++)for(let x=0;x<w;x++)if(p[(y*w+x)*4+3]){sum+=x;n++;}
  const left=Math.round(132-sum/n),top=232-h-([3,7].includes(i)?4:0);if(left<16||top<16||left+w>248||top+h>248)throw Error('Run margin');
  await write(`${root}/run/${dir}/${i}.png`,await sharp({create:{width:264,height:264,channels:4,background:'#00000000'}}).composite([{input:p,raw:{width:w,height:h,channels:4},left,top}]).png().toBuffer(),264);
 }
}
// SwipeBoth: preserve its original logical foot alignment, calculate low-res placement
// exactly as the approved bake, then render the source into that placement at 2x.
for(const dir of dirs){const cells=[];for(let i=0;i<16;i++)cells.push(await cell(`tools/BeastSwipeBothArt/smooth-sources/${dir==='south-west'?'south-east':dir}.png`,4,4,i%4,Math.floor(i/4),dir==='south-west'));
 const scale=Math.min(104/(cells[0].b.maxY-cells[0].b.minY+1),...cells.map(c=>72/Math.max(c.b.footX-c.b.minX,c.b.maxX-c.b.footX)),...cells.map(c=>132/(c.b.maxY-c.b.minY+1)));
 for(let i=0;i<16;i++){const c=cells[i],w=Math.round(c.w*scale),h=Math.round(c.h*scale),low=await sharp(c.p,{raw:{width:c.w,height:c.h,channels:4}}).resize(w,h,{kernel:'nearest'}).raw().toBuffer(),b=bounds(low,w,h),x=Math.round(88-b.footX),y=148-b.maxY;
  const high=await sharp(c.p,{raw:{width:c.w,height:c.h,channels:4}}).resize(w*2,h*2,{kernel:'nearest'}).raw().toBuffer(),hb=bounds(high,w*2,h*2);
  const crop=await sharp(high,{raw:{width:w*2,height:h*2,channels:4}}).extract({left:hb.minX,top:hb.minY,width:hb.maxX-hb.minX+1,height:hb.maxY-hb.minY+1}).png().toBuffer();
  await write(`${root}/swipe-both/${dir}/${i}.png`,await sharp({create:{width:352,height:352,channels:4,background:'#00000000'}}).composite([{input:crop,left:x*2+hb.minX,top:y*2+hb.minY}]).png().toBuffer(),352);
 }
}
// All remaining original sprites retain native pixels and size; remove lossy import compression.
function walk(d){return fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(`${d}/${e.name}`):[`${d}/${e.name}`]);}
let compression=0;for(const file of walk(art).filter(f=>f.endsWith('.png.meta'))){const old=fs.readFileSync(file,'utf8'),next=old.replace(/textureCompression: \d+/g,'textureCompression: 0');if(old!==next){fs.writeFileSync(file,next);compression++;}}
fs.writeFileSync('tools/BeastClarity/rebuilt-files.json',JSON.stringify(report,null,2));
console.log(`Rebuilt ${report.length} PNGs from source; disabled compression on ${compression} additional textures. No clips, scenes or prefabs written.`);
