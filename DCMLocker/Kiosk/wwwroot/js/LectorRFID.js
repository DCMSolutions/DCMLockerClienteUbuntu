let handler = null;
let keepFocusTimer = null;

export function attach(dotnetRef, { acceptTabAsEnter = true } = {}) {
    detach();

    let buf = "";

    handler = (e) => {
        if (e.ctrlKey || e.altKey || e.metaKey) return;

        // Fin de lectura: Enter (o Tab si está habilitado)
        if (e.key === "Enter" || (acceptTabAsEnter && e.key === "Tab")) {
            const text = buf;
            buf = "";
            if (text) {
                dotnetRef.invokeMethodAsync("OnScanEnter", text);
            }
            e.preventDefault(); e.stopPropagation();
            return;
        }

        // Limpiar con Escape
        if (e.key === "Escape") {
            buf = "";
            e.preventDefault(); e.stopPropagation();
            return;
        }

        // Acumular sólo caracteres imprimibles
        if (e.key.length === 1) {
            buf += e.key;
            e.preventDefault(); e.stopPropagation();
        }
    };

    // Captura a nivel ventana para que funcione en kiosk
    window.addEventListener("keydown", handler, true);

    // “keeper” de foco en kiosk (por si el SO/Chromium pierde visibilidad)
    keepFocusTimer = setInterval(() => {
        if (document.visibilityState !== "visible") window.focus();
    }, 1000);
}

export function detach() {
    if (handler) {
        window.removeEventListener("keydown", handler, true);
        handler = null;
    }
    if (keepFocusTimer) {
        clearInterval(keepFocusTimer);
        keepFocusTimer = null;
    }
}
