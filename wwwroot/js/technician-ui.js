(() => {
  const root = document.body;
  const themeKey = 'nmb-tech-theme';
  root.classList.add('tech-ui');

  const setTheme = (theme) => {
    const isDark = theme === 'dark';
    root.classList.toggle('tech-theme-dark', isDark);
    document.querySelectorAll('[data-tech-theme]').forEach((button) => {
      button.innerHTML = isDark ? '<i class="fas fa-sun"></i>' : '<i class="fas fa-moon"></i>';
      button.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
      button.setAttribute('title', isDark ? 'Switch to light mode' : 'Switch to dark mode');
    });
  };

  setTheme(localStorage.getItem(themeKey) || 'light');

  const closeCommand = () => document.querySelector('#techCommand')?.classList.remove('open');
  const closeBrief = () => document.querySelector('#techBrief')?.classList.remove('open');
  const openCommand = () => {
    const modal = document.querySelector('#techCommand');
    modal?.classList.add('open');
    modal?.querySelector('input')?.focus();
  };

  document.addEventListener('click', (event) => {
    if (event.target.closest('[data-tech-theme]')) {
      const next = root.classList.contains('tech-theme-dark') ? 'light' : 'dark';
      localStorage.setItem(themeKey, next);
      setTheme(next);
    }
    if (event.target.closest('[data-command-open]')) openCommand();
    if (event.target.closest('[data-command-close]')) closeCommand();
    if (event.target.closest('[data-brief-open]')) document.querySelector('#techBrief')?.classList.add('open');
    if (event.target.closest('[data-brief-close]')) closeBrief();
    if (event.target.closest('[data-tech-toast-close]')) {
      const toast = event.target.closest('[data-tech-toast]');
      toast?.classList.add('is-leaving');
      window.setTimeout(() => toast?.remove(), 220);
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.ctrlKey && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      openCommand();
    }
    if (event.key === 'Escape') { closeCommand(); closeBrief(); }
  });

  const commandInput = document.querySelector('#techCommand input');
  commandInput?.addEventListener('input', () => {
    const query = commandInput.value.toLowerCase();
    document.querySelectorAll('.tech-command-item').forEach((item) => {
      item.hidden = !item.textContent.toLowerCase().includes(query);
    });
  });

  document.querySelectorAll('[data-tech-search]').forEach((input) => {
    input.addEventListener('input', () => {
      const table = document.querySelector(input.dataset.techSearch);
      if (!table) return;
      const query = input.value.toLowerCase().trim();
      table.querySelectorAll('tbody tr').forEach((row) => {
        row.hidden = Boolean(query) && !row.textContent.toLowerCase().includes(query);
      });
    });
  });

  document.querySelectorAll('[data-tech-toast]').forEach((toast) => {
    if (toast.classList.contains('tech-toast-success')) {
      window.setTimeout(() => {
        toast.classList.add('is-leaving');
        window.setTimeout(() => toast.remove(), 220);
      }, 5200);
    }
  });

  document.querySelectorAll('.tech-shell form').forEach((form) => {
    form.addEventListener('submit', () => {
      const button = form.querySelector('button[type="submit"], button:not([type])');
      if (!button || button.disabled) return;
      button.disabled = true;
      button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Working…';
    });
  });
})();
