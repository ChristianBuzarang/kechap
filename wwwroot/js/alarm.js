window.kechapAlarm = {
    play: function (sound) {
        const AudioCtx = window.AudioContext || window.webkitAudioContext;
        if (!AudioCtx) return;

        const ctx = new AudioCtx();
        if (ctx.state === "suspended") ctx.resume();

        const tone = (freq, start, duration, type, volume) => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            const t0 = ctx.currentTime + start;

            osc.type = type;
            osc.frequency.setValueAtTime(freq, t0);
            gain.gain.setValueAtTime(0.0001, t0);
            gain.gain.exponentialRampToValueAtTime(volume, t0 + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, t0 + duration);

            osc.connect(gain).connect(ctx.destination);
            osc.start(t0);
            osc.stop(t0 + duration + 0.05);
        };

        let totalSeconds;

        switch (sound) {
            case "Digital Beep":
                for (let i = 0; i < 4; i++) tone(1000, i * 0.25, 0.15, "square", 0.15);
                totalSeconds = 1.2;
                break;

            case "Bird Chirp":
                for (let i = 0; i < 3; i++) {
                    tone(2000, i * 0.4, 0.08, "sine", 0.25);
                    tone(2600, i * 0.4 + 0.1, 0.12, "sine", 0.25);
                }
                totalSeconds = 1.6;
                break;

            default: // Kitchen Bell
                tone(880, 0, 1.5, "triangle", 0.35);
                tone(1760, 0, 1.0, "sine", 0.15);
                tone(880, 1.2, 1.5, "triangle", 0.35);
                totalSeconds = 3;
                break;
        }

        setTimeout(() => ctx.close(), (totalSeconds + 0.5) * 1000);
    }
};