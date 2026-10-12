const {chromium} = require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL} = require('node:url');
const path = require('node:path');

(async () => {
    const browser = await chromium.launch({
        headless: true,
        executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe'
    });

    const page = await browser.newPage({viewport: {width: 1360, height: 960}});
    const url = pathToFileURL(path.resolve('docs/自动排版设置界面优化方案.html')).href;
    await page.goto(url);

    // 1. 设置模式 (941x529)
    await page.locator('#btnSize941').click();
    await page.locator('#btnModeGlobal').click();
    await page.waitForTimeout(300);

    // 截取整页（对应用户引用的方案AB结合_设置模式.png）
    await page.screenshot({path: 'output/reviews/方案AB结合_设置模式.png'});

    // 截取 1:1 原生窗口特写
    const win1 = page.locator('#windowContainer');
    await win1.screenshot({path: 'output/reviews/真实941x529_系统设置模式_含倍数布局.png'});

    // 2. 任务模式 (1160x720)
    await page.locator('#btnSize1160').click();
    await page.locator('#btnModeTask').click();
    await page.waitForTimeout(300);
    const win2 = page.locator('#windowContainer');
    await win2.screenshot({path: 'output/reviews/真实1160x720_任务排版模式_含倍数布局.png'});

    await browser.close();
    console.log('Successfully captured screenshots: Arrow position calibrated.');
})().catch(err => {
    console.error(err);
    process.exitCode = 1;
});
