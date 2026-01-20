/**
 * 缩略图渲染Worker
 * 在后台线程中渲染PDF缩略图，避免阻塞主线程
 */

// 导入PDF.js
self.importScripts('build/pdf.min.js');

// 配置PDF.js worker
pdfjsLib.GlobalWorkerOptions.workerSrc = 'build/pdf.worker.min.js';

let pdfDoc = null;

// 监听主线程消息
self.addEventListener('message', async (e) => {
    const { type, data } = e.data;
    
    try {
        switch (type) {
            case 'loadPdf':
                // 加载PDF文档
                const loadingTask = pdfjsLib.getDocument(data.url);
                pdfDoc = await loadingTask.promise;
                self.postMessage({ 
                    type: 'pdfLoaded', 
                    totalPages: pdfDoc.numPages 
                });
                break;
                
            case 'renderThumbnail':
                // 渲染单个缩略图
                if (!pdfDoc) {
                    throw new Error('PDF未加载');
                }
                
                const page = await pdfDoc.getPage(data.pageNum);
                const viewport = page.getViewport({ 
                    scale: data.scale, 
                    rotation: 0 
                });
                
                // 创建OffscreenCanvas（如果支持）
                const canvas = new OffscreenCanvas(viewport.width, viewport.height);
                const context = canvas.getContext('2d');
                
                await page.render({
                    canvasContext: context,
                    viewport: viewport
                }).promise;
                
                // 转换为ImageBitmap传回主线程
                const imageBitmap = await canvas.convertToBlob()
                    .then(blob => createImageBitmap(blob));
                
                self.postMessage({
                    type: 'thumbnailReady',
                    pageNum: data.pageNum,
                    imageBitmap: imageBitmap,
                    width: viewport.width,
                    height: viewport.height
                }, [imageBitmap]);
                break;
                
            case 'renderBatch':
                // 批量渲染缩略图
                if (!pdfDoc) {
                    throw new Error('PDF未加载');
                }
                
                for (const pageNum of data.pageNumbers) {
                    const page = await pdfDoc.getPage(pageNum);
                    const viewport = page.getViewport({ 
                        scale: data.scale, 
                        rotation: 0 
                    });
                    
                    const canvas = new OffscreenCanvas(viewport.width, viewport.height);
                    const context = canvas.getContext('2d');
                    
                    await page.render({
                        canvasContext: context,
                        viewport: viewport
                    }).promise;
                    
                    const imageBitmap = await canvas.convertToBlob()
                        .then(blob => createImageBitmap(blob));
                    
                    self.postMessage({
                        type: 'thumbnailReady',
                        pageNum: pageNum,
                        imageBitmap: imageBitmap,
                        width: viewport.width,
                        height: viewport.height
                    }, [imageBitmap]);
                }
                
                self.postMessage({ 
                    type: 'batchComplete',
                    batchId: data.batchId
                });
                break;
        }
    } catch (error) {
        self.postMessage({
            type: 'error',
            message: error.message,
            stack: error.stack
        });
    }
});
