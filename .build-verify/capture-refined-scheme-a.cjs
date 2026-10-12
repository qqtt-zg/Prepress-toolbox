const { chromium } = require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const fs = require('node:fs');
(async () => {
  const folder = path.resolve('output/playwright/scheme-a-refined');
  fs.mkdirSync(folder, { recursive: true });
  const browser = await chromium.launch({ headless: true, executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe' });
  const page = await browser.newPage();
  const results = [];
  for (const [width, height, state] of [[1280,800,'specified'],[1280,900,'specified'],[1280,800,'multiple'],[1280,800,'flat'],[800,600,'specified'],[600,720,'extend']]) {
    await page.setViewportSize({ width, height });
    await page.goto(pathToFileURL(path.resolve('docs/V5卷装参数与居中布局预览方案.html')).href);
    if (state === 'multiple') await page.locator('#strategyMultiple').click();
    if (state === 'flat') await page.locator('[data-material=flat]').click();
    if (state === 'extend') await page.locator('#scenarioSelect').selectOption('extend');
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path: path.join(folder, `${width}x${height}-${state}.png`) });
    results.push(await page.evaluate(() => {
      const parameters = document.querySelector('#parametersPane').getBoundingClientRect();
      const last = document.querySelector('.strategy-group').getBoundingClientRect();
      const preview = document.querySelector('#previewPane').getBoundingClientRect();
      return { viewport:[innerWidth,innerHeight],parameterWidth:parameters.width,previewWidth:preview.width,unusedHeight:Math.max(0,parameters.bottom-last.bottom),summaryHeight:document.querySelector('#previewResults').getBoundingClientRect().height };
    }));
  }
  await page.setViewportSize({ width:1280,height:800 });
  await page.goto(pathToFileURL(path.resolve('docs/V5卷装参数与居中布局预览方案.html')).href);
  await page.locator('#width').focus();
  await page.keyboard.press('Tab');
  if (await page.evaluate(() => document.activeElement.id) !== 'length') throw new Error('尺寸输入键盘顺序异常');
  await page.locator('#showHelp').click();
  if (!await page.evaluate(() => document.querySelector('#helpDialog').contains(document.activeElement))) throw new Error('说明对话框未接收焦点');
  await page.keyboard.press('Escape');
  if (await page.evaluate(() => document.activeElement.id) !== 'showHelp') throw new Error('说明关闭后焦点未返回');
  await page.locator('[data-material=flat]').click();
  await page.locator('#getPdfSize').click();
  await page.keyboard.press('Escape');
  if (await page.evaluate(() => document.activeElement.id) !== 'getPdfSize') throw new Error('纸张选择关闭后焦点未返回');
  await browser.close();
  console.log(JSON.stringify({results,keyboardAndDialogFocus:'passed'}));
})().catch(e => { console.error(e); process.exitCode = 1; });
