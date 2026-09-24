import fs from 'node:fs';

const { chromium } = await import('file:///C:/Users/admin/AppData/Local/npm-cache/_npx/e41f203b7505f1fb/node_modules/playwright/index.mjs');

const htmlPath = 'file:///D:/vibe_codeing/Prepress-toolbox/docs/%E6%9D%90%E6%96%99%E9%80%89%E6%8B%A9%E6%A1%86%E7%BB%88%E7%89%88%E4%B8%80%E4%BD%93%E5%8C%96%E5%B7%A5%E4%BD%9C%E5%8F%B0.html';
const outDir = 'D:/vibe_codeing/Prepress-toolbox/output/playwright';
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch();
const context = await browser.newContext({ deviceScaleFactor: 4, viewport: { width: 1100, height: 900 } });
const page = await context.newPage();
await page.goto(htmlPath);
await page.waitForTimeout(400);

async function shot(effect, mode, name) {
  await page.evaluate(({ effect, mode }) => {
    const panel = document.getElementById('v4MetricsPanel');
    panel.className = 'metrics-pills-pure effect-border-inset ' + mode;
    const c = document.getElementById('v4ColorVal');
    c.textContent = mode === 'mode-color' ? '彩色' : '黑白';
  }, { effect, mode });
  await page.waitForTimeout(250);
  const box = await page.locator('#v4MetricsPanel').boundingBox();
  await page.locator('#v4MetricsPanel').screenshot({ path: `${outDir}/${name}.png` });
  const meta = await page.evaluate(() => {
    const panel = document.getElementById('v4MetricsPanel');
    const r = panel.getBoundingClientRect();
    return { width: r.width, height: r.height };
  });
  console.log(name, JSON.stringify({ box, meta }));
  fs.writeFileSync(`${outDir}/${name}.json`, JSON.stringify({ box, meta }, null, 2));
}

await shot('effect-border-inset', 'mode-color', 'inset-color-panel');
await shot('effect-border-inset', 'mode-mono', 'inset-mono-panel');
await shot('effect-text-glow', 'mode-color', 'textglow-color-panel');

await page.evaluate(() => {
  const panel = document.getElementById('v4MetricsPanel');
  panel.className = 'metrics-pills-pure effect-border-inset mode-color';
});
await page.waitForTimeout(200);
await page.screenshot({ path: `${outDir}/inset-color-full.png` });

await browser.close();
