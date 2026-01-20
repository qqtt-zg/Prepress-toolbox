/**
 * PDF.js 桥接脚本
 * 用于 C# WinForms 和 PDF.js 之间的通信
 */

// 调试日志
function debugLog(message) {
    console.log('[PDF Bridge] ' + message);
    // 发送调试信息到C#
    sendToHost('debug', { message: message });
}

// 检查PDF.js是否加载
if (typeof pdfjsLib === 'undefined') {
    debugLog('ERROR: pdfjsLib 未定义，PDF.js库未正确加载');
    document.getElementById('loadingMessage').textContent = 'PDF.js库加载失败';
} else {
    debugLog('PDF.js库已加载，版本: ' + pdfjsLib.version);
    // PDF.js 配置
    pdfjsLib.GlobalWorkerOptions.workerSrc = 'build/pdf.worker.min.js';
    debugLog('Worker源设置为: build/pdf.worker.min.js');
}

// 状态管理
const state = {
    pdfDoc: null,
    currentPage: 1,
    totalPages: 0,
    scale: 1.0,
    rotation: 0,
    rendering: false,
    pendingPage: null
};

// DOM 元素
const elements = {
    viewer: document.getElementById('viewer'),
    viewerContainer: document.getElementById('viewerContainer'),
    pageInfo: document.getElementById('pageInfo'), // 保留兼容性
    pageInput: document.getElementById('pageInput'),
    totalPages: document.getElementById('totalPages'),
    zoomLevel: document.getElementById('zoomLevel'),
    loadingMessage: document.getElementById('loadingMessage'),
    loadingText: document.getElementById('loadingText'),
    loadingOverlay: document.getElementById('loadingOverlay'),
    prevPage: document.getElementById('prevPage'),
    nextPage: document.getElementById('nextPage'),
    zoomIn: document.getElementById('zoomIn'),
    zoomOut: document.getElementById('zoomOut'),
    fitWidth: document.getElementById('fitWidth'),
    fitPage: document.getElementById('fitPage'),
    rotateCW: document.getElementById('rotateCW'),
    rotateCCW: document.getElementById('rotateCCW'),
    // 边栏元素
    sidebar: document.getElementById('sidebar'),
    toggleSidebar: document.getElementById('toggleSidebar'),
    sidebarTabs: document.querySelectorAll('.sidebar-tab'),
    thumbnailContainer: document.getElementById('thumbnailContainer'),
    layerContainer: document.getElementById('layerContainer'),
    thumbnailView: document.getElementById('thumbnailView'),
    layerView: document.getElementById('layerView')
};

// 加载状态管理
let isLoading = false;

function showLoading(message = '正在加载...') {
    isLoading = true;
    elements.loadingText.textContent = message;
    elements.loadingMessage.classList.add('show');
    elements.loadingOverlay.classList.add('show');
    disableAllControls();
}

function hideLoading() {
    isLoading = false;
    elements.loadingMessage.classList.remove('show');
    elements.loadingOverlay.classList.remove('show');
    enableAllControls();
}

function disableAllControls() {
    elements.prevPage.disabled = true;
    elements.nextPage.disabled = true;
    elements.zoomIn.disabled = true;
    elements.zoomOut.disabled = true;
    elements.fitWidth.disabled = true;
    elements.fitPage.disabled = true;
    elements.rotateCW.disabled = true;
    elements.rotateCCW.disabled = true;
    elements.pageInput.disabled = true;
    elements.toggleSidebar.disabled = true;
}

function enableAllControls() {
    elements.zoomIn.disabled = false;
    elements.zoomOut.disabled = false;
    elements.fitWidth.disabled = false;
    elements.fitPage.disabled = false;
    elements.rotateCW.disabled = false;
    elements.rotateCCW.disabled = false;
    elements.pageInput.disabled = false;
    elements.toggleSidebar.disabled = false;
    // prevPage和nextPage由updateUI控制
}

/**
 * 加载PDF文件
 */
async function loadPdf(url) {
    debugLog('loadPdf被调用，URL长度: ' + url.length);
    debugLog('URL前100字符: ' + url.substring(0, 100));
    
    try {
        showLoading('正在加载PDF文档...');

        // 清空现有内容
        elements.viewer.innerHTML = '';

        debugLog('开始加载PDF文档...');
        
        // 加载PDF
        const loadingTask = pdfjsLib.getDocument(url);
        debugLog('getDocument已调用，等待promise...');
        
        state.pdfDoc = await loadingTask.promise;
        debugLog('PDF文档加载成功，页数: ' + state.pdfDoc.numPages);
        
        state.totalPages = state.pdfDoc.numPages;
        state.currentPage = 1;
        state.rotation = 0;

        // 先生成所有缩略图（在显示任何内容之前）
        debugLog('生成缩略图...');
        showLoading(`正在生成缩略图 (共${state.totalPages}页)...`);
        await generateThumbnails();
        
        // 加载图层
        debugLog('加载图层...');
        showLoading('正在加载图层...');
        await loadLayers();

        // 缩略图和图层准备完成后，才适应页面和渲染第一页
        debugLog('开始适应页面...');
        showLoading('正在准备显示...');
        await fitToPage(); // 使用适应页面模式，确保整个页面完整显示

        // 最后渲染第一页
        debugLog('开始渲染第一页...');
        showLoading('正在渲染第一页...');
        await renderPage(1);

        // 通知C#
        sendToHost('pdfLoaded', {
            totalPages: state.totalPages,
            currentPage: 1
        });
        
        debugLog('PDF加载完成，所有准备工作已就绪');
        hideLoading();
        
    } catch (error) {
        debugLog('PDF加载错误: ' + error.message);
        debugLog('错误详情: ' + error.stack);
        showLoading('加载失败: ' + error.message);
        setTimeout(hideLoading, 3000);
        sendToHost('loadError', { error: error.message });
    }
}

/**
 * 快速加载PDF（用于撤回/重做）
 */
async function loadPdfQuick(url) {
    debugLog('loadPdfQuick被调用（快速模式）');
    
    try {
        // 清空现有内容
        elements.viewer.innerHTML = '';
        
        // 加载PDF
        const loadingTask = pdfjsLib.getDocument(url);
        state.pdfDoc = await loadingTask.promise;
        
        state.totalPages = state.pdfDoc.numPages;
        state.currentPage = 1;
        state.rotation = 0;
        
        // 直接渲染第一页
        await renderPage(1);
        
        // 后台异步更新缩略图
        updateThumbnailsInBackground();
        
        // 通知C#
        sendToHost('pdfLoaded', {
            totalPages: state.totalPages,
            currentPage: 1
        });
        
        debugLog('PDF快速加载完成');
        
    } catch (error) {
        debugLog('PDF快速加载错误: ' + error.message);
        sendToHost('loadError', { error: error.message });
    }
}

/**
 * 后台更新缩略图
 */
async function updateThumbnailsInBackground() {
    setTimeout(async () => {
        try {
            debugLog('开始后台更新缩略图...');
            await generateThumbnails();
            debugLog('缩略图后台更新完成');
        } catch (error) {
            debugLog('缩略图后台更新失败: ' + error.message);
        }
    }, 100);
}

/**
 * 渲染指定页面
 */
async function renderPage(pageNum) {
    if (!state.pdfDoc) {
        debugLog('renderPage: pdfDoc为空');
        return;
    }

    // 如果正在渲染，只更新待处理页面为最新请求的页面
    // 不是追加到队列，而是覆盖
    if (state.rendering) {
        // 如果待处理页面与请求页面相同，忽略（避免重复日志）
        if (state.pendingPage === pageNum) {
            return;
        }
        state.pendingPage = pageNum;
        debugLog(`renderPage: 正在渲染，更新待处理页面为: ${pageNum}`);
        return;
    }

    state.rendering = true;
    state.currentPage = pageNum;
    state.pendingPage = null; // 清空待处理页面
    
    debugLog(`开始渲染第${pageNum}页，缩放: ${state.scale.toFixed(2)}`);
    
    // 显示小型加载提示（不阻塞交互，只是视觉提示）
    if (!isLoading) {
        elements.viewer.innerHTML = '<div style="text-align: center; padding: 50px; color: #999;">正在加载页面...</div>';
    }

    try {
        const page = await state.pdfDoc.getPage(pageNum);

        // 计算视口
        const viewport = page.getViewport({ 
            scale: state.scale, 
            rotation: state.rotation 
        });
        
        debugLog(`视口尺寸: ${viewport.width.toFixed(0)}x${viewport.height.toFixed(0)}`);

        // 创建画布
        elements.viewer.innerHTML = '';
        const canvas = document.createElement('canvas');
        canvas.className = 'page';
        const context = canvas.getContext('2d');
        canvas.height = viewport.height;
        canvas.width = viewport.width;
        
        // 保存当前渲染的scale，用于CSS缩放计算
        canvas.dataset.renderScale = state.scale;
        
        elements.viewer.appendChild(canvas);

        // 渲染
        await page.render({
            canvasContext: context,
            viewport: viewport
        }).promise;
        
        // 🎯 重置所有CSS样式（渲染完成后已是正确的scale）
        canvas.style.transform = 'none';
        canvas.style.transformOrigin = '';
        canvas.style.transition = ''; // 移除过渡效果
        canvas.style.width = '';
        canvas.style.height = '';
        
        debugLog(`第${pageNum}页渲染完成`);

        // 绘制框线 (如果开启)
        if (typeof showBoxes !== 'undefined' && showBoxes && typeof getPageBoxInfo === 'function') {
            try {
                const boxes = await getPageBoxInfo(pageNum);
                drawBoxOverlay(canvas, boxes, viewport);
            } catch (e) {
                debugLog(`绘制框线失败: ${e.message}`);
            }
        }

        // 更新UI
        updateUI();

        // 通知C#页面变化
        sendToHost('pageChanged', {
            currentPage: state.currentPage,
            totalPages: state.totalPages
        });

        // 更新缩略图高亮
        updateThumbnailSelection(state.currentPage);
        
        // 优先加载当前页的缩略图（如果还未加载）
        priorityLoadThumbnail(state.currentPage);

        // 检查是否可拖拽（更新光标）
        setTimeout(checkDraggable, 100);
        
        // 🎯 如果有保存的鼠标位置（缩放操作），精确调整滚动位置
        if (zoomMousePos) {
            // 使用双重 requestAnimationFrame 确保 DOM 布局完全稳定
            // 第一帧：触发布局重排
            requestAnimationFrame(() => {
                // 强制浏览器计算布局（读取任何布局属性都会触发）
                const _ = elements.viewerContainer.scrollHeight;
                
                // 第二帧：布局已稳定，可以安全调整滚动
                requestAnimationFrame(() => {
                    const newContentX = zoomMousePos.contentX * zoomMousePos.scaleFactor;
                    const newContentY = zoomMousePos.contentY * zoomMousePos.scaleFactor;
                    
                    const newScrollX = Math.max(0, newContentX - zoomMousePos.mouseX);
                    const newScrollY = Math.max(0, newContentY - zoomMousePos.mouseY);
                    
                    // 精确调整滚动位置
                    elements.viewerContainer.scrollLeft = newScrollX;
                    elements.viewerContainer.scrollTop = newScrollY;
                    
                    debugLog(`滚动调整: (${newScrollX.toFixed(0)}, ${newScrollY.toFixed(0)})`);
                    
                    zoomMousePos = null;
                });
            });
        }

    } catch (error) {
        debugLog('渲染错误: ' + error.message);
        debugLog('错误堆栈: ' + error.stack);
        elements.loadingMessage.textContent = '渲染失败: ' + error.message;
        elements.loadingMessage.style.display = 'block';
    } finally {
        // 确保无论成功还是失败都重置渲染标志
        state.rendering = false;
        debugLog(`渲染标志已重置，rendering=${state.rendering}`);
    }

    // 处理待渲染的页面
    if (state.pendingPage !== null) {
        const pending = state.pendingPage;
        state.pendingPage = null;
        debugLog(`处理待渲染页面: ${pending}`);
        await renderPage(pending);
    }
}

/**
 * 更新UI显示
 */
function updateUI() {
    // 更新页码输入框和总页数
    elements.pageInput.value = state.currentPage;
    elements.totalPages.textContent = state.totalPages;
    
    elements.zoomLevel.textContent = Math.round(state.scale * 100) + '%';
    elements.prevPage.disabled = state.currentPage <= 1;
    elements.nextPage.disabled = state.currentPage >= state.totalPages;
}

/**
 * 适应宽度
 */
async function fitToWidth() {
    if (!state.pdfDoc) return;

    const page = await state.pdfDoc.getPage(state.currentPage);
    const viewport = page.getViewport({ scale: 1, rotation: state.rotation });
    const containerWidth = elements.viewerContainer.clientWidth - 20; // 减少留白从60到20
    state.scale = containerWidth / viewport.width;
    await renderPage(state.currentPage);
}

/**
 * 适应页面
 */
async function fitToPage() {
    if (!state.pdfDoc) return;

    const page = await state.pdfDoc.getPage(state.currentPage);
    const viewport = page.getViewport({ scale: 1, rotation: state.rotation });
    const containerWidth = elements.viewerContainer.clientWidth - 20; // 减少留白从60到20
    const containerHeight = elements.viewerContainer.clientHeight - 20; // 减少留白从60到20

    const scaleW = containerWidth / viewport.width;
    const scaleH = containerHeight / viewport.height;
    state.scale = Math.min(scaleW, scaleH);
    await renderPage(state.currentPage);
}

/**
 * 发送消息到C#（支持WebView2和CefSharp两种方式）
 */
function sendToHost(action, data = {}) {
    // 方式1: WebView2
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({ action, ...data }));
    }
    
    // 方式2: CefSharp
    if (typeof CefSharp !== 'undefined' && CefSharp.BindObjectAsync) {
        (async function() {
            await CefSharp.BindObjectAsync('csharpBridge');
            if (window.csharpBridge) {
                switch (action) {
                    case 'pdfLoaded':
                        window.csharpBridge.pdfLoaded(data.totalPages, data.currentPage);
                        break;
                    case 'pageChanged':
                        window.csharpBridge.pageChanged(data.currentPage, data.totalPages);
                        break;
                    case 'loadError':
                        window.csharpBridge.loadError(data.error);
                        break;
                    case 'debug':
                        window.csharpBridge.debugMessage(data.message);
                        break;
                }
            }
        })();
    }
}

/**
 * 设置深色主题
 */
function setDarkMode(isDark) {
    document.body.classList.toggle('dark', isDark);
}

// 事件绑定
elements.prevPage.addEventListener('click', () => {
    if (state.currentPage > 1) renderPage(state.currentPage - 1);
});

elements.nextPage.addEventListener('click', () => {
    if (state.currentPage < state.totalPages) renderPage(state.currentPage + 1);
});

elements.zoomIn.addEventListener('click', () => {
    state.scale *= 1.25;
    renderPage(state.currentPage);
});

elements.zoomOut.addEventListener('click', () => {
    state.scale /= 1.25;
    if (state.scale < 0.25) state.scale = 0.25;
    renderPage(state.currentPage);
});

elements.fitWidth.addEventListener('click', fitToWidth);
elements.fitPage.addEventListener('click', fitToPage);

elements.rotateCW.addEventListener('click', () => {
    state.rotation = (state.rotation + 90) % 360;
    renderPage(state.currentPage);
});

elements.rotateCCW.addEventListener('click', () => {
    state.rotation = (state.rotation - 90 + 360) % 360;
    renderPage(state.currentPage);
});

// 键盘快捷键
document.addEventListener('keydown', (e) => {
    switch (e.key) {
        case 'ArrowLeft':
        case 'PageUp':
            if (state.currentPage > 1) renderPage(state.currentPage - 1);
            break;
        case 'ArrowRight':
        case 'PageDown':
            if (state.currentPage < state.totalPages) renderPage(state.currentPage + 1);
            break;
        case 'Home':
            renderPage(1);
            break;
        case 'End':
            renderPage(state.totalPages);
            break;
        case '+':
        case '=':
            state.scale *= 1.25;
            renderPage(state.currentPage);
            break;
        case '-':
            state.scale /= 1.25;
            if (state.scale < 0.25) state.scale = 0.25;
            renderPage(state.currentPage);
            break;
    }
});

// 接收C#消息
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (e) => {
        try {
            const data = JSON.parse(e.data);
            debugLog('收到C#消息: ' + data.action);
            switch (data.action) {
                case 'loadPdf':
                    loadPdf(data.url);
                    break;
                case 'goToPage':
                    if (data.page >= 1 && data.page <= state.totalPages) {
                        renderPage(data.page);
                    }
                    break;
                case 'setDarkMode':
                    setDarkMode(data.isDark);
                    break;
                case 'zoomIn':
                    state.scale *= 1.25;
                    renderPage(state.currentPage);
                    break;
                case 'zoomOut':
                    state.scale /= 1.25;
                    if (state.scale < 0.25) state.scale = 0.25;
                    renderPage(state.currentPage);
                    break;
                case 'fitWidth':
                    fitToWidth();
                    break;
                case 'fitPage':
                    fitToPage();
                    break;
                case 'rotateCW':
                    state.rotation = (state.rotation + 90) % 360;
                    renderPage(state.currentPage);
                    break;
                case 'rotateCCW':
                    state.rotation = (state.rotation - 90 + 360) % 360;
                    renderPage(state.currentPage);
                    break;
            }
        } catch (err) {
            debugLog('处理消息错误: ' + err.message);
        }
    });
}

// 窗口大小变化时重新适应
let resizeTimeout;
window.addEventListener('resize', () => {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        if (state.pdfDoc) fitToWidth();
    }, 200);
});

// 鼠标滚轮功能（Ctrl+滚轮缩放，普通滚轮翻页）
let wheelTimeout;
let isWheelHandling = false;
let zoomTimeout;
let zoomMousePos = null; // 记录缩放时的鼠标位置

elements.viewerContainer.addEventListener('wheel', (e) => {
    // 阻止默认滚动行为
    e.preventDefault();
    
    if (!state.pdfDoc) return;
    
    // Ctrl+滚轮 -> 缩放（优化版：立即CSS缩放+后台重绘）
    if (e.ctrlKey) {
        // 记录缩放前的鼠标位置（相对于容器）
        const rect = elements.viewerContainer.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;
        
        // 记录缩放前的滚动位置
        const scrollX = elements.viewerContainer.scrollLeft;
        const scrollY = elements.viewerContainer.scrollTop;
        
        // 计算鼠标在内容中的位置（包括滚动偏移）
        const contentX = mouseX + scrollX;
        const contentY = mouseY + scrollY;
        
        const oldScale = state.scale;
        
        const zoomDelta = e.deltaY > 0 ? 0.9 : 1.1; // 向下缩小，向上放大
        state.scale *= zoomDelta;
        if (state.scale < 0.25) state.scale = 0.25;
        if (state.scale > 5.0) state.scale = 5.0;
        
        // 计算缩放比例
        const scaleFactor = state.scale / oldScale;
        
        // 调试日志
        debugLog(`缩放触发: 鼠标(${mouseX.toFixed(0)},${mouseY.toFixed(0)}) 滚动(${scrollX.toFixed(0)},${scrollY.toFixed(0)}) scale:${oldScale.toFixed(2)}->${state.scale.toFixed(2)} factor:${scaleFactor.toFixed(3)}`);
        
        // 保存鼠标位置信息，用于渲染后调整滚动
        zoomMousePos = {
            mouseX: mouseX,
            mouseY: mouseY,
            contentX: contentX,
            contentY: contentY,
            scaleFactor: scaleFactor
        };
        
        // 立即更新缩放显示
        elements.zoomLevel.textContent = Math.round(state.scale * 100) + '%';
        
        // 🎯 优化防抖延迟：50ms -> 20ms（更快响应）
        clearTimeout(zoomTimeout);
        zoomTimeout = setTimeout(() => {
            debugLog(`缩放完成，重新渲染: ${state.scale.toFixed(2)}`);
            renderPage(state.currentPage);
        }, 20); // 减少到20ms，极速响应
    } 
    // 普通滚轮 -> 翻页（有防抖）
    else {
        // 防抖：避免过快翻页
        if (isWheelHandling) return;
        
        isWheelHandling = true;
        
        if (e.deltaY > 0) {
            // 向下滚动 -> 下一页
            if (state.currentPage < state.totalPages) {
                renderPage(state.currentPage + 1);
            }
        } else if (e.deltaY < 0) {
            // 向上滚动 -> 上一页
            if (state.currentPage > 1) {
                renderPage(state.currentPage - 1);
            }
        }
        
        // 300ms 防抖延迟
        clearTimeout(wheelTimeout);
        wheelTimeout = setTimeout(() => {
            isWheelHandling = false;
        }, 300);
    }
}, { passive: false });

// 导出全局函数供C#调用
window.loadPdf = loadPdf;
window.loadPdfQuick = loadPdfQuick;
window.goToPage = (page) => renderPage(page);
window.setDarkMode = setDarkMode;

// ========== 边栏功能 ==========

// 边栏状态
let sidebarVisible = localStorage.getItem('sidebarVisible') !== 'false'; // 默认显示

// 初始化边栏
function initializeSidebar() {
    // 恢复边栏状态
    if (!sidebarVisible) {
        elements.sidebar.classList.add('hidden');
    }
    
    // 边栏切换按钮
    elements.toggleSidebar.addEventListener('click', toggleSidebar);
    
    // 标签页切换
    elements.sidebarTabs.forEach(tab => {
        tab.addEventListener('click', () => switchSidebarTab(tab.dataset.tab));
    });
}

// 切换边栏显示/隐藏
function toggleSidebar() {
    sidebarVisible = !sidebarVisible;
    elements.sidebar.classList.toggle('hidden');
    localStorage.setItem('sidebarVisible', sidebarVisible);
}

// 切换标签页
function switchSidebarTab(tabName) {
    // 更新标签按钮状态
    elements.sidebarTabs.forEach(tab => {
        if (tab.dataset.tab === tabName) {
            tab.classList.add('active');
        } else {
            tab.classList.remove('active');
        }
    });
    
    // 更新面板显示
    if (tabName === 'thumbnails') {
        elements.thumbnailView.classList.add('active');
        elements.layerView.classList.remove('active');
    } else if (tabName === 'layers') {
        elements.thumbnailView.classList.remove('active');
        elements.layerView.classList.add('active');
    }
}

// 缩略图加载状态管理
const thumbnailState = {
    loaded: new Set(), // 已加载的页码
    loading: new Set(), // 正在加载的页码
    priorityQueue: [], // 优先队列
    backgroundLoading: false, // 后台加载标志
    backgroundController: null // 后台加载控制器
};

// 生成所有页面的缩略图（智能加载：前200个+后台异步）
async function generateThumbnails() {
    elements.thumbnailContainer.innerHTML = '';
    thumbnailState.loaded.clear();
    thumbnailState.loading.clear();
    thumbnailState.priorityQueue = [];
    
    const INITIAL_COUNT = 100; // 初始加载100个
    const THUMBNAIL_SCALE = 0.1;
    
    // 1. 先生成所有占位符
    for (let pageNum = 1; pageNum <= state.totalPages; pageNum++) {
        createThumbnailPlaceholder(pageNum);
    }
    
    // 2. 快速加载前200个真实缩略图
    debugLog(`快速加载前${INITIAL_COUNT}个缩略图...`);
    const initialCount = Math.min(INITIAL_COUNT, state.totalPages);
    
    for (let batch = 0; batch < initialCount; batch += 50) {
        const batchStart = batch + 1;
        const batchEnd = Math.min(batch + 50, initialCount);
        
        if (isLoading) {
            elements.loadingText.textContent = `正在生成缩略图 (${batchEnd}/${state.totalPages})...`;
        }
        
        const batchPromises = [];
        for (let pageNum = batchStart; pageNum <= batchEnd; pageNum++) {
            batchPromises.push(loadThumbnail(pageNum, THUMBNAIL_SCALE));
        }
        
        await Promise.all(batchPromises);
    }
    
    debugLog(`前${initialCount}个缩略图加载完成`);
    
    // 3. 启动后台异步加载剩余缩略图
    if (state.totalPages > INITIAL_COUNT) {
        startBackgroundThumbnailLoading(INITIAL_COUNT + 1, THUMBNAIL_SCALE);
    }
}

// 创建缩略图占位符
function createThumbnailPlaceholder(pageNum) {
    const thumbnailWrapper = document.createElement('div');
    thumbnailWrapper.className = 'thumbnail-wrapper';
    thumbnailWrapper.dataset.page = pageNum;
    thumbnailWrapper.id = `thumbnail-${pageNum}`;
    
    // 占位符内容
    const placeholder = document.createElement('div');
    placeholder.className = 'thumbnail-placeholder';
    placeholder.style.cssText = `
        width: 80px;
        height: 100px;
        background: #2a2a2a;
        display: flex;
        align-items: center;
        justify-content: center;
        border: 1px solid #444;
        color: #666;
        font-size: 12px;
    `;
    placeholder.textContent = pageNum;
    
    thumbnailWrapper.appendChild(placeholder);
    
    thumbnailWrapper.addEventListener('click', () => {
        renderPage(pageNum);
    });
    
    elements.thumbnailContainer.appendChild(thumbnailWrapper);
}

// 加载单个缩略图
async function loadThumbnail(pageNum, scale) {
    if (thumbnailState.loaded.has(pageNum) || thumbnailState.loading.has(pageNum)) {
        return;
    }
    
    thumbnailState.loading.add(pageNum);
    
    try {
        const page = await state.pdfDoc.getPage(pageNum);
        const viewport = page.getViewport({ scale: scale, rotation: 0 });
        
        const canvas = document.createElement('canvas');
        canvas.className = 'thumbnail-item';
        canvas.height = viewport.height;
        canvas.width = viewport.width;
        
        const context = canvas.getContext('2d');
        await page.render({
            canvasContext: context,
            viewport: viewport
        }).promise;
        
        // 更新DOM - 替换占位符
        const wrapper = document.getElementById(`thumbnail-${pageNum}`);
        if (wrapper) {
            wrapper.innerHTML = '';
            wrapper.appendChild(canvas);
            
            const pageLabel = document.createElement('div');
            pageLabel.textContent = pageNum;
            pageLabel.style.fontSize = '11px';
            pageLabel.style.textAlign = 'center';
            pageLabel.style.marginTop = '4px';
            wrapper.appendChild(pageLabel);
        }
        
        thumbnailState.loaded.add(pageNum);
        thumbnailState.loading.delete(pageNum);
    } catch (error) {
        debugLog(`缩略图${pageNum}加载失败: ${error.message}`);
        thumbnailState.loading.delete(pageNum);
    }
}

// 启动后台缩略图加载
function startBackgroundThumbnailLoading(startPage, scale) {
    thumbnailState.backgroundLoading = true;
    
    debugLog(`开始后台加载第${startPage}-${state.totalPages}页缩略图`);
    
    // 使用异步加载，不阻塞主线程
    (async () => {
        for (let pageNum = startPage; pageNum <= state.totalPages; pageNum++) {
            // 检查优先队列
            while (thumbnailState.priorityQueue.length > 0) {
                const priorityPage = thumbnailState.priorityQueue.shift();
                await loadThumbnail(priorityPage, scale);
            }
            
            // 加载当前页
            if (!thumbnailState.loaded.has(pageNum)) {
                await loadThumbnail(pageNum, scale);
            }
            
            // 每10个休息一下，避免阻塞
            if (pageNum % 10 === 0) {
                await new Promise(resolve => setTimeout(resolve, 50));
            }
        }
        
        thumbnailState.backgroundLoading = false;
        debugLog('所有缩略图后台加载完成');
    })();
}

// 优先加载指定页面的缩略图（用户跳转时调用）
async function priorityLoadThumbnail(pageNum) {
    if (thumbnailState.loaded.has(pageNum)) {
        return; // 已加载
    }
    
    if (!thumbnailState.loading.has(pageNum)) {
        // 添加到优先队列
        if (!thumbnailState.priorityQueue.includes(pageNum)) {
            thumbnailState.priorityQueue.unshift(pageNum); // 插队到最前面
            debugLog(`优先加载缩略图: ${pageNum}`);
        }
    }
}

// 渲染单个缩略图
async function renderThumbnail(pageNum, canvas) {
    try {
        const page = await state.pdfDoc.getPage(pageNum);
        const viewport = page.getViewport({ scale: 0.2, rotation: state.rotation });
        
        const context = canvas.getContext('2d');
        canvas.height = viewport.height;
        canvas.width = viewport.width;
        
        await page.render({
            canvasContext: context,
            viewport: viewport
        }).promise;
    } catch (error) {
        debugLog(`缩略图 ${pageNum} 渲染失败: ${error.message}`);
    }
}

// 加载图层信息
async function loadLayers() {
    if (!state.pdfDoc) return;
    
    try {
        const optionalContentConfig = await state.pdfDoc.getOptionalContentConfig();
        const groups = optionalContentConfig.getGroups();
        
        if (!groups || Object.keys(groups).length === 0) {
            elements.layerContainer.innerHTML = '<p class="empty-message">此PDF无可用图层</p>';
            return;
        }
        
        elements.layerContainer.innerHTML = '';
        
        for (const [id, group] of Object.entries(groups)) {
            const item = document.createElement('div');
            item.className = 'layer-item';
            
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'layer-checkbox';
            checkbox.checked = group.visible !== false;
            checkbox.dataset.layerId = id;
            
            const name = document.createElement('span');
            name.className = 'layer-name';
            name.textContent = group.name || `图层 ${id}`;
            
            checkbox.addEventListener('change', (e) => toggleLayer(id, e.target.checked));
            
            item.appendChild(checkbox);
            item.appendChild(name);
            elements.layerContainer.appendChild(item);
        }
    } catch (error) {
        debugLog(`加载图层失败: ${error.message}`);
        elements.layerContainer.innerHTML = '<p class="empty-message">加载图层失败</p>';
    }
}

// 切换图层可见性
async function toggleLayer(layerId, visible) {
    try {
        const optionalContentConfig = await state.pdfDoc.getOptionalContentConfig();
        await optionalContentConfig.setVisibility(layerId, visible);
        
        // 重新渲染当前页
        await renderPage(state.currentPage);
    } catch (error) {
        debugLog(`切换图层失败: ${error.message}`);
    }
}

// 更新缩略图高亮
function updateThumbnailSelection(pageNum) {
    const thumbnails = elements.thumbnailContainer.querySelectorAll('.thumbnail-item');
    thumbnails.forEach(item => {
        if (parseInt(item.dataset.page) === pageNum) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}

// 初始化边栏
initializeSidebar();

// ========== 页码输入跳转功能 ==========

// 页码输入框事件
elements.pageInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        const pageNum = parseInt(elements.pageInput.value);
        if (!isNaN(pageNum) && pageNum >= 1 && pageNum <= state.totalPages) {
            renderPage(pageNum);
            elements.pageInput.blur(); // 跳转后失焦
        } else {
            // 输入无效，恢复当前页码
            elements.pageInput.value = state.currentPage;
        }
    }
});

// 点击输入框时选中所有内容
elements.pageInput.addEventListener('focus', () => {
    elements.pageInput.select();
});

// 失焦时恢复当前页码（防止留下无效输入）
elements.pageInput.addEventListener('blur', () => {
    elements.pageInput.value = state.currentPage;
});

// ========== 拖拽移动功能 ==========

let isDragging = false;
let startX = 0;
let startY = 0;
let scrollLeft = 0;
let scrollTop = 0;

// 检查页面是否大于显示区域
function checkDraggable() {
    const container = elements.viewerContainer;
    const canvas = elements.viewer.querySelector('canvas');
    
    if (!canvas) {
        container.classList.remove('draggable');
        return;
    }
    
    // 检查实际PDF内容（canvas）是否大于容器
    const canvasRect = canvas.getBoundingClientRect();
    const containerRect = container.getBoundingClientRect();
    
    const isDraggableX = canvasRect.width > containerRect.width;
    const isDraggableY = canvasRect.height > containerRect.height;
    
    if (isDraggableX || isDraggableY) {
        container.classList.add('draggable');
    } else {
        container.classList.remove('draggable');
    }
}

// 鼠标按下开始拖拽
elements.viewerContainer.addEventListener('mousedown', (e) => {
    // 只响应鼠标左键
    if (e.button !== 0) return;
    
    // 检查是否可拖拽
    if (!elements.viewerContainer.classList.contains('draggable')) return;
    
    isDragging = true;
    elements.viewerContainer.classList.add('dragging');
    
    startX = e.pageX - elements.viewerContainer.offsetLeft;
    startY = e.pageY - elements.viewerContainer.offsetTop;
    scrollLeft = elements.viewerContainer.scrollLeft;
    scrollTop = elements.viewerContainer.scrollTop;
    
    e.preventDefault();
});

// 鼠标移动拖拽
elements.viewerContainer.addEventListener('mousemove', (e) => {
    if (!isDragging) return;
    
    e.preventDefault();
    
    const x = e.pageX - elements.viewerContainer.offsetLeft;
    const y = e.pageY - elements.viewerContainer.offsetTop;
    
    const walkX = (x - startX) * 1.5; // 拖拽速度倍数
    const walkY = (y - startY) * 1.5;
    
    elements.viewerContainer.scrollLeft = scrollLeft - walkX;
    elements.viewerContainer.scrollTop = scrollTop - walkY;
});

// 鼠标抬起结束拖拽
elements.viewerContainer.addEventListener('mouseup', () => {
    isDragging = false;
    elements.viewerContainer.classList.remove('dragging');
});

// 鼠标离开容器结束拖拽
elements.viewerContainer.addEventListener('mouseleave', () => {
    isDragging = false;
    elements.viewerContainer.classList.remove('dragging');
});

// 渲染后检查是否可拖拽
// 在renderPage函数完成后调用checkDraggable()
// 这将在后续集成

// 页面加载完成后输出调试信息
debugLog('bridge.js 初始化完成');

// ========== PDF框线显示功能 ==========

// 框线显示状态
let showBoxes = false;
let currentBoxInfo = null;

/**
 * 切换框线显示
 */
function toggleBoxDisplay() {
    showBoxes = !showBoxes;
    debugLog(`框线显示: ${showBoxes ? '开启' : '关闭'}`);
    
    // 更新按钮状态  
    const btn = document.getElementById('toggleBoxes');
    if (btn) {
        btn.style.background = showBoxes ? 'var(--btn-active)' : 'transparent';
    }
    
    // 重新渲染当前页以显示/隐藏框线
    if (state.pdfDoc) {
        renderPage(state.currentPage);
    }
}

/**
 * 获取页面的框信息
 */
async function getPageBoxInfo(pageNum) {
    try {
        const page = await state.pdfDoc.getPage(pageNum);
        
        // MediaBox始终存在
        const mediaBox = page.view; // [x, y, width, height]
        
        // 尝试获取其他框
        let cropBox = null;
        let trimBox = null;
        let bleedBox = null;
        
        // 通过page对象获取
        if (page.ref) {
            try {
                // CropBox
                const cropBoxArray = page.getAttribute('CropBox');
                if (cropBoxArray) cropBox = cropBoxArray;
                
                // TrimBox
                const trimBoxArray = page.getAttribute('TrimBox');
                if (trimBoxArray) trimBox = trimBoxArray;
                
                // BleedBox  
                const bleedBoxArray = page.getAttribute('BleedBox');
                if (bleedBoxArray) bleedBox = bleedBoxArray;
            } catch (e) {
                debugLog(`获取框信息部分失败: ${e.message}`);
            }
        }
        
        // 如果没有CropBox,默认等于MediaBox
        if (!cropBox) cropBox = mediaBox;
        
        return {
            mediaBox: mediaBox,
            cropBox: cropBox,
            trimBox: trimBox,
            bleedBox: bleedBox
        };
    } catch (error) {
        debugLog(`获取页面框信息失败: ${error.message}`);
        return null;
    }
}

/**
 * 在Canvas上绘制框线叠加层
 */
function drawBoxOverlay(canvas, boxInfo, viewport) {
    if (!boxInfo || !showBoxes) return;
    
    const ctx = canvas.getContext('2d');
    ctx.save();
    
    // 框线配置 (按绘制顺序: 从外到内)
    const boxes = [
        { data: boxInfo.bleedBox, color: '#FF8C00', label: 'BleedBox', dash: [10, 5], width: 2 },
        { data: boxInfo.mediaBox, color: '#0000FF', label: 'MediaBox', dash: [], width: 2 },
        { data: boxInfo.cropBox, color: '#00FF00', label: 'CropBox', dash: [], width: 1.5 },
        { data: boxInfo.trimBox, color: '#FF0000', label: 'TrimBox', dash: [5, 3], width: 2 }
    ];
    
    boxes.forEach(box => {
        if (!box.data) return;
        
        try {
            // PDF坐标 [x1, y1, x2, y2]
            const [x1, y1, x2, y2] = box.data;
            
            // 转换为Canvas坐标
            const canvasRect = viewport.convertToViewportRectangle([x1, y1, x2, y2]);
            const [cx1, cy1, cx2, cy2] = canvasRect;
            
            // 绘制矩形
            ctx.strokeStyle = box.color;
            ctx.lineWidth = box.width;
            ctx.setLineDash(box.dash);
            
            const rectX = Math.min(cx1, cx2);
            const rectY = Math.min(cy1, cy2);
            const rectWidth = Math.abs(cx2 - cx1);
            const rectHeight = Math.abs(cy2 - cy1);
            
            ctx.strokeRect(rectX, rectY, rectWidth, rectHeight);
            
            // 绘制标签
            ctx.fillStyle = box.color;
            ctx.font = 'bold 12px Arial';
            ctx.setLineDash([]); // 标签文字不使用虚线
            
            // 标签位置: 左上角外侧
            const labelX = rectX + 5;
            const labelY = rectY - 5;
            
            // 绘制标签背景
            const textMetrics = ctx.measureText(box.label);
            ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
            ctx.fillRect(labelX - 2, labelY - 12, textMetrics.width + 4, 16);
            
            // 绘制标签文字
            ctx.fillStyle = box.color;
            ctx.fillText(box.label, labelX, labelY);
            
        } catch (e) {
            debugLog(`绘制${box.label}失败: ${e.message}`);
        }
    });
    
    ctx.restore();
}

// 绑定框线切换按钮
const toggleBoxesBtn = document.getElementById('toggleBoxes');
if (toggleBoxesBtn) {
    toggleBoxesBtn.addEventListener('click', toggleBoxDisplay);
}

// 导出函数供C#调用
window.toggleBoxDisplay = toggleBoxDisplay;

debugLog('PDF框线显示功能已加载');
