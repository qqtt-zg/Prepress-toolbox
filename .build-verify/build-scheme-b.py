from pathlib import Path
import re

source = Path('docs/V5卷装参数与居中布局预览方案.html')
target = Path('docs/V5自动排版_方案B_简化操作.html')
h = source.read_text(encoding='utf-8')
h = h.replace('<body>', '<body class="scheme-b">', 1)
h = h.replace('<title>V5 · 自动排版设置与本次参数共用界面</title>', '<title>V5 · 方案 B · 简化操作</title>')
h = h.replace('自动排版 · 单屏方案', '自动排版 · 方案 B')
h = h.replace('<button type="button" class="btn" id="showHelp">说明</button>', '<a class="compare-link" href="V5卷装参数与居中布局预览方案.html" target="_blank" rel="noopener">查看方案 A ↗</a>', 1)
h = re.sub(r'<div class="field-resets">[\s\S]*?</div>', '', h, count=1)

# 方向与指定方式使用字段选择；保留三种核心策略的直接入口。
a = h.index('<section class="parameter-group direction-group">')
b = h.index('</section>', a) + len('</section>')
h = h[:a] + '''<section class="parameter-group direction-group"><div class="field select-field"><label for="rotationSetting">页面方向</label><select id="rotationSetting"><option value="auto">自动选择方向</option><option value="0">禁止旋转 · 0°</option><option value="270">固定方向 · 270°</option></select></div></section>''' + h[b:]
h = h.replace('<div class="layouttabs" role="group" aria-label="指定布局的输入方式"><button type="button" id="byGrid">按行列</button><button type="button" id="byCount">按布局数</button></div>', '<div class="field select-field format-field"><label for="specifiedFormat">指定方式</label><select id="specifiedFormat"><option value="grid">按行列</option><option value="count" id="countOption">按布局数</option></select></div>')

# 从尺寸导入与恢复操作均移入更多，直接删除旧按钮及其旧绑定。
a = h.index('<div class="pdf-tools"')
b = h.index('</div>', a) + len('</div>')
paper_tools = h[a:b]
h = h[:a] + h[b:]
a = h.index('<div class="align"')
b = h.index('</div>', a) + len('</div>')
h = h[:a] + h[b:]
a = h.index('<div class="zoom-toolbar">')
b = h.index('<div class="stage"', a)
h = h[:a] + '''<div class="zoom-toolbar simplified-zoom"><div class="field select-field"><label for="previewView">预览范围</label><select id="previewView"><option value="fit">整张预览</option><option value="detail">按幅宽看局部</option><option value="custom">自定比例</option></select></div><div class="preview-percent"><label for="previewScale">缩放</label><input id="previewScale" type="number" min="1" max="400" step="1" aria-label="预览缩放百分比"><span>%</span></div></div>
   ''' + h[b:]
a = h.index('<div class="windowfoot">')
b = h.index('\n </form>', a)
h = h[:a] + '''<div class="windowfoot"><div class="more-entry"><button type="button" class="btn more-button" id="showMore">更多 ⋯</button><span>恢复参数、PDF 导入与说明</span></div><div><span class="draftstate" id="draftState">尚未应用</span><button type="button" class="btn" id="cancel">取消</button><button type="submit" class="btn primary" id="apply">应用本次参数</button></div></div>''' + h[b:]
dialog = '''<dialog id="moreDialog" class="more-dialog"><div class="more-head"><div><h2>更多操作</h2><p>只作用于当前编辑的材料，不会自动保存。</p></div><button type="button" class="btn" id="closeMore">关闭</button></div><section class="more-section"><label for="parameterAction">恢复参数</label><div class="more-action"><select id="parameterAction"><option value="default" id="defaultAction">恢复当前材料默认参数</option><option value="auto">恢复自动择优</option><option value="width">仅恢复宽度</option><option value="length">仅恢复高度／最小长度</option><option value="margins">仅恢复四边距</option><option value="rotation">仅恢复页面方向</option><option value="rows">仅恢复行列方式</option></select><button type="button" class="btn" id="executeParameterAction">执行</button></div></section><section class="more-section paper-more">''' + paper_tools + '''</section><section class="more-section"><div class="field select-field"><label for="previewAlignment">预览对齐</label><select id="previewAlignment"><option value="center">按可印刷区域居中</option><option value="start">靠左上（对比）</option></select></div></section><div class="more-foot"><button type="button" class="btn" id="showHelp">参数与操作说明</button><span>选择预览对齐不会修改文件。</span></div></dialog>
'''
pos = h.index('<dialog class="help-dialog"')
h = h[:pos] + dialog + h[pos:]

# 改写交互入口，计算、互斥策略及保存范围继续使用同一套逻辑。
a = h.index("    for(const id of ['zoomOut'")
b = h.index('\n', a)
h = h[:a] + "    $('previewView').disabled=$('previewScale').disabled=!r.ok;" + h[b:]
h = h.replace("$('zoomLabel').textContent=fmt(Math.round(zoom))+'%';", "$('previewScale').value=Math.round(zoom);$('previewView').value=viewMode==='fit'?'fit':viewMode==='detail'?'detail':'custom';")
h = h.replace("    $('fitView').classList.toggle('selected',viewMode==='fit');$('detailView').classList.toggle('selected',viewMode==='detail');", '')
h = re.sub(r"\s*for\(const btn of document.querySelectorAll\('\[data-rotation\]'\)\)\{[^\n]+", "\n    $('rotationSetting').value=s.rotation;", h, count=1)
h = re.sub(r"\s*for\(const btn of document.querySelectorAll\('\[data-align\]'\)\)\{[^\n]+", "\n    $('previewAlignment').value=alignment;", h, count=1)
h = h.replace("$('byCount').disabled=global;$('byCount').title=global?'指定布局数量属于本次操作':'指定当前文件每纸布局数量';", "$('countOption').disabled=global;$('countOption').textContent=global?'按布局数（仅本次）':'按布局数';")
h = h.replace("$('byGrid').classList.toggle('selected',s.mode==='grid');$('byCount').classList.toggle('selected',s.mode==='count');", "$('specifiedFormat').value=s.mode;")
h = h.replace("    $('byGrid').setAttribute('aria-pressed',s.mode==='grid');$('byCount').setAttribute('aria-pressed',s.mode==='count');", '')
h = h.replace("$('apply').textContent=global?'保存默认设置':'应用本次参数';$('restoreSettings').textContent=global?'↺ 撤销当前材料修改':'↺ 恢复默认参数';", "$('apply').textContent=global?'保存默认设置':'应用本次参数';$('defaultAction').textContent=global?'撤销当前材料未保存修改':'恢复当前材料默认参数';")
a = h.index("for(const btn of document.querySelectorAll('[data-align]'))btn.addEventListener")
b = h.index("$('getPdfSize').addEventListener", a)
h = h[:a] + '''$('rotationSetting').addEventListener('change',()=>{current().rotation=$('rotationSetting').value;current().choice='';render();});
function setSpecifiedFormat(value){
    if(hostMode==='settings'&&value==='count')return;
    const s=current();
    if(value==='grid'&&s.mode==='count'&&result.ok){s.rows=result.r;s.columns=result.c;}
    if(value==='count'&&s.mode==='grid'&&result.ok)s.target=result.n;
    s.mode=value;s.choice='';syncSharedRows();write();
}
$('specifiedFormat').addEventListener('change',()=>setSpecifiedFormat($('specifiedFormat').value));
''' + h[b:]
h = h.replace("$('getPdfSize').addEventListener('click',()=>{pdfPickerOpen=true;render();$('pdfSizePicker').showModal();});", "$('getPdfSize').addEventListener('click',()=>{$('moreDialog').close();pdfPickerOpen=true;render();$('pdfSizePicker').showModal();});")
a = h.index("for(const btn of document.querySelectorAll('[data-reset]'))btn.addEventListener")
b = h.index("$('cancel').addEventListener", a)
h = h[:a] + '''$('showMore').addEventListener('click',()=>$('moreDialog').showModal());
$('closeMore').addEventListener('click',()=>$('moreDialog').close());
$('executeParameterAction').addEventListener('click',()=>{
    const action=$('parameterAction').value,s=current(),base=savedDefaults.configs[draft.type];
    if(action==='default')draft.configs[draft.type]=structuredClone(base);
    else if(action==='auto'){s.strategy='auto';s.rows=s.columns=0;s.constraint=false;s.choice='';}
    else if(action==='margins'){for(const key of ['left','right','top','bottom'])s[key]=base[key];}
    else if(action==='rows'){s.rows=base.rows;s.columns=base.columns;s.mode='grid';s.strategy=strategy(base);s.constraint=false;}
    else s[action]=base[action];
    current().choice='';syncSharedRows();$('moreDialog').close();write();showToast('已恢复所选参数；尚未保存或应用。');
});
$('previewAlignment').addEventListener('change',()=>{alignment=$('previewAlignment').value;render();});
''' + h[b:]
a = h.index("$('fitView').addEventListener")
b = h.index("$('panX').addEventListener", a)
h = h[:a] + '''function setPreviewView(value){
    if(!result.ok)return;
    if(value==='fit'){viewMode='fit';panHorizontal=50;panVertical=0;}
    else if(value==='detail'){viewMode='detail';zoom=($('stage').clientWidth||450)/(current().width+130)/3.77952756*100;panHorizontal=50;panVertical=0;}
    else viewMode='zoom';
    render();
}
$('previewView').addEventListener('change',()=>setPreviewView($('previewView').value));
$('previewScale').addEventListener('change',()=>{const value=Number($('previewScale').value);if(Number.isFinite(value)&&value>=1&&value<=400){viewMode='zoom';zoom=value;render();}else{$('previewScale').value=Math.round(zoom);showToast('缩放范围为 1%–400%。');}});
''' + h[b:]
h = h.replace("if(btn.dataset.scene==='extend')$('detailView').click();", "if(btn.dataset.scene==='extend')setPreviewView('detail');")
h = h.replace("$('showHelp').addEventListener('click',()=>$('helpDialog').showModal());", "$('showHelp').addEventListener('click',()=>{$('moreDialog').close();$('helpDialog').showModal();});")

css = '''
/* 方案 B：三种策略直接可见，常用参数采用表单控件，低频操作集中到更多。 */
.scheme-b .compare-link{font-size:11px;color:#607e9f;text-decoration:none;white-space:nowrap;padding:4px 6px}.scheme-b .compare-link:hover{color:var(--blue)}
.scheme-b .select-field select{flex:1;min-width:0;height:28px;padding:3px 7px;border:1px solid #d4e0ee;border-radius:4px;background:#fff;font-size:12px;color:#385573}
.scheme-b .direction-group{margin-top:2px}.scheme-b .select-field>label{min-width:50px;color:#6a7e94;font-size:11px}
.scheme-b .specified-fields{grid-template-columns:180px minmax(0,1fr)}.scheme-b .specified-fields .format-field{height:28px}.scheme-b .specified-fields .select-field>label{min-width:48px}
.scheme-b .zoom-toolbar{padding-bottom:2px}.scheme-b .simplified-zoom{gap:12px}.scheme-b .simplified-zoom>.select-field{flex:1;max-width:250px;display:flex;gap:7px}.scheme-b .simplified-zoom select{height:25px;font-size:11px}.scheme-b .preview-percent{display:flex;gap:5px;align-items:center;font-size:11px;color:var(--muted)}.scheme-b .preview-percent input{width:55px;height:25px;border:1px solid var(--line);border-radius:4px;background:white;text-align:center;font:12px Consolas,monospace;padding:2px 3px}
.scheme-b .previewhead{min-height:30px}.scheme-b .preview-results{padding-top:7px;padding-bottom:7px}.scheme-b .more-entry{display:flex;gap:9px;align-items:center}.scheme-b .more-entry>span{font-size:10px;color:#8291a2}.scheme-b .more-button{background:#f9fbfd;color:#536e8e}.scheme-b .windowfoot .primary{min-width:112px}
.scheme-b .strategy-tabs{background:#f5f7fa;border-color:#e2e8f0;padding:3px}.scheme-b .strategy-tabs button{font-size:12px}.scheme-b .strategy-tabs button.selected{background:var(--blue)}
.more-dialog{width:min(440px,calc(100vw - 24px));padding:16px;border:1px solid #c2d2e5;border-radius:6px;background:white;color:var(--ink)}.more-dialog::backdrop{background:#26394c66}.more-head{display:flex;justify-content:space-between;gap:12px;align-items:center;margin-bottom:13px}.more-head h2{font-size:16px;font-weight:600}.more-head p{font-size:10px;color:var(--muted);margin-top:3px}.more-section{padding:11px 0;border-top:1px solid #e1e8f1}.more-section>label{font-size:11px;color:#6c8198;display:block;margin-bottom:6px}.more-action{display:flex;gap:7px}.more-action select{height:29px;min-width:0;flex:1;border:1px solid var(--line);border-radius:4px;background:white;padding:3px 6px;font-size:12px}.more-foot{padding-top:10px;border-top:1px solid var(--line);display:flex;justify-content:space-between;gap:9px;align-items:center}.more-foot>span{font-size:10px;color:var(--muted)}.paper-more:has(#flatPdfTools[hidden]){display:none}
@media(max-width:1100px){.scheme-b .specified-fields{grid-template-columns:155px minmax(0,1fr)}.scheme-b .select-field>label{min-width:44px}.scheme-b .specified-fields .select-field>label{min-width:44px}}
@media(max-width:759px){.scheme-b .specified-fields{grid-template-columns:130px minmax(0,1fr)}.scheme-b .simplified-zoom{gap:6px;flex-wrap:wrap}.scheme-b .simplified-zoom>.select-field{flex:1}.scheme-b .preview-percent{gap:3px}.scheme-b .preview-percent input{width:48px}.scheme-b .more-entry>span{display:none}}
@media(max-width:560px){.scheme-b .specified-fields{grid-template-columns:1fr}.scheme-b .simplified-zoom>.select-field{max-width:none;flex-basis:100%}.scheme-b .preview-percent{justify-content:flex-end;width:100%}.scheme-b .select-field select{font-size:11px;padding:3px 5px}.scheme-b .windowfoot .primary{min-width:100px}.scheme-b .strategy-tabs button{font-size:11px}}
'''
h = h.replace('</style>', css + '\n</style>', 1)
h = h.replace('参数、预览与操作按钮单屏显示', '方案 B：常驻主操作按钮减少，参数完整保留')
h = h.replace('主界面保留全部参数；详细解释在此查看。', '方案 B：保留三种策略；方向、指定方式和预览范围用下拉选择；低频操作集中到“更多”。')
target.write_text(h, encoding='utf-8')
print(target)
