window.conversacionesAudio = (function () {
    let _recorder = null;
    let _chunks = [];

    return {
        startRecording: async function () {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            _chunks = [];
            _recorder = new MediaRecorder(stream);
            _recorder.ondataavailable = e => { if (e.data.size > 0) _chunks.push(e.data); };
            _recorder.start();
            return true;
        },

        stopRecording: function () {
            return new Promise(resolve => {
                _recorder.onstop = () => {
                    const blob = new Blob(_chunks, { type: _recorder.mimeType || 'audio/webm' });
                    const reader = new FileReader();
                    reader.onloadend = () => resolve(reader.result.split(',')[1]);
                    reader.readAsDataURL(blob);
                    _recorder.stream.getTracks().forEach(t => t.stop());
                    _recorder = null;
                    _chunks = [];
                };
                _recorder.stop();
            });
        },

        isRecording: function () {
            return _recorder !== null && _recorder.state === 'recording';
        }
    };
})();

window.conversacionesUi = {
    _threadWatchers: new WeakMap(),

    isNearBottom: function (element) {
        if (!element) return false;
        const distance = element.scrollHeight - element.scrollTop - element.clientHeight;
        return distance < 96;
    },

    scrollToBottom: function (element) {
        if (!element) return;
        element.scrollTop = element.scrollHeight;
    },

    watchThreadScroll: function (element, dotNetRef) {
        if (!element || !dotNetRef) return false;

        const previous = this._threadWatchers.get(element);
        if (previous) {
            element.removeEventListener('scroll', previous.handler);
        }

        let scheduled = false;
        let lastAway = !this.isNearBottom(element);
        const notify = () => {
            scheduled = false;
            const away = !window.conversacionesUi.isNearBottom(element);
            if (away === lastAway) return;
            lastAway = away;
            dotNetRef.invokeMethodAsync('OnThreadScrollStateChanged', away).catch(() => {});
        };

        const handler = () => {
            if (scheduled) return;
            scheduled = true;
            window.requestAnimationFrame(notify);
        };

        element.addEventListener('scroll', handler, { passive: true });
        this._threadWatchers.set(element, { handler: handler });
        return lastAway;
    }
};
