const {chromium} = require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL} = require('node:url');
const path = require('node:path');
const fs = require('node:fs');

(async () => {
    const outDir = path.resolve('output/reviews');
    if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, {recursive: true});

    const browser = await chromium.launch({
        headless: true,
        executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe'
    });

    const page = await browser.newPage({viewport: {width: 1360, height: 960}});
    const url = pathToFileURL(path.resolve('docs/自动排版设置界面优化方案.html')).href;
    await page.goto(url);

    // 1. 截图方案 A
    await page.locator('#btnSchemeA').click();
    await page.waitForTimeout(300);
    await page.screenshot({path: 'output/reviews/设置优化方案A_精细打磨.png'});

    // 2. 截图方案 B
    await page.locator('#btnSchemeB').click();
    await page.waitForTimeout(300);
    await page.screenshot({path: 'output/reviews/设置优化方案B_规格图示与沙盒解耦.png'});

    await browser.close();
    console.log('Successfully captured screenshots for Scheme A and Scheme B.');
})().catch(err => {
    console.error(err);
    process.exitCode = 1;
});
