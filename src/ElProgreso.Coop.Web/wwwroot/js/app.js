/**
 * Cooperativa Financiera El Progreso - Main Web Application Engine
 * Impeccable Architecture: Unified Zero-Redundancy Frontend, Live TRM & Paginated Reports
 */

// Global State
let currentTrm = 4185.50;
let trmValidDate = 'Hoy';
let allAssociates = [];
let filteredAssociates = [];
let selectedAssociate = null;
let currentPage = 1;
const PAGE_SIZE = 10;
let searchDebounceTimer = null;

// Reports Pagination State (Zero-Redundancy Data Storage)
const ReportState = {
  data: {
    dormant: [],
    largest: [],
    movements: []
  },
  page: {
    dormant: 1,
    largest: 1,
    movements: 1
  }
};

// Currency & Date Formatters
const Formatters = {
  cop(val) {
    return '$' + new Intl.NumberFormat('es-CO', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(Math.round(val || 0)) + ' COP';
  },
  copRaw(val) {
    return '$' + new Intl.NumberFormat('es-CO', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(Math.round(val || 0));
  },
  usd(val) {
    return 'US$ ' + new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(val || 0);
  },
  date(dateStr) {
    if (!dateStr) return '--';
    const d = new Date(dateStr);
    return isNaN(d.getTime()) ? dateStr : d.toISOString().substring(0, 16).replace('T', ' ');
  },
  dateOnly(dateStr) {
    if (!dateStr) return '--';
    const d = new Date(dateStr);
    return isNaN(d.getTime()) ? dateStr : d.toISOString().substring(0, 10);
  }
};

// API Client
const API = {
  async get(endpoint) {
    const res = await fetch(endpoint);
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.error || err.message || 'Error en la petición');
    }
    return res.json();
  },
  async post(endpoint, data) {
    const res = await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.error || err.message || 'Error al procesar la solicitud');
    }
    return res.json();
  },
  async put(endpoint, data) {
    const res = await fetch(endpoint, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.error || err.message || 'Error al actualizar');
    }
    return res.json();
  },
  async delete(endpoint) {
    const res = await fetch(endpoint, { method: 'DELETE' });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }));
      throw new Error(err.error || err.message || 'Error al eliminar');
    }
    return true;
  }
};

// Toast Notifications
function showToast(message, type = 'success') {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;
  toast.innerText = message;

  container.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}

// Modal System
function openModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
  }
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.remove('active');
    document.body.style.overflow = '';
  }
}

function handleBackdropClick(event, modalId) {
  if (event.target.id === modalId) {
    closeModal(modalId);
  }
}

document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal-overlay.active').forEach(m => closeModal(m.id));
  }
});

// View Navigation Switcher
function switchView(viewName) {
  document.querySelectorAll('.view-section').forEach(el => el.classList.add('hidden'));
  document.querySelectorAll('.nav-tab-btn').forEach(btn => btn.classList.remove('active'));

  const viewEl = document.getElementById(`view-${viewName}`);
  const btnEl = document.getElementById(`tab-btn-${viewName}`);

  if (viewEl) viewEl.classList.remove('hidden');
  if (btnEl) btnEl.classList.add('active');

  window.scrollTo({ top: 0, behavior: 'smooth' });

  if (viewName === 'cashier') {
    loadAssociates();
  } else if (viewName === 'reports') {
    loadReportsDashboard();
  }
}

// Live TRM Synchronization & Interactions
async function fetchLiveTrm() {
  const trmDisplay = document.getElementById('trm-val-display');
  const syncTime = document.getElementById('trm-sync-time');
  const syncBadge = document.getElementById('trm-sync-badge');

  try {
    const data = await API.get('/api/trm/live');
    if (data && data.rate > 0) {
      currentTrm = data.rate;
      trmValidDate = data.validFrom ? data.validFrom.substring(0, 10) : 'Hoy';
      if (trmDisplay) trmDisplay.innerText = '$' + new Intl.NumberFormat('es-CO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(currentTrm);
      if (syncTime) syncTime.innerText = `Sincronizado: ${trmValidDate} (datos.gov.co)`;
      if (syncBadge) syncBadge.innerText = `TRM Oficial: $${new Intl.NumberFormat('es-CO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(currentTrm)} (Vigente)`;
      convertCopToUsd();
      return true;
    }
  } catch (e) {
    console.info('API local TRM no disponible, intentando fallback directo...', e);
  }

  // Fallback direct endpoint
  try {
    const res = await fetch('https://www.datos.gov.co/resource/32sa-7pih.json?$limit=1&$order=vigenciahasta%20DESC');
    if (res.ok) {
      const ext = await res.json();
      if (ext && ext.length > 0 && ext[0].valor) {
        currentTrm = parseFloat(ext[0].valor);
        trmValidDate = ext[0].vigenciadesde ? ext[0].vigenciadesde.substring(0, 10) : 'Hoy';
        if (trmDisplay) trmDisplay.innerText = '$' + new Intl.NumberFormat('es-CO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(currentTrm);
        if (syncTime) syncTime.innerText = `Sincronizado: ${trmValidDate} (datos.gov.co)`;
        convertCopToUsd();
        return true;
      }
    }
  } catch (err) {
    console.info('Usando TRM de respaldo por defecto:', currentTrm);
  }
  return false;
}

async function refreshTrmManual() {
  const icon = document.getElementById('trm-sync-icon');
  if (icon) {
    icon.style.transition = 'transform 0.6s ease';
    icon.style.transform = 'rotate(360deg)';
  }

  const success = await fetchLiveTrm();
  
  setTimeout(() => {
    if (icon) {
      icon.style.transition = 'none';
      icon.style.transform = 'rotate(0deg)';
    }
  }, 600);

  if (success) {
    showToast(`TRM Oficial actualizada: ${Formatters.copRaw(currentTrm)} por USD`);
  } else {
    showToast('TRM sincronizada con tasa de contingencia oficial');
  }
}

function convertCopToUsd() {
  const copInput = document.getElementById('cop-input');
  const usdInput = document.getElementById('usd-input');
  if (!copInput || !usdInput) return;
  const copVal = parseFloat(copInput.value) || 0;
  usdInput.value = (copVal / currentTrm).toFixed(2);
}

function convertUsdToCop() {
  const copInput = document.getElementById('cop-input');
  const usdInput = document.getElementById('usd-input');
  if (!copInput || !usdInput) return;
  const usdVal = parseFloat(usdInput.value) || 0;
  copInput.value = Math.round(usdVal * currentTrm);
}

function swapCurrencyConversion() {
  const copInput = document.getElementById('cop-input');
  const usdInput = document.getElementById('usd-input');
  if (!copInput || !usdInput) return;

  const currentCop = parseFloat(copInput.value) || 0;
  const targetUsd = currentCop > 0 ? currentCop / currentTrm : 100;
  usdInput.value = targetUsd.toFixed(2);
  convertUsdToCop();
  usdInput.focus();
}

// Savings Simulator
function updateSimulation() {
  const amountSlider = document.getElementById('slider-amount');
  const monthsSlider = document.getElementById('slider-months');
  const amountValLabel = document.getElementById('slider-amount-val');
  const monthsValLabel = document.getElementById('slider-months-val');
  const finalTotalLabel = document.getElementById('sim-final-total');
  const interestsLabel = document.getElementById('sim-interests');

  if (!amountSlider || !monthsSlider) return;

  const principal = parseFloat(amountSlider.value) || 5000000;
  const months = parseInt(monthsSlider.value) || 12;
  const annualRate = 0.105; // 10.5% E.A.

  amountValLabel.innerText = Formatters.cop(principal);
  monthsValLabel.innerText = `${months} meses`;

  // Compound interest projection
  const total = principal * Math.pow(1 + annualRate, months / 12);
  const yieldEarnings = total - principal;

  finalTotalLabel.innerText = Formatters.cop(total);
  interestsLabel.innerText = `+ ${Formatters.cop(yieldEarnings)} en rendimientos netos`;
}

// =============================================================================
// ASSOCIATES DIRECTORY & 360° MANAGEMENT
// =============================================================================

async function loadAssociates() {
  try {
    allAssociates = await API.get('/api/associates');
    applyFilters();
    updatePortalStats();
  } catch (err) {
    showToast(`Error al cargar asociados: ${err.message}`, 'error');
  }
}

function handleSearchInput(e) {
  clearTimeout(searchDebounceTimer);
  searchDebounceTimer = setTimeout(() => {
    applyFilters();
  }, 250);
}

function applyFilters() {
  const query = (document.getElementById('associate-search-input')?.value || '').toLowerCase().trim();
  const docType = document.getElementById('filter-doc-type')?.value || '';
  const balanceFilter = document.getElementById('filter-balance')?.value || '';
  const sortBy = document.getElementById('sort-associates')?.value || 'name_asc';

  filteredAssociates = allAssociates.filter(a => {
    const matchQuery = !query || a.document.toLowerCase().includes(query) || a.name.toLowerCase().includes(query);
    if (!matchQuery) return false;

    if (docType && a.documentType.toString() !== docType) return false;
    if (balanceFilter === 'positive' && (a.balance || 0) <= 0) return false;
    if (balanceFilter === 'zero' && (a.balance || 0) > 0) return false;

    return true;
  });

  filteredAssociates.sort((a, b) => {
    if (sortBy === 'name_asc') return a.name.localeCompare(b.name);
    if (sortBy === 'name_desc') return b.name.localeCompare(a.name);
    if (sortBy === 'balance_desc') return (b.balance || 0) - (a.balance || 0);
    if (sortBy === 'balance_asc') return (a.balance || 0) - (b.balance || 0);
    if (sortBy === 'date_desc') return new Date(b.registrationDate) - new Date(a.registrationDate);
    return 0;
  });

  currentPage = 1;
  renderAssociatesTable();
}

function renderAssociatesTable() {
  const tbody = document.getElementById('associates-directory-tbody');
  const paginationInfo = document.getElementById('pagination-info');
  const pageLabel = document.getElementById('pagination-current-page');
  const prevBtn = document.getElementById('btn-prev-page');
  const nextBtn = document.getElementById('btn-next-page');

  if (!tbody) return;

  if (filteredAssociates.length === 0) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center py-8 text-coop-text-tertiary">No se encontraron asociados que coincidan con la búsqueda.</td></tr>`;
    if (paginationInfo) paginationInfo.innerText = 'Mostrando 0 de 0 asociados';
    if (prevBtn) prevBtn.disabled = true;
    if (nextBtn) nextBtn.disabled = true;
    return;
  }

  const total = filteredAssociates.length;
  const totalPages = Math.ceil(total / PAGE_SIZE);
  const startIdx = (currentPage - 1) * PAGE_SIZE;
  const endIdx = Math.min(startIdx + PAGE_SIZE, total);
  const pageItems = filteredAssociates.slice(startIdx, endIdx);

  let html = '';
  pageItems.forEach(a => {
    const txCount = a.transactions ? a.transactions.length : 0;
    html += `
      <tr class="cursor-pointer" onclick="selectAssociate('${a.document}')">
        <td class="text-center"><span class="ds-badge badge-doc">${a.documentType}</span></td>
        <td class="font-mono font-bold text-coop-green-deep">${a.document}</td>
        <td class="font-semibold text-coop-text-primary">${a.name}</td>
        <td class="text-xs text-coop-text-secondary">${a.phone || '--'}</td>
        <td class="text-center font-mono text-xs">${txCount}</td>
        <td class="num-cell text-coop-green-forest font-bold">${Formatters.cop(a.balance)}</td>
        <td class="text-center" onclick="event.stopPropagation()">
          <button type="button" class="btn btn-secondary btn-sm" onclick="selectAssociate('${a.document}')">Ver Ficha</button>
        </td>
      </tr>
    `;
  });

  tbody.innerHTML = html;
  if (paginationInfo) paginationInfo.innerText = `Mostrando ${startIdx + 1} a ${endIdx} de ${total} asociados`;
  if (pageLabel) pageLabel.innerText = `${currentPage} / ${totalPages}`;
  if (prevBtn) prevBtn.disabled = currentPage <= 1;
  if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
}

function prevPage() {
  if (currentPage > 1) {
    currentPage--;
    renderAssociatesTable();
  }
}

function nextPage() {
  const totalPages = Math.ceil(filteredAssociates.length / PAGE_SIZE);
  if (currentPage < totalPages) {
    currentPage++;
    renderAssociatesTable();
  }
}

async function selectAssociate(documentNumber) {
  try {
    selectedAssociate = await API.get(`/api/associates/${documentNumber}`);
    renderAssociateCard360();
    loadAssociateTransactions(documentNumber);

    const container = document.getElementById('selected-associate-container');
    if (container) {
      container.classList.remove('hidden');
      container.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  } catch (err) {
    showToast(`Error al consultar asociado: ${err.message}`, 'error');
  }
}

function renderAssociateCard360() {
  if (!selectedAssociate) return;

  document.getElementById('card-doc-type').innerText = selectedAssociate.documentType;
  document.getElementById('card-document').innerText = selectedAssociate.document;
  document.getElementById('card-name').innerText = selectedAssociate.name;
  document.getElementById('card-registration-date').innerText = `Registrado el: ${Formatters.dateOnly(selectedAssociate.registrationDate)}`;
  
  const balanceCop = selectedAssociate.balance || 0;
  const balanceUsd = currentTrm > 0 ? balanceCop / currentTrm : 0;
  
  document.getElementById('card-balance-cop').innerText = Formatters.cop(balanceCop);
  document.getElementById('card-balance-usd').innerText = `≈ ${Formatters.usd(balanceUsd)} (TRM: ${Formatters.copRaw(currentTrm)})`;
  
  document.getElementById('card-phone').innerText = selectedAssociate.phone || '(No registrado)';
  document.getElementById('card-email').innerText = selectedAssociate.email || '(No registrado)';
  document.getElementById('card-address').innerText = selectedAssociate.address || '(No registrado)';
  
  const txCount = selectedAssociate.transactions ? selectedAssociate.transactions.length : 0;
  document.getElementById('card-tx-count').innerText = `${txCount} operaciones registradas`;

  const statusBadge = document.getElementById('card-status-badge');
  if (statusBadge) {
    if (txCount > 0) {
      statusBadge.className = 'ds-badge badge-deposit';
      statusBadge.innerText = 'Activo con Movimientos';
    } else {
      statusBadge.className = 'ds-badge badge-withdraw';
      statusBadge.innerText = 'Sin Movimientos';
    }
  }
}

async function loadAssociateTransactions(doc) {
  const tbody = document.getElementById('associate-tx-tbody');
  if (!tbody) return;

  try {
    const txs = await API.get(`/api/transactions/associate/${doc}`);
    if (!txs || txs.length === 0) {
      tbody.innerHTML = `<tr><td colspan="5" class="text-center text-coop-text-tertiary py-4">Sin transacciones registradas</td></tr>`;
      return;
    }

    let html = '';
    txs.forEach(t => {
      const isDeposit = t.type === 0 || t.type === 'Deposit';
      const badgeClass = isDeposit ? 'badge-deposit' : 'badge-withdraw';
      const typeLabel = isDeposit ? 'Consignación' : 'Retiro';
      const sign = isDeposit ? '+' : '-';
      const colorClass = isDeposit ? 'text-green-700' : 'text-red-700';

      html += `
        <tr>
          <td class="font-mono text-xs">${Formatters.date(t.date)}</td>
          <td class="text-center"><span class="ds-badge ${badgeClass}">${typeLabel}</span></td>
          <td class="num-cell ${colorClass} font-bold">${sign} ${Formatters.cop(t.amount)}</td>
          <td class="num-cell text-coop-gold-tag-text">${t.commission > 0 ? Formatters.cop(t.commission) : '$0'}</td>
          <td class="num-cell font-mono text-xs text-coop-text-tertiary">${t.id}</td>
        </tr>
      `;
    });
    tbody.innerHTML = html;
  } catch (e) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-red-600 py-4">Error al cargar historial</td></tr>`;
  }
}

function closeAssociateCard() {
  const container = document.getElementById('selected-associate-container');
  if (container) container.classList.add('hidden');
  selectedAssociate = null;
}

// =============================================================================
// CASHIER OPERATIONS (DEPOSIT & WITHDRAWAL)
// =============================================================================

function openRegisterModal() {
  document.getElementById('form-register-associate')?.reset();
  openModal('modal-register');
}

async function handleRegisterAssociate(e) {
  e.preventDefault();
  const docType = document.getElementById('reg-doc-type').value;
  const documentNum = document.getElementById('reg-document').value.trim();
  const name = document.getElementById('reg-name').value.trim();
  const phone = document.getElementById('reg-phone').value.trim();
  const email = document.getElementById('reg-email').value.trim();
  const address = document.getElementById('reg-address').value.trim();

  try {
    const docTypeEnum = { 'CC': 0, 'TI': 1, 'CE': 2, 'NIT': 3, 'PAS': 4 }[docType] ?? 0;
    const created = await API.post('/api/associates', {
      document: documentNum,
      name,
      documentType: docTypeEnum,
      phone: phone || null,
      email: email || null,
      address: address || null
    });

    closeModal('modal-register');
    showToast(`Asociado ${created.name} registrado con éxito.`);
    await loadAssociates();
    selectAssociate(created.document);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

function openEditModal() {
  if (!selectedAssociate) return;
  document.getElementById('edit-document-hidden').value = selectedAssociate.document;
  document.getElementById('edit-name').value = selectedAssociate.name;
  document.getElementById('edit-phone').value = selectedAssociate.phone || '';
  document.getElementById('edit-email').value = selectedAssociate.email || '';
  document.getElementById('edit-address').value = selectedAssociate.address || '';
  openModal('modal-edit');
}

async function handleEditAssociate(e) {
  e.preventDefault();
  const doc = document.getElementById('edit-document-hidden').value;
  const name = document.getElementById('edit-name').value.trim();
  const phone = document.getElementById('edit-phone').value.trim();
  const email = document.getElementById('edit-email').value.trim();
  const address = document.getElementById('edit-address').value.trim();

  try {
    const updated = await API.put(`/api/associates/${doc}`, {
      name,
      phone: phone || null,
      email: email || null,
      address: address || null
    });

    closeModal('modal-edit');
    showToast(`Datos de ${updated.name} actualizados.`);
    await loadAssociates();
    selectAssociate(doc);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

async function confirmDeleteAssociate() {
  if (!selectedAssociate) return;

  const confirmed = confirm(`¿Está seguro de eliminar al asociado ${selectedAssociate.name} (${selectedAssociate.document})?`);
  if (!confirmed) return;

  try {
    await API.delete(`/api/associates/${selectedAssociate.document}`);
    showToast(`Asociado ${selectedAssociate.name} eliminado.`);
    closeAssociateCard();
    await loadAssociates();
  } catch (err) {
    showToast(`No se pudo eliminar: ${err.message}`, 'error');
  }
}

function openDepositModal() {
  if (!selectedAssociate) return;
  document.getElementById('deposit-associate-name').innerText = selectedAssociate.name;
  document.getElementById('deposit-associate-doc').innerText = `${selectedAssociate.documentType} ${selectedAssociate.document}`;
  document.getElementById('deposit-amount').value = '';
  openModal('modal-deposit');
}

async function handleDepositSubmit(e) {
  e.preventDefault();
  if (!selectedAssociate) return;

  const amount = parseFloat(document.getElementById('deposit-amount').value);
  if (!amount || amount <= 0) {
    showToast('El monto debe ser superior a $0 COP', 'error');
    return;
  }

  try {
    const tx = await API.post('/api/transactions/deposit', {
      document: selectedAssociate.document,
      amount
    });

    closeModal('modal-deposit');
    showToast(`Consignación de ${Formatters.cop(amount)} realizada con éxito.`);
    await selectAssociate(selectedAssociate.document);
    await loadAssociates();
    showReceipt('Consignación', tx, selectedAssociate);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

function openWithdrawModal() {
  if (!selectedAssociate) return;
  document.getElementById('withdraw-associate-name').innerText = selectedAssociate.name;
  document.getElementById('withdraw-associate-balance').innerText = Formatters.cop(selectedAssociate.balance);
  document.getElementById('withdraw-amount').value = '';
  document.getElementById('withdraw-commission-alert').classList.add('hidden');
  openModal('modal-withdraw');
}

function checkWithdrawalCommission() {
  const amount = parseFloat(document.getElementById('withdraw-amount').value) || 0;
  const alertEl = document.getElementById('withdraw-commission-alert');
  const debitEl = document.getElementById('withdraw-total-debit');

  if (amount > 1000000) {
    alertEl.classList.remove('hidden');
    debitEl.innerText = Formatters.cop(amount + 8000);
  } else {
    alertEl.classList.add('hidden');
  }
}

async function handleWithdrawSubmit(e) {
  e.preventDefault();
  if (!selectedAssociate) return;

  const amount = parseFloat(document.getElementById('withdraw-amount').value);
  if (!amount || amount <= 0) {
    showToast('El monto debe ser superior a $0 COP', 'error');
    return;
  }

  try {
    const tx = await API.post('/api/transactions/withdraw', {
      document: selectedAssociate.document,
      amount
    });

    closeModal('modal-withdraw');
    showToast(`Retiro de ${Formatters.cop(amount)} debitado con éxito.`);
    await selectAssociate(selectedAssociate.document);
    await loadAssociates();
    showReceipt('Retiro de Fondos', tx, selectedAssociate);
  } catch (err) {
    showToast(err.message, 'error');
  }
}

function showReceipt(operationTitle, transaction, associate) {
  document.getElementById('receipt-tx-title').innerText = `COMPROBANTE DE ${operationTitle.toUpperCase()}`;
  document.getElementById('receipt-tx-id').innerText = transaction.id;
  document.getElementById('receipt-tx-date').innerText = Formatters.date(transaction.date);
  document.getElementById('receipt-tx-associate').innerText = associate.name;
  document.getElementById('receipt-tx-doc').innerText = `${associate.documentType} ${associate.document}`;
  document.getElementById('receipt-tx-amount').innerText = Formatters.cop(transaction.amount);

  const commRow = document.getElementById('receipt-row-commission');
  const totalDebitedRow = document.getElementById('receipt-row-total-debited');

  if (transaction.commission > 0) {
    commRow.classList.remove('hidden');
    totalDebitedRow.classList.remove('hidden');
    document.getElementById('receipt-tx-commission').innerText = Formatters.cop(transaction.commission);
    document.getElementById('receipt-tx-total-debited').innerText = Formatters.cop(transaction.amount + transaction.commission);
  } else {
    commRow.classList.add('hidden');
    totalDebitedRow.classList.add('hidden');
  }

  document.getElementById('receipt-tx-new-balance').innerText = Formatters.cop(associate.balance);
  openModal('modal-receipt');
}

// =============================================================================
// REPORTS DASHBOARD & PAGINATION ENGINE (MANAGEMENT INTELLIGENCE)
// =============================================================================

async function loadReportsDashboard() {
  await loadExecutiveKpis();
  selectReportTab('top');
}

async function loadExecutiveKpis() {
  try {
    const overview = await API.get('/api/reports/overview');
    document.getElementById('kpi-total-associates').innerText = overview.totalAssociates;
    document.getElementById('kpi-total-balance').innerText = Formatters.cop(overview.totalCooperativeBalance);
    document.getElementById('kpi-avg-balance').innerText = Formatters.cop(overview.averageBalance);

    document.getElementById('stat-total-associates').innerText = overview.totalAssociates;
    document.getElementById('stat-total-balance').innerText = Formatters.cop(overview.totalCooperativeBalance);
  } catch (err) {
    console.error('Error loading KPIs:', err);
  }
}

function updatePortalStats() {
  const totalAssoc = allAssociates.length;
  const totalBal = allAssociates.reduce((acc, curr) => acc + (curr.balance || 0), 0);
  const statAssoc = document.getElementById('stat-total-associates');
  const statBal = document.getElementById('stat-total-balance');
  if (statAssoc) statAssoc.innerText = totalAssoc;
  if (statBal) statBal.innerText = Formatters.cop(totalBal);
}

function selectReportTab(tabId) {
  document.querySelectorAll('.report-panel').forEach(p => p.classList.add('hidden'));
  document.querySelectorAll('[id^="rep-tab-"]').forEach(btn => btn.classList.remove('active'));

  const panel = document.getElementById(`rep-subview-${tabId}`);
  const btn = document.getElementById(`rep-tab-${tabId}`);

  if (panel) panel.classList.remove('hidden');
  if (btn) btn.classList.add('active');

  if (tabId === 'top') loadTopAssociatesReport();
  if (tabId === 'flow') initDateRangeReport();
  if (tabId === 'dormant') loadDormantAssociatesReport();
  if (tabId === 'largest') loadLargestTransactionsReport();
  if (tabId === 'movements') loadCashierMovementsReport();
}

// Reusable Paginated Report Engine
function renderReportPage(type) {
  const items = ReportState.data[type] || [];
  const page = ReportState.page[type] || 1;
  const total = items.length;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const startIdx = (page - 1) * PAGE_SIZE;
  const endIdx = Math.min(startIdx + PAGE_SIZE, total);
  const pageItems = items.slice(startIdx, endIdx);

  const tbody = document.getElementById(`rep-tbody-${type}`);
  const infoEl = document.getElementById(`${type}-pagination-info`);
  const pageNumEl = document.getElementById(`${type}-page-num`);
  const prevBtn = document.getElementById(`btn-prev-${type}`);
  const nextBtn = document.getElementById(`btn-next-${type}`);

  if (tbody) {
    if (total === 0) {
      tbody.innerHTML = `<tr><td colspan="6" class="text-center py-6 text-coop-text-tertiary">Sin registros disponibles</td></tr>`;
    } else {
      let html = '';
      if (type === 'dormant') {
        pageItems.forEach(item => {
          html += `
            <tr>
              <td class="font-mono font-bold text-coop-green-deep">${item.document}</td>
              <td class="font-semibold text-coop-text-primary">${item.name}</td>
              <td class="font-mono text-xs">${Formatters.date(item.registrationDate)}</td>
              <td class="text-center"><span class="ds-badge badge-withdraw">Inactivo (0 TX)</span></td>
            </tr>
          `;
        });
      } else if (type === 'largest') {
        pageItems.forEach((t, idx) => {
          const globalIdx = startIdx + idx + 1;
          const isDeposit = t.type === 0 || t.type === 'Deposit';
          const badgeClass = isDeposit ? 'badge-deposit' : 'badge-withdraw';
          const typeLabel = isDeposit ? 'Consignación' : 'Retiro';
          html += `
            <tr>
              <td class="text-center font-bold font-mono text-coop-gold-rich">#${globalIdx}</td>
              <td class="font-mono text-xs">${Formatters.date(t.date)}</td>
              <td class="text-center"><span class="ds-badge ${badgeClass}">${typeLabel}</span></td>
              <td class="font-semibold">${t.associateName}</td>
              <td class="num-cell font-bold text-coop-green-forest">${Formatters.cop(t.amount)}</td>
              <td class="num-cell text-coop-gold-tag-text">${t.commission > 0 ? Formatters.cop(t.commission) : '$0'}</td>
            </tr>
          `;
        });
      } else if (type === 'movements') {
        pageItems.forEach(item => {
          html += `
            <tr>
              <td class="font-mono font-bold text-coop-green-deep">${item.associateDocument}</td>
              <td class="font-semibold">${item.associateName}</td>
              <td class="text-center font-mono font-bold">${item.transactionCount}</td>
              <td class="num-cell text-coop-green-forest font-bold">${Formatters.cop(item.totalDeposited)}</td>
              <td class="num-cell text-red-700 font-bold">${Formatters.cop(item.totalWithdrawn + item.totalCommissions)}</td>
              <td class="num-cell text-coop-green-deep font-bold">${Formatters.cop(item.currentBalance)}</td>
            </tr>
          `;
        });
      }
      tbody.innerHTML = html;
    }
  }

  if (infoEl) {
    infoEl.innerText = total > 0 ? `Mostrando ${startIdx + 1} a ${endIdx} de ${total} registros` : 'Mostrando 0 de 0 registros';
  }
  if (pageNumEl) {
    pageNumEl.innerText = `${page} / ${totalPages}`;
  }
  if (prevBtn) prevBtn.disabled = page <= 1;
  if (nextBtn) nextBtn.disabled = page >= totalPages;
}

function prevReportPage(type) {
  if (ReportState.page[type] > 1) {
    ReportState.page[type]--;
    renderReportPage(type);
  }
}

function nextReportPage(type) {
  const total = (ReportState.data[type] || []).length;
  const totalPages = Math.ceil(total / PAGE_SIZE);
  if (ReportState.page[type] < totalPages) {
    ReportState.page[type]++;
    renderReportPage(type);
  }
}

async function loadTopAssociatesReport() {
  const tbody = document.getElementById('rep-tbody-top');
  if (!tbody) return;

  try {
    const data = await API.get('/api/reports/top-associates');
    if (!data || data.length === 0) {
      tbody.innerHTML = `<tr><td colspan="5" class="text-center py-6 text-coop-text-tertiary">No hay registros disponibles</td></tr>`;
      return;
    }

    let html = '';
    data.forEach((item, idx) => {
      html += `
        <tr>
          <td class="text-center font-bold text-coop-gold-rich font-mono">#${idx + 1}</td>
          <td class="font-mono font-bold">${item.document}</td>
          <td class="font-semibold text-coop-text-primary">${item.name}</td>
          <td class="text-center font-mono">${item.transactionCount}</td>
          <td class="num-cell text-coop-green-forest font-bold">${Formatters.cop(item.balance)}</td>
        </tr>
      `;
    });
    tbody.innerHTML = html;
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-red-600 py-4">Error al cargar ranking</td></tr>`;
  }
}

function initDateRangeReport() {
  const startInput = document.getElementById('flow-start-date');
  const endInput = document.getElementById('flow-end-date');
  if (!startInput || !endInput) return;

  if (!startInput.value) {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    startInput.value = d.toISOString().substring(0, 10);
  }
  if (!endInput.value) {
    endInput.value = new Date().toISOString().substring(0, 10);
  }
  loadDateRangeReport();
}

async function loadDateRangeReport() {
  const start = document.getElementById('flow-start-date')?.value;
  const end = document.getElementById('flow-end-date')?.value;
  if (!start || !end) return;

  try {
    const report = await API.get(`/api/reports/date-range-summary?start=${start}&end=${end}`);
    document.getElementById('flow-period-label').innerText = `Resumen del Período: ${start} al ${end}`;
    document.getElementById('flow-deposits-val').innerText = Formatters.cop(report.totalDeposited);
    document.getElementById('flow-deposits-count').innerText = `${report.depositCount} operaciones`;
    document.getElementById('flow-withdrawals-val').innerText = Formatters.cop(report.totalWithdrawn);
    document.getElementById('flow-withdrawals-count').innerText = `${report.withdrawalCount} operaciones`;
    document.getElementById('flow-commissions-val').innerText = Formatters.cop(report.totalCommissions);
    document.getElementById('flow-net-val').innerText = Formatters.cop(report.netDifference);
    document.getElementById('flow-total-txs').innerText = `${report.totalTransactions} transacciones totales`;
  } catch (err) {
    showToast(`Error al consultar flujo: ${err.message}`, 'error');
  }
}

async function loadDormantAssociatesReport() {
  try {
    const data = await API.get('/api/reports/dormant-associates');
    ReportState.data.dormant = data || [];
    ReportState.page.dormant = 1;
    renderReportPage('dormant');
  } catch (err) {
    const tbody = document.getElementById('rep-tbody-dormant');
    if (tbody) tbody.innerHTML = `<tr><td colspan="4" class="text-center text-red-600 py-4">Error al cargar asociados inactivos</td></tr>`;
  }
}

async function loadLargestTransactionsReport() {
  try {
    const data = await API.get('/api/reports/largest-transactions');
    ReportState.data.largest = data || [];
    ReportState.page.largest = 1;
    renderReportPage('largest');
  } catch (err) {
    const tbody = document.getElementById('rep-tbody-largest');
    if (tbody) tbody.innerHTML = `<tr><td colspan="6" class="text-center text-red-600 py-4">Error al cargar movimientos</td></tr>`;
  }
}

async function loadCashierMovementsReport() {
  try {
    const data = await API.get('/api/reports/cashier-movement');
    ReportState.data.movements = data || [];
    ReportState.page.movements = 1;
    renderReportPage('movements');
  } catch (err) {
    const tbody = document.getElementById('rep-tbody-movements');
    if (tbody) tbody.innerHTML = `<tr><td colspan="6" class="text-center text-red-600 py-4">Error al cargar resumen</td></tr>`;
  }
}

// Initialization on DOM ready
document.addEventListener('DOMContentLoaded', () => {
  fetchLiveTrm();
  updateSimulation();
  loadExecutiveKpis();
});
