// WPF WebView Bridge
function log(message) {
    const logDiv = document.getElementById('log');
    if (!logDiv) return;
    const time = new Date().toLocaleTimeString();
    logDiv.innerHTML += `[${time}] ${message}<br>`;
    logDiv.scrollTop = logDiv.scrollHeight;
}

// Flag to prevent duplicate calls
let isSending = false;

function sendToWpf() {
    // Prevent duplicate calls
    if (isSending) return;
    isSending = true;
    
    const input = document.getElementById('messageInput');
    if (!input) {
        isSending = false;
        return;
    }
    
    // Get message value before clearing
    let message = (input.value || '').trim();
    if (!message) {
        message = 'Hello from WebView!';
    }
    
    // Send message
    if (window.wpfBridge) {
        try {
            window.wpfBridge.sendMessage(message);
            log('📤 发送消息到 WPF: ' + message);
            input.value = '';
        } catch (error) {
            log('❌ 发送消息失败: ' + error.message);
        } finally {
            setTimeout(() => { isSending = false; }, 200);
        }
    } else {
        log('❌ WPF Bridge 未初始化');
        isSending = false;
    }
}

async function callWpfMethod() {
    try {
        if (window.chrome?.webview?.hostObjects) {
            const wpfHost = window.chrome.webview.hostObjects.wpfHost;
            const result = await wpfHost.ShowMessage('这是从 JavaScript 调用的 C# 方法！');
            const responseDiv = document.getElementById('wpfResponse');
            if (responseDiv) {
                responseDiv.textContent = 'WPF 响应: ' + result;
            }
            log('✅ 调用 WPF 方法成功，返回值: ' + result);
        } else {
            log('❌ Host Objects 不可用');
        }
    } catch (error) {
        log('❌ 调用 WPF 方法失败: ' + error.message);
    }
}

// Expose functions to global scope
window.sendToWpf = sendToWpf;
window.callWpfMethod = callWpfMethod;

// Listen for messages from WPF
window.onWpfMessage = function(message) {
    const messageDiv = document.getElementById('wpfMessage');
    if (!messageDiv) return;
    
    const messageText = message?.data || message || '';
    messageDiv.textContent = '收到 WPF 消息: ' + messageText;
    log('📥 收到 WPF 消息: ' + messageText);
};

// Setup event handlers
function setupEventHandlers() {
    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.onkeydown = (e) => {
            if (e.key === 'Enter') {
                sendToWpf();
            }
        };
    }

    const sendButton = document.getElementById('sendButton');
    if (sendButton) {
        sendButton.onclick = (e)=>{
            e.preventDefault();
            e.stopPropagation();
            sendToWpf();
        };
    }

    const callButton = document.getElementById('callButton');
    if (callButton) {
        callButton.onclick = callWpfMethod;
    }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        setupEventHandlers();
        log('✅ 页面加载完成，准备与 WPF 交互');
        
        // Check if wpfBridge is available
        setTimeout(() => {
            if (window.wpfBridge) {
                log('✅ WPF Bridge 已就绪');
            } else {
                log('⚠️ WPF Bridge 尚未初始化，等待中...');
                setTimeout(() => {
                    if (window.wpfBridge) {
                        log('✅ WPF Bridge 已就绪');
                    } else {
                        log('❌ WPF Bridge 初始化失败');
                    }
                }, 1000);
            }
        }, 100);
    });
} else {
    setupEventHandlers();
    log('✅ 页面加载完成，准备与 WPF 交互');
}
