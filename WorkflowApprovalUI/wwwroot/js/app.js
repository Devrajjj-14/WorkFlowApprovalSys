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

  function activateTab(targetName) {
    tabs.forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    const matchTab = [...tabs].find(t => t.dataset.tab === targetName);
    const matchPanel = document.getElementById('tab-' + targetName);
    if (matchTab) matchTab.classList.add('active');
    if (matchPanel) matchPanel.classList.add('active');
  }

  // Activate tab from URL query param ?tab=approvals
  const urlParams = new URLSearchParams(window.location.search);
  const tabParam = urlParams.get('tab');
  if (tabParam) activateTab(tabParam);

  tabs.forEach(tab => {
    tab.addEventListener('click', () => activateTab(tab.dataset.tab));
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

// ── Task comment thread toggle ─────────────────────────────
function toggleTaskComments(taskId) {
  const thread = document.getElementById('thread-' + taskId);
  if (thread) thread.classList.toggle('open');
}
