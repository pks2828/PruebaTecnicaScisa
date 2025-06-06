/**
 * Valida cada campo: subject, body, emailList.
 * - subject: longitud entre 5 y 100 caracteres.
 * - body: longitud entre 20 y 2000 caracteres.
 * - emailList: cada correo válida tenga ≥ 6 caracteres, contenga '@', pase regex,
 *   sin duplicados y máx. 50 correos.
 */
function validateBulkEmailForm() {
    // Recuperar valores
    const subjectField = document.getElementById('subject');
    const bodyField = document.getElementById('body');
    const emailListField = document.getElementById('emailList');

    const subject = subjectField.value.trim();
    const body = bodyField.value.trim();
    const rawEmails = emailListField.value.trim();

    // Limpiar mensajes anteriores
    clearError('subject');
    clearError('body');
    clearError('emailList');

    let isValid = true;

    // 1) Validación "Asunto"
    if (!subject) {
        showError('subject', 'El asunto es obligatorio.');
        isValid = false;
    } else if (subject.length < 5) {
        showError('subject', 'El asunto debe tener al menos 5 caracteres.');
        isValid = false;
    } else if (subject.length > 50) {
        showError('subject', 'El asunto no puede exceder 50 caracteres.');
        isValid = false;
    } else {
        const subjectPattern = /^[A-Za-z0-9ÁÉÍÓÚáéíóúÑñ¿?¡!()\-_:;,.\s]+$/;
        if (!subjectPattern.test(subject)) {
            showError('subject', 'El asunto contiene caracteres no permitidos.');
            isValid = false;
        }
    }

    // 2) Validación "Cuerpo"
    if (!body) {
        showError('body', 'El cuerpo del correo es obligatorio.');
        isValid = false;
    } else if (body.length < 5) {
        showError('body', 'El cuerpo del correo debe tener al menos 5 caracteres.');
        isValid = false;
    } else if (body.length > 100) {
        showError('body', 'El cuerpo del correo no puede exceder 100 caracteres.');
        isValid = false;
    }

    // 3) Validación "emailList"
    if (!rawEmails) {
        showError('emailList', 'Debes escribir al menos un correo.');
        isValid = false;
    } else {
        // Separar por comas y limpiar espacios
        const emailArray = rawEmails
            .split(',')
            .map(e => e.trim())
            .filter(e => e !== '');

        if (emailArray.length === 0) {
            showError('emailList', 'No se detectaron correos válidos.');
            isValid = false;
        } else if (emailArray.length > 50) {
            showError('emailList', 'No puedes enviar más de 50 correos a la vez.');
            isValid = false;
        } else {
            // Regex básico para validar formato de email
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const seen = new Set();

            for (let i = 0; i < emailArray.length; i++) {
                const email = emailArray[i];

                // 3.1) Longitud mínima 5
                if (email.length < 5) {
                    showError('emailList', `El correo "${email}" debe tener al menos 5 caracteres.`);
                    isValid = false;
                    break;
                }

                // 3.2) Debe contener '@' y cumplir regex
                if (!emailRegex.test(email)) {
                    showError('emailList', `El correo "${email}" no tiene formato válido.`);
                    isValid = false;
                    break;
                }

                // 3.3) Sin duplicados (ignore mayúsculas/minúsculas)
                const lower = email.toLowerCase();
                if (seen.has(lower)) {
                    showError('emailList', `El correo "${email}" está duplicado.`);
                    isValid = false;
                    break;
                }
                seen.add(lower);
            }
        }
    }

    // Si hay algún error, impedimos el envío
    return isValid;
}

/**
 * Limpia mensaje de error en el campo dado (idField = 'subject' | 'body' | 'emailList')
 */
function clearError(idField) {
    const input = document.getElementById(idField);
    const errorDiv = document.getElementById(`error-${idField}`);
    if (input) input.classList.remove('is-invalid');
    if (errorDiv) {
        errorDiv.innerText = '';
        errorDiv.style.display = 'none';
    }
}

/**
 * Muestra mensaje de error para el campo dado
 */
function showError(idField, message) {
    const input = document.getElementById(idField);
    const errorDiv = document.getElementById(`error-${idField}`);
    if (input) {
        input.classList.add('is-invalid');
    }
    if (errorDiv) {
        errorDiv.innerText = message;
        errorDiv.style.display = 'block';
    }
}
