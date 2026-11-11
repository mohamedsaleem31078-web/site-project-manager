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
      msg = Object.values(body.errors || {}).flat()[0] || body.message || body.title || msg;
    } catch { /* use the general message */ }
    throw new Error(msg);
  }
  return response.status === 204 ? null : response.json();
};
const refresh = async () => {
  try {
    const [projects, snags, reports, letters] = await Promise.all([
      api('/api/projects'), api('/api/snags'), api('/api/reports'), api('/api/letters')
    ]);
    Object.assign(state, { projects, snags, reports, letters });
    render();
  } catch (error) {
    document.querySelector('#projects-body').innerHTML = `<tr><td colspan="4">${esc(error.message)} تأكد من تشغيل التطبيق.</td></tr>`;
    document.querySelector('#snags-list').textContent = 'تعذر تحميل البيانات.';
  }
};
const projectName = (id) => state.projects.find((project) => project.id === id)?.name || 'مشروع';
const render = () => {
  const count = (status) => state.snags.filter((snag) => snag.status === status).length;
  document.querySelector('#project-count').textContent = state.projects.length;
  document.querySelector('#open-count').textContent = count('Open');
  document.querySelector('#progress-count').textContent = count('In Progress');
  document.querySelector('#closed-count').textContent = count('Closed');

  document.querySelector('#projects-body').innerHTML = state.projects.length
    ? state.projects.map((p) => `<tr><td><b>${esc(p.name)}</b></td><td>${esc(p.clientName || '—')}</td><td>${esc(p.siteName || '—')}</td><td>${p.openSnagCount}</td></tr>`).join('')
    : '<tr><td colspan="4">لا توجد مشاريع بعد.</td></tr>';

  document.querySelector('#snags-list').innerHTML = state.snags.length
    ? state.snags.slice(0, 8).map((s) => `<div class="snag"><select data-id="${s.id}" aria-label="حالة الملاحظة"><option value="Open" ${s.status === 'Open' ? 'selected' : ''}>مفتوحة</option><option value="In Progress" ${s.status === 'In Progress' ? 'selected' : ''}>قيد التنفيذ</option><option value="Closed" ${s.status === 'Closed' ? 'selected' : ''}>مغلقة</option></select><strong>${esc(s.title)}</strong><small>${esc(projectName(s.projectId))} · ${esc(s.locationDescription || 'الموقع غير محدد')} · ${esc(s.priority)}</small>${(s.attachments || []).map((a) => `<a class="attachment-link" href="/api/attachments/${a.id}" target="_blank" rel="noopener">📎 ${esc(a.fileName)}</a>`).join('')}</div>`).join('')
    : '<div class="empty">لا توجد ملاحظات مسجلة.</div>';

  document.querySelector('#reports-list').innerHTML = state.reports.length
    ? state.reports.slice(0, 5).map((r) => `<div class="list-item"><b>${esc(r.title)}</b><small>${esc(projectName(r.projectId))} · ${new Date(r.generatedAt).toLocaleString('ar-EG')}</small><p>${esc(r.content.slice(0, 180) || 'لا يوجد محتوى.')}</p></div>`).join('')
    : '<div class="list-item"><b>لا توجد تقارير</b><small>أضف تقريرًا جديدًا</small></div>';

  document.querySelector('#letters-list').innerHTML = state.letters.length
    ? state.letters.slice(0, 5).map((l) => `<div class="list-item"><b>${esc(l.subject)}</b><small>${esc(projectName(l.projectId))} · ${new Date(l.createdAt).toLocaleString('ar-EG')}</small><p>${esc(l.body.slice(0, 180) || 'لا يوجد نص.')}</p></div>`).join('')
    : '<div class="list-item"><b>لا توجد خطابات</b><small>أضف خطابًا جديدًا</small></div>';

  document.querySelectorAll('#report-project, #letter-project, #project-select').forEach((select) => {
    const current = select.value;
    select.innerHTML = '<option value="">اختر المشروع</option>' + state.projects.map((p) => `<option value="${p.id}">${esc(p.name)}</option>`).join('');
    if (current) select.value = current;
  });
};
const openDialog = (id) => document.querySelector(id)?.showModal();
const closeDialog = () => document.querySelectorAll('dialog[open]').forEach((dialog) => dialog.close());

['#add-project', '#add-project-2'].forEach((selector) => document.querySelector(selector).addEventListener('click', () => openDialog('#project-dialog')));
document.querySelector('#add-snag').addEventListener('click', () => {
  if (!state.projects.length) { showToast('أضف مشروعًا أولًا.'); openDialog('#project-dialog'); return; }
  openDialog('#snag-dialog');
});
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
  } catch (error) { document.querySelector('#project-error').textContent = error.message; }
});

document.querySelector('#snag-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const formElement = event.currentTarget;
  const form = Object.fromEntries(new FormData(formElement));
  form.projectId = Number(form.projectId);
  form.dueDate = form.dueDate ? new Date(`${form.dueDate}T00:00:00Z`).toISOString() : null;
  const files = [...formElement.querySelector('[name="attachment"]').files];
  try {
    const snag = await api('/api/snags', { method: 'POST', body: JSON.stringify(form) });
    for (const file of files) {
      const data = new FormData();
      data.append('file', file);
      const response = await fetch(`/api/snags/${snag.id}/attachments`, { method: 'POST', body: data });
      if (!response.ok) {
        const body = await response.json().catch(() => ({}));
        throw new Error(body.message || 'تم حفظ الملاحظة لكن تعذر رفع أحد المرفقات.');
      }
    }
    formElement.reset(); closeDialog(); await refresh(); showToast('تم حفظ الملاحظة ومرفقاتها.');
  } catch (error) {
    document.querySelector('#snag-error').textContent = error.message;
    await refresh();
  }
});

document.querySelector('#report-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  try {
    await api('/api/reports', { method: 'POST', body: JSON.stringify({ projectId: Number(form.projectId), type: form.type || 'Summary', title: form.title || 'تقرير', content: form.content || '' }) });
    event.currentTarget.reset(); await refresh(); showToast('تم حفظ التقرير بنجاح.');
  } catch (error) { showToast(error.message); }
});

document.querySelector('#letter-form').addEventListener('submit', async (event) => {
  event.preventDefault();
  const form = Object.fromEntries(new FormData(event.currentTarget));
  try {
    await api('/api/letters', { method: 'POST', body: JSON.stringify({ projectId: Number(form.projectId), subject: form.subject || 'خطاب رسمي', body: form.body || '' }) });
    event.currentTarget.reset(); await refresh(); showToast('تم حفظ الخطاب بنجاح.');
  } catch (error) { showToast(error.message); }
});

document.querySelector('#snags-list').addEventListener('change', async (event) => {
  const select = event.target.closest('select[data-id]');
  if (!select) return;
  try {
    await api(`/api/snags/${select.dataset.id}/status`, { method: 'PUT', body: JSON.stringify({ status: select.value }) });
    await refresh(); showToast('تم تحديث حالة الملاحظة.');
  } catch (error) { showToast(error.message); }
});

refresh();
