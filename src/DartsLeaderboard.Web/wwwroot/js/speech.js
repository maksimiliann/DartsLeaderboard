window.dartsSpeech = (function () {
    const context = new AudioContext();
    let source = null;
    let buffer = null;
    let text = "";
    let startedAt = 0;
    let offset = 0;
    let phase = "idle";
    let ended = null;

    document.addEventListener("pointerdown", function () {
        if (context.state !== "running") {
            context.resume();
        }
    });

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
        stopSource();
        const next = context.createBufferSource();
        next.buffer = buffer;
        next.connect(context.destination);
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
        startedAt = context.currentTime - startOffset;
        offset = startOffset;
        source = next;
        phase = "playing";
    }

    return {
        setEnded: function (dotNetRef) {
            ended = dotNetRef;
        },
        stop: function () {
            stopSource();
            buffer = null;
            text = "";
            offset = 0;
            phase = "idle";
        },
        toggle: async function (nextText) {
            if (context.state !== "running") {
                await context.resume();
            }

            if (buffer && text === nextText && phase === "playing") {
                offset = Math.max(0, context.currentTime - startedAt);
                stopSource();
                phase = "paused";
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
            buffer = await context.decodeAudioData(data.slice(0));
            text = nextText;
            offset = 0;
            startAt(0);
            return phase === "playing" ? "playing" : "idle";
        }
    };
})();
