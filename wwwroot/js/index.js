// wwwroot/js/index.js

/**
 * Archivo principal de JavaScript que maneja alertas, exportación a Excel y modales.
 * Este script se ejecuta una vez que el DOM se ha cargado completamente.
 */

document.addEventListener('DOMContentLoaded', () => {
    console.log('index.js → DOM cargado completamente');

    // Inicializar manejo de alertas
    initializeAlerts();

    // Preparar exportación a Excel
    const exportBtn = document.getElementById('exportExcelBtn');
    if (exportBtn) {
        exportBtn.addEventListener('click', prepareExcelRows);
    } else {
        console.warn('Botón de exportación a Excel no encontrado');
    }

    // Referencias a los modales principales
    const modals = {
        detailModal: document.getElementById('detailModal'),
        emailModal: document.getElementById('emailModal'),
        bulkEmailModal: document.getElementById('bulkEmailModal')
    };

    // Configurar cierre de modal al hacer clic fuera de su contenido
    Object.entries(modals).forEach(([modalId, modalElement]) => {
        if (!modalElement) {
            console.warn(`Modal ${modalId} no encontrado`);
            return;
        }

        modalElement.addEventListener('click', (event) => {
            // Si el clic ocurrió directamente en el fondo del modal, cerrarlo
            if (event.target === modalElement) {
                closeModal(modalId);
            }
        });
    });

    // Cerrar modales con la tecla Escape
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeModal('detailModal');
            closeModal('emailModal');
            closeModal('bulkEmailModal');
        }
    });
});

/**
 * Inicializa el comportamiento de las alertas: cierre manual y auto-cierre.
 */
function initializeAlerts() {
    console.log('Inicializando alertas...');

    const alerts = document.querySelectorAll('.alert');
    alerts.forEach((alertElement) => {
        // Configurar cierre manual
        const closeBtn = alertElement.querySelector('.btn-close, [data-bs-dismiss="alert"]');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                closeAlert(alertElement);
            });
        }

        // Cerrar la alerta después de 5 segundos si aún existe en el DOM
        setTimeout(() => {
            if (document.body.contains(alertElement)) {
                closeAlert(alertElement);
            }
        }, 5000);
    });
}

/**
 * Cierra una alerta con transición (fade out) y luego la remueve del DOM.
 * @param {HTMLElement} alertElement - Elemento de la alerta a cerrar.
 */
function closeAlert(alertElement) {
    if (!alertElement) return;

    if (!alertElement.classList.contains('fade')) {
        alertElement.classList.add('fade');
    }
    alertElement.classList.remove('show');

    // Esperar transición CSS (150ms) antes de remover del DOM
    setTimeout(() => {
        if (alertElement.parentNode) {
            alertElement.remove();
            console.log('Alerta removida del DOM');
        }
    }, 150);
}

/**
 * Remueve una alerta del DOM de manera inmediata, sin animación.
 * @param {HTMLElement} alertElement - Elemento de la alerta a cerrar.
 */
function closeAlertImmediately(alertElement) {
    if (alertElement && alertElement.parentNode) {
        alertElement.remove();
        console.log('Alerta removida inmediatamente');
    }
}

/**
 * Extrae filas de la tabla de Pokémons para enviarlas al servidor y generar un Excel.
 * Convierte cada <tr> en un objeto con Id, Name y Types, y envía el JSON en un campo oculto.
 */
function prepareExcelRows() {
    const rows = [];
    const pokemonRows = document.querySelectorAll('tbody tr');

    pokemonRows.forEach((row) => {
        const btnDetalle = row.querySelector('button[onclick^="showDetail"]');
        const onclickAttr = btnDetalle?.getAttribute('onclick') || '';
        const match = onclickAttr.match(/\d+/);
        const id = match ? parseInt(match[0], 10) : null;
        const nameCell = row.querySelector('td:nth-child(2)');
        const typesCell = row.querySelector('td:nth-child(3)');

        const name = nameCell ? nameCell.textContent.trim() : '';
        const types = typesCell ? typesCell.textContent.trim() : '';

        if (id !== null) {
            rows.push({ Id: id, Name: name, Types: types });
        }
    });

    const hiddenField = document.getElementById('excelRowsJson');
    if (hiddenField) {
        hiddenField.value = JSON.stringify(rows);
        const exportForm = document.getElementById('exportForm');
        if (exportForm) {
            exportForm.submit();
        } else {
            console.error('Formulario de exportación no encontrado');
        }
    } else {
        console.error('Campo oculto para filas de Excel no encontrado');
    }
}

/**
 * Abre un modal dado su ID, agregando clases y atributos ARIA correspondientes.
 * @param {string} modalId - Identificador del elemento modal.
 * @param {Function} [onOpenCallback] - Función opcional a ejecutar justo después de abrir.
 */
function openModal(modalId, onOpenCallback) {
    const modal = document.getElementById(modalId);
    if (!modal) {
        console.error(`Modal ${modalId} no encontrado`);
        return;
    }

    modal.classList.add('active');
    modal.setAttribute('aria-hidden', 'false');
    console.log(`Modal ${modalId} abierto`);

    if (typeof onOpenCallback === 'function') {
        onOpenCallback();
    }
}

/**
 * Cierra un modal dado su ID, removiendo clases y actualizando atributos ARIA.
 * @param {string} modalId - Identificador del elemento modal.
 */
function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) {
        console.error(`Modal ${modalId} no encontrado`);
        return;
    }

    modal.classList.remove('active');
    modal.setAttribute('aria-hidden', 'true');
    console.log(`Modal ${modalId} cerrado`);
}

/**
 * Solicita detalles de un Pokémon por ID al servidor y muestra el resultado en un modal.
 * @param {number|string} id - ID o nombre del Pokémon a consultar.
 */
function showDetail(id) {
    if (!id) {
        console.error('ID inválido para showDetail');
        return;
    }

    console.log('showDetail llamado con ID:', id);
    fetch(`/Pokemon/Detail?id=${encodeURIComponent(id)}`)
        .then((res) => {
            if (!res.ok) {
                throw new Error(`Respuesta no OK: ${res.status}`);
            }
            return res.text();
        })
        .then((html) => {
            const detailBody = document.getElementById('detailModalBody');
            if (detailBody) {
                detailBody.innerHTML = html;
                openModal('detailModal');
            } else {
                console.error('Elemento detailModalBody no encontrado');
            }
        })
        .catch((error) => {
            console.error('Error al cargar detalles:', error);
            alert('Error al cargar detalles.');
        });
}

/**
 * Abre el modal de envíos masivos de correo, limpiando previamente el campo de lista de correos.
 */
function openBulkEmailModal(pokemonName) {
    openModal('bulkEmailModal', () => {
        const emailListField = document.getElementById('emailList');
        if (emailListField) {
            emailListField.value = '';
        } else {
            console.warn('Campo de lista de correos no encontrado en bulkEmailModal');
        }

        const subjectField = document.getElementById('subject');
        if (subjectField && pokemonName) {
            subjectField.value = `Información sobre ${pokemonName}`;
        } else {
            console.warn('Campo de asunto no encontrado o nombre de Pokémon no proporcionado');
        }
    });
}


/**
 * Cierra el modal de envíos masivos de correo.
 */
function closeBulkEmailModal() {
    closeModal('bulkEmailModal');
}

/**
 * Cierra el modal de detalles de Pokémon.
 */
function closeDetailModal() {
    closeModal('detailModal');
}
