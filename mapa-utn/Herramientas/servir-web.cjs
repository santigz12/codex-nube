const http = require('http');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../Builds/Web');
const mime = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.png':'image/png','.css':'text/css'};
http.createServer((req,res)=>{
 let requested;
 try { requested = decodeURIComponent(new URL(req.url,'http://localhost').pathname); } catch {res.writeHead(400).end();return;}
 const file=path.resolve(root,'.'+(requested==='/'?'/index.html':requested));
 if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}
 fs.stat(file,(error,stat)=>{
  if(error||!stat.isFile()){res.writeHead(404).end('No encontrado');return;}
  res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':'no-cache'});
  fs.createReadStream(file).pipe(res);
 });
}).listen(8080,'127.0.0.1',()=>console.log('Mapa UTN disponible en http://127.0.0.1:8080 · Ctrl+C para cerrar'));
