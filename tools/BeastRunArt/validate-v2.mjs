import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createRequire} from 'node:module';
const require=createRequire(path.resolve('.utmp/skill-icons-runtime/package.json'));
const sharp=require('sharp');
const root='Assets/2.Resources/Art/Beast_Zombie_Boss/Idle/animations/run';
const dirs=['north','north-east','east','south-east','south','south-west','west','north-west'];
let passed=0;
for(const dir of dirs){
 const unique=new Set();
 for(let i=0;i<8;i++){
  const file=`${root}/${dir}/${i}.png`,png=fs.readFileSync(file),meta=fs.readFileSync(file+'.meta','utf8');
  const {data,info}=await sharp(png).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  assert.equal(info.width,132);assert.equal(info.height,132);
  for(let y=0;y<132;y++)for(let x=0;x<132;x++){const a=data[(y*132+x)*4+3];assert(a===0||a===255);if(x<8||x>=124||y<8||y>=124)assert.equal(a,0);}
  const guid=meta.match(/^guid: (\w+)/m)[1];assert(meta.includes('width: 132'));assert(meta.includes('height: 132'));
  const title=dir.split('-').map(s=>s[0].toUpperCase()+s.slice(1)).join('');
  const clip=fs.readFileSync(`Assets/2.Resources/Art/Beast_Zombie_Boss/Animator/Clips/BossRun_${title}.anim`,'utf8');assert(clip.includes(guid));assert(clip.includes('m_SampleRate: 12'));assert(clip.includes('m_LoopTime: 1'));
  const before=`.utmp/run-feedback/before/${dir}/${i}.png`;
  if(fs.existsSync(before)){assert(fs.readFileSync(before+'.meta').equals(fs.readFileSync(file+'.meta')));if(dir==='west')assert(fs.readFileSync(before).equals(png));else assert(!fs.readFileSync(before).equals(png));}
  unique.add(png.toString('base64'));passed++;
 }
 assert.equal(unique.size,8,dir);
}
console.log(`${passed}/64 frame checks passed: unique PNG, canvas, alpha, margins, clip references and loop/12fps. Available before-snapshot meta/West checks passed. Motion quality still requires playback review.`);
