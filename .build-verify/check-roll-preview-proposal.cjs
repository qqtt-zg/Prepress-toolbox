const fs = require('fs');
const vm = require('vm');
const assert = require('node:assert/strict');
const html = fs.readFileSync('docs/V5卷装参数与居中布局预览方案.html', 'utf8');
const script = html.match(/<script>([\s\S]*?)<\/script>/)[1];
new vm.Script(script);

class Element {
    constructor(tag, attrs = {}) {
        this.tag = tag;
        this.attrs = { ...attrs };
        this.dataset = {};
        this.children = [];
        this.events = {};
        this.value = '';
        this.textContent = '';
        this.checked = false;
        this.classList = { toggle() {}, add() {}, remove() {} };
        for (const [key, value] of Object.entries(attrs)) {
            if (key.startsWith('data-')) this.dataset[key.slice(5)] = value;
        }
    }
    setAttribute(key, value) { this.attrs[key] = value; }
    append(...items) { this.children.push(...items); }
    replaceChildren(...items) { this.children = items; }
    addEventListener(event, action) { this.events[event] = action; }
    showModal() { this.open = true; }
    close() { this.open = false; }
    click() { this.events.click?.({ preventDefault() {} }); }
}
const elements = new Map();
const buttons = [];
for (const match of html.matchAll(/<([\w-]+)\b([^<>]*)>/g)) {
    const attrs = Object.fromEntries([...match[2].matchAll(/([\w-]+)="([^"]*)"/g)].map(a => [a[1], a[2]]));
    const element = new Element(match[1], attrs);
    if (attrs.id) {
        assert.equal(elements.has(attrs.id), false, 'duplicate id: ' + attrs.id);
        elements.set(attrs.id, element);
    }
    if (match[1] === 'button') buttons.push(element);
}
const document = {
    getElementById(id) { assert.ok(elements.has(id), 'missing DOM id: ' + id); return elements.get(id); },
    createElement(tag) { return new Element(tag); },
    createElementNS(ns, tag) { return new Element(tag); },
    querySelectorAll(selector) {
        const attribute = selector.slice(1, -1);
        return buttons.filter(b => attribute in b.attrs);
    }
};
const context = vm.createContext({ document, structuredClone, setTimeout, clearTimeout });
vm.runInContext(script, context);
const read = expression => vm.runInContext(expression, context);
const click = id => elements.get(id).events.click({ preventDefault() {} });
const scenario = name => buttons.find(b => b.dataset.scene === name).events.click();
const result = () => JSON.parse(read('JSON.stringify(result)'));


const set = (id,value) => { elements.get(id).value=String(value); elements.get(id).events.input(); };
const choose = (id,value) => { elements.get(id).value=value; elements.get(id).events.change(); };
const strategies = () => buttons.filter(b=>b.dataset.strategy);
assert.equal(read('strategy(current())'), 'fixed');
assert.equal(result().n, 8);
assert.equal(elements.get('specifiedFields').hidden, false);
assert.equal(elements.get('automaticFields').hidden, true);
assert.equal(elements.get('constraintBox').hidden, true);
assert.equal(elements.has('constraint'), false);
click('strategyAuto');
assert.equal(result().n, 15);
assert.equal(result().r, 5);
assert.equal(result().c, 3);
assert.equal(elements.get('automaticFields').hidden, false);
assert.equal(elements.get('specifiedFields').hidden, true);
assert.equal(read('current().rows'), 4);
assert.equal(read('current().columns'), 2);
click('strategyMultiple');
choose('direction','row');
set('multiple',3);
assert.equal(result().n,18);
assert.equal(result().r,6);
assert.equal(result().c,3);
assert.equal(elements.get('constraintBox').hidden,false);
assert.equal(elements.get('specifiedFields').hidden,true);
assert.equal(elements.get('layoutModeBadge').textContent,'行3 倍数');
assert.equal(read('current().rows'),4);
click('strategyFixed');
assert.equal(result().n,8);
assert.equal(read('current().multiple'),3);
assert.equal(result().c,2);
assert.equal(elements.get('layoutModeBadge').textContent,'指定 4×2');
click('strategyAuto');
assert.equal(result().n,15);
click('strategyMultiple');
assert.equal(result().n,18);
assert.equal(strategies().filter(b=>b.attrs['aria-pressed']===true).length,1);
click('strategyFixed');
click('byCount');
set('targetCount',6);
const selectedChoice=elements.get('choices').children[0];
selectedChoice.value='3-2-0';selectedChoice.events.change();
assert.equal(result().r,3);
assert.equal(result().c,2);
click('strategyAuto');
assert.equal(read('current().choice'),'3-2-0');
click('strategyFixed');
assert.equal(result().r,3);
assert.equal(result().c,2);
set('targetCount',997);
assert.equal(result().ok,true);
assert.equal(result().n,997);
assert.equal(result().r,997);
assert.ok(result().actualLength>420);
assert.ok(elements.get('stage').children[0].children.length<30);
click('strategyMultiple');
assert.equal(result().n,18);
click('strategyFixed');
assert.equal(read('current().mode'),'count');
assert.equal(read('current().target'),997);
assert.equal(result().n,997);
scenario('extend');
set('rows',2000);
assert.equal(result().ok,true);
assert.equal(result().n,4000);
assert.equal(result().actualLength,160020);
scenario('asymmetric');
assert.deepEqual([result().leftGap,result().rightGap,result().topGap,result().bottomGap],[60,80,40,60]);
buttons.find(b=>b.dataset.align==='start').events.click();
assert.equal(result().leftGap,10);
buttons.find(b=>b.dataset.align==='center').events.click();
scenario('conflict');
assert.equal(read('strategy(current())'),'multiple');
assert.equal(result().ok,false);
assert.equal(result().kind,'roll-width-or-combination');
assert.equal(elements.get('conflictActions').hidden,false);
click('autoAxis');
assert.equal(read('strategy(current())'),'auto');
assert.equal(result().ok,true);
assert.equal(result().n,15);
scenario('roll');
elements.get('autoRows').checked=true;elements.get('autoRows').events.change();
assert.equal(result().n,10);
elements.get('autoColumns').checked=true;elements.get('autoColumns').events.change();
assert.equal(result().ok,false);
assert.equal(result().kind,'manual-empty');
click('autoAxis');
assert.equal(result().ok,true);
assert.equal(result().n,15);
const taskBeforeDefaultSave=read('JSON.stringify(applied)');
buttons.find(b=>b.dataset.entry==='settings').events.click();
assert.equal(elements.get('strategyMultiple').disabled,true);
assert.equal(elements.get('multipleScope').hidden,false);
assert.equal(elements.get('strategyAuto').attrs['aria-pressed'],true);
click('strategyMultiple');
assert.equal(read('strategy(current())'),'auto');
assert.equal(elements.get('productWidth').disabled,false);
buttons.find(b=>b.dataset.material==='flat').events.click();
set('width',110);set('length',100);choose('referenceSelect','large');
assert.equal(result().ok,false);
assert.equal(elements.get('apply').disabled,false);
assert.match(elements.get('validation').textContent,/可以保存默认设置/);
click('strategyFixed');set('rows',5);
assert.equal(read('draft.configs.flat.rows'),5);
assert.equal(read('draft.configs.roll.rows'),5);
assert.equal(read('strategy(draft.configs.roll)'),'fixed');
elements.get('settingsForm').events.submit({preventDefault(){}});
assert.equal(read('savedDefaults.configs.flat.rows'),5);
assert.equal(read('JSON.stringify(applied)'),taskBeforeDefaultSave);
click('strategyAuto');
elements.get('settingsForm').events.submit({preventDefault(){}});
assert.equal(read('savedDefaults.configs.flat.rows'),0);
assert.equal(read('savedDefaults.configs.roll.rows'),0);
assert.equal(read('current().rows'),5);
click('strategyFixed');
assert.equal(read('current().rows'),5);
buttons.find(b=>b.dataset.entry==='temporary').events.click();
assert.equal(elements.get('productWidth').disabled,true);
scenario('roll');
set('rows',7);
const defaultsBeforeApply=read('JSON.stringify(savedDefaults)');
elements.get('settingsForm').events.submit({preventDefault(){}});
assert.equal(read('applied.configs.roll.rows'),7);
assert.equal(read('JSON.stringify(savedDefaults)'),defaultsBeforeApply);
set('rows',9);click('cancel');
assert.equal(read('current().rows'),7);
scenario('extend');
assert.equal(read('viewMode'),'detail');
const first=elements.get('stage').children[0].attrs.viewBox.split(' ').map(Number);
set('panY',100);
const last=elements.get('stage').children[0].attrs.viewBox.split(' ').map(Number);
assert.ok(last[1]>first[1]);
click('fitView');
assert.equal(elements.get('previewNavigation').hidden,true);
const zoom=read('zoom');click('zoomIn');assert.ok(read('zoom')>zoom);click('zoomOut');
assert.ok(Math.abs(read('zoom')-zoom)<0.001);
buttons.find(b=>b.dataset.material==='flat').events.click();
click('getPdfSize');
assert.equal(elements.get('pdfSizePicker').open,true);
buttons.find(b=>b.dataset.pdfsize==='210,297').events.click();
assert.equal(read('current().width'),210);
assert.equal(elements.get('pdfSizePicker').open,false);
console.log('PASS: mutually exclusive strategies, remembered grid/count/multiple inputs, effective-only validation/calculation, global restriction/shared strategy, save/cancel isolation, long roll and preview interactions.');
