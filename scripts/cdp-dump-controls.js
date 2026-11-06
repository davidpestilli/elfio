// Dump read-only dos controles de formulario da aba atual (via CDP).
// Usado por scripts/cdp-probe.ps1 para capturar seletores reais antes de montar o fluxo.
(() => {
  const esc = (s) => (s || '').toString().replace(/\s+/g, ' ').trim().slice(0, 140);

  const visible = (el) => {
    if (!el || !el.getBoundingClientRect) return false;
    const r = el.getBoundingClientRect();
    if (r.width <= 0 || r.height <= 0) return false;
    const st = getComputedStyle(el);
    return st.visibility !== 'hidden' && st.display !== 'none' && st.opacity !== '0';
  };

  const labelOf = (el) => {
    const parts = [];
    if (el.labels && el.labels.length) parts.push(esc(Array.from(el.labels).map((l) => l.innerText).join(' ')));
    const aria = el.getAttribute('aria-label');
    if (aria) parts.push(esc(aria));
    const title = el.getAttribute('title');
    if (title) parts.push(esc(title));
    const cell = el.closest('td, th');
    if (cell) {
      const row = cell.parentElement;
      const first = row && row.querySelector('td, th');
      if (first && first !== cell) parts.push(esc(first.innerText));
    }
    const tr = el.closest('tr');
    if (!cell && tr) {
      const first = tr.querySelector('td, th');
      if (first) parts.push(esc(first.innerText));
    }
    return Array.from(new Set(parts.filter(Boolean))).join(' | ');
  };

  // Seletor: id > name > caminho estrutural curto. Suficiente para montar o flow.json.
  const cssPath = (el) => {
    if (el.id) return '#' + el.id;
    const name = el.getAttribute('name');
    if (name) return el.tagName.toLowerCase() + '[name="' + name + '"]';
    const parts = [];
    let cur = el;
    let depth = 0;
    while (cur && cur.nodeType === 1 && depth < 6) {
      let part = cur.tagName.toLowerCase();
      const parent = cur.parentElement;
      if (parent) {
        const idx = Array.prototype.indexOf.call(parent.children, cur) + 1;
        part += ':nth-child(' + idx + ')';
      }
      parts.unshift(part);
      if (cur.id) { parts[0] = '#' + cur.id; break; }
      cur = cur.parentElement;
      depth++;
    }
    return parts.join(' > ');
  };

  const info = (el) => ({
    tag: el.tagName.toLowerCase(),
    type: el.getAttribute('type') || '',
    id: el.id || '',
    name: el.getAttribute('name') || '',
    selector: cssPath(el),
    label: labelOf(el),
    visible: visible(el),
    disabled: !!el.disabled,
    value: el.tagName === 'SELECT' ? el.value : (el.value || '').toString().slice(0, 60),
  });

  const selects = Array.from(document.querySelectorAll('select')).map((el) => {
    const d = info(el);
    d.options = Array.from(el.options).map((o, i) => ({ i: i, value: o.value, text: esc(o.text), selected: o.selected }));
    d.optionCount = el.options.length;
    return d;
  });

  const inputs = Array.from(document.querySelectorAll('input, textarea')).map(info);

  const buttons = Array.from(document.querySelectorAll('input[type=submit], input[type=button], input[type=reset], button, a.botao, a[onclick]'))
    .map((el) => {
      const d = info(el);
      d.text = esc(el.innerText || el.value || '');
      d.onclick = esc(el.getAttribute('onclick'));
      return d;
    })
    .filter((d) => d.visible || d.id || d.name);

  const headings = Array.from(document.querySelectorAll('#divInfraBarraLocalizacao h1, #divInfraBarraLocalizacao h2, h1, h2, .infraTitulo, legend'))
    .map((el) => ({ sel: cssPath(el), text: esc(el.innerText) }))
    .filter((d) => d.text)
    .slice(0, 12);

  const messages = Array.from(document.querySelectorAll('.infraMensagem, .infraErro, .infraAviso, [role=alert]'))
    .map((el) => ({ sel: cssPath(el), text: esc(el.innerText) }))
    .slice(0, 10);

  return JSON.stringify({
    url: location.href,
    title: document.title,
    headings: headings,
    messages: messages,
    selects: selects,
    inputs: inputs,
    buttons: buttons,
  });
})()
