// Main application initialization
document.addEventListener('DOMContentLoaded', () => {
    console.log('Agent Orchestrator Lab - Initializing...');

    // Initialize modules
    Auth.init();
    Trace.init();
    Chat.init();

    console.log('Agent Orchestrator Lab - Ready');
});
