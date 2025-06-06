// wwwroot/js/index.js
console.log("index.js → cargado correctamente");

document.addEventListener('DOMContentLoaded', () => {
    console.log("DOM cargado completamente");

    // ========== MANEJO DE ALERTAS ==========
    initializeAlerts();

    // 1) Preparar exportación a Excel
    const exportBtn = document.getElementById('exportExcelBtn');
    exportBtn?.addEventListener('click', prepareExcelRows);

    // 2) Referencias a modales
    const modals = {
        detailModal: document.getElementById('detailModal'),
        emailModal: document.getElementById('emailModal'),
        bulkEmailModal: document.getElementById('bulkEmailModal')
    };

    Object.entries(modals).forEach(([id, modal]) => {
        console.log(`Modal ${id} encontrado:`, !!modal);
        // 3) Cerrar al hacer clic fuera del contenido
        modal?.addEventListener('click', (event) => {
            if (event.target === modal) closeModal(id);
        });
    });

    // 4) Cerrar con tecla Escape
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeModal('detailModal');
            closeModal('emailModal');
            closeModal('bulkEmailModal');
        }
    });
});

// ========== MANEJO DE ALERTAS ==========
function initializeAlerts() {
    console.log("Inicializando alertas...");

    // Encontrar todas las alertas
    const alerts = document.querySelectorAll('.alert');

    alerts.forEach(alert => {
        console.log("Alerta encontrada:", alert);

        // 1) Configurar botón de cerrar manual
        const closeBtn = alert.querySelector('.btn-close, [data-bs-dismiss="alert"]');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                console.log("Cerrando alerta manualmente");
                closeAlert(alert);
            });
        }

        // 2) Auto-cerrar después de 5 segundos
        setTimeout(() => {
            console.log("Auto-cerrando alerta después de 5 segundos");
            closeAlert(alert);
        }, 5000);
    });
}

function closeAlert(alertElement) {
    if (!alertElement) return;

    // Agregar clase de fade out si no existe
    if (!alertElement.classList.contains('fade')) {
        alertElement.classList.add('fade');
    }

    // Remover clase 'show' para activar la transición
    alertElement.classList.remove('show');

    // Esperar a que termine la transición antes de remover del DOM
    setTimeout(() => {
        if (alertElement.parentNode) {
            alertElement.remove();
            console.log("Alerta removida del DOM");
        }
    }, 150); // Bootstrap usa 150ms para la transición
}

// Función alternativa para cerrar inmediatamente sin transición
function closeAlertImmediately(alertElement) {
    if (alertElement && alertElement.parentNode) {
        alertElement.remove();
        console.log("Alerta removida inmediatamente");
    }
}

// ========== EXPORTACIÓN A EXCEL ==========
function prepareExcelRows() {
    const rows = [];
    const pokemonRows = document.querySelectorAll('tbody tr');

    pokemonRows.forEach(row => {
        const btnDetalle = row.querySelector('button[onclick^="showDetail"]');
        const match = btnDetalle?.getAttribute('onclick')?.match(/\d+/);
        const id = match ? parseInt(match[0]) : null;
        const name = row.querySelector('td:nth-child(2)')?.textContent.trim() || '';
        const types = row.querySelector('td:nth-child(3)')?.textContent.trim() || '';

        if (id !== null) {
            rows.push({ Id: id, Name: name, Types: types });
        }
    });

    const hiddenField = document.getElementById('excelRowsJson');
    if (hiddenField) hiddenField.value = JSON.stringify(rows);
    document.getElementById('exportForm')?.submit();
}

// ========== MODALES GENÉRICOS ==========
function openModal(modalId, onOpenCallback) {
    const modal = document.getElementById(modalId);
    if (!modal) return console.error(`Modal ${modalId} no encontrado`);

    modal.classList.add('active');
    modal.setAttribute('aria-hidden', 'false');
    console.log(`Modal ${modalId} abierto`);

    if (onOpenCallback) onOpenCallback();
}

function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return console.error(`Modal ${modalId} no encontrado`);

    modal.classList.remove('active');
    modal.setAttribute('aria-hidden', 'true');
    console.log(`Modal ${modalId} cerrado`);
}

// ========== MODAL DETALLE ==========
function showDetail(id) {
    console.log("showDetail llamado con ID:", id);

    fetch(`/Pokemon/Detail?id=${id}`)
        .then(res => {
            if (!res.ok) throw new Error('Error en la respuesta');
            return res.text();
        })
        .then(html => {
            const detailBody = document.getElementById('detailModalBody');
            if (detailBody) detailBody.innerHTML = html;
            openModal('detailModal');
        })
        .catch(error => {
            console.error('Error al cargar detalles:', error);
            alert('Error al cargar detalles.');
        });
}

// ========== MODAL BULK EMAIL ==========
function openBulkEmailModal() {
    openModal('bulkEmailModal', () => {
        const emailListField = document.getElementById('emailList');
        if (emailListField) emailListField.value = '';
    });
}

function closeBulkEmailModal() {
    closeModal('bulkEmailModal');
}

function closeDetailModal() {
    closeModal('detailModal');
}