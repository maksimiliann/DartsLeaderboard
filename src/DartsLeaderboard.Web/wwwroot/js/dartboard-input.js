// Набор подхода считается здесь, чтобы клик по мишени не ждал круг до сервера.
// Правила перебора и закрытия совпадают с DartboardScoring.EvaluateX01Visit.
window.dartboardInput = (function () {
    const sessions = new WeakMap();

    function attach(root, dotNet, options) {
        let session = sessions.get(root);
        if (!session) {
            session = createSession(root, dotNet);
            root.addEventListener("click", session.onClick, true);
            sessions.set(root, session);
        }

        session.configure(options);
        session.render();
    }

    function createSession(root, dotNet) {
        let visit = [];
        let remaining = null;
        let sectorInput = true;
        let disabled = false;
        let selected = null;
        let lastHitId = null;
        let busy = false;
        let player = null;

        function configure(options) {
            const nextPlayer = options.player ?? null;
            const nextRemaining = options.remaining === null || options.remaining === undefined
                ? null
                : Number(options.remaining);
            if (nextPlayer !== player || nextRemaining !== remaining) {
                visit = [];
                selected = null;
                lastHitId = null;
            }

            player = nextPlayer;
            remaining = nextRemaining;
            disabled = Boolean(options.disabled);
            sectorInput = Boolean(options.sectorInput);
            if (!sectorInput) {
                selected = null;
            }
        }

        function onClick(event) {
            const target = event.target;
            if (!(target instanceof Element) || !root.contains(target)) {
                return;
            }

            const undo = target.closest("[data-testid=undo-dart]");
            const ring = target.closest("[data-testid=ring-single], [data-testid=ring-double], [data-testid=ring-triple], [data-testid=ring-miss]");
            const sector = target.closest("[data-sector]");
            const dart = target.closest("[data-dart]");
            const handled = undo || ring || (sector && sectorInput) || dart;
            if (!handled) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            if (busy || disabled) {
                return;
            }

            if (undo) {
                undoDart();
                return;
            }

            if (ring) {
                chooseRing(ring);
                return;
            }

            if (sector && sectorInput) {
                selectSector(Number(sector.dataset.sector));
                return;
            }

            if (dart) {
                hitElement(dart);
            }
        }

        function selectSector(sector) {
            if (!sectorInput || visit.length >= 3 || Number.isNaN(sector)) {
                return;
            }

            selected = sector;
            render();
        }

        function chooseRing(button) {
            if (selected === null) {
                return;
            }

            if (button.matches("[data-testid=ring-miss]")) {
                addDart({ points: 0, isDouble: false, id: "miss-" + selected });
                return;
            }

            let multiplier = 1;
            let prefix = "x1o";
            let isDouble = false;
            if (button.matches("[data-testid=ring-double]")) {
                multiplier = 2;
                prefix = "x2";
                isDouble = true;
            } else if (button.matches("[data-testid=ring-triple]")) {
                multiplier = 3;
                prefix = "x3";
            }

            addDart({
                points: selected * multiplier,
                isDouble: isDouble,
                id: prefix + "-" + selected
            });
        }

        function hitElement(element) {
            addDart({
                points: Number(element.dataset.points),
                isDouble: String(element.dataset.double).toLowerCase() === "true",
                id: element.dataset.dart
            });
        }

        function addDart(dart) {
            if (visit.length >= 3 || Number.isNaN(dart.points)) {
                return;
            }

            visit.push(dart);
            selected = null;
            lastHitId = dart.id;
            if (remaining !== null) {
                const result = evaluate(remaining, visit);
                if (result === "bust") {
                    visit = [];
                    lastHitId = null;
                    render();
                    finish([]);
                    return;
                }

                if (result === "checkout") {
                    finish(visit.slice());
                    return;
                }
            }

            if (visit.length === 3) {
                finish(visit.slice());
                return;
            }

            render();
        }

        function undoDart() {
            if (visit.length === 0) {
                return;
            }

            visit.pop();
            selected = null;
            lastHitId = visit.length === 0 ? null : visit[visit.length - 1].id;
            render();
        }

        async function finish(darts) {
            if (busy) {
                return;
            }

            busy = true;
            render();
            try {
                await dotNet.invokeMethodAsync("SubmitVisit", darts.map(function (dart) {
                    return { points: dart.points, isDouble: dart.isDouble };
                }));
                visit = [];
                lastHitId = null;
                selected = null;
            } catch (ignored) {
            } finally {
                busy = false;
                render();
            }
        }

        function render() {
            const sum = visit.reduce(function (total, dart) { return total + dart.points; }, 0);
            setText("[data-testid=visit-sum]", "Сумма " + sum);
            if (remaining !== null) {
                setText("[data-testid=visit-remaining]", "Остаток " + (remaining - sum));
            }

            const panel = root.querySelector("[data-testid=ring-choice]");
            if (panel) {
                panel.classList.toggle("is-open", selected !== null && sectorInput);
            }

            if (selected !== null) {
                setText("[data-testid=selected-sector]", "Номер " + selected);
            }

            root.querySelectorAll("[data-sector].is-selected").forEach(function (element) {
                element.classList.remove("is-selected");
            });
            if (selected !== null && sectorInput) {
                const sectorElement = root.querySelector('[data-sector="' + selected + '"]');
                if (sectorElement) {
                    sectorElement.classList.add("is-selected");
                }
            }

            root.querySelectorAll(".dartboard-slice-hit").forEach(function (element) {
                element.classList.remove("dartboard-slice-hit");
            });
            if (lastHitId) {
                const hit = root.querySelector('[data-dart="' + lastHitId + '"]');
                if (hit) {
                    hit.classList.add("dartboard-slice-hit");
                }
            }

            const undo = root.querySelector("[data-testid=undo-dart]");
            if (undo) {
                undo.disabled = disabled || busy || visit.length === 0;
            }

            renderMarkers();
        }

        function renderMarkers() {
            const counts = new Map();
            visit.forEach(function (dart) {
                counts.set(dart.id, (counts.get(dart.id) || 0) + 1);
            });

            const seen = new Map();
            for (let index = 0; index < visit.length; index++) {
                const dart = visit[index];
                const total = counts.get(dart.id);
                const sameIndex = seen.get(dart.id) || 0;
                seen.set(dart.id, sameIndex + 1);
                const source = root.querySelector('[data-dart="' + dart.id + '"]');
                const slot = root.querySelector('[data-hit-slot="' + (index + 1) + '"]');
                if (!source || !slot) {
                    continue;
                }

                const position = markerPosition(source, sameIndex, total);
                const x = format(position.x);
                const y = format(position.y);
                slot.setAttribute("visibility", "visible");
                const circle = slot.querySelector("circle");
                const text = slot.querySelector("text");
                if (circle) {
                    circle.setAttribute("cx", x);
                    circle.setAttribute("cy", y);
                }

                if (text) {
                    text.setAttribute("x", x);
                    text.setAttribute("y", y);
                }
            }

            for (let number = visit.length + 1; number <= 3; number++) {
                const slot = root.querySelector('[data-hit-slot="' + number + '"]');
                if (slot) {
                    slot.setAttribute("visibility", "hidden");
                }
            }
        }

        function setText(selector, value) {
            const element = root.querySelector(selector);
            if (!element) {
                return;
            }

            const text = element.firstChild;
            if (text && text.nodeType === Node.TEXT_NODE && element.childNodes.length === 1) {
                text.nodeValue = value;
                return;
            }

            element.textContent = value;
        }

        return { configure: configure, render: render, onClick: onClick };
    }

    function evaluate(remaining, darts) {
        let left = remaining;
        for (let index = 0; index < darts.length; index++) {
            const dart = darts[index];
            if (dart.points > left) {
                return "bust";
            }

            left -= dart.points;
            if (left === 1) {
                return "bust";
            }

            if (left === 0) {
                return dart.isDouble ? "checkout" : "bust";
            }
        }

        return "continue";
    }

    function markerPosition(element, sameIndex, sameCount) {
        if (element.dataset.markerKind === "bull50") {
            const radius = sameCount <= 1 ? 0 : 10;
            return polar(radius, -90 + sameIndex * 120);
        }

        const spread = sameCount <= 1 ? 0 : (sameIndex - (sameCount - 1) / 2) * 7;
        return polar(Number(element.dataset.markerRadius), Number(element.dataset.markerAngle) + spread);
    }

    function polar(radius, degrees) {
        const radians = degrees * Math.PI / 180;
        return {
            x: 320 + radius * Math.cos(radians),
            y: 320 + radius * Math.sin(radians)
        };
    }

    function format(value) {
        return (Math.round(value * 100) / 100).toString();
    }

    return { attach: attach };
})();
