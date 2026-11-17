import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));
const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/swipe';
const source='tools/BeastSwipeArt/swipe-source.png';
const {width,height}=await sharp(source).metadata();
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
const frames=[];
for(let r=0;r<8;r++) for(let i=0;i<8;i++){
 const left=Math.round(i*width/8),top=Math.round(r*height/8);
 const cell=await sharp(source).extract({left,top,width:Math.round((i+1)*width/8)-left,height:Math.round((r+1)*height/8)-top}).resize(144,144,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<cell.length;p+=4){cell[p+3]=cell[p+3]>=200?255:0;if(!cell[p+3])cell.fill(0,p,p+4);}
 const raw=await sharp({create:{width:176,height:176,channels:4,background:'#00000000'}}).composite([{input:cell,raw:{width:144,height:144,channels:4},left:16,top:16}]).raw().toBuffer();
 // The generated sheet's preceding hand crosses into north frame 6.
 // Apply only the imagegen-cleaned left strip; preserve every body pixel.
 if(r===0 && i===6){
  const patch=await sharp('tools/BeastSwipeArt/north-6-left-cleanup.png').ensureAlpha().raw().toBuffer();
  for(let y=0;y<176;y++)patch.copy(raw,y*176*4,y*32*4,(y+1)*32*4);
 }
 const file=`${root}/${dirs[r]}/${i}.png`;
 const png=await sharp(raw,{raw:{width:176,height:176,channels:4}}).png().toBuffer();
 for(let attempt=0;;attempt++){try{fs.writeFileSync(file,png);break;}catch(e){if(attempt===9)throw e;await new Promise(resolve=>setTimeout(resolve,150));}} 
 const meta=fs.readFileSync(file+'.meta','utf8');
 fs.writeFileSync(file+'.meta',meta.replace(/(      rect:\r?\n        serializedVersion: 2\r?\n)[\s\S]*?(      alignment:)/,'$1        x: 0\n        y: 0\n        width: 176\n        height: 176\n$2'));
 frames.push({r,i,raw});
}
const previews=[];
for(let i=0;i<8;i++){
 const canvas=await sharp({create:{width:704,height:352,channels:4,background:'#353a45'}}).composite(frames.filter(f=>f.i===i).map(f=>({input:f.raw,raw:{width:176,height:176,channels:4},left:(f.r%4)*176,top:Math.floor(f.r/4)*176}))).raw().toBuffer();
 previews.push(canvas);
}
await sharp(Buffer.concat(previews),{raw:{width:704,height:352*8,channels:4,pageHeight:352}}).gif({loop:0,delay:[83,83,84,417,83,83,83,167]}).toFile('tools/BeastSwipeArt/preview.gif');
console.log('Wrote 64 swipe frames, full 176x176 rects, 12fps preview.');




// These existing Sprite Single copies are the images actually used by three directions.
for(const [group,dirs] of [['SwipeEastWestFullFrame',['east','west']],['SwipeNorthEastFullFrame',['north-east']]]){
 for(const dir of dirs)for(let i=0;i<8;i++){
  const dest=`Assets/2.Resources/Art/Beast_Zombie_Boss/Animator/FrameSources/${group}/${dir}-${i}.png`;
  fs.copyFileSync(`${root}/${dir}/${i}.png`,dest);
 }
}
console.log('Synced 24 existing full-frame copies; preserved their meta files and all animation clips.');

