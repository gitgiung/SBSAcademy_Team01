import fs from 'node:fs';import path from 'node:path';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations';const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
fs.mkdirSync('tools/BeastChargeArt/gait-frames',{recursive:true});
async function frame(source,cols,rows,c,r){
 const {width,height}=await sharp(source).metadata();const left=Math.round(c*width/cols),top=Math.round(r*height/rows);
 const pixels=await sharp(source).extract({left,top,width:Math.round((c+1)*width/cols)-left,height:Math.round((r+1)*height/rows)-top}).resize(110,110,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<pixels.length;p+=4){pixels[p+3]=pixels[p+3]>=200?255:0;if(!pixels[p+3])pixels.fill(0,p,p+4);}
 return sharp({create:{width:132,height:132,channels:4,background:'#00000000'}}).composite([{input:pixels,raw:{width:110,height:110,channels:4},left:11,top:11}]).raw().toBuffer();
}
async function write(p,raw){const png=await sharp(raw,{raw:{width:132,height:132,channels:4}}).png().toBuffer();for(let a=0;;a++){try{fs.writeFileSync(p,png);break;}catch(e){if(a===9)throw e;await new Promise(r=>setTimeout(r,150));}}return png;}
function rect(p){const text=fs.readFileSync(p+'.meta','utf8');fs.writeFileSync(p+'.meta',text.replace(/(      rect:\r?\n        serializedVersion: 2\r?\n)[\s\S]*?(      alignment:)/,'$1        x: 0\n        y: 0\n        width: 132\n        height: 132\n$2'));}
const charge=[];
for(let r=0;r<8;r++)for(let i=0;i<11;i++){const raw=await frame('tools/BeastChargeArt/charge-source.png',11,8,i,r);const p=`${root}/body-charge/${dirs[r]}/${i}.png`;await write(p,raw);rect(p);charge.push({r,i,raw});}
const gait=[];
for(let r=0;r<4;r++)for(let i=0;i<8;i++){const raw=await frame('tools/BeastChargeArt/gait-source.png',8,4,i,r);const m=r<2?'move':'run',d=r%2?'south':'north',p=`${root}/${m}/${d}/${i}.png`;const png=await write(p,raw);rect(p);fs.writeFileSync(`tools/BeastChargeArt/gait-frames/${m}-${d}-${i}.png`,png);gait.push({r,i,raw});}
async function gif(frames,order,count,width,height,p){const output=[];for(const i of order){output.push(await sharp({create:{width,height,channels:4,background:'#353a45'}}).composite(frames.filter(f=>f.i===i).map(f=>({input:f.raw,raw:{width:132,height:132,channels:4},left:f.r%count*132,top:Math.floor(f.r/count)*132}))).raw().toBuffer());}await sharp(Buffer.concat(output),{raw:{width,height:height*order.length,channels:4,pageHeight:height}}).gif({loop:0,delay:order.map(()=>83)}).toFile(p);}
await gif(charge,[0,1,2,3,4,5,6,7,8,9,10,4,5,6,7,8,9,10],4,528,264,'tools/BeastChargeArt/charge-preview.gif');
await gif(gait,[0,1,2,3,4,5,6,7],4,528,132,'tools/BeastChargeArt/gait-preview.gif');
console.log('Wrote 88 body-charge + 32 N/S gait frames and previews.');
