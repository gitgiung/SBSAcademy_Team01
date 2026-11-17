import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));
const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/move';
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
const frames=[];
for(let r=0;r<8;r++)for(let i=0;i<8;i++){
 const ns=dirs[r]==='north'||dirs[r]==='south';
 const source=ns?'tools/BeastChargeArt/gait-source.png':'tools/BeastMoveArt/move-source.png';
 const rows=ns?4:8,row=ns?(dirs[r]==='north'?0:1):r;
 const {width,height}=await sharp(source).metadata();
 const left=Math.round(i*width/8),top=Math.round(row*height/rows);
 const raw=await sharp(source).extract({left,top,width:Math.round((i+1)*width/8)-left,height:Math.round((row+1)*height/rows)-top}).resize(220,220,{kernel:'nearest'}).ensureAlpha().raw().toBuffer();
 for(let p=0;p<raw.length;p+=4){raw[p+3]=raw[p+3]>=200?255:0;if(!raw[p+3])raw.fill(0,p,p+4);}
 const png=await sharp({create:{width:264,height:264,channels:4,background:'#00000000'}}).composite([{input:raw,raw:{width:220,height:220,channels:4},left:22,top:22}]).png().toBuffer();
 const file=`${root}/${dirs[r]}/${i}.png`;
 for(let attempt=0;;attempt++){try{fs.writeFileSync(file,png);break;}catch(e){if(attempt===9)throw e;await new Promise(resolve=>setTimeout(resolve,150));}}
 let meta=fs.readFileSync(file+'.meta','utf8');
 meta=meta.replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: 200').replace(/textureCompression: \d+/g,'textureCompression: 0');
 meta=meta.replace(/(      rect:\r?\n        serializedVersion: 2\r?\n)[\s\S]*?(      alignment:)/,'$1        x: 0\n        y: 0\n        width: 264\n        height: 264\n$2');
 fs.writeFileSync(file+'.meta',meta);frames.push({r,i,png});
}
const pages=[];
for(let i=0;i<8;i++)pages.push(await sharp({create:{width:1056,height:528,channels:4,background:'#353a45'}}).composite(frames.filter(f=>f.i===i).map(f=>({input:f.png,left:f.r%4*264,top:Math.floor(f.r/4)*264}))).raw().toBuffer());
await sharp(Buffer.concat(pages),{raw:{width:1056,height:528*8,channels:4,pageHeight:528}}).gif({loop:0,delay:[83,83,83,83,83,83,83,167]}).toFile('tools/BeastMoveArt/preview.gif');
console.log('64 frames rebuilt directly from source: 264px/200PPU, compression off, same world canvas and timing.');
