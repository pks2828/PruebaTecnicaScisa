// wwwroot/js/pokemon-list.js
console.log("pokemon-list.js → cargado correctamente");


document.addEventListener('DOMContentLoaded', () => {
    // 1) Preparar datos para exportar a Excel
    const exportBtn = document.getElementById('exportExcelBtn');
    if (exportBtn) {
        exportBtn.addEventListener('click', prepareExcelRows);
    }

    // 2) Limpiar email al abrir modal individual
    const emailModal = document.getElementById('emailModal');
    if (emailModal) {
        // Esto solo aplica si sigues usando Bootstrap para el modal de correo.
        emailModal.addEventListener('show.bs.modal', () => {
            const inputEmail = document.getElementById('emailTo');
            if (inputEmail) {
                inputEmail.value = '';
            }
        });
    }

    // 3) Registrar clic en overlay para cerrar el modal de detalle
    const detailModal = document.getElementById('detailModal');
    if (detailModal) {
        detailModal.addEventListener('click', function (event) {
            // Si el clic fue exactamente sobre el overlay (no dentro del contenido), cerramos
            if (event.target === this) {
                closeDetailModal();
            }
        });
    }
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

    // Enviar el formulario manualmente después de llenar el input
    document.getElementById('exportForm').submit();
}



// Función que hace fetch al controlador para obtener el partial y abre el modal
function showDetail(id) {
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
            openDetailModal(); // En lugar de style.display, agregamos la clase .show
        })
        .catch(() => {
            alert('Error al cargar detalles.');
        });
}

// Agrega la clase .show para que tu CSS muestre el overlay y el container
function openDetailModal() {
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.classList.add('show');
        modal.setAttribute('aria-hidden', 'false');
    }
}

// Quita la clase .show para ocultar el modal
function closeDetailModal() {
    const modal = document.getElementById('detailModal');
    if (modal) {
        modal.classList.remove('show');
        modal.setAttribute('aria-hidden', 'true');
    }
}

