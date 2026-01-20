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
        
        // 尝试获取其他框 (需要访问底层PDF Dictionary)
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
