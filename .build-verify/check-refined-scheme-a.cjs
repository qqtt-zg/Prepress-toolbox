const fs = require('node:fs');
const Module = require('node:module');
const path = require('node:path');

// 复用已有单屏验证，保留旧报告，并补齐布局方式往返和断点尺寸。
let source = fs.readFileSync(path.join(__dirname, 'check-single-screen.cjs'), 'utf8');
source = source.replaceAll('.build-verify/single-screen-layout-report.json', '.build-verify/scheme-a-refined-layout-report.json');
source = source.replace('[[1280,900],[1280,800],[1280,720]', '[[1440,900],[1280,900],[1280,800],[1280,781],[1280,780],[1280,760],[1280,750],[1280,720]');
source = source.replace("['settings-auto',async()=>", "['quantity-roundtrip',async()=>{await page.locator('#targetCount').fill('6');await page.locator('#byGrid').click();if(await page.locator('#byGrid').getAttribute('aria-pressed')!=='true'||await page.locator('#layoutValue').textContent()!=='6')throw new Error('数量切回行列未保留布局');}],['settings-auto',async()=>");
source = source.replace("console.log('Interaction blocked: '+width+'x'+height+' '+state);", "errors.push(width+'x'+height+' '+state+': '+e.message);console.log(errors[errors.length-1]);");
const check = new Module(__filename, module);
check.filename = __filename;
check.paths = module.paths;
check._compile(source, __filename);
