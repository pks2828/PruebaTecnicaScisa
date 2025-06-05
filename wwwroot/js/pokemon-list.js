// pokemon-list.js

document.addEventListener('DOMContentLoaded', () => {
    // Preparar datos para exportar a Excel
    const exportBtn = document.getElementById('exportExcelBtn');
    exportBtn?.addEventListener('click', prepareExcelRows);

    // Limpiar email al abrir modal individual
    const emailModal = document.getElementById('emailModal');
    if (emailModal) {
        emailModal.addEventListener('show.bs.modal', () => {
            document.getElementById('emailTo').value = '';
        });
    }
});

// Función para serializar filas visibles para exportar a Excel
function prepareExcelRows() {
    const rows = [];
    const pokemonRows = document.querySelectorAll('tbody tr');

    pokemonRows.forEach(row => {
        const id = row.querySelector('button[onclick^="showDetail"]').getAttribute('onclick').match(/\d+/)[0];
        const name = row.querySelector('td:nth-child(2)').textContent.trim();
        const species = row.querySelector('td:nth-child(3)').textContent.trim();

        rows.push({ Id: parseInt(id), Name: name, Species: species });
    });

    document.getElementById('excelRowsJson').value = JSON.stringify(rows);
}

// Cargar detalle en modal con fetch
function showDetail(id) {
    fetch(`/Pokemon/Detail?id=${id}`)
        .then(res => res.text())
        .then(html => {
            document.getElementById('detailModalBody').innerHTML = html;
            new bootstrap.Modal(document.getElementById('detailModal')).show();
        })
        .catch(() => alert('Error al cargar detalles.'));
}

// Exportar la función para que esté disponible en el scope global (para onclick inline)
window.showDetail = showDetail;
window.prepareExcelRows = prepareExcelRows;
