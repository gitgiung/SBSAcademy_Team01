import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(process.argv[2] || '.utmp/skill-pilot/server');
const port = Number(process.argv[3] || 18763);
// 개발 검증용. 기기는 향후 adb reverse를 통해 접근하며 운영 CDN으로 사용하지 않는다.
http.createServer((request, response) => {
  const match = /^\/content\/(Android|Windows)\/([A-Za-z0-9_.-]+)$/.exec(request.url || '');
  if (!match || !['GET', 'HEAD'].includes(request.method)) { response.writeHead(404).end(); return; }
  const fileName = match[2];
  const publicFile = fileName === 'release.json' || /^catalog.*\.(bin|hash)$/.test(fileName) || fileName.endsWith('.bundle');
  const file = path.resolve(root, match[1], fileName);
  if (!publicFile || !file.startsWith(root + path.sep) || !fs.existsSync(file)) { response.writeHead(404).end(); return; }
  const stat = fs.statSync(file);
  response.writeHead(200, { 'Content-Length': stat.size, 'Content-Type': 'application/octet-stream' });
  if (request.method === 'HEAD') { response.end(); return; }
  const stream = fs.createReadStream(file);
  stream.on('error', () => response.destroy());
  response.on('close', () => stream.destroy());
  stream.pipe(response);
}).listen(port, '127.0.0.1', () => console.log(`Skill pilot development server ready on ${port}`));
