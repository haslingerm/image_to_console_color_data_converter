window.imageHelper = {
    /**
     * Decodes a data-URL image, resizes it to targetWidth (preserving aspect ratio)
     * using the browser's Canvas API, and returns the raw RGBA pixel data.
     *
     * @param {string} dataUrl  - A data: URL containing a PNG or JPEG image.
     * @param {number} targetWidth - Desired output width in pixels.
     * @returns {Promise<{width: number, height: number, data: number[]}>}
     */
    getResizedPixels: function (dataUrl, targetWidth) {
        return new Promise((resolve, reject) => {
            const img = new Image();

            img.onload = () => {
                const aspectRatio = img.naturalWidth / img.naturalHeight;
                const targetHeight = Math.round(targetWidth / aspectRatio);

                const canvas = document.createElement('canvas');
                canvas.width = targetWidth;
                canvas.height = targetHeight;

                const ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

                const imageData = ctx.getImageData(0, 0, targetWidth, targetHeight);

                // imageData.data is a Uint8ClampedArray of [R,G,B,A, R,G,B,A, ...]
                // Convert to a plain JS Array so it serialises correctly over JS interop.
                resolve({
                    width: targetWidth,
                    height: targetHeight,
                    data: Array.from(imageData.data)
                });
            };

            img.onerror = () => reject('imageHelper: failed to decode image from data URL');

            img.src = dataUrl;
        });
    }
};

