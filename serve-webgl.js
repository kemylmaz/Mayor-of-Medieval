// Minimal static server for the Unity WebGL build.
// Unity ships .br files pre-compressed, so the server must advertise the encoding —
// without these headers the loader cannot decompress and the player never starts.
const http = require('http');
const fs = require('fs');
const path = require('path');

const ROOT = path.join(__dirname, 'Builds', 'WebGL');
const PORT = 8080;

const TYPES = {
  '.html': 'text/html',
  '.js': 'application/javascript',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.json': 'application/json',
  '.css': 'text/css',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
};

http.createServer((req, res) => {
  const urlPath = decodeURIComponent(req.url.split('?')[0]);
  let filePath = path.join(ROOT, urlPath === '/' ? 'index.html' : urlPath);

  fs.readFile(filePath, (err, data) => {
    if (err) {
      res.writeHead(404);
      res.end('not found: ' + urlPath);
      return;
    }

    let ext = path.extname(filePath);
    const headers = {};

    if (ext === '.br') {
      headers['Content-Encoding'] = 'br';
      ext = path.extname(filePath.slice(0, -3));
    } else if (ext === '.gz') {
      headers['Content-Encoding'] = 'gzip';
      ext = path.extname(filePath.slice(0, -3));
    }

    headers['Content-Type'] = TYPES[ext] || 'application/octet-stream';
    res.writeHead(200, headers);
    res.end(data);
  });
}).listen(PORT, () => console.log('serving ' + ROOT + ' on http://localhost:' + PORT));
