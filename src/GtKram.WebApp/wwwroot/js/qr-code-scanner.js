class QrCodeScanner {
    constructor({ video, canvas, onDetected, onError }) {
        this.video = video;
        this.canvas = canvas;
        this.ctx = this.canvas.getContext("2d", { willReadFrequently: true });
        this.onDetectedCallback = onDetected;
        this.onErrorCallback = onError;
        this.scanning = false;
        this.stream = null;
    }

    async start() {
        if (this.scanning) return;
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: "environment" },
            });
            this.video.srcObject = this.stream;
            await this.video.play();
            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;
            this.scanning = true;
            requestAnimationFrame(() => this.tick());
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
        requestAnimationFrame(() => this.tick());
    }

    tick() {
        if (!this.scanning) return;
        if (this.video.readyState === this.video.HAVE_ENOUGH_DATA) {
            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;
            this.ctx.drawImage(this.video, 0, 0, this.canvas.width, this.canvas.height);
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
        requestAnimationFrame(() => this.tick());
    }
}
