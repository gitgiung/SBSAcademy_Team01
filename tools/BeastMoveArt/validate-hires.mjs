import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/move';
let count=0;
for(const dir of fs.readdirSync(root).filter(d=>fs.statSync(`${root}/${d}`).isDirectory()))for(let i=0;i<8;i++){
 const file=`${root}/${dir}/${i}.png`,meta=fs.readFileSync(file+'.meta','utf8');
 const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 assert.equal(info.width,264);assert.equal(info.height,264);assert(meta.includes('spritePixelsToUnits: 200'));assert(meta.includes('width: 264'));assert(meta.includes('height: 264'));
 assert([...meta.matchAll(/textureCompression: (\d+)/g)].every(m=>m[1]==='0'));
 for(let y=0;y<264;y++)for(let x=0;x<264;x++){const a=data[(y*264+x)*4+3];assert(a===0||a===255);if(x<22||x>=242||y<22||y>=242)assert.equal(a,0);}
 const oldPath=`.utmp/move-clarity/before/${dir}/${i}.png.meta`;
 if(fs.existsSync(oldPath)){
  const old=fs.readFileSync(oldPath,'utf8');
  const normalize=s=>s.replace(/\r/g,'').replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: X').replace(/textureCompression: \d+/g,'textureCompression: X').replace(/width: (132|264)/g,'width: X').replace(/height: (132|264)/g,'height: X');
  assert.equal(normalize(meta),normalize(old));assert.equal(264/200,132/100);
 }
 const title=dir.split('-').map(s=>s[0].toUpperCase()+s.slice(1)).join('');
 const clip=fs.readFileSync(`Assets/2.Resources/Art/Beast_Zombie_Boss/Animator/Clips/BossWalk_${title}.anim`,'utf8');assert(clip.includes(meta.match(/^guid: (\w+)/m)[1]));count++;
}
const comparisons=[];
for(const [j,dir] of ['south','east','north','south-east'].entries()){
 const before=`.utmp/move-clarity/before/${dir}/0.png`;
 if(!fs.existsSync(before))continue;
 comparisons.push({input:await sharp(before).resize(264,264,{kernel:'nearest'}).png().toBuffer(),left:j*264,top:0});
 comparisons.push({input:fs.readFileSync(`${root}/${dir}/0.png`),left:j*264,top:264});
}
if(comparisons.length)await sharp({create:{width:1056,height:528,channels:4,background:'#353a45'}}).composite(comparisons).png().toFile('tools/BeastMoveArt/clarity-comparison.png');
console.log(`${count}/64 passed: source output size/alpha/margin, no compression, same world canvas, clip GUIDs, only intended meta fields changed. Comparison top=before bottom=after.`);
