import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));
const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/move';
const source='tools/BeastMoveArt/move-source.png';
const {width,height}=await sharp(source).metadata();
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
const frames=[];
for(let r=0;r<8;r++) for(let i=0;i<8;i++){
 const left=Math.round(i*width/8),top=Math.round(r*height/8);
 const cell=await sharp(source).extract({left,top,width:Math.round((i+1)*width/8)-left,height:Math.round((r+1)*height/8)-top}).resize(110,110,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<cell.length;p+=4){cell[p+3]=cell[p+3]>=200?255:0;if(!cell[p+3])cell.fill(0,p,p+4);}
 let raw=await sharp({create:{width:132,height:132,channels:4,background:'#00000000'}}).composite([{input:cell,raw:{width:110,height:110,channels:4},left:11,top:11}]).raw().toBuffer();
 const gaitOverride=`tools/BeastChargeArt/gait-frames/move-${dirs[r]}-${i}.png`;
 if(fs.existsSync(gaitOverride))raw=await sharp(gaitOverride).ensureAlpha().raw().toBuffer();
 const file=`${root}/${dirs[r]}/${i}.png`;
 const png=await sharp(raw,{raw:{width:132,height:132,channels:4}}).png().toBuffer();
 for(let attempt=0;;attempt++){try{fs.writeFileSync(file,png);break;}catch(e){if(attempt===9)throw e;await new Promise(resolve=>setTimeout(resolve,150));}} 
 const meta=fs.readFileSync(file+'.meta','utf8');
 fs.writeFileSync(file+'.meta',meta.replace(/(      rect:\r?\n        serializedVersion: 2\r?\n)[\s\S]*?(      alignment:)/,'$1        x: 0\n        y: 0\n        width: 132\n        height: 132\n$2'));
 frames.push({r,i,raw});
}
const previews=[];
for(let i=0;i<8;i++){
 const canvas=await sharp({create:{width:528,height:264,channels:4,background:'#353a45'}}).composite(frames.filter(f=>f.i===i).map(f=>({input:f.raw,raw:{width:132,height:132,channels:4},left:(f.r%4)*132,top:Math.floor(f.r/4)*132}))).raw().toBuffer();
 previews.push(canvas);
}
await sharp(Buffer.concat(previews),{raw:{width:528,height:264*8,channels:4,pageHeight:264}}).gif({loop:0,delay:[83,83,83,83,83,83,83,167]}).toFile('tools/BeastMoveArt/preview.gif');
console.log('Wrote 64 move frames, full 132x132 rects, 12fps preview.');




