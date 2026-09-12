/**
 * Chatbot AI Client Script - Chuyên Viên Tư Vấn Thuê Trọ Siêu Thông Minh
 * QuanLyPhongTro - Đồ Án Tốt Nghiệp CNTT
 * Hỗ trợ Multi-turn Context, Suggestions Chips, Compressed Room Cards, API Diagnostics
 * CƠ CHẾ LƯU TRỮ VÀ KHÔI PHỤC CUỘC TRÒ CHUYỆN (PERSISTENCE) QUA LOCALSTORAGE:
 * - Tự động lưu và khôi phục khi chuyển tab, chuyển trang, F5 lại trang
 * - Lưu trạng thái mở/đóng cửa sổ chat
 * - Lưu nội dung đang gõ dở dở trong ô input
 */

(function () {
    const launcher = document.getElementById("aiChatLauncher");
    const windowEl = document.getElementById("aiChatWindow");
    const closeBtn = document.getElementById("aiChatCloseBtn");
    const clearBtn = document.getElementById("aiChatClearBtn");
    const checkApiBtn = document.getElementById("aiChatCheckApiBtn");
    const bodyEl = document.getElementById("aiChatBody");
    const inputEl = document.getElementById("aiChatInput");
    const sendBtn = document.getElementById("aiChatSendBtn");
    const confirmModal = document.getElementById("aiChatConfirmModal");
    const cancelClearBtn = document.getElementById("aiChatCancelClearBtn");
    const confirmClearBtn = document.getElementById("aiChatConfirmClearBtn");

    if (!launcher || !windowEl || !inputEl) return;

    const STORAGE_MSGS_KEY = "qlpt_ai_chat_messages_v1";
    const STORAGE_HISTORY_KEY = "qlpt_ai_conversation_history_v1";
    const STORAGE_STATE_KEY = "qlpt_ai_chat_open_state_v1";
    const STORAGE_DRAFT_KEY = "qlpt_ai_chat_draft_v1";

    let conversationHistory = []; // Lưu trữ ngữ cảnh đa lượt cho LLM: [{ role: "user"|"assistant", content: "..." }]
    let savedMessages = []; // Lưu trữ toàn bộ tin nhắn UI để restore

    // 1. Toggle Cửa sổ Chat & Lưu trạng thái
    function toggleChat(open) {
        const isOpen = typeof open === "boolean" ? open : !windowEl.classList.contains("active");
        if (isOpen) {
            windowEl.classList.add("active");
            document.body.classList.add("ai-chat-open");
            try { localStorage.setItem(STORAGE_STATE_KEY, "open"); } catch (e) { }
            setTimeout(() => {
                inputEl.focus();
                adjustTextareaHeight();
            }, 250);
        } else {
            windowEl.classList.remove("active");
            document.body.classList.remove("ai-chat-open");
            try { localStorage.setItem(STORAGE_STATE_KEY, "closed"); } catch (e) { }
        }
    }

    launcher.addEventListener("click", () => toggleChat());
    closeBtn.addEventListener("click", () => toggleChat(false));

    // 2. Cuộn xuống cuối
    function scrollToBottom() {
        bodyEl.scrollTop = bodyEl.scrollHeight;
    }

    // 3. Format markdown cơ bản
    function formatMessageText(text) {
        if (!text) return "";
        let escaped = text
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");

        // Bold **text**
        escaped = escaped.replace(/\*\*(.*?)\*\*/g, "<strong>$1</strong>");
        // Italic *text*
        escaped = escaped.replace(/\*(.*?)\*/g, "<em>$1</em>");
        // Line breaks
        escaped = escaped.replace(/\n/g, "<br />");
        return escaped;
    }

    // 4. Lưu danh sách tin nhắn và lịch sử vào LocalStorage
    function saveStorage() {
        try {
            localStorage.setItem(STORAGE_MSGS_KEY, JSON.stringify(savedMessages));
            localStorage.setItem(STORAGE_HISTORY_KEY, JSON.stringify(conversationHistory));
        } catch (e) {
            console.warn("Không thể lưu lịch sử chat vào localStorage:", e);
        }
    }

    // 5. Render tin nhắn User
    function appendUserMessage(text, timeStr, skipSave) {
        const time = timeStr || new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const html = `
            <div class="ai-msg user">
                <div class="ai-msg-bubble">
                    ${formatMessageText(text)}
                    <div class="ai-msg-time" style="color: rgba(255,255,255,0.7);">${time}</div>
                </div>
            </div>
        `;
        bodyEl.insertAdjacentHTML("beforeend", html);
        scrollToBottom();

        if (!skipSave) {
            savedMessages.push({
                type: "user",
                text: text,
                time: time
            });
            saveStorage();
        }
    }

    // 6. Render tin nhắn Bot (kèm Thẻ Phòng Trọ, Cảnh Báo API & Nút Gợi Ý nếu có)
    function appendBotMessage(text, rooms, suggestions, provider, model, apiWarning, timeStr, skipSave) {
        const time = timeStr || new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        // Warning banner nếu có
        let warningHtml = "";
        if (apiWarning) {
            warningHtml = `
                <div class="ai-api-warning" style="background: #fff8e1; color: #856404; padding: 8px 10px; border-radius: 8px; font-size: 11.5px; margin-bottom: 8px; border: 1px solid #ffeeba; display: flex; align-items: flex-start; gap: 6px; line-height: 1.4;">
                    <span style="font-size: 14px;">⚠️</span>
                    <div>${formatMessageText(apiWarning)}</div>
                </div>
            `;
        }

        // Room Cards
        let roomsHtml = "";
        if (rooms && rooms.length > 0) {
            roomsHtml = `<div class="ai-room-cards">`;
            rooms.forEach(r => {
                const img = r.imageUrl || "/Assets/Images/banner-thuecanho.jpg";
                roomsHtml += `
                    <div class="ai-room-card">
                        <div class="ai-room-img-wrap">
                            <img src="${img}" alt="${r.name}" class="ai-room-img" onerror="this.src='/Assets/Images/banner-thuecanho.jpg'" />
                            ${r.areaName ? `<span class="ai-room-badge">${r.areaName}</span>` : ''}
                        </div>
                        <div class="ai-room-info">
                            <a href="/User/ChiTiet/${r.id}" class="ai-room-title" title="${r.name}">${r.name}</a>
                            <div class="ai-room-price">${r.price}</div>
                            <div class="ai-room-meta">
                                <span>📐 ${r.area}</span>
                                <span>📍 ${r.district || ''}</span>
                            </div>
                            <div class="ai-room-address" title="${r.address}">🏠 ${r.address}</div>
                            <a href="/User/ChiTiet/${r.id}" class="ai-room-btn">Xem chi tiết phòng &rarr;</a>
                        </div>
                    </div>
                `;
            });
            roomsHtml += `</div>`;
        }

        // Suggestions Chips
        let suggestionsHtml = "";
        if (suggestions && suggestions.length > 0) {
            suggestionsHtml = `<div class="ai-chat-quick-questions" style="margin-top: 10px;">`;
            suggestions.forEach(s => {
                suggestionsHtml += `<button class="ai-quick-btn" data-query="${s}">${s}</button>`;
            });
            suggestionsHtml += `</div>`;
        }

        // Badge Provider
        let aiBadge = "";
        if (provider) {
            aiBadge = `<span style="opacity: 0.75; margin-right: 6px;">🤖 ${provider}</span>`;
        }

        const html = `
            <div class="ai-msg bot">
                <div class="ai-msg-avatar">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="4" y="8" width="16" height="12" rx="3"></rect>
                        <circle cx="9" cy="13" r="1.5" fill="currentColor"></circle>
                        <circle cx="15" cy="13" r="1.5" fill="currentColor"></circle>
                    </svg>
                </div>
                <div class="ai-msg-bubble">
                    ${warningHtml}
                    ${formatMessageText(text)}
                    ${roomsHtml}
                    ${suggestionsHtml}
                    <div class="ai-msg-time">${aiBadge}${time}</div>
                </div>
            </div>
        `;
        bodyEl.insertAdjacentHTML("beforeend", html);
        scrollToBottom();

        if (!skipSave) {
            savedMessages.push({
                type: "bot",
                text: text,
                rooms: rooms,
                suggestions: suggestions,
                provider: provider,
                model: model,
                apiWarning: apiWarning,
                time: time
            });
            saveStorage();
        }
    }

    // 7. Hiệu ứng đang gõ (Typing Indicator)
    let typingEl = null;
    function showTyping() {
        if (typingEl) return;
        typingEl = document.createElement("div");
        typingEl.className = "ai-msg bot";
        typingEl.id = "aiTypingIndicator";
        typingEl.innerHTML = `
            <div class="ai-msg-avatar">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <rect x="4" y="8" width="16" height="12" rx="3"></rect>
                    <circle cx="9" cy="13" r="1.5" fill="currentColor"></circle>
                    <circle cx="15" cy="13" r="1.5" fill="currentColor"></circle>
                </svg>
            </div>
            <div class="ai-typing-indicator">
                <span></span><span></span><span></span>
            </div>
        `;
        bodyEl.appendChild(typingEl);
        scrollToBottom();
    }

    function hideTyping() {
        if (typingEl && typingEl.parentNode) {
            typingEl.parentNode.removeChild(typingEl);
        }
        typingEl = null;
    }

    // 7b. Tự động co giãn chiều cao của Textarea kiểu ChatGPT
    function adjustTextareaHeight() {
        if (!inputEl) return;
        inputEl.style.height = "auto";
        const maxHeight = 120; // Chiều cao tối đa ~5 dòng
        const scrollH = inputEl.scrollHeight;

        if (scrollH > maxHeight) {
            inputEl.style.height = maxHeight + "px";
            inputEl.style.overflowY = "auto";
        } else {
            inputEl.style.height = Math.max(24, scrollH) + "px";
            inputEl.style.overflowY = "hidden";
        }

        // Tự động đẩy nội dung hội thoại lên trên khi ô nhập co giãn cao lên
        scrollToBottom();

        // Kích hoạt màu nút gửi khi có nội dung
        if (inputEl.value.trim().length > 0) {
            sendBtn.classList.add("active");
        } else {
            sendBtn.classList.remove("active");
        }
    }

    // 8. Gửi câu hỏi lên Server với Multi-turn History
    async function sendMessage(text) {
        const query = (text || inputEl.value || "").trim();
        if (!query) return;

        inputEl.value = "";
        adjustTextareaHeight();
        inputEl.disabled = true;
        sendBtn.disabled = true;

        // Xóa bản nháp khi đã gửi
        try { localStorage.removeItem(STORAGE_DRAFT_KEY); } catch (e) { }

        appendUserMessage(query);
        showTyping();

        // Ghi nhận lượt của người dùng vào context
        conversationHistory.push({ role: "user", content: query });
        if (conversationHistory.length > 8) {
            conversationHistory = conversationHistory.slice(conversationHistory.length - 8);
        }
        saveStorage();

        try {
            const response = await fetch("/Chatbot/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json; charset=utf-8"
                },
                body: JSON.stringify({
                    question: query,
                    history: conversationHistory
                })
            });

            hideTyping();

            if (!response.ok) {
                throw new Error("Mã lỗi: " + response.status);
            }

            const data = await response.json();
            if (data && data.success) {
                appendBotMessage(data.reply, data.rooms, data.suggestions, data.provider, data.model, data.apiWarning);
                // Ghi nhận phản hồi bot vào context
                conversationHistory.push({ role: "assistant", content: data.reply });
                saveStorage();
            } else {
                appendBotMessage(data.reply || "Xin lỗi, hiện tại hệ thống AI đang bận. Vui lòng thử lại sau!", [], [], null, null, null);
            }
        } catch (err) {
            hideTyping();
            appendBotMessage("⚠️ Không thể kết nối tới máy chủ AI. Bạn hãy thử kiểm tra lại kết nối mạng nhé!", [], [], null, null, null);
        } finally {
            inputEl.disabled = false;
            sendBtn.disabled = false;
            inputEl.focus();
            adjustTextareaHeight();
        }
    }

    // 9. Khôi phục trạng thái và toàn bộ tin nhắn từ LocalStorage khi khởi động
    function restoreChatSession() {
        try {
            // Khôi phục nội dung đang gõ dở
            const savedDraft = localStorage.getItem(STORAGE_DRAFT_KEY);
            if (savedDraft) {
                inputEl.value = savedDraft;
                adjustTextareaHeight();
            }

            // Khôi phục danh sách tin nhắn
            const msgsJson = localStorage.getItem(STORAGE_MSGS_KEY);
            const histJson = localStorage.getItem(STORAGE_HISTORY_KEY);

            if (histJson) {
                conversationHistory = JSON.parse(histJson) || [];
            }

            if (msgsJson) {
                const parsedMsgs = JSON.parse(msgsJson);
                if (Array.isArray(parsedMsgs) && parsedMsgs.length > 0) {
                    savedMessages = parsedMsgs;
                    // Render lại từng tin nhắn
                    parsedMsgs.forEach(m => {
                        if (m.type === "user") {
                            appendUserMessage(m.text, m.time, true);
                        } else if (m.type === "bot") {
                            appendBotMessage(m.text, m.rooms, m.suggestions, m.provider, m.model, m.apiWarning, m.time, true);
                        }
                    });
                }
            }

            // Khôi phục trạng thái mở/đóng cửa sổ
            const chatState = localStorage.getItem(STORAGE_STATE_KEY);
            if (chatState === "open") {
                windowEl.classList.add("active");
                document.body.classList.add("ai-chat-open");
                adjustTextareaHeight();
            }
        } catch (e) {
            console.error("Lỗi khi khôi phục lịch sử chat:", e);
        }
    }

    // 10. Event listeners
    sendBtn.addEventListener("click", () => sendMessage());

    // Enter gửi tin nhắn, Shift + Enter để xuống dòng (chuẩn ChatGPT)
    inputEl.addEventListener("keydown", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // Tự động co giãn và lưu bản nháp khi người dùng đang gõ dở
    inputEl.addEventListener("input", () => {
        adjustTextareaHeight();
        try {
            if (inputEl.value) {
                localStorage.setItem(STORAGE_DRAFT_KEY, inputEl.value);
            } else {
                localStorage.removeItem(STORAGE_DRAFT_KEY);
            }
        } catch (e) { }
    });

    // Nút kiểm tra API Key
    if (checkApiBtn) {
        checkApiBtn.addEventListener("click", () => {
            sendMessage("kiểm tra api");
        });
    }

    // Quick buttons click (cả nút ban đầu và các gợi ý sau từng câu trả lời)
    bodyEl.addEventListener("click", (e) => {
        const btn = e.target.closest(".ai-quick-btn");
        if (btn) {
            const q = btn.getAttribute("data-query");
            if (q) sendMessage(q);
        }
    });

    // 11. Modal xác nhận xóa lịch sử cuộc trò chuyện đẹp mắt (thay thế browser confirm)
    function showClearModal() {
        if (confirmModal) {
            confirmModal.style.setProperty("display", "flex", "important");
            confirmModal.classList.add("active");
        }
    }

    function hideClearModal() {
        if (confirmModal) {
            confirmModal.style.setProperty("display", "none", "important");
            confirmModal.classList.remove("active");
        }
    }

    // Đảm bảo ban đầu ẩn hoàn toàn
    if (confirmModal) {
        confirmModal.style.setProperty("display", "none", "important");
        confirmModal.classList.remove("active");
    }

    function doClearChat() {
        conversationHistory = [];
        savedMessages = [];

        try {
            localStorage.removeItem(STORAGE_MSGS_KEY);
            localStorage.removeItem(STORAGE_HISTORY_KEY);
            localStorage.removeItem(STORAGE_DRAFT_KEY);
        } catch (e) { }

        // Giữ lại lời chào đầu tiên
        const welcomeMsg = bodyEl.querySelector(".ai-msg.bot");
        bodyEl.innerHTML = "";
        if (welcomeMsg) {
            bodyEl.appendChild(welcomeMsg);
        }

        inputEl.value = "";
        adjustTextareaHeight();
        inputEl.focus();
    }

    if (clearBtn) {
        clearBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            e.preventDefault();
            showClearModal();
        });
    }

    if (cancelClearBtn) {
        cancelClearBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            e.preventDefault();
            hideClearModal();
        });
    }

    if (confirmClearBtn) {
        confirmClearBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            e.preventDefault();
            doClearChat();
            hideClearModal();
        });
    }

    if (confirmModal) {
        confirmModal.addEventListener("click", (e) => {
            if (e.target === confirmModal) {
                e.stopPropagation();
                hideClearModal();
            }
        });
    }

    // Đóng modal khi nhấn phím Escape
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && confirmModal && confirmModal.classList.contains("active")) {
            hideClearModal();
        }
    });

    // Thực hiện khôi phục khi trang được tải xong
    restoreChatSession();

})();
