// Trace visualization module
const Trace = {
    container: null,
    steps: new Map(),

    init() {
        this.container = document.getElementById('traceContent');
        document.getElementById('clearTraceBtn').addEventListener('click', () => this.clear());
    },

    clear() {
        this.steps.clear();
        this.container.innerHTML = '<div class="trace-placeholder">Trace information will appear here when you send a message.</div>';
    },

    startNewTrace() {
        this.steps.clear();
        this.container.innerHTML = '';
    },

    addStep(step) {
        const stepId = step.stepId;

        if (step.status === 'started') {
            // Create new step element
            const stepEl = this.createStepElement(step);
            this.steps.set(stepId, stepEl);
            this.container.appendChild(stepEl);
        } else {
            // Update existing step
            const existingEl = this.steps.get(stepId);
            if (existingEl) {
                this.updateStepElement(existingEl, step);
            } else {
                // Step wasn't started, create completed version
                const stepEl = this.createStepElement(step);
                this.steps.set(stepId, stepEl);
                this.container.appendChild(stepEl);
            }
        }

        // Auto-scroll to bottom
        this.container.scrollTop = this.container.scrollHeight;
    },

    createStepElement(step) {
        const el = document.createElement('div');
        el.className = `trace-step ${step.status}`;
        el.dataset.stepId = step.stepId;

        el.innerHTML = `
            <div class="trace-step-header">
                <span>
                    <span class="trace-step-agent">${this.formatAgentName(step.agent)}</span>
                    <span class="trace-step-action">${this.formatAction(step.action)}</span>
                </span>
                <span class="trace-step-duration">${step.durationMs ? step.durationMs + 'ms' : '...'}</span>
            </div>
            <div class="trace-step-details">
                ${this.formatDetails(step)}
            </div>
        `;

        return el;
    },

    updateStepElement(el, step) {
        el.className = `trace-step ${step.status}`;

        const durationEl = el.querySelector('.trace-step-duration');
        if (durationEl && step.durationMs) {
            durationEl.textContent = step.durationMs + 'ms';
        }

        const detailsEl = el.querySelector('.trace-step-details');
        if (detailsEl) {
            detailsEl.innerHTML = this.formatDetails(step);
        }
    },

    formatAgentName(agent) {
        const names = {
            'orchestrator': 'Orchestrator',
            'm365_copilot': 'M365 Copilot',
            'azure_openai': 'Azure OpenAI'
        };
        return names[agent] || agent;
    },

    formatAction(action) {
        const actions = {
            'intent_analysis': 'Analyzing intent',
            'plan_creation': 'Creating plan',
            'synthesis': 'Synthesizing response',
            'email_query': 'Email query',
            'calendar_query': 'Calendar query',
            'files_query': 'Files query',
            'people_query': 'People query',
            'general_knowledge': 'General knowledge'
        };
        return actions[action] || action;
    },

    formatDetails(step) {
        if (step.error) {
            return `<span class="trace-step-error">Error: ${step.error}</span>`;
        }

        if (step.result) {
            if (step.result.intents) {
                const intents = step.result.intents.map(i => i.type).join(', ');
                return `Detected: ${intents}`;
            }
            if (step.result.steps !== undefined) {
                return `${step.result.steps} step(s), parallel: ${step.result.parallel}`;
            }
        }

        switch (step.status) {
            case 'started':
                return 'Processing...';
            case 'completed':
                return 'Completed';
            case 'failed':
                return 'Failed';
            default:
                return '';
        }
    },

    showSummary(totalDurationMs) {
        const summaryEl = document.createElement('div');
        summaryEl.className = 'trace-summary';
        summaryEl.textContent = `Total: ${totalDurationMs}ms`;
        this.container.appendChild(summaryEl);
    }
};
