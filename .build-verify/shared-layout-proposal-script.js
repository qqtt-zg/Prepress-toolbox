'use strict';
const $ = id => document.getElementById(id);
const SETTINGS_BASE = {enabled:true,rows:0,columns:0,flat:{width:320,length:464,left:10,right:10,top:10,bottom:10},roll:{width:320,length:420,left:10,right:10,top:10,bottom:10}};
const makeConfig = roll => ({...SETTINGS_BASE[roll?'roll':'flat'],rotation:'auto',mode:'grid',rows:4,columns:2,target:8,choice:'',constraint:roll,direction:'column',multiple:2});
let savedDefaults = {enabled:true,type:'roll',configs:{flat:{...makeConfig(false),rows:0,columns:0,constraint:false},roll:{...makeConfig(true),rows:0,columns:0,constraint:false}}};
let applied = {enabled:true,type:'roll',configs:{flat:makeConfig(false),roll:makeConfig(true)}};
let globalDraft = structuredClone(savedDefaults), temporaryDraft = structuredClone(applied), draft = temporaryDraft;
let hostMode='temporary', alignment='center', result=null, timer, pdfPickerOpen=false;
let viewMode='fit', zoom=20, panHorizontal=50, panVertical=0;
const files = {normal:{name:'示例订单_90×80.pdf',width:90,height:80},small:{name:'示例标签_40×60.pdf',width:40,height:60},large:{name:'示例大页_200×300.pdf',width:200,height:300}};
let taskFile='normal', referenceFile='normal', referenceSize={width:90,height:80};
const numberFields = {width:'width',length:'length',marginLeft:'left',marginRight:'right',marginTop:'top',marginBottom:'bottom',rows:'rows',columns:'columns',targetCount:'target',multiple:'multiple'};
const current = () => draft.configs[draft.type];
const product = () => hostMode==='settings'?referenceSize:files[taskFile];
const integer = (n,min,max) => Number.isInteger(n)&&n>=min&&n<=max;
const fmt = n => Number.isInteger(n)?String(n):n.toFixed(2).replace(/0+$/,'').replace(/\.$/,'');
function validateParameters(type,s) {
    if(!Number.isFinite(s.width)||s.width<=0||!Number.isFinite(s.length)||s.length<=0||![s.left,s.right,s.top,s.bottom].every(n=>Number.isFinite(n)&&n>=0))return {ok:false,kind:'parameter',message:'材料尺寸须为正数，四边距须为非负数。'};
    if(s.width<=s.left+s.right||(type==='flat'&&s.length<=s.top+s.bottom))return {ok:false,kind:'parameter',message:'边距已占满材料，可印刷区域必须大于 0。'};
    if(s.constraint&&!integer(s.multiple,2,16))return {ok:false,kind:'parameter',message:'约束倍数请输入 2–16 的整数。'};
    if(s.mode==='grid'&&(!Number.isSafeInteger(s.rows)||s.rows<0||!Number.isSafeInteger(s.columns)||s.columns<0))return {ok:false,kind:'parameter',message:'手动行列须为非负整数；自动模式不固定行列。'};
    if(s.mode==='count'&&(!Number.isSafeInteger(s.target)||s.target<1))return {ok:false,kind:'parameter',message:'布局数须为正整数。'};
    return {ok:true};
}
/* CALCULATE */
/* SVG_HELPER */
function draw(s,r) {
    const stage=$('stage'); stage.replaceChildren();
    for(const id of ['zoomOut','zoomIn','fitView','detailView'])$(id).disabled=!r.ok;
    if(!r.ok){$('previewNavigation').hidden=true;const empty=document.createElement('div');empty.className='empty';empty.textContent=r.disabled?'排版已停用 · 参数草稿保留':'暂无可用布局 · 请检查尺寸、方向或约束';stage.append(empty);return;}
    const stageWidth=stage.clientWidth||450,stageHeight=stage.clientHeight||280;
    const fullWidth=s.width+130,fullHeight=r.actualLength+110;
    const fitScale=Math.min(stageWidth/fullWidth,stageHeight/fullHeight);
    if(viewMode==='fit')zoom=fitScale/3.77952756*100;
    const scale=viewMode==='fit'?fitScale:zoom/100*3.77952756;
    const visibleWidth=stageWidth/scale,visibleHeight=stageHeight/scale;
    const x=-65+Math.max(0,fullWidth-visibleWidth)*panHorizontal/100-Math.max(0,visibleWidth-fullWidth)/2;
    const y=-55+Math.max(0,fullHeight-visibleHeight)*panVertical/100-Math.max(0,visibleHeight-fullHeight)/2;
    $('zoomLabel').textContent=fmt(Math.round(zoom))+'%';
    $('fitView').classList.toggle('selected',viewMode==='fit');$('detailView').classList.toggle('selected',viewMode==='detail');
    $('previewNavigation').hidden=viewMode==='fit'||(visibleWidth>=fullWidth&&visibleHeight>=fullHeight);
    $('panXRow').hidden=visibleWidth>=fullWidth;$('panYRow').hidden=visibleHeight>=fullHeight;
    $('panX').value=panHorizontal;$('panY').value=panVertical;
    const start=Math.max(0,Math.min(r.actualLength,y)),end=Math.max(start,Math.min(r.actualLength,y+visibleHeight));
    $('visibleRange').textContent=`查看纵向 ${fmt(start)}–${fmt(end)} mm / 全长 ${fmt(r.actualLength)} mm`;
    const svg=svgElement('svg',{viewBox:`${x} ${y} ${visibleWidth} ${visibleHeight}`,role:'img','aria-label':`${draft.type==='roll'?'卷装':'平张'}，${r.r}行${r.c}列；左留白${fmt(r.leftGap)}毫米，右留白${fmt(r.rightGap)}毫米。`});
    const defs=svgElement('defs'),marker=svgElement('marker',{id:'arrow',markerWidth:6,markerHeight:6,refX:3,refY:3,orient:'auto-start-reverse'});
    marker.append(svgElement('path',{d:'M0,0 L6,3 L0,6',fill:'#8498b1'}));defs.append(marker);svg.append(defs);
    svg.append(svgElement('rect',{x:5,y:6,width:s.width,height:r.actualLength,fill:'#e8edf5'}));
    svg.append(svgElement('rect',{x:0,y:0,width:s.width,height:r.actualLength,fill:'#fff',stroke:'#b4c5d9','stroke-width':1.5}));
    const pw=s.width-s.left-s.right,ph=r.actualLength-s.top-s.bottom;
    svg.append(svgElement('rect',{x:s.left,y:s.top,width:pw,height:ph,fill:'none',stroke:'#97aabd','stroke-dasharray':'4 4','stroke-width':1}));
    if(alignment==='center'){
        svg.append(svgElement('line',{x1:s.left+pw/2,y1:s.top,x2:s.left+pw/2,y2:r.actualLength-s.bottom,stroke:'#d5e1f0','stroke-dasharray':'3 4'}));
        svg.append(svgElement('line',{x1:s.left,y1:s.top+ph/2,x2:s.width-s.right,y2:s.top+ph/2,stroke:'#d5e1f0','stroke-dasharray':'3 4'}));
    }
    if(r.n>400){
        const pattern=svgElement('pattern',{id:'page-grid',width:r.w,height:r.h,patternUnits:'userSpaceOnUse'});
        pattern.append(svgElement('rect',{x:.6,y:.6,width:r.w-1.2,height:r.h-1.2,fill:'#e6f1ff',stroke:'#79aceb','stroke-width':1}));defs.append(pattern);
        const group=svgElement('g',{transform:`translate(${r.x} ${r.y})`});group.append(svgElement('rect',{x:0,y:0,width:r.usedWidth,height:r.usedHeight,fill:'url(#page-grid)'}));svg.append(group);
    }else for(let row=0;row<r.r;row++)for(let col=0;col<r.c;col++){
        svg.append(svgElement('rect',{x:r.x+col*r.w+.6,y:r.y+row*r.h+.6,width:r.w-1.2,height:r.h-1.2,fill:'#e6f1ff',stroke:'#79aceb','stroke-width':1}));
        if(r.r<=10)svg.append(svgElement('text',{x:r.x+(col+.5)*r.w,y:r.y+(row+.5)*r.h+4,'text-anchor':'middle',fill:'#789dcc','font-family':'Consolas, monospace','font-size':12},row*r.c+col+1));
    }
    const line=(x1,y1,x2,y2)=>svg.append(svgElement('line',{x1,y1,x2,y2,stroke:'#8498b1','stroke-width':1,'marker-start':'url(#arrow)','marker-end':'url(#arrow)'}));
    line(0,-24,s.width,-24);line(-24,0,-24,r.actualLength);
    svg.append(svgElement('text',{x:s.width/2,y:-34,'text-anchor':'middle',fill:'#658098','font-size':12,'font-family':'Microsoft YaHei UI, sans-serif'},`${draft.type==='roll'?'幅宽':'纸宽'} ${fmt(s.width)} mm`));
    svg.append(svgElement('text',{x:-34,y:r.actualLength/2,transform:`rotate(-90 -34 ${r.actualLength/2})`,'text-anchor':'middle',fill:'#658098','font-size':12,'font-family':'Microsoft YaHei UI, sans-serif'},`${draft.type==='roll'?'开料长度':'纸高'} ${fmt(r.actualLength)} mm`));
    if(draft.type==='roll')svg.append(svgElement('line',{x1:-6,y1:r.actualLength,x2:s.width+6,y2:r.actualLength,stroke:'#b78244','stroke-width':1.8,'stroke-dasharray':'5 3'}));
    stage.append(svg);
}
function showToast(text){clearTimeout(timer);$('toast').textContent=text;$('toast').classList.add('show');timer=setTimeout(()=>$('toast').classList.remove('show'),3000);}
function syncSharedRows(){if(hostMode!=='settings')return;const s=current();for(const type of ['flat','roll']){draft.configs[type].rows=s.rows;draft.configs[type].columns=s.columns;}}
function write(){const s=current();for(const [id,key]of Object.entries(numberFields))$(id).value=(key==='rows'||key==='columns')?(s[key]||''):s[key];$('autoRows').checked=s.rows===0;$('autoColumns').checked=s.columns===0;$('impositionEnabled').checked=draft.enabled;$('constraint').checked=s.constraint;$('direction').value=s.direction;const size=product();$('productWidth').value=size.width;$('productHeight').value=size.height;$('referenceSelect').value=hostMode==='settings'?referenceFile:taskFile;render();}
function read(){const s=current();for(const [id,key]of Object.entries(numberFields)){if(key==='rows'&&$('autoRows').checked)s[key]=0;else if(key==='columns'&&$('autoColumns').checked)s[key]=0;else s[key]=$(id).value===''?NaN:Number($(id).value);}draft.enabled=$('impositionEnabled').checked;s.constraint=hostMode==='temporary'&&$('constraint').checked;s.direction=$('direction').value;s.choice='';syncSharedRows();render();}
function defaultComparable(s){return Object.fromEntries(['width','length','left','right','top','bottom','rotation','rows','columns'].map(key=>[key,s[key]]));}
function render(){
    const s=current(),roll=draft.type==='roll',global=hostMode==='settings';
    $('windowTitle').textContent=global?'自动排版设置':'本次排版参数';$('entryTag').textContent=global?'默认设置':'本次任务';
    $('entryDescription').textContent=global?'保存后用于后续任务':'应用仅影响当前任务';$('enableCaption').textContent=global?'默认启用排版':'启用本次排版';
    $('originDescription').innerHTML=global?'编辑默认参数<small>保存默认设置，不覆盖当前材料选择框中的草稿</small>':'默认设置 → 本次草稿<small>仅应用当前材料；倍数约束不写入默认设置</small>';
    for(const btn of document.querySelectorAll('[data-entry]')){const active=btn.dataset.entry===hostMode;btn.classList.toggle('selected',active);btn.setAttribute('aria-pressed',active);}
    $('specTitle').textContent=roll?'卷装材料规格':'平张纸张规格';$('widthLabel').textContent=roll?'固定宽度':'纸张宽度';$('lengthLabel').textContent=roll?'最小长度':'纸张高度';
    $('flatPdfTools').hidden=roll;$('pdfSizePicker').hidden=roll||!pdfPickerOpen;
    const custom=JSON.stringify(defaultComparable(s))!==JSON.stringify(defaultComparable(savedDefaults.configs[draft.type]));
    $('sourceTag').textContent=global?(custom?'未保存修改':'已保存默认'):(custom?'本次覆盖默认':'沿用默认参数');$('sourceTag').classList.toggle('custom',custom);
    $('lengthHelp').textContent=roll?'固定宽度限制横向；长度随行数延展，最小长度仅为开料下限。':'纸张宽高限定可印刷区域；导入 PDF 纸张尺寸只回填这里的宽、高。';
    $('materialDescription').textContent=global?'平张／卷装切换用于编辑各自默认参数':'草稿仅作用于本次材料选择；应用后更新工作台';
    $('manualScope').textContent=global?'默认行列共用':roll?'横向限宽 · 纵向延展':'固定纸张宽高';
    $('gridHelp').textContent=global?'自动值按每个文件计算，不写为固定行列。默认手动行列由平张、卷装共用。':'勾选自动时显示计算值；取消自动后才固定行列。两种材料分别保留本次草稿。';
    $('constraintBox').hidden=global;$('byCount').disabled=global;$('byCount').title=global?'指定布局数量属于本次操作':'指定当前文件每纸布局数量';
    $('productTitle').textContent=global?'预览成品尺寸':'当前文件成品尺寸';$('productTag').textContent=global?'仅用于预览':'读取任务（示例）';
    $('productWidth').disabled=$('productHeight').disabled=!global;
    $('referenceCaption').textContent=global?'参考成品 PDF':'切换示例任务';
    const file=files[global?referenceFile:taskFile];$('productSource').textContent=`${file.name} · 第 1 页 · CropBox（示例）`;
    $('productHelp').textContent=global?'可直接输入或选择参考成品尺寸；预览尺寸不作为默认纸张参数保存。':'成品尺寸读取当前文件，不在参数窗口修改。切换示例仅用于体验尺寸联动。';
    $('previewScope').textContent=global?'样本放不下只提示，不阻止合法默认参数保存':'当前文件必须适合本次布局后才能应用';
    for(const btn of document.querySelectorAll('[data-material]')){const active=btn.dataset.material===draft.type;btn.classList.toggle('selected',active);btn.setAttribute('aria-pressed',active);}
    for(const btn of document.querySelectorAll('[data-rotation]')){const active=btn.dataset.rotation===s.rotation;btn.classList.toggle('selected',active);btn.setAttribute('aria-pressed',active);}
    for(const btn of document.querySelectorAll('[data-align]')){const active=btn.dataset.align===alignment;btn.classList.toggle('selected',active);btn.setAttribute('aria-pressed',active);}
    $('gridFields').hidden=s.mode!=='grid';$('countFields').hidden=s.mode!=='count';$('byGrid').classList.toggle('selected',s.mode==='grid');$('byCount').classList.toggle('selected',s.mode==='count');
    $('byGrid').setAttribute('aria-pressed',s.mode==='grid');$('byCount').setAttribute('aria-pressed',s.mode==='count');
    $('rows').disabled=s.rows===0;$('columns').disabled=s.columns===0;
    $('constraintFields').classList.toggle('disabled',!s.constraint);$('direction').disabled=$('multiple').disabled=!s.constraint;
    result=calculate(draft.type,s);const active=draft.enabled&&result.ok;draw(s,draft.enabled?result:{ok:false,disabled:true});$('choices').replaceChildren();
    $('rows').placeholder=result.ok?`${result.r}（自动）`:'自动';$('columns').placeholder=result.ok?`${result.c}（自动）`:'自动';
    if(s.mode==='count'&&result.ok){s.choice=`${result.r}-${result.c}-${result.angle}`;for(const item of result.candidates){const key=`${item.r}-${item.c}-${item.angle}`;const btn=document.createElement('button');btn.type='button';btn.textContent=`${item.r}×${item.c} · ${item.angle}°`;btn.className='choice'+(key===s.choice?' selected':'');btn.setAttribute('aria-pressed',key===s.choice);btn.addEventListener('click',()=>{s.choice=key;render();});$('choices').append(btn);}}
    $('gridValue').textContent=active?`${result.r} × ${result.c}`:'— × —';$('layoutValue').textContent=active?result.n:'—';$('orientation').textContent=active?`行 × 列 · ${result.angle}°`:'请核对成品或参数';
    $('lengthMetricLabel').textContent=roll?'实际开料长度':'纸张高度';$('actualLength').textContent=active?`${fmt(result.actualLength)} mm`:'—';
    for(const key of ['leftGap','rightGap','topGap','bottomGap'])$(key).textContent=active?`${fmt(result[key])} mm`:'—';
    $('alignmentLegend').textContent=alignment==='center'?'按可印刷区域居中':'当前靠左上放置';
    $('conflictActions').hidden=global||result.kind!=='multiple-conflict';
    if(result.kind==='multiple-conflict')$('roundAxis').textContent=`改为 ${Math.ceil(result.value/s.multiple)*s.multiple} ${result.axis}`;
    const checks=['flat','roll'].map(type=>validateParameters(type,draft.configs[type]));const invalid=checks.find(r=>!r.ok);
    const warning=global&&!result.ok&&!invalid;
    $('validation').classList.toggle('warning',warning);$('validation').classList.toggle('error',!result.ok&&!warning);$('validation').classList.toggle('paused',!draft.enabled&&result.ok);
    $('validation').textContent=global&&invalid?invalid.message:warning?'当前预览样本不适合这组参数；参数本身合法，可以保存默认设置。':!result.ok?result.message:!draft.enabled?'排版已停用，参数草稿保留。':roll?`${s.rows>0?'指定':'自动计算'} ${result.r} 行；实际长度 ${fmt(result.actualLength)} mm。卷装纵向无固定上限。`:`${result.r} 行 × ${result.c} 列可容纳；网格按可印刷区域${alignment==='center'?'居中':'左上'}显示。`;
    $('apply').disabled=global?!!invalid:!result.ok;
    $('apply').textContent=global?'保存默认设置':'应用本次参数';$('restoreSettings').textContent=global?'↺ 撤销当前材料修改':'↺ 恢复默认参数';
    const baseline=global?savedDefaults:applied;const dirty=draft.enabled!==baseline.enabled||draft.type!==baseline.type||JSON.stringify(draft.configs)!==JSON.stringify(baseline.configs);
    $('draftState').textContent=dirty?(global?'默认草稿 · 未保存':'本次草稿 · 未应用'):(global?'已保存默认':'已应用本次参数');
}
function renderApplied(){
    const global=hostMode==='settings',state=global?savedDefaults:applied,s=state.configs[state.type],r=calculate(state.type,s),valid=state.enabled&&r.ok;
    $('feedbackTitle').textContent=global?'默认设置 · 已保存参数':'材料选择框 · 已应用参数';$('appliedEnabled').textContent=state.enabled?'启用':'停用';
    $('appliedMaterial').textContent=state.type==='roll'?'卷装':'平张';
    $('appliedGrid').textContent=global?`${s.rows||'自动'}×${s.columns||'自动'}`:valid?`${r.r}×${r.c}`:'—';
    $('appliedCount').textContent=global?'按文件计算':valid?r.n:'—';$('appliedLength').textContent=global?`${fmt(s.length)} mm`:valid?`${fmt(r.actualLength)} mm`:'—';
    $('appliedNote').textContent=global?`平张 ${fmt(state.configs.flat.width)}×${fmt(state.configs.flat.length)} mm；卷装幅宽 ${fmt(state.configs.roll.width)} mm / 最小长度 ${fmt(state.configs.roll.length)} mm；倍数不保存。`:!state.enabled?'本次排版停用；页序复制保持原设置。':!r.ok?'已应用参数在新示例文件下不适合，请调整本次参数。':`本次${state.type==='roll'?'卷装':'平张'} · ${s.constraint?(s.direction==='column'?'列':'行')+'数 '+s.multiple+' 倍数':'无倍数约束'} · 页序复制独立。`;
}
function switchHost(mode){if(hostMode==='settings')globalDraft=draft;else temporaryDraft=draft;hostMode=mode;draft=mode==='settings'?globalDraft:temporaryDraft;pdfPickerOpen=false;write();renderApplied();}
for(const id of Object.keys(numberFields))$(id).addEventListener('input',read);
$('impositionEnabled').addEventListener('change',read);$('constraint').addEventListener('change',read);$('direction').addEventListener('change',read);
for(const [id,key]of [['autoRows','rows'],['autoColumns','columns']])$(id).addEventListener('change',()=>{current()[key]=$(id).checked?0:result?.ok?result[key==='rows'?'r':'c']:1;syncSharedRows();write();});
for(const id of ['productWidth','productHeight'])$(id).addEventListener('input',()=>{if(hostMode!=='settings')return;referenceSize={width:Number($('productWidth').value),height:Number($('productHeight').value)};current().choice='';render();renderApplied();});
$('referenceSelect').addEventListener('change',()=>{if(hostMode==='settings'){referenceFile=$('referenceSelect').value;referenceSize={width:files[referenceFile].width,height:files[referenceFile].height};}else taskFile=$('referenceSelect').value;viewMode='fit';write();renderApplied();});
for(const btn of document.querySelectorAll('[data-entry]'))btn.addEventListener('click',()=>switchHost(btn.dataset.entry));
for(const btn of document.querySelectorAll('[data-material]'))btn.addEventListener('click',()=>{draft.type=btn.dataset.material;pdfPickerOpen=false;viewMode='fit';write();});
for(const btn of document.querySelectorAll('[data-align]'))btn.addEventListener('click',()=>{alignment=btn.dataset.align;render();});
for(const btn of document.querySelectorAll('[data-rotation]'))btn.addEventListener('click',()=>{current().rotation=btn.dataset.rotation;current().choice='';render();});
$('byGrid').addEventListener('click',()=>{const s=current();if(s.mode==='count'&&result.ok){s.rows=result.r;s.columns=result.c;}s.mode='grid';syncSharedRows();write();});
$('byCount').addEventListener('click',()=>{if(hostMode==='settings')return;const s=current();if(result.ok)s.target=result.n;s.mode='count';s.choice='';write();});
$('getPdfSize').addEventListener('click',()=>{pdfPickerOpen=!pdfPickerOpen;render();});
for(const btn of document.querySelectorAll('[data-pdfsize]'))btn.addEventListener('click',()=>{const size=btn.dataset.pdfsize.split(',').map(Number);draft.configs.flat.width=size[0];draft.configs.flat.length=size[1];pdfPickerOpen=false;write();showToast('示例大版 PDF 尺寸已回填纸张宽高；成品预览尺寸不变。');});
for(const btn of document.querySelectorAll('[data-reset]'))btn.addEventListener('click',()=>{const s=current(),base=savedDefaults.configs[draft.type],key=btn.dataset.reset;if(key==='margins'){for(const k of ['left','right','top','bottom'])s[k]=base[k];}else if(key==='rows'){s.rows=base.rows;s.columns=base.columns;s.mode='grid';syncSharedRows();}else s[key]=base[key];s.choice='';write();});
$('restoreSettings').addEventListener('click',()=>{draft.configs[draft.type]=structuredClone(savedDefaults.configs[draft.type]);syncSharedRows();write();showToast(hostMode==='settings'?'已撤销当前材料的默认草稿修改。':'已恢复当前材料的默认参数，清除本次倍数约束。');});
$('restore').addEventListener('click',()=>{const s=current();s.mode='grid';s.rows=s.columns=0;s.constraint=false;s.choice='';syncSharedRows();write();});
$('cancel').addEventListener('click',()=>{draft=structuredClone(hostMode==='settings'?savedDefaults:applied);write();showToast(hostMode==='settings'?'已放弃默认设置草稿，已保存参数不变。':'已放弃本次草稿，默认设置不变。');});
$('settingsForm').addEventListener('submit',e=>{e.preventDefault();if(hostMode==='settings'){if(['flat','roll'].some(type=>!validateParameters(type,draft.configs[type]).ok))return;savedDefaults=structuredClone(draft);for(const type of ['flat','roll']){savedDefaults.configs[type].constraint=false;savedDefaults.configs[type].mode='grid';savedDefaults.configs[type].choice='';}draft=structuredClone(savedDefaults);showToast('已模拟保存默认设置；预览成品尺寸、倍数和本次草稿未写入默认参数。');}else{if(!result.ok)return;applied.enabled=draft.enabled;applied.type=draft.type;applied.configs[draft.type]=structuredClone(current());showToast('已应用当前材料的本次参数，默认设置和页序复制不变。');}write();renderApplied();});
$('roundAxis').addEventListener('click',()=>{const s=current(),key=s.direction==='column'?'columns':'rows';s[key]=Math.ceil(s[key]/s.multiple)*s.multiple;write();});
$('autoAxis').addEventListener('click',()=>{const s=current();s[s.direction==='column'?'columns':'rows']=0;write();});
$('fitView').addEventListener('click',()=>{viewMode='fit';panHorizontal=50;panVertical=0;render();});
$('detailView').addEventListener('click',()=>{if(!result.ok)return;viewMode='detail';zoom=($('stage').clientWidth||450)/(current().width+130)/3.77952756*100;panHorizontal=50;panVertical=0;render();});
$('zoomIn').addEventListener('click',()=>{viewMode='zoom';zoom=Math.min(400,Math.max(1,zoom*1.25));render();});
$('zoomOut').addEventListener('click',()=>{viewMode='zoom';zoom=Math.max(1,zoom/1.25);render();});
$('panX').addEventListener('input',()=>{panHorizontal=Number($('panX').value);render();});$('panY').addEventListener('input',()=>{panVertical=Number($('panY').value);render();});
for(const btn of document.querySelectorAll('[data-scene]'))btn.addEventListener('click',()=>{if(hostMode!=='temporary')switchHost('temporary');draft.enabled=true;draft.type='roll';draft.configs.roll=makeConfig(true);const s=current();alignment='center';viewMode='fit';if(btn.dataset.scene==='extend'){s.rows=30;s.columns=2;}if(btn.dataset.scene==='auto'){s.rows=s.columns=0;s.direction='row';s.multiple=3;}if(btn.dataset.scene==='asymmetric'){s.left=10;s.right=30;s.top=15;s.bottom=35;}if(btn.dataset.scene==='conflict'){s.rows=3;s.direction='row';s.multiple=2;}write();if(btn.dataset.scene==='extend')$('detailView').click();});
write();renderApplied();
