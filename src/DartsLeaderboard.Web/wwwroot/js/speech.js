window.dartsSpeech = (function () {
    const context = new AudioContext();
    let current = null;

    document.addEventListener("pointerdown", function () {
        if (context.state !== "running") {
            context.resume();
        }
    });

    return {
        playText: async function (text) {
            const response = await fetch("/api/speech", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ text: text })
            });
            if (!response.ok) {
                throw new Error("speech " + response.status);
            }

            const data = await response.arrayBuffer();
            if (context.state !== "running") {
                await context.resume();
            }

            const buffer = await context.decodeAudioData(data.slice(0));
            if (current) {
                try {
                    current.stop();
                } catch (ignored) {
                }
            }

            const source = context.createBufferSource();
            source.buffer = buffer;
            source.connect(context.destination);
            source.start();
            current = source;
        }
    };
})();
