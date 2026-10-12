const {chromium}=require('C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL}=require('node:url');
const path=require('node:path');
const fs=require('node:fs');

function measure(){
    const visible=e=>e.getClientRects().length&&getComputedStyle(e).visibility!=='hidden';
    const box=e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,w:r.width,h:r.height,right:r.right,bottom:r.bottom};};
    const regions=['.page','.window','#parametersPane','#previewPane','.windowfoot','.workbench'];
    const regionsReport=regions.map(sel=>{const e=document.querySelector(sel);return {sel,rect:box(e),client:[e.clientWidth,e.clientHeight],scroll:[e.scrollWidth,e.scrollHeight],overflow:[getComputedStyle(e).overflowX,getComputedStyle(e).overflowY]};});
    const outside=[...document.querySelectorAll('input,select,button')].filter(visible).map(e=>({name:e.id||e.textContent.trim(),rect:box(e)})).filter(o=>o.rect.x<-.5||o.rect.y<-.5||o.rect.right>innerWidth+.5||o.rect.bottom>innerHeight+.5);
    const panes=['#parametersPane','#previewPane'];
    const escaped=panes.flatMap(sel=>{const pane=document.querySelector(sel),b=box(pane);return [...pane.querySelectorAll('input,select,button')].filter(visible).map(e=>({pane:sel,name:e.id||e.textContent.trim(),rect:box(e)})).filter(o=>o.rect.x<b.x-.5||o.rect.right>b.right+.5||o.rect.y<b.y-.5||o.rect.bottom>b.bottom+.5);});
    const overflow=regionsReport.filter(r=>r.scroll[0]>r.client[0]+1||r.scroll[1]>r.client[1]+1);
    return {viewport:[innerWidth,innerHeight],document:[document.documentElement.scrollWidth,document.documentElement.scrollHeight],outside,escaped,overflow,regions:regionsReport,ok:document.documentElement.scrollWidth<=innerWidth+1&&document.documentElement.scrollHeight<=innerHeight+1&&!outside.length&&!escaped.length&&!overflow.length};
}

(async()=>{
    const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});
    const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
    const file=pathToFileURL(path.resolve('docs/V5卷装参数与居中布局预览方案.html')).href;
    const sizes=[[1280,900],[1280,800],[1280,720],[1024,768],[960,640],[800,700],[800,600],[600,900],[600,720],[460,900]];
    const reports=[];page.setDefaultTimeout(2500);
    for(const [width,height]of sizes){
        await page.setViewportSize({width,height});await page.goto(file);await page.locator('#apply').waitFor();
        const states=[['specified-roll',async()=>{}],['automatic',async()=>page.locator('#strategyAuto').click()],['multiples',async()=>{await page.locator('#strategyMultiple').click();await page.locator('#direction').selectOption('row');await page.locator('#multiple').fill('3');}],['specified-flat',async()=>{await page.locator('#strategyFixed').click();await page.locator('[data-material=flat]').click();}],['paper-picker',async()=>page.locator('#getPdfSize').click()],['multiple-conflict',async()=>page.locator('#scenarioSelect').selectOption('conflict')],['long-roll',async()=>page.locator('#scenarioSelect').selectOption('extend')],['quantity',async()=>{await page.locator('#byCount').click();await page.locator('#targetCount').fill('120');}],['settings-auto',async()=>{await page.locator('[data-entry=settings]').click();await page.locator('[data-material=flat]').click();}],['settings-specified',async()=>page.locator('#strategyFixed').click()],['settings-warning',async()=>{await page.locator('#width').fill('110');await page.locator('#length').fill('100');await page.locator('#referenceSelect').selectOption('large');}]];
        for(const [state,action]of states){
            try{await action();}catch(e){console.log('Interaction blocked: '+width+'x'+height+' '+state);}
            await page.waitForTimeout(60);const report=await page.evaluate(measure);report.state=state;reports.push(report);
            fs.writeFileSync('.build-verify/single-screen-layout-report.json',JSON.stringify({errors,reports},null,2));
            if(!report.ok)console.log(JSON.stringify({viewport:report.viewport,state,document:report.document,outside:report.outside.map(x=>x.name),escaped:report.escaped.map(x=>x.name),overflow:report.overflow.map(x=>x.sel)}));
            if(await page.locator('#pdfSizePicker').evaluate(e=>e.open))await page.locator('#closePaperPicker').click();
        }
    }
    fs.writeFileSync('.build-verify/single-screen-layout-report.json',JSON.stringify({errors,reports},null,2));
    console.log(JSON.stringify({passed:reports.filter(r=>r.ok).length,total:reports.length,pageErrors:errors}));
    await browser.close();process.exitCode=reports.every(r=>r.ok)&&!errors.length?0:1;
})().catch(e=>{console.error(e);process.exitCode=1;});
