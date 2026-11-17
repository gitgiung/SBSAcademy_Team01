import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

// 검증 파일만 loopback에 노출한다. 앱/운영 호스팅 서버가 아니다.
const folder = path.resolve(process.argv[2]);
const port = Number(process.argv[3] || 18763);
const releaseAttempts = new Map();
const server = http.createServer((request, response) => {
  if (request.url === '/large-release.json' || request.url === '/chunked-release.json') {
    if (request.url === '/large-release.json') response.setHeader('Content-Length', 80000);
    response.write('x'.repeat(40000));
    response.end('y'.repeat(40000));
    return;
  }
  if (request.url === '/redirect-release') {
    response.writeHead(302, { Location: '/release.json' }).end();
    return;
  }
  if (request.url === '/flaky-release.json' || request.url === '/ui-flaky-release.json') {
    const attempt = (releaseAttempts.get(request.url) || 0) + 1;
    releaseAttempts.set(request.url, attempt);
    if (attempt === 1) { response.writeHead(404).end(); return; }
    request.url = '/release.json';
  }
  if (request.url === '/slow-release.json') {
    setTimeout(() => response.end(fs.readFileSync(path.join(folder, 'release.json'))), 300);
    return;
  }
  const allowed = new Set(['/remote-download-fixture', '/release.json', '/catalog.bin']);
  if (!allowed.has(request.url)) {
    response.writeHead(404).end();
    return;
  }
  const file = path.join(folder, request.url.slice(1));
  const stat = fs.statSync(file);
  response.writeHead(200, { 'Content-Length': stat.size, 'Content-Type': 'application/octet-stream' });
  const stream = fs.createReadStream(file, { highWaterMark: 8192 });
  stream.on('data', chunk => {
    stream.pause();
    response.write(chunk);
    setTimeout(() => stream.resume(), 15);
  });
  stream.on('end', () => response.end());
  stream.on('error', () => response.destroy());
  response.on('close', () => stream.destroy());
});
server.listen(port, '127.0.0.1', () => console.log(`Remote fixture ready on port ${port}`));
