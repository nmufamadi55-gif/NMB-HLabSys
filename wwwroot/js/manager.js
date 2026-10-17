/* ═══════════════════════════════════════════════════════════
   NMB-HLabSys — Manager JS Helpers
   v1.0 — Phase 0 Foundation
   ═══════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    /* ─── Toast System ─── */
    const ICONS = {
        success: 'fa-check-circle',
        error: 'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle',
        info: 'fa-info-circle'
    };
    const TITLES = {
        success: 'Success',
        error: 'Error',
        warning: 'Warning',
        info: 'Info'
    };

    window.toast = function (message, type = 'info', duration = 4500) {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const el = document.createElement('div');
        el.className = `toast toast-${type}`;
        el.innerHTML = `
            <i class="fas ${ICONS[type] || ICONS.info} toast-icon"></i>
            <div class="toast-body">
                <div class="toast-title">${TITLES[type] || 'Info'}</div>
                <div class="toast-message">${escapeHtml(message)}</div>
            </div>
            <button type="button" class="toast-close" aria-label="Dismiss">
                <i class="fas fa-times"></i>
            </button>
        `;

        const remove = () => {
            el.style.transition = 'opacity 0.2s ease, transform 0.2s ease';
            el.style.opacity = '0';
            el.style.transform = 'translateX(20px)';
            setTimeout(() => el.remove(), 200);
        };

        el.querySelector('.toast-close').addEventListener('click', remove);
        container.appendChild(el);
        if (duration > 0) setTimeout(remove, duration);
    };

    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /* ─── Auto-fire TempData toasts ─── */
    document.querySelectorAll('.toast-trigger').forEach(el => {
        const type = el.dataset.toastType || 'info';
        const msg = el.dataset.toastMessage;
        if (msg) window.toast(msg, type);
        el.remove();
    });

    /* ─── Confirm Modal (returns Promise<boolean>) ─── */
    window.confirmModal = function (opts) {
        return new Promise(resolve => {
            const overlay = document.getElementById('confirm-modal');
            if (!overlay) { resolve(window.confirm(opts.message || 'Are you sure?')); return; }

            overlay.querySelector('#confirm-modal-title').textContent = opts.title || 'Confirm';
            overlay.querySelector('#confirm-modal-message').textContent = opts.message || 'Are you sure?';
            const okBtn = overlay.querySelector('#confirm-modal-ok');
            const cancelBtn = overlay.querySelector('#confirm-modal-cancel');
            okBtn.textContent = opts.okText || 'Confirm';
            cancelBtn.textContent = opts.cancelText || 'Cancel';
            okBtn.className = 'btn ' + (opts.danger === false ? 'btn-primary' : 'btn-danger');

            const cleanup = (result) => {
                overlay.style.display = 'none';
                okBtn.removeEventListener('click', onOk);
                cancelBtn.removeEventListener('click', onCancel);
                overlay.removeEventListener('click', onOverlay);
                document.removeEventListener('keydown', onKey);
                resolve(result);
            };
            const onOk = () => cleanup(true);
            const onCancel = () => cleanup(false);
            const onOverlay = e => { if (e.target === overlay) cleanup(false); };
            const onKey = e => {
                if (e.key === 'Escape') cleanup(false);
                if (e.key === 'Enter') cleanup(true);
            };

            okBtn.addEventListener('click', onOk);
            cancelBtn.addEventListener('click', onCancel);
            overlay.addEventListener('click', onOverlay);
            document.addEventListener('keydown', onKey);
            overlay.style.display = 'flex';
            okBtn.focus();
        });
    };

    /* ─── Prevent double-submit on POST forms ─── */
    document.querySelectorAll('form[method="post"]').forEach(form => {
        form.addEventListener('submit', () => {
            const btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.dataset.submitting) {
                btn.dataset.submitting = '1';
                const original = btn.innerHTML;
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Working...';
                btn.disabled = true;
                // Re-enable after 15s in case of validation failure (server re-render)
                setTimeout(() => {
                    if (btn.isConnected) {
                        btn.innerHTML = original;
                        btn.disabled = false;
                        delete btn.dataset.submitting;
                    }
                }, 15000);
            }
        });
    });

    /* ─── Row click → data-href ─── */
    document.querySelectorAll('[data-href]').forEach(row => {
        row.style.cursor = 'pointer';
        row.addEventListener('click', e => {
            if (!e.target.closest('a, button, input, select, label, textarea')) {
                window.location = row.dataset.href;
            }
        });
    });

    /* ─── Sidebar toggle (mobile) ─── */
    const sidebarToggle = document.getElementById('sidebar-toggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', () => {
            document.querySelector('.app-sidebar')?.classList.toggle('open');
        });
    }
})();