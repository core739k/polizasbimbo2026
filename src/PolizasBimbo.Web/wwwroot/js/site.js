(function () {
    'use strict';

    const form = {
        idColaborador: () => document.getElementById('fIDColaborador'),
        email: () => document.getElementById('fEmail'),
        telefono: () => document.getElementById('fTel'),
        resultados: () => document.getElementById('divForPol')
    };

    document.addEventListener('DOMContentLoaded', function () {
        const btnBuscar = document.getElementById('btnBuscar');
        const btnLimpiar = document.getElementById('btnLimpiar');
        if (btnBuscar) btnBuscar.addEventListener('click', onBuscar);
        if (btnLimpiar) btnLimpiar.addEventListener('click', limpiarControles);

        const id = form.idColaborador();
        if (id) id.addEventListener('keypress', soloNumeros);
        const tel = form.telefono();
        if (tel) tel.addEventListener('keypress', soloNumeros);

        document.querySelectorAll('.lcase').forEach(el => {
            el.addEventListener('blur', e => { e.target.value = (e.target.value || '').toLowerCase(); });
        });
    });

    function soloNumeros(evt) {
        const cc = evt.which || evt.keyCode;
        if (cc > 31 && (cc < 48 || cc > 57)) {
            evt.preventDefault();
            return false;
        }
        return true;
    }

    function validarEmail(valor) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(valor || '');
    }

    function validarTelefono(valor) {
        return (valor || '').replace(/\D/g, '').length === 10;
    }

    function validarIdColaborador(valor) {
        const n = parseInt(valor, 10);
        return Number.isInteger(n) && n > 0;
    }

    function validarFormulario() {
        let ok = true;
        const id = form.idColaborador();
        const e = form.email();
        const t = form.telefono();

        [id, e, t].forEach(x => { if (x) x.classList.remove('GlowError'); });

        if (!validarIdColaborador(id.value)) { id.classList.add('GlowError'); ok = false; }
        if (!validarEmail(e.value)) { e.classList.add('GlowError'); ok = false; }
        if (!validarTelefono(t.value)) { t.classList.add('GlowError'); ok = false; }

        if (!ok) {
            Swal.fire({ icon: 'error', title: 'Revisa los campos marcados en rojo' });
        }
        return ok;
    }

    function onBuscar() {
        if (!validarFormulario()) return;
        const idColaborador = parseInt(form.idColaborador().value, 10);
        const email = form.email().value.trim();
        const telefono = form.telefono().value.trim();

        Swal.fire({ title: 'Consultando...', didOpen: () => Swal.showLoading(), allowOutsideClick: false });

        fetch('/api/search', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify({ idColaborador, email, telefono })
        })
        .then(r => r.json().then(body => ({ ok: r.ok, body: body })))
        .then(({ ok, body }) => {
            Swal.close();
            if (!ok) {
                Swal.fire({ icon: 'error', title: 'No se pudo consultar', text: body.error || '' });
                return;
            }
            renderResultados(body.results || []);
        })
        .catch(() => {
            Swal.close();
            Swal.fire({ icon: 'error', title: 'Error de red' });
        });
    }

    function renderResultados(results) {
        const cont = form.resultados();
        cont.innerHTML = '';

        if (results.length === 0) {
            Swal.fire({ icon: 'info', title: 'Sin resultados', text: 'No se encontraron pólizas para el ID indicado.' });
            return;
        }

        const ul = document.createElement('ul');
        ul.className = 'download-list';
        results.forEach(r => {
            const li = document.createElement('li');
            const a = document.createElement('a');
            a.className = 'download-btn';
            a.href = '/d/' + encodeURIComponent(r.downloadToken);
            a.textContent = r.displayName;
            a.target = '_blank';
            a.rel = 'noopener';
            li.appendChild(a);
            ul.appendChild(li);
        });
        cont.appendChild(ul);
    }

    function limpiarControles() {
        const id = form.idColaborador();
        const e = form.email();
        const t = form.telefono();
        if (id) id.value = '';
        if (e) e.value = '';
        if (t) t.value = '';
        const r = form.resultados();
        if (r) r.innerHTML = '';
    }
})();
