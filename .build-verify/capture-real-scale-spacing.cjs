const {chromium} = require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL} = require('node:url');
const path = require('node:path');

(async () => {
    const browser = await chromium.launch({
        headless: true,
        executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe'
    });

    const page = await browser.newPage({viewport: {width: 1440, height: 960}});
    const url = pathToFileURL(path.resolve('docs/自动排版设置界面优化方案.html')).href;
    await page.goto(url);

    // 1. 设置模式 + 真实设置页 941x529 尺寸 (含间距设置 + 方案B纯净安全区)
    await page.locator('#btnSize941').click();
    await page.locator('#btnModeGlobal').click();
    await page.waitForTimeout(300);
    const win1 = page.locator('#windowContainer');
    await win1.screenshot({path: 'output/reviews/真实941x529_设置模式_含排版间距_纯净安全区.png'});
    await win1.screenshot({path: 'output/reviews/方案AB结合_设置模式.png'});

    // 2. 任务模式 + 真实任务弹窗 1160x720 尺寸 (含间距设置 + 真实物理比例成品 + 序号)
    await page.locator('#btnSize1160').click();
    await page.locator('#btnModeTask').click();
    await page.waitForTimeout(300);
    const win2 = page.locator('#windowContainer');
    await win2.screenshot({path: 'output/reviews/真实1160x720_任务模式_真实比例成品_含排版间距.png'});
    await win2.screenshot({path: 'output/reviews/方案AB结合_任务模式.png'});

    await browser.close();
    console.log('Successfully captured real scale and spacing screenshots.');
})().catch(err => {
    console.error(err);
    process.exitCode = 1;
});
