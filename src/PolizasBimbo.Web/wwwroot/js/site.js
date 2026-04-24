(function () {
    'use strict';

    const form = {
        nombre: () => document.getElementById('fNombre'),
        email: () => document.getElementById('fEmail'),
        telefono: () => document.getElementById('fTel'),
        resultados: () => document.getElementById('divForPol')
    };

    document.addEventListener('DOMContentLoaded', function () {
        const btnBuscar = document.getElementById('btnBuscar');
        const btnLimpiar = document.getElementById('btnLimpiar');
        if (btnBuscar) btnBuscar.addEventListener('click', onBuscar);
        if (btnLimpiar) btnLimpiar.addEventListener('click', limpiarControles);

        const tel = form.telefono();
        if (tel) tel.addEventListener('keypress', soloNumeros);

        document.querySelectorAll('.ucase').forEach(el => {
            el.addEventListener('blur', e => { e.target.value = (e.target.value || '').toUpperCase(); });
        });
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

    function validarFormulario() {
        let ok = true;
        const n = form.nombre();
        const e = form.email();
        const t = form.telefono();

        [n, e, t].forEach(x => { if (x) x.classList.remove('GlowError'); });

        if (!n.value || n.value.trim().length < 5) { n.classList.add('GlowError'); ok = false; }
        if (!validarEmail(e.value)) { e.classList.add('GlowError'); ok = false; }
        if (!validarTelefono(t.value)) { t.classList.add('GlowError'); ok = false; }

        if (!ok) {
            Swal.fire({ icon: 'error', title: 'Revisa los campos marcados en rojo' });
        }
        return ok;
    }

    function getGeoSafe() {
        try {
            const pais = (typeof geotargetly_country_name === 'function')
                ? (geotargetly_country_name() || 'Desconocido') : 'Desconocido';
            const ciudad = (typeof geotargetly_city_name === 'function')
                ? (geotargetly_city_name() || 'Desconocido') : 'Desconocido';
            return { pais, ciudad };
        } catch (_) {
            return { pais: 'Desconocido', ciudad: 'Desconocido' };
        }
    }

    function onBuscar() {
        if (!validarFormulario()) return;
        const nombre = form.nombre().value.trim();

        Swal.fire({ title: 'Consultando...', didOpen: () => Swal.showLoading(), allowOutsideClick: false });

        fetch('/api/search', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify({ nombre: nombre })
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
            Swal.fire({ icon: 'info', title: 'Sin resultados', text: 'No se encontraron pólizas para el nombre indicado.' });
            return;
        }

        const ul = document.createElement('ul');
        ul.className = 'download-list';
        results.forEach(r => {
            const li = document.createElement('li');
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'download-btn';
            btn.textContent = r.fileName;
            btn.addEventListener('click', () => descargar(r.downloadToken, r.fileName));
            li.appendChild(btn);
            ul.appendChild(li);
        });
        cont.appendChild(ul);
    }

    async function descargar(token, fileName) {
        const geo = getGeoSafe();
        const body = new URLSearchParams({
            email: form.email().value.trim(),
            telefono: form.telefono().value.trim(),
            pais: geo.pais,
            ciudad: geo.ciudad
        });

        Swal.fire({ title: 'Descargando...', didOpen: () => Swal.showLoading(), allowOutsideClick: false });

        try {
            const res = await fetch('/d/' + encodeURIComponent(token), {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body
            });
            if (!res.ok) {
                const msg = await res.json().catch(() => ({ error: 'No se pudo descargar.' }));
                Swal.fire({ icon: 'error', title: 'No se pudo descargar', text: msg.error || '' });
                return;
            }
            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
            Swal.close();
        } catch (_) {
            Swal.fire({ icon: 'error', title: 'Error de red' });
        }
    }

    function limpiarControles() {
        const n = form.nombre();
        const e = form.email();
        const t = form.telefono();
        if (n) n.value = '';
        if (e) e.value = '';
        if (t) t.value = '';
        const r = form.resultados();
        if (r) r.innerHTML = '';
    }
})();
