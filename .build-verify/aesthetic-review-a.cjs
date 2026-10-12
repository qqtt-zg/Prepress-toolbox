const { chromium } = require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');

(async () => {
  const folder = path.resolve('output/reviews/scheme-a');
  fs.mkdirSync(folder, { recursive: true });
  const browser = await chromium.launch({ headless: true, executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe' });
  const page = await browser.newPage();
  const results = [];
  for (const height of [900, 800]) {
    await page.setViewportSize({ width: 1280, height });
    await page.goto(pathToFileURL(path.resolve('docs/V5卷装参数与居中布局预览方案.html')).href);
    await page.locator('#gridValue').waitFor();
    await page.evaluate(() => document.fonts.ready);
    const metrics = await page.evaluate(() => {
      const rect = selector => { const r = document.querySelector(selector).getBoundingClientRect(); return { x: r.x, y: r.y, width: r.width, height: r.height, bottom: r.bottom }; };
      const visible = e => e.getClientRects().length > 0 && getComputedStyle(e).visibility !== 'hidden';
      return {
        parameters: rect('#parametersPane'), preview: rect('#previewPane'), strategy: rect('.strategy-group'), summary: rect('#previewResults'), stage: rect('#stage'),
        visibleButtons: [...document.querySelectorAll('button')].filter(visible).length,
        editorButtons: [...document.querySelectorAll('.window button')].filter(visible).length,
        fonts: Object.fromEntries(['.sectionhead h3', '.field>label', '.hint', '.inputwrap input', '.result-item>b'].map(s => [s, getComputedStyle(document.querySelector(s)).fontSize]))
      };
    });
    await page.screenshot({ path: path.join(folder, `scheme-a-1280x${height}.png`) });
    results.push({ viewport: [1280, height], ...metrics });
  }
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.reload();
  await page.locator('#strategyMultiple').click();
  await page.screenshot({ path: path.join(folder, 'scheme-a-multiple.png') });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.locator('#strategyFixed').click();
  await page.locator('#byCount').click();
  await page.locator('#targetCount').fill('6');
  await page.locator('#byGrid').click();
  await browser.close();
  fs.writeFileSync(path.join(folder, 'evidence.json'), JSON.stringify({ results, roundTripErrors: errors }, null, 2));
  console.log(JSON.stringify({ results, roundTripErrors: errors }, null, 2));
})().catch(e => { console.error(e); process.exitCode = 1; });
