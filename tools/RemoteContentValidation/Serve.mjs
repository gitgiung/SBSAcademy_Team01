import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

// 검증 파일 하나만 loopback에 노출한다. 앱/운영 호스팅 서버가 아니다.
const folder = path.resolve(process.argv[2]);
const port = Number(process.argv[3] || 18763);
const server = http.createServer((request, response) => {
  if (request.url !== '/remote-download-fixture') {
    response.writeHead(404).end();
    return;
  }
  const file = path.join(folder, 'remote-download-fixture');
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
