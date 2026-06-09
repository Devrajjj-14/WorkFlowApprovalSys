// ── Modal helpers ──────────────────────────────────────────
function openModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('open');
}
function closeModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('open');
}
function closeModalOutside(event, id) {
  if (event.target.id === id) closeModal(id);
}

// Escape key closes any open modal
document.addEventListener('keydown', e => {
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal-overlay.open').forEach(el => el.classList.remove('open'));
  }
});

// ── Tab switching ──────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  const tabs = document.querySelectorAll('.tab');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      const target = tab.dataset.tab;
      tabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
      const panel = document.getElementById('tab-' + target);
      if (panel) panel.classList.add('active');
    });
  });

  // Auto-dismiss toasts
  document.querySelectorAll('.toast').forEach(t => {
    setTimeout(() => {
      t.style.transition = 'opacity .5s';
      t.style.opacity = '0';
      setTimeout(() => t.remove(), 500);
    }, 4000);
  });
});

// ── Dropzone label update ──────────────────────────────────
function updateDropzone(input) {
  const display = document.getElementById('file-name-display');
  if (display && input.files.length > 0) {
    display.textContent = '✓ ' + input.files[0].name;
  }
}
