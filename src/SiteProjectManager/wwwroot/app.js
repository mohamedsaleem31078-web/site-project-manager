const state = { projects: [], snags: [], reports: [], letters: [] };
const esc = (value = '') => String(value).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const showToast = (msg) => {
  const toast = document.querySelector('#toast');
  toast.textContent = msg;
  toast.classList.add('show');
  setTimeout(() => toast.classList.remove('show'), 2600);
};
const api = async (url, options = {}) => {
  const response = await fetch(url, { ...options, headers: { 'Content-Type': 'application/json', ...(options.headers || {}) } });
  if (!response.ok) {
    let msg = 'تعذر إتمام العملية.';
    try {
      const body = await response.json();
      msg = Object.values(body.errors || {}).flat()[0] || body.title || msg;
    } catch {
      // no-op
    }
    throw new Error(msg);
  }
  return response.status === 204 ? null : response.json();
};
const refresh = async () => {
  try {
    const [projects, snags, reports, letters] = await Promise.all([
      api('/api/projects'),
      api('/api/snags'),
      api('/api/reports'),
      api('/api/letters')
    ]);
    state.projects = projects;
    state.snags = snags;
    state.reports = reports;
    state.letters = letters;
    render();
  } catch (error) {
    document.querySelector('#projects-body').innerHTML = `<tr><td colspan="4">${esc(error.message)} تأكد من تشغيل التطبيق.</td></tr>`;
    document.querySelector('#snags-list').textContent = 'تعذر تحميل البيانات.';
  }
};
const render = () => {
  const open = state.snags.filter((s) => s.status === 'Open').length;
  const progress = state.snags.filter((s) => s.status === 'In Progress').length;
  const closed = state.snags.filter((s) => s.status === 'Closed').length;
  document.querySelector('#project-count').textContent = state.projects.length;
  document.querySelector('#open-count').textContent = open;
  document.querySelector('#progress-count').textContent = progress;
  document.querySelector('#closed-count').textContent = closed;

  const rows = state.projects.length ? state.projects.map((project) => `<tr><td><b>${esc(project.name)}</b></td><td>${esc(project.clientName || '—')}</td><td>${esc(project.siteName || '—')}</td><td>${project.openSnagCount}</td></tr>`).join('') : '<tr><td colspan="4">لا توجد مشاريع بعد.</td></tr>';
  document.querySelector('#projects-body').innerHTML = rows;

  const snagHtml = state.snags.length ? state.snags.slice(0, 8).map((snag) => `<div class="snag"><select data-id="${snag.id}" aria-label="حالة الملاحظة"><option value="Open" ${snag.status === 'Open' ? 'selected' : ''}>مفتوحة</option><option value="In Progress" ${snag.status === 'In Progress' ? 'selected' : ''}>قيد التنفيذ</option><option value="Closed" ${snag.status === 'Closed' ? 'selected' : ''}>مغلقة</option></select><strong>${esc(snag.title)}</strong><small>${esc(projectName(snag.projectId))} · ${esc(snag.locationDescription || 'الموقع غير محدد')} · ${esc(snag.priority)}</small></div>`).join('') : '<div class="empty">لا توجد ملاحظات مسجلة.</div>';
  document.querySelector('#snags-list').innerHTML = snagHtml;

  const reportHtml = state.reports.length ? state.reports.slice(0, 5).map((report) => `<div class="list-item"><b>${esc(report.title)}</b><small>${esc(projectName(report.projectId))} · ${new Date(report.generatedAt).toLocaleString('ar-EG')}</small><p>${esc(report.content.slice(0, 180) || 'لا يوجد محتوى.' )}</p></div>`).join('') : '<div class="list-item"><b>لا توجد تقارير</b><small>أضف تقريرًا جديدًا</small></div>';
  document.querySelector('#reports-list').innerHTML = reportHtml;

  const letterHtml = state.letters.length ? state.letters.slice(0, 5).map((letter) => `<div class="list-item"><b>${esc(letter.subject)}</b><small>${esc(projectName(letter.projectId))} · ${new Date(letter.createdAt).toLocaleString('ar-EG')}</small><p>${esc(letter.body.slice(0, 180) || 'لا يوجد نص.' )}</p></div>`).join('') : '<div class="list-item"><b>لا توجد خطابات</b><small>أضف خطابًا جديدًا</small></div>';
  document.querySelector('#letters-list').innerHTML = letterHtml;

  const projectSelect = document.querySelectorAll('#report-project, #letter-project, #project-select');
  projectSelect.forEach((select) => {
    const current = select.value;
    const options = ['<option value="">اختر المشروع</option>']
      .concat(state.projects.map((project) => `<option value="${project.id}">${esc(project.name)}</option>`))
      .join('');
    select.innerHTML = options;
    if (current) select.value = current;
  });
};
const projectName = (projectId) => state.projects.find((project) => project.id === projectId)?.name || 'مشروع';
const openDialog = (id) => document.querySelector(id)?.showModal();
const closeDialog = () => document.querySelectorAll('dialog[open]').forEach((dialog) => dialog.close());

['#add-project', '#add-project-2'].forEach((selector) => document.querySelector(selector).addEventListener('click', () => openDialog('#project-dialog')));
['#add-snag'].forEach((selector) => document.querySelector(selector).addEventListener('click', () => {
  if (!state.projects.length) { showToast('أضف مشروعًا أولًا.'); openDialog('#project-dialog'); return; }
  openDialog('#snag-dialog');
}));
document.querySelectorAll('.close').forEach((button) => button.addEventListener('click', closeDialog));
document.querySelectorAll('dialog').forEach((dialog) => dialog.addEventListener('click', (event) => { if (event.target === dialog) dialog.close(); }));

document.querySelector('#project-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  if (form.startDate) form.startDate = new Date(`${form.startDate}T00:00:00Z`).toISOString();
  if (form.endDate) form.endDate = new Date(`${form.endDate}T00:00:00Z`).toISOString();
  try {
    await api('/api/projects', { method: 'POST', body: JSON.stringify(form) });
    event.currentTarget.reset(); closeDialog(); await refresh(); showToast('تم حفظ المشروع بنجاح.');
  } catch (error) {
    document.querySelector('#project-error').textContent = error.message;
  }
});

document.querySelector('#snag-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  form.projectId = Number(form.projectId);
  form.dueDate = form.dueDate ? new Date(`${form.dueDate}T00:00:00Z`).toISOString() : null;
  try {
    await api('/api/snags', { method: 'POST', body: JSON.stringify(form) });
    event.currentTarget.reset(); closeDialog(); await refresh(); showToast('تم تسجيل الملاحظة بنجاح.');
  } catch (error) {
    document.querySelector('#snag-error').textContent = error.message;
  }
});

document.querySelector('#report-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  try {
    await api('/api/reports', { method: 'POST', body: JSON.stringify({
      projectId: Number(form.projectId),
      type: form.type || 'Summary',
      title: form.title || 'تقرير',
      content: form.content || ''
    }) });
    event.currentTarget.reset(); await refresh(); showToast('تم حفظ التقرير بنجاح.');
  } catch (error) {
    showToast(error.message);
  }
});

document.querySelector('#letter-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  try {
    await api('/api/letters', { method: 'POST', body: JSON.stringify({
      projectId: Number(form.projectId),
      subject: form.subject || 'خطاب رسمي',
      body: form.body || ''
    }) });
    event.currentTarget.reset(); await refresh(); showToast('تم حفظ الخطاب بنجاح.');
  } catch (error) {
    showToast(error.message);
  }
});

document.querySelector('#snags-list').addEventListener('change', async (event) => {
  const select = event.target.closest('select[data-id]');
  if (!select) return;
  try {
    await api(`/api/snags/${select.dataset.id}/status`, { method: 'PUT', body: JSON.stringify({ status: select.value }) });
    await refresh(); showToast('تم تحديث حالة الملاحظة.');
  } catch (error) {
    showToast(error.message);
  }
});

refresh();
