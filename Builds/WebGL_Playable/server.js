const http = require('http');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');

const PORT = 8088;
const DIR = __dirname;

const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.htm': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.json': 'application/json',
  '.css': 'text/css',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.ico': 'image/x-icon',
  '.wasm': 'application/wasm'
};

const server = http.createServer((req, res) => {
  let urlPath = decodeURIComponent(req.url.split('?')[0]).replace(/^\/+/, '');
  if (!urlPath || urlPath === 'index.htm') urlPath = 'index.html';

  let filePath = path.join(DIR, urlPath);
  if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
    res.statusCode = 404;
    res.setHeader('Content-Type', 'text/plain; charset=utf-8');
    res.end('404 Not Found: ' + urlPath);
    return;
  }

  let ext = path.extname(filePath).toLowerCase();
  let contentType = MIME_TYPES[ext] || 'application/octet-stream';

  if (filePath.endsWith('.wasm.unityweb')) {
    contentType = 'application/wasm';
    res.setHeader('Content-Encoding', 'gzip');
  } else if (filePath.endsWith('.data.unityweb')) {
    contentType = 'application/octet-stream';
    res.setHeader('Content-Encoding', 'gzip');
  } else if (filePath.endsWith('.framework.js.unityweb')) {
    contentType = 'application/javascript';
    res.setHeader('Content-Encoding', 'gzip');
  }

  res.setHeader('Content-Type', contentType);
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Cache-Control', 'no-cache');

  fs.createReadStream(filePath).pipe(res);
});

server.listen(PORT, '127.0.0.1', () => {
  console.log('======================================================================');
  console.log('  CARO XO PRO - WEBGL PLAYABLE PREVIEW SERVER');
  console.log(`  Local URL: http://localhost:${PORT}/index.html`);
  console.log('  (Nhan Ctrl+C tren cua so nay de dung server)');
  console.log('======================================================================');

  const openCmd = process.platform === 'win32'
    ? `start http://localhost:${PORT}/index.html`
    : (process.platform === 'darwin' ? `open http://localhost:${PORT}/index.html` : `xdg-open http://localhost:${PORT}/index.html`);
  exec(openCmd);
});

