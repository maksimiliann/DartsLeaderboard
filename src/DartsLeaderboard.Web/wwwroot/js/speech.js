window.dartsSpeech = (function () {
    let context = null;
    let source = null;
    let keepAlive = null;
    let buffer = null;
    let text = "";
    let startedAt = 0;
    let offset = 0;
    let phase = "idle";
    let ended = null;

    function allowPlaybackDuringSilentMode() {
        try {
            const session = navigator.audioSession;
            if (session && session.type !== "playback") {
                session.type = "playback";
            }
        } catch (ignored) {
        }
    }

    function ensureContext() {
        allowPlaybackDuringSilentMode();
        if (!context || context.state === "closed") {
            context = new AudioContext();
        }

        return context;
    }

    // A silent tone started in the tap keeps the phone's audio permission
    // while speech is synthesized. Playback itself happens later.
    function holdSession() {
        const audio = ensureContext();
        if (audio.state !== "running") {
            audio.resume();
        }

        if (keepAlive || phase === "playing") {
            return;
        }

        const tone = audio.createOscillator();
        const gain = audio.createGain();
        gain.gain.value = 0;
        tone.connect(gain);
        gain.connect(audio.destination);
        tone.start();
        keepAlive = tone;
    }

    function releaseSession() {
        if (!keepAlive) {
            return;
        }

        try {
            keepAlive.stop();
        } catch (ignored) {
        }

        keepAlive.disconnect();
        keepAlive = null;
    }

    document.addEventListener("pointerdown", function (event) {
        const target = event.target;
        if (!target || !target.closest || !target.closest("[data-testid=speak-results]")) {
            return;
        }

        holdSession();
    }, true);

    function stopSource() {
        if (!source) {
            return;
        }

        source.onended = null;
        try {
            source.stop();
        } catch (ignored) {
        }
        source = null;
    }

    function startAt(from) {
        const audio = ensureContext();
        releaseSession();
        stopSource();
        const next = audio.createBufferSource();
        next.buffer = buffer;
        next.connect(audio.destination);
        const duration = buffer.duration;
        const startOffset = Math.min(Math.max(from, 0), Math.max(duration - 0.05, 0));
        next.onended = function () {
            if (source !== next) {
                return;
            }

            source = null;
            phase = "idle";
            offset = 0;
            if (ended) {
                ended.invokeMethodAsync("OnSpeechEnded");
            }
        };
        next.start(0, startOffset);
        startedAt = audio.currentTime - startOffset;
        offset = startOffset;
        source = next;
        phase = "playing";
    }

    return {
        setEnded: function (dotNetRef) {
            ended = dotNetRef;
        },
        stop: function () {
            releaseSession();
            stopSource();
            buffer = null;
            text = "";
            offset = 0;
            phase = "idle";
        },
        toggle: async function (nextText) {
            holdSession();
            const audio = ensureContext();
            if (audio.state !== "running") {
                await audio.resume();
            }

            try {
                if (buffer && text === nextText && phase === "playing") {
                    offset = Math.max(0, audio.currentTime - startedAt);
                    stopSource();
                    phase = "paused";
                    releaseSession();
                    return "paused";
                }

                if (buffer && text === nextText && phase === "paused") {
                    startAt(offset);
                    return phase === "playing" ? "playing" : "idle";
                }

                if (buffer && text === nextText && phase === "idle") {
                    startAt(0);
                    return phase === "playing" ? "playing" : "idle";
                }

                const response = await fetch("/api/speech", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ text: nextText })
                });
                if (!response.ok) {
                    throw new Error("speech " + response.status);
                }

                const data = await response.arrayBuffer();
                if (audio.state !== "running") {
                    await audio.resume();
                }

                buffer = await audio.decodeAudioData(data.slice(0));
                text = nextText;
                offset = 0;
                startAt(0);
                return phase === "playing" ? "playing" : "idle";
            } catch (error) {
                releaseSession();
                throw error;
            }
        }
    };
})();
