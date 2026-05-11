class QrCodeScanner {
    constructor({ video, canvas, onDetected, onError }) {
        this.video = video;
        this.canvas = canvas;
        this.ctx = this.canvas.getContext("2d", { willReadFrequently: true });
        this.onDetectedCallback = onDetected;
        this.onErrorCallback = onError;
        this.minDelay = 40;
        this.scanning = false;
        this.stream = null;
        this.barcodeDetector = null;

        if (typeof BarcodeDetector !== 'undefined') {
            try {
                this.barcodeDetector = new BarcodeDetector({ formats: ['qr_code'] });
            } catch (err) {
                console.error('Failed to initialize BarcodeDetector:', err);
            }
        }
    }

    async start() {
        if (this.scanning) return;
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                audio: false,
                video: {
                    facingMode: "environment",
                    focusMode: "continuous",
                    exposureMode: "continuous",
                },
            });
            this.video.srcObject = this.stream;
            await this.video.play();
            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;
            this.scanning = true;
            window.requestAnimationFrame((time) => this.processFrame({ lastScanned: 0, minDelay: this.minDelay }, time));
        } catch (err) {
            if (this.onErrorCallback) this.onErrorCallback(err);
        }
    }

    destroy() {
        this.scanning = false;
        if (this.stream) {
            this.stream.getTracks().forEach((track) => track.stop());
            this.stream = null;
        }
        this.video.srcObject = null;
    }

    resume() {
        if (this.scanning) return;
        this.scanning = true;
        window.requestAnimationFrame((time) => this.processFrame({ lastScanned: 0, minDelay: this.minDelay }, time));
    }

    async processFrame(state, timeNow) {
        if (!this.scanning || this.video.readyState === 0) return;

        // Scanning is expensive and we don't need to scan camera frames with he maximum possible frequency. 
        if ((timeNow - state.lastScanned) < state.minDelay) {
            window.requestAnimationFrame((time) => this.processFrame(state, time));
            return;
        }

        if (this.video.readyState === this.video.HAVE_ENOUGH_DATA) {
            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;
            this.ctx.drawImage(this.video, 0, 0, this.canvas.width, this.canvas.height);

            if (this.barcodeDetector) {
                try {
                    const barcodes = await this.barcodeDetector.detect(this.video);
                    if (barcodes.length > 0 && barcodes[0].rawValue) {
                        this.scanning = false;
                        if (this.onDetectedCallback) this.onDetectedCallback(barcodes[0].rawValue);
                        return;
                    }
                } catch (err) {
                    console.error('BarcodeDetector detection failed:', err);
                }
            }

            const imageData = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
            const code = jsQR(imageData.data, imageData.width, imageData.height, {
                inversionAttempts: "dontInvert",
            });
            if (code && code.data) {
                this.scanning = false;
                if (this.onDetectedCallback) this.onDetectedCallback(code.data);
                return;
            }
        }

        window.requestAnimationFrame((time) => this.processFrame({ lastScanned: timeNow, minDelay: state.minDelay }, time));
    }
}