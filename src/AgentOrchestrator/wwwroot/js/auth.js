// Authentication module
const Auth = {
    isAuthenticated: false,
    userName: null,

    async checkStatus() {
        try {
            const response = await fetch('/auth/status');
            const data = await response.json();

            this.isAuthenticated = data.isAuthenticated;
            this.userName = data.userName;

            this.updateUI();
            return data;
        } catch (error) {
            console.error('Error checking auth status:', error);
            return { isAuthenticated: false };
        }
    },

    login() {
        window.location.href = '/auth/login';
    },

    async logout() {
        try {
            await fetch('/auth/logout', { method: 'POST' });
            this.isAuthenticated = false;
            this.userName = null;
            this.updateUI();
        } catch (error) {
            console.error('Error logging out:', error);
        }
    },

    updateUI() {
        const loginBtn = document.getElementById('loginBtn');
        const logoutBtn = document.getElementById('logoutBtn');
        const userNameSpan = document.getElementById('userName');
        const chatInput = document.getElementById('chatInput');
        const sendBtn = document.getElementById('sendBtn');

        if (this.isAuthenticated) {
            loginBtn.style.display = 'none';
            logoutBtn.style.display = 'block';
            userNameSpan.textContent = this.userName || 'User';
            chatInput.disabled = false;
            sendBtn.disabled = false;
            chatInput.placeholder = 'Type your message...';
        } else {
            loginBtn.style.display = 'block';
            logoutBtn.style.display = 'none';
            userNameSpan.textContent = '';
            chatInput.disabled = true;
            sendBtn.disabled = true;
            chatInput.placeholder = 'Please login to chat...';
        }
    },

    init() {
        document.getElementById('loginBtn').addEventListener('click', () => this.login());
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());

        // Check status on load
        this.checkStatus();
    }
};
