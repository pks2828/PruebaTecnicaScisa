// wwwroot/js/pokemon-list.js
console.log("pokemon-list.js → cargado correctamente");

document.addEventListener('DOMContentLoaded', () => {
    console.log("DOM cargado completamente");

    // 1) Preparar datos para exportar a Excel
    const exportBtn = document.getElementById('exportExcelBtn');
    if (exportBtn) {
        exportBtn.addEventListener('click', prepareExcelRows);
        console.log("Event listener agregado al botón de exportar");
    }

    // 2) Verificar que los modales existen
    const detailModal = document.getElementById('detailModal');
    const emailModal = document.getElementById('emailModal');
    const bulkEmailModal = document.getElementById('bulkEmailModal');

    console.log("Modal detalle encontrado:", !!detailModal);
    console.log("Modal email encontrado:", !!emailModal);
    console.log("Modal bulk email encontrado:", !!bulkEmailModal);

    // 3) Registrar clic en overlay para cerrar modales
    if (detailModal) {
        detailModal.addEventListener('click', function (event) {
            if (event.target === this) {
                closeDetailModal();
            }
        });
    }

    if (emailModal) {
        emailModal.addEventListener('click', function (event) {
            if (event.target === this) {
                closeEmailModal();
            }
        });
    }

    if (bulkEmailModal) {
        bulkEmailModal.addEventListener('click', function (event) {
            if (event.target === this) {
                closeBulkEmailModal();
            }
        });
    }

    // 4) Cerrar modales con tecla Escape
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeDetailModal();
            closeEmailModal();
            closeBulkEmailModal();
        }
    });
});

// Función para serializar filas visibles y guardarlas en el campo hidden para Excel
function prepareExcelRows() {
    const rows = [];
    const pokemonRows = document.querySelectorAll('tbody tr');

    pokemonRows.forEach(row => {
        const btnDetalle = row.querySelector('button[onclick^="showDetail"]');
        let id = null;
        if (btnDetalle) {
            const match = btnDetalle.getAttribute('onclick').match(/\d+/);
            if (match) {
                id = parseInt(match[0]);
            }
        }

        const nameCell = row.querySelector('td:nth-child(2)');
        const speciesCell = row.querySelector('td:nth-child(3)');

        const name = nameCell ? nameCell.textContent.trim() : '';
        const species = speciesCell ? speciesCell.textContent.trim() : '';

        if (id !== null) {
            rows.push({ Id: id, Name: name, Species: species });
        }
    });

    const hiddenField = document.getElementById('excelRowsJson');
    if (hiddenField) {
        hiddenField.value = JSON.stringify(rows);
    }

    document.getElementById('exportForm').submit();
}

// ========== FUNCIONES PARA MODAL DE DETALLE ==========
function showDetail(id) {
    console.log("showDetail llamado con ID:", id);
    fetch(`/Pokemon/Detail?id=${id}`)
        .then(res => {
            if (!res.ok) throw new Error('Error en la respuesta');
            return res.text();
        })
        .then(html => {
            const detailBody = document.getElementById('detailModalBody');
            if (detailBody) {
                detailBody.innerHTML = html;
            }
            openDetailModal();
        })
        .catch((error) => {
            console.error('Error al cargar detalles:', error);
            alert('Error al cargar detalles.');
        });
}

function openDetailModal() {
    console.log("openDetailModal llamado");
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.classList.add('active');
        modal.setAttribute('aria-hidden', 'false');
        console.log("Modal de detalle abierto (clase .active añadida)");
    } else {
        console.error("Modal de detalle no encontrado");
    }
}

function closeDetailModal() {
    console.log("closeDetailModal llamado");
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.classList.remove('active');
        modal.setAttribute('aria-hidden', 'true');
        console.log("Modal de detalle cerrado (clase .active removida)");
    }
}


// ========== FUNCIONES PARA MODAL DE CORREO MASIVO ==========
function openBulkEmailModal() {
    console.log("openBulkEmailModal llamado");

    const emailListField = document.getElementById('emailList');
    if (emailListField) {
        emailListField.value = '';
        console.log("Campo emailList limpiado");
    }

    const modal = document.getElementById('bulkEmailModal');
    if (modal) {
        modal.classList.add('active');
        modal.setAttribute('aria-hidden', 'false');
        console.log("Modal bulk email abierto (clase .active añadida)");
    } else {
        console.error("Modal bulkEmailModal no encontrado");
    }
}

function closeBulkEmailModal() {
    console.log("closeBulkEmailModal llamado");
    const modal = document.getElementById('bulkEmailModal');
    if (modal) {
        modal.classList.remove('active');
        modal.setAttribute('aria-hidden', 'true');
        console.log("Modal bulk email cerrado (clase .active removida)");
    } else {
        console.error("Modal bulkEmailModal no encontrado para cerrar");
    }
}