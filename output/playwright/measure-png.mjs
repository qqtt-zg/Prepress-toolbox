// 读取 PNG 指定剖面像素，用于和 HTML 参考截图做像素级对照。
import fs from 'node:fs';

const { chromium } = await import('file:///C:/Users/admin/AppData/Local/npm-cache/_npx/e41f203b7505f1fb/node_modules/playwright/index.mjs');

const file = process.argv[2];
const spec = process.argv[3]; // e.g. "row:10:0:40" or "col:5:0:20" or "box"
const browser = await chromium.launch();
const page = await browser.newPage();
const base64 = fs.readFileSync(file).toString('base64');
const result = await page.evaluate(async ({ base64, spec }) => {
  const img = new Image();
  img.src = 'data:image/png;base64,' + base64;
  await img.decode();
  const c = document.createElement('canvas');
  c.width = img.width;
  c.height = img.height;
  const ctx = c.getContext('2d');
  ctx.drawImage(img, 0, 0);
  const hex = (r, g, b) => '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0').toUpperCase()).join('');
  const at = (x, y) => {
    const d = ctx.getImageData(x, y, 1, 1).data;
    return hex(d[0], d[1], d[2]);
  };
  const out = { size: [img.width, img.height] };
  const parts = spec.split(':');
  if (parts[0] === 'row') {
    const y = +parts[1], x0 = +parts[2], x1 = +parts[3];
    out.values = [];
    for (let x = x0; x <= x1; x++) out.values.push(at(x, y));
  } else if (parts[0] === 'col') {
    const x = +parts[1], y0 = +parts[2], y1 = +parts[3];
    out.values = [];
    for (let y = y0; y <= y1; y++) out.values.push(at(x, y));
  } else if (parts[0] === 'box') {
    const x0 = +parts[1], y0 = +parts[2], x1 = +parts[3], y1 = +parts[4];
    out.values = [];
    for (let y = y0; y <= y1; y++) {
      const row = [];
      for (let x = x0; x <= x1; x++) row.push(at(x, y));
      out.values.push(row.join(' '));
    }
  }
  return out;
}, { base64, spec });
console.log(JSON.stringify(result, null, 1));
await browser.close();
