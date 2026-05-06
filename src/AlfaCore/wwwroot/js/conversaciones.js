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
