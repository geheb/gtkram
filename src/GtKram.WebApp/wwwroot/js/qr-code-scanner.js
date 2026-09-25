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
            } catch (err) { }
        }

        this.handleVisibilityChange = () => {
            if (document.hidden) {
                this.scanning = false;
            } else {
                this.resume();
            }
        };

        document.addEventListener("visibilitychange", this.handleVisibilityChange);
    }

    async getBestCameraId() {

        const tempStream = await navigator.mediaDevices.getUserMedia({
            audio: false,
            video: { facingMode: "environment" }
        });
        tempStream.getTracks().forEach(t => t.stop());

        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoInputs = devices.filter(d => d.kind === 'videoinput');

        if (videoInputs.length === 0) throw new Error("Keine Kamera erkannt!");;
        if (videoInputs.length === 1) return videoInputs[0].deviceId;

        let bestDeviceId = videoInputs[0].deviceId;
        let highestScore = -Infinity;

        for (const dev of videoInputs) {
            try {
                const testStream = await navigator.mediaDevices.getUserMedia({
                    audio: false,
                    video: { deviceId: { exact: dev.deviceId } }
                });
                const track = testStream.getVideoTracks()[0];
                const caps = track.getCapabilities ? track.getCapabilities() : {};
                const label = dev.label.toLowerCase();

                let score = 0;

                const isUnwanted = ['ultra', '0.5', 'wide', 'weit', 'tele', 'depth', 'front', 'vorder', 'selfie'].some(kw => label.includes(kw));
                if (isUnwanted) {
                    score -= 50000;
                }

                if (caps.focusMode && caps.focusMode.includes('continuous')) {
                    score += 20000;
                }

                const maxW = caps.width ? caps.width.max : 0;
                const maxH = caps.height ? caps.height.max : 0;
                score += (maxW * maxH);

                if (score > highestScore) {
                    highestScore = score;
                    bestDeviceId = dev.deviceId;
                }

                testStream.getTracks().forEach(t => t.stop());
            } catch (e) { }
        }

        return bestDeviceId;
    }

    async start() {
        if (this.scanning) return;
        try {

            let deviceId = sessionStorage.getItem("scannerDeviceId");
            if (!deviceId) {
                deviceId = await this.getBestCameraId();
                sessionStorage.setItem("scannerDeviceId", deviceId);
            }

            this.stream = await navigator.mediaDevices.getUserMedia({
                audio: false,
                video: {
                    deviceId: { exact: deviceId },
                    width: { ideal: 1920 },
                    height: { ideal: 1080 }
                }
            });

            this.video.srcObject = this.stream;
            await this.video.play();

            const track = this.stream.getVideoTracks()[0];
            const caps = track.getCapabilities ? track.getCapabilities() : {};
            if (caps.focusMode && caps.focusMode.includes("continuous")) {
                try {
                    await track.applyConstraints({ advanced: [{ focusMode: "continuous" }] });
                } catch (err) { }
            }

            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;

            this.scanning = true;
            window.requestAnimationFrame(time => this.processFrame({ lastScanned: 0, minDelay: this.minDelay }, time));
        } catch (err) {
            sessionStorage.removeItem("scannerDeviceId");
            if (this.onErrorCallback) this.onErrorCallback(err);
        }
    }

    destroy() {
        this.scanning = false;
        document.removeEventListener("visibilitychange", this.handleVisibilityChange);

        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }
        this.video.srcObject = null;
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    }

    resume() {
        if (this.scanning || !this.stream) return;
        this.scanning = true;
        window.requestAnimationFrame((time) => this.processFrame({ lastScanned: 0, minDelay: this.minDelay }, time));
    }

    async processFrame(state, timeNow) {
        if (!this.scanning || this.video.readyState === 0) return;

        if ((timeNow - state.lastScanned) < state.minDelay) {
            window.requestAnimationFrame(time => this.processFrame(state, time));
            return;
        }

        if (this.video.readyState === this.video.HAVE_ENOUGH_DATA) {

            if (this.barcodeDetector) {
                try {
                    const barcodes = await this.barcodeDetector.detect(this.video);
                    if (barcodes.length > 0 && barcodes[0].rawValue) {
                        this.scanning = false;
                        if (this.onDetectedCallback) this.onDetectedCallback(barcodes[0].rawValue);
                        return;
                    }
                } catch (err) { }
            }
			
			// fallback if previous fails
            if (typeof jsQR !== 'undefined') {

                const targetWidth = Math.floor(this.video.videoWidth * 0.5);
                const targetHeight = Math.floor(this.video.videoHeight * 0.5);

                if (this.canvas.width !== targetWidth || this.canvas.height !== targetHeight) {
                    this.canvas.width = targetWidth;
                    this.canvas.height = targetHeight;
                }

                this.ctx.drawImage(this.video, 0, 0, targetWidth, targetHeight);
                const imageData = this.ctx.getImageData(0, 0, targetWidth, targetHeight);

                const code = jsQR(imageData.data, imageData.width, imageData.height, {
                    inversionAttempts: "dontInvert",
                });

                if (code && code.data) {
                    this.scanning = false;
                    if (this.onDetectedCallback) this.onDetectedCallback(code.data);
                    return;
                }
            }
        }

        window.requestAnimationFrame(time => this.processFrame({ lastScanned: timeNow, minDelay: state.minDelay }, time));
    }
}