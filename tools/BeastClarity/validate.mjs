import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));const sharp=require('sharp');
const art='Assets/2.Resources/Art/Beast_Zombie_Boss',before='.utmp/beast-clarity/before',rebuilt=new Set(JSON.parse(fs.readFileSync('tools/BeastClarity/rebuilt-files.json')));
function walk(d){return fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(`${d}/${e.name}`):[`${d}/${e.name}`]);}
const normalize=s=>s.replace(/\r/g,'').replace(/spritePixelsToUnits: \d+/,'spritePixelsToUnits: X').replace(/textureCompression: \d+/g,'textureCompression: X').replace(/width: (132|176|264|352)/g,'width: X').replace(/height: (132|176|264|352)/g,'height: X');
let textures=0,unchanged=0,nonPng=0;const all=walk(art);
for(const file of all){const old=`${before}/${file.slice(art.length+1)}`;assert(fs.existsSync(old),`Unexpected new file ${file}`);
 if(file.endsWith('.png')){
  const m=await sharp(file).metadata(),om=await sharp(old).metadata(),meta=fs.readFileSync(file+'.meta','utf8'),oldMeta=fs.readFileSync(old+'.meta','utf8');
  const ppu=Number(meta.match(/spritePixelsToUnits: ([\d.]+)/)[1]),oldPpu=Number(oldMeta.match(/spritePixelsToUnits: ([\d.]+)/)[1]);
  assert.equal(m.width/ppu,om.width/oldPpu,file);assert.equal(m.height/ppu,om.height/oldPpu,file);assert.equal(normalize(meta),normalize(oldMeta),file);
  assert([...meta.matchAll(/textureCompression: (\d+)/g)].every(m=>m[1]==='0'));
  if(rebuilt.has(file)){assert.equal(m.width,om.width*2);assert.equal(m.height,om.height*2);const p=await sharp(file).ensureAlpha().raw().toBuffer();for(let y=0;y<m.height;y++)for(let x=0;x<m.width;x++){const a=p[(y*m.width+x)*4+3];assert(a===0||a===255);if(x===0||y===0||x===m.width-1||y===m.height-1)assert.equal(a,0);}}
  else{assert(fs.readFileSync(file).equals(fs.readFileSync(old)),file);unchanged++;}textures++;
 }else if(!file.endsWith('.png.meta')){assert(fs.readFileSync(file).equals(fs.readFileSync(old)),file);nonPng++;}
}
const examples=['Idle/animations/run/south/0.png','Idle/animations/body-charge/east/4.png','Idle/animations/swipe/south/3.png','Idle/animations/swipe-both/east/5.png'];const layers=[];
for(let i=0;i<examples.length;i++){const rel=examples[i],m=await sharp(`${art}/${rel}`).metadata();layers.push({input:await sharp(`${before}/${rel}`).resize(m.width,m.height,{kernel:'nearest'}).png().toBuffer(),left:i*352,top:352-m.height});layers.push({input:fs.readFileSync(`${art}/${rel}`),left:i*352,top:704-m.height});}
await sharp({create:{width:1408,height:704,channels:4,background:'#353a45'}}).composite(layers).png().toFile('tools/BeastClarity/comparison.png');
const summary={textures,rebuilt:rebuilt.size,unchangedPng:unchanged,unchangedOtherFiles:nonPng,checks:'world dimensions, metadata except size/PPU/compression, binary alpha/margins, all other files byte identical',unityPlay:'not run'};
fs.writeFileSync('tools/BeastClarity/validation.json',JSON.stringify(summary,null,2));console.log(summary);
