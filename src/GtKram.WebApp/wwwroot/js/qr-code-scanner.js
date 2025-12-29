class QrCodeScanner {

    constructor(videoElement, scanCallback) {
        this.videoElement = videoElement;
        this.canvasElement = document.createElement('canvas');
        this.canvasContext = this.canvasElement.getContext("2d", { alpha: false, willReadFrequently: true });
        this.canvasContext.imageSmoothingEnabled = false;
        this.scanCallback = scanCallback;

        this.scanning = false;
        this.requestFrameId = null;
        this.nativeDetector = null;
    }

    async start() {
        if (!navigator.mediaDevices) {
            console.error("Camera not found.");
            return false;
        }

        if ('BarcodeDetector' in window && (await BarcodeDetector.getSupportedFormats()).includes('qr_code')) {
            this.nativeDetector = new BarcodeDetector({ formats: ['qr_code'] });
        }

        try {
            const videoStream = await navigator.mediaDevices.getUserMedia({ audio: false, video: { facingMode: "environment" } });
            this.videoElement.srcObject = videoStream;
            this.play();
            return true;
        } catch (err) {
            console.error("Accessing camera failed: " + err.message);
            return false;
        }
    }

    stop() {
        this.scanning = false;

        if (this.requestFrameId) {
            cancelAnimationFrame(this.requestFrameId);
            this.requestFrameId = null;
        }

        this.videoElement.pause();
        const videoStream = this.videoElement.srcObject;
        this.videoElement.srcObject = null;

        if (videoStream) {
            videoStream.getTracks().forEach(t => t.stop());
        }
    }

    play() {
        this.videoElement.play().then(() => {
            this.scanning = true;
            this.scanLoop();
        });
    }


    scanLoop() {
        if (!this.scanning) {
            return;
        }

        if (this.videoElement.readyState === HTMLMediaElement.HAVE_ENOUGH_DATA) {
            this.canvasElement.width = this.videoElement.videoWidth;
            this.canvasElement.height = this.videoElement.videoHeight;
            this.canvasContext.drawImage(this.videoElement, 0, 0, this.canvasElement.width, this.canvasElement.height);
            const imageData = this.canvasContext.getImageData(0, 0, this.canvasElement.width, this.canvasElement.height);

            if (this.nativeDetector) {
                this.nativeDetector.detect(imageData).then(barCodes => {
                    if (barCodes.length == 0) {
                        this.scanLoop();
                    } else {
                        this.scanning = false;
                        this.videoElement.pause();
                        this.scanCallback(barCodes[0].rawValue);
                    }
                });
                return;
            }

            const code = jsQR(imageData.data, imageData.width, imageData.height);
            if (code) {
                this.scanning = false;
                this.videoElement.pause();
                this.scanCallback(code.data);
                return;
            }
        }

        this.requestFrameId = requestAnimationFrame(() => this.scanLoop());
    }
}

