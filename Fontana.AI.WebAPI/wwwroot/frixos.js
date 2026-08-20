/**
 * Frixos — Fontana AI Chat Widget
 *
 * Lägg till på valfri hemsida med en enda rad:
 *   <script src="https://din-server.se/frixos.js"></script>
 *
 * Valfria inställningar via data-attribut:
 *   <script src="..." data-name="Frixos" data-color="#072852" data-accent="#c8a96e"></script>
 */
(function () {
    'use strict';

    // Härledd API-bas från script-taggens src — fungerar oavsett vilken domän widgeten bäddas in på
    const _script = document.currentScript;
    const API_BASE = _script ? new URL(_script.src).origin : window.location.origin;

    // Konfiguration — kan åsidosättas via data-attribut på script-taggen
    const PRIMARY = _script?.dataset.color  || '#072852';
    const ACCENT  = _script?.dataset.accent || '#c8a96e';
    const BOT_NAME = _script?.dataset.name  || 'Frixos';
    const WELCOME  = _script?.dataset.welcome
        || 'Hej och välkommen till Fontana! 👋 Jag är Frixos och hjälper dig med frågor om våra produkter. Vad kan jag hjälpa dig med?';

    // Förhindra dubbel-initiering
    if (document.getElementById('frixos-bubble')) return;

    // ─────────────────────────────────────────
    // CSS — injiceras i <head>
    // ─────────────────────────────────────────
    const styleEl = document.createElement('style');
    styleEl.textContent = `
    #frixos-bubble {
        position: fixed; bottom: 28px; right: 28px;
        width: 60px; height: 60px;
        background: ${PRIMARY};
        border-radius: 50%;
        display: flex; align-items: center; justify-content: center;
        cursor: pointer;
        box-shadow: 0 4px 20px rgba(7,40,82,0.4);
        z-index: 2147483640;
        transition: transform 0.2s, box-shadow 0.2s;
    }
    #frixos-bubble:hover { transform: scale(1.08); box-shadow: 0 6px 28px rgba(7,40,82,0.5); }
    #frixos-bubble svg { width: 28px; height: 28px; fill: white; pointer-events: none; }
    #frixos-bubble .frixos-badge {
        position: absolute; top: -4px; right: -4px;
        background: ${ACCENT}; color: ${PRIMARY};
        font-size: 10px; font-weight: bold;
        width: 20px; height: 20px; border-radius: 50%;
        display: flex; align-items: center; justify-content: center;
        font-family: sans-serif;
    }
    #frixos-window {
        position: fixed; bottom: 100px; right: 28px;
        width: 380px; height: 540px;
        background: white; border-radius: 18px;
        box-shadow: 0 8px 40px rgba(0,0,0,0.18);
        display: none; flex-direction: column;
        z-index: 2147483639;
        overflow: hidden;
        animation: frixos-up 0.25s ease;
        font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
        font-size: 14px; line-height: 1.5;
    }
    #frixos-window.frixos-open { display: flex; }
    @keyframes frixos-up {
        from { opacity: 0; transform: translateY(20px); }
        to   { opacity: 1; transform: translateY(0); }
    }
    .frixos-header {
        background: ${PRIMARY}; padding: 18px 20px;
        display: flex; align-items: center; gap: 12px; flex-shrink: 0;
    }
    .frixos-avatar {
        width: 40px; height: 40px; background: ${ACCENT};
        border-radius: 50%; display: flex; align-items: center;
        justify-content: center; font-size: 20px; flex-shrink: 0;
    }
    .frixos-header-text { flex: 1; min-width: 0; }
    .frixos-header-text h4 { color: white; font-size: 15px; margin: 0 0 2px; font-weight: 600; }
    .frixos-header-text span { color: rgba(255,255,255,0.6); font-size: 12px; }
    .frixos-dot {
        width: 8px; height: 8px; background: #4caf50;
        border-radius: 50%; margin-right: 5px; display: inline-block;
    }
    .frixos-btn-clear, .frixos-btn-close {
        color: rgba(255,255,255,0.6); cursor: pointer;
        background: none; border: none; font-family: inherit;
        padding: 4px; line-height: 1; transition: color 0.15s;
    }
    .frixos-btn-clear { font-size: 13px; letter-spacing: 0.5px; }
    .frixos-btn-close { font-size: 20px; }
    .frixos-btn-clear:hover, .frixos-btn-close:hover { color: white; }
    .frixos-msgs {
        flex: 1; padding: 20px; overflow-y: auto;
        display: flex; flex-direction: column; gap: 12px;
        background: #faf8f5;
    }
    .frixos-msgs::-webkit-scrollbar { width: 4px; }
    .frixos-msgs::-webkit-scrollbar-thumb { background: #ddd; border-radius: 2px; }
    .frixos-msg {
        max-width: 82%; padding: 10px 14px; border-radius: 16px;
    }
    .frixos-msg.frixos-bot {
        align-self: flex-start; background: white; color: #2c2c2c;
        border-bottom-left-radius: 4px;
        box-shadow: 0 1px 4px rgba(0,0,0,0.06);
    }
    .frixos-msg.frixos-user {
        align-self: flex-end; background: ${PRIMARY}; color: white;
        border-bottom-right-radius: 4px;
    }
    .frixos-msg a { color: ${PRIMARY}; word-break: break-word; }
    .frixos-msg.frixos-user a { color: white; }
    .frixos-msg.frixos-typing span {
        display: inline-block; width: 7px; height: 7px;
        background: #aaa; border-radius: 50%;
        animation: frixos-bounce 1.2s infinite; margin: 0 2px;
    }
    .frixos-msg.frixos-typing span:nth-child(2) { animation-delay: 0.2s; }
    .frixos-msg.frixos-typing span:nth-child(3) { animation-delay: 0.4s; }
    @keyframes frixos-bounce {
        0%,60%,100% { transform: translateY(0); }
        30% { transform: translateY(-6px); }
    }
    .frixos-time {
        font-size: 10px; color: #bbb;
        font-family: sans-serif; margin-top: 2px; padding: 0 14px;
    }
    .frixos-time.frixos-r { text-align: right; }
    .frixos-feedback { display: flex; gap: 4px; padding: 0 14px; margin-top: 2px; }
    .frixos-fb {
        background: none; border: 1px solid #e8e2d9; border-radius: 6px;
        cursor: pointer; font-size: 13px; padding: 2px 7px;
        color: #bbb; transition: all 0.15s;
    }
    .frixos-fb:hover { border-color: #aaa; color: #666; }
    .frixos-fb.frixos-up   { border-color: #4caf50; color: #4caf50; background: #f1faf1; }
    .frixos-fb.frixos-dn   { border-color: #e57373; color: #e57373; background: #fdf3f3; }
    .frixos-input-row {
        padding: 14px 16px; background: white;
        border-top: 1px solid #ebe8e2;
        display: flex; gap: 10px; align-items: center; flex-shrink: 0;
    }
    .frixos-input-row input {
        flex: 1; border: 1px solid #e0dbd2; border-radius: 22px;
        padding: 10px 16px; font-size: 14px; outline: none;
        background: #faf8f5; font-family: inherit;
        transition: border-color 0.2s;
    }
    .frixos-input-row input:focus { border-color: ${PRIMARY}; }
    .frixos-send {
        width: 42px; height: 42px; background: ${PRIMARY};
        border: none; border-radius: 50%; cursor: pointer;
        display: flex; align-items: center; justify-content: center;
        flex-shrink: 0; transition: background 0.2s;
    }
    .frixos-send:hover { background: #0a3d6b; }
    .frixos-send svg { width: 18px; height: 18px; fill: white; pointer-events: none; }
    .frixos-send:disabled { background: #ccc; cursor: not-allowed; }
    .frixos-suggestions {
        display: flex; flex-wrap: wrap; gap: 8px;
        padding: 0 20px 4px; background: #faf8f5;
    }
    .frixos-suggestion {
        background: white; border: 1px solid #e0dbd2;
        border-radius: 16px; padding: 6px 14px;
        font-size: 13px; color: ${PRIMARY}; cursor: pointer;
        font-family: inherit; transition: all 0.15s;
        white-space: nowrap;
    }
    .frixos-suggestion:hover { background: ${PRIMARY}; color: white; border-color: ${PRIMARY}; }
    .frixos-footer {
        text-align: center; padding: 8px; font-size: 10px;
        color: #bbb; background: white; font-family: sans-serif; flex-shrink: 0;
    }
    @media (max-width: 480px) {
        #frixos-bubble { bottom: 16px; right: 16px; }
        #frixos-window {
            bottom: 0; right: 0; left: 0;
            width: 100%; height: 90dvh; max-height: 90dvh;
            border-radius: 18px 18px 0 0;
        }
        .frixos-input-row input { font-size: 16px; }
    }
    `;
    document.head.appendChild(styleEl);

    // ─────────────────────────────────────────
    // HTML — injiceras i <body>
    // ─────────────────────────────────────────
    const wrap = document.createElement('div');
    wrap.innerHTML = `
    <div id="frixos-bubble" role="button" aria-label="Öppna chatt" tabindex="0">
        <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm-2 12H6v-2h12v2zm0-3H6V9h12v2zm0-3H6V6h12v2z"/>
        </svg>
        <div class="frixos-badge" aria-hidden="true">AI</div>
    </div>
    <div id="frixos-window" role="dialog" aria-label="${BOT_NAME} chatt" aria-modal="true">
        <div class="frixos-header">
            <div class="frixos-avatar" aria-hidden="true">🫒</div>
            <div class="frixos-header-text">
                <h4>${BOT_NAME}</h4>
                <span><span class="frixos-dot" aria-hidden="true"></span>Online nu</span>
            </div>
            <button class="frixos-btn-clear" id="frixos-btn-clear" title="Ny chatt">Ny chatt</button>
            <button class="frixos-btn-close" id="frixos-btn-close" title="Stäng" aria-label="Stäng chatt">✕</button>
        </div>
        <div class="frixos-msgs" id="frixos-msgs" aria-live="polite"></div>
        <div class="frixos-suggestions" id="frixos-suggestions"></div>
        <div class="frixos-input-row">
            <input type="text" id="frixos-input"
                   placeholder="Fråga om ingredienser, allergener, produkter…"
                   aria-label="Skriv din fråga">
            <button class="frixos-send" id="frixos-send" aria-label="Skicka">
                <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2 21l21-9L2 3v7l15 2-15 2z"/></svg>
            </button>
        </div>
        <div class="frixos-footer" aria-hidden="true">Frixos · Fontana AI · GPT-4o</div>
    </div>
    `;
    document.body.appendChild(wrap);

    // ─────────────────────────────────────────
    // JavaScript — logik
    // ─────────────────────────────────────────
    const STORAGE_KEY       = 'frixos_chat_v1';
    const MAX_HISTORY_MSGS  = 10;
    const TIMEOUT_MS        = 30000;

    let chatHistory    = [];   // skickas till API
    let savedMessages  = [];   // sparas i localStorage för visning

    // Elementer
    const SUGGESTIONS = [
        'Vilka produkter har ni?',
        'Innehåller era produkter gluten?',
        'Var tillverkas era produkter?',
        'Hur kontaktar jag er?',
    ];

    const bubble      = document.getElementById('frixos-bubble');
    const win         = document.getElementById('frixos-window');
    const msgs        = document.getElementById('frixos-msgs');
    const suggestionsEl = document.getElementById('frixos-suggestions');
    const input       = document.getElementById('frixos-input');
    const sendBtn     = document.getElementById('frixos-send');
    const closeBtn    = document.getElementById('frixos-btn-close');
    const clearBtn    = document.getElementById('frixos-btn-clear');

    // ── localStorage ──
    function saveToStorage() {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify({ chatHistory, savedMessages }));
        } catch {}
    }

    function loadFromStorage() {
        try {
            const data = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
            if (data?.savedMessages?.length) {
                chatHistory   = data.chatHistory   ?? [];
                savedMessages = data.savedMessages;
                return true;
            }
        } catch {}
        return false;
    }

    // ── Rendera ──
    function renderAll() {
        msgs.innerHTML = '';
        savedMessages.forEach(m => _renderMsg(m.text, m.role, m.time, m.logId || 0));
        msgs.scrollTop = msgs.scrollHeight;
    }

    function _renderMsg(text, role, time, logId) {
        // Meddelandebubbla
        const el = document.createElement('div');
        el.className = `frixos-msg frixos-${role}`;
        if (role === 'typing') {
            el.innerHTML = '<span></span><span></span><span></span>';
            el.id = 'frixos-typing';
        } else {
            el.innerHTML = _linkify(_esc(text)).replace(/\n/g, '<br>');
        }
        msgs.appendChild(el);

        if (role !== 'typing') {
            // Tidsstämpel
            const t = document.createElement('div');
            t.className = 'frixos-time' + (role === 'user' ? ' frixos-r' : '');
            t.textContent = time;
            msgs.appendChild(t);

            // Feedback-knappar (enbart för bot-svar med logId)
            if (role === 'bot' && logId > 0) {
                const fb = document.createElement('div');
                fb.className = 'frixos-feedback';
                fb.innerHTML = `
                    <button class="frixos-fb" data-logid="${logId}" data-pos="true"  title="Bra svar">👍</button>
                    <button class="frixos-fb" data-logid="${logId}" data-pos="false" title="Dåligt svar">👎</button>`;
                msgs.appendChild(fb);
            }
        }

        msgs.scrollTop = msgs.scrollHeight;
        return el;
    }

    // ── Lägg till meddelande + spara ──
    function pushMsg(text, role, logId = 0) {
        const time = _now();
        savedMessages.push({ text, role, time, logId });
        saveToStorage();
        _renderMsg(text, role, time, logId);
    }

    // ── Typing-indikator ──
    function showTyping() { _renderMsg('', 'typing', '', 0); }
    function hideTyping() { document.getElementById('frixos-typing')?.remove(); }

    // ── Feedback (event delegation) ──
    msgs.addEventListener('click', function (e) {
        const btn = e.target.closest('.frixos-fb');
        if (!btn) return;
        const logId    = parseInt(btn.dataset.logid || '0', 10);
        const positive = btn.dataset.pos === 'true';
        if (!logId) return;

        fetch(`${API_BASE}/api/conversation/${logId}/feedback`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ positive })
        }).catch(() => {});

        btn.closest('.frixos-feedback')
           .querySelectorAll('.frixos-fb')
           .forEach(b => b.classList.remove('frixos-up', 'frixos-dn'));
        btn.classList.add(positive ? 'frixos-up' : 'frixos-dn');
    });

    // ── Öppna / stäng ──
    function toggleChat() {
        const isOpen = win.classList.toggle('frixos-open');
        if (isOpen) input.focus();
        bubble.setAttribute('aria-expanded', isOpen);
    }

    // ── Snabbfrågor ──
    function showSuggestions() {
        suggestionsEl.innerHTML = '';
        SUGGESTIONS.forEach(q => {
            const btn = document.createElement('button');
            btn.className = 'frixos-suggestion';
            btn.textContent = q;
            btn.addEventListener('click', () => {
                hideSuggestions();
                input.value = q;
                sendMessage();
            });
            suggestionsEl.appendChild(btn);
        });
    }

    function hideSuggestions() {
        suggestionsEl.innerHTML = '';
    }

    // ── Ny chatt ──
    function clearChat() {
        chatHistory   = [];
        savedMessages = [];
        localStorage.removeItem(STORAGE_KEY);
        msgs.innerHTML = '';
        pushMsg(WELCOME, 'bot');
        showSuggestions();
    }

    // ── Skicka meddelande ──
    async function sendMessage() {
        const text = input.value.trim();
        if (!text) return;

        input.value  = '';
        sendBtn.disabled = true;
        hideSuggestions();
        pushMsg(text, 'user');
        chatHistory.push({ role: 'user', content: text });
        showTyping();

        const controller = new AbortController();
        const timer      = setTimeout(() => controller.abort(), TIMEOUT_MS);

        try {
            const res = await fetch(`${API_BASE}/api/chat/ask`, {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({
                    message: text,
                    history: chatHistory.slice(-MAX_HISTORY_MSGS)
                }),
                signal: controller.signal
            });

            clearTimeout(timer);
            hideTyping();

            if (res.status === 429) {
                pushMsg('Du har skickat för många meddelanden. Vänta en stund och försök igen.', 'bot');
                return;
            }
            if (!res.ok) throw new Error(res.status);

            const data = await res.json();
            pushMsg(data.answer, 'bot', data.logId ?? 0);
            chatHistory.push({ role: 'assistant', content: data.answer });

        } catch (err) {
            clearTimeout(timer);
            hideTyping();
            pushMsg(
                err.name === 'AbortError'
                    ? 'Förfrågan tog för lång tid. Kontrollera din uppkoppling och försök igen.'
                    : 'Något gick fel. Kontrollera att servern körs och försök igen.',
                'bot'
            );
        } finally {
            sendBtn.disabled = false;
            input.focus();
        }
    }

    // ── Hjälpfunktioner ──
    function _now() {
        return new Date().toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit' });
    }
    function _esc(s) {
        return String(s ?? '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
    // Gör om enkel Markdown i redan HTML-escapad text till riktig HTML.
    // Körs EFTER _esc(), så inmatningen innehåller inga råa < > " tecken — säkert att bygga taggar av.
    // GPT skriver ibland Markdown trots instruktioner om ren text, så vi hanterar det som faktiskt
    // dyker upp istället för att lita blint på att modellen alltid lyder: Markdown-länkar ([text](url)),
    // bara URL:er, och **fetstil**. Länkvarianterna provas först per position så en URL inuti [](...)
    // aldrig dubbel-länkifieras av den råa URL-grenen.
    function _linkify(escapedText) {
        return escapedText.replace(
            /\[([^\[\]]+)\]\((https?:\/\/[^\s<)]+)\)|(https?:\/\/[^\s<]+[^\s<.,;:!?)"'])|\*\*([^*]+)\*\*/g,
            (match, label, mdUrl, bareUrl, boldText) => {
                if (mdUrl) return `<a href="${mdUrl}" target="_blank" rel="noopener noreferrer">${label}</a>`;
                if (bareUrl) return `<a href="${bareUrl}" target="_blank" rel="noopener noreferrer">${bareUrl}</a>`;
                return `<strong>${boldText}</strong>`;
            }
        );
    }

    // ── Event listeners ──
    bubble.addEventListener('click',   toggleChat);
    bubble.addEventListener('keydown', e => (e.key === 'Enter' || e.key === ' ') && toggleChat());
    closeBtn.addEventListener('click', toggleChat);
    clearBtn.addEventListener('click', clearChat);
    sendBtn.addEventListener('click',  sendMessage);
    input.addEventListener('keydown',  e => {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
    });
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && win.classList.contains('frixos-open')) toggleChat();
    });

    // ── Initiering ──
    if (loadFromStorage()) {
        renderAll();
    } else {
        pushMsg(WELCOME, 'bot');
        showSuggestions();
    }

})();
