const fs = require('node:fs');
const path = require('node:path');
const folder = path.resolve('output/reviews/scheme-a');
const reportPath = path.join(folder, '方案A_审美评审.html');
let html = fs.readFileSync(reportPath, 'utf8');
for (const [placeholder, filename] of [['SHOT900', 'scheme-a-1280x900.png'], ['SHOT800', 'scheme-a-1280x800.png'], ['SHOTMULTIPLE', 'scheme-a-multiple.png']]) {
  html = html.replace(`{{${placeholder}}}`, `data:image/png;base64,${fs.readFileSync(path.join(folder, filename)).toString('base64')}`);
}
const axis = n => { const a = -Math.PI / 2 + n * 2 * Math.PI / 5; return [Math.cos(a), Math.sin(a)]; };
const point = (n, value) => { const [x,y] = axis(n); return `${(200+x*110*value).toFixed(1)},${(180+y*110*value).toFixed(1)}`; };
const rings = [0.2,0.4,0.6,0.8,1].map(v=>`<polygon points="${[0,1,2,3,4].map(n=>point(n,v)).join(' ')}" fill="none" stroke="#dbe4ef"/>`).join('');
const axes = [0,1,2,3,4].map(n=>`<line x1="200" y1="180" x2="${point(n,1).split(',')[0]}" y2="${point(n,1).split(',')[1]}" stroke="#dbe4ef"/>`).join('');
const scoreShape = [7,5,6,5,6].map((value,n)=>point(n,value/10)).join(' ');
const svg = `<svg class="radar" viewBox="0 0 400 350" role="img" aria-labelledby="radarTitle radarDesc"><title id="radarTitle">五维评分雷达图</title><desc id="radarDesc">一致性7、层级5、细节6、功能5、创新6，满分10</desc>${rings}${axes}<polygon points="${scoreShape}" fill="#1677ff22" stroke="#1677ff" stroke-width="2"/><text x="200" y="42" text-anchor="middle">一致性 7</text><text x="326" y="144" text-anchor="middle">层级 5</text><text x="282" y="307" text-anchor="middle">细节 6</text><text x="118" y="307" text-anchor="middle">功能 5</text><text x="74" y="144" text-anchor="middle">创新 6</text></svg>`;
html = html.replace('{{RADAR}}', svg);
fs.writeFileSync(reportPath, html);
console.log(JSON.stringify({ reportPath, bytes: Buffer.byteLength(html), unresolvedPlaceholders: /\{\{[A-Z]+\}\}/.test(html) }));
