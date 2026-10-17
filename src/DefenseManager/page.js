// Defense Manager's web half. Runs in Root.html, the document every game page iframe sits in. Draws the panel over
// all of them, styled like the other mod panels with the game's own colours and fonts, and puts a button in the HUD's
// tool column (bottom left, above the phone and bag buttons), built like the game's own buttons there so it reads as
// part of the game. While the game's Layout screen is up it also puts a bar of buttons at the top of that screen,
// built like Layout's own buttons. Talks to the plugin through vuplex.postMessage("slmod|defensemanager|<verb>") and is
// driven by window.__sldefense.
(function () {
  if (window.__sldefense) return;

  var PREFIX = 'slmod|defensemanager|';
  var HUD_CLASS = 'dm-hud-btn';
  var LAYOUT_ID = 'dm-layout';
  var LAYOUT_STYLE_ID = 'dm-layout-style';
  var BUSY_TIMEOUT_MS = 5000;
  var TOAST_MS = 3500;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A shield, drawn on the 24-unit grid the game's HUD icons use.
  var SHIELD_PATH = 'M12 2l8 3v6c0 5.25-3.4 9.74-8 11-4.6-1.26-8-5.75-8-11V5l8-3zm0 2.14L6 6.39V11c0 4.1 2.5 7.67 6 8.9 3.5-1.23 6-4.8 6-8.9V6.39l-6-2.25zM11 8h2v3h3v2h-3v3h-2v-3H8v-2h3z';
  var STATE_WORD = { broken: 'broken', damaged: 'damaged', intact: 'intact' };
  var QUEUED_WORD = { repair: 'repairQueued', remove: 'removeQueued' };
  // The plugin hands over the words in the game's language before every call (Texts.cs); English stands in until then.
  var ENGLISH = {
    broken: 'Broken', damaged: 'Damaged', intact: 'Intact', repairQueued: 'Repair queued', removeQueued: 'Removal queued',
    repairAll: 'Repair all ({0})', clearAll: 'Clear all broken ({0})', materials: 'Materials free / needed:',
    empty: 'No traps or barricades outside your home yet.', repairCost: 'Repair:', short: '({0} short)', powerOn: 'Power on',
    powerOff: 'Power off', mode: 'Mode: {0}', unmanaged: 'Not switched while broken or not working', thisOne: 'This one: {0}',
    every: 'Every {0}: {1}', repair: 'Repair', remove: 'Remove', escClose: 'Esc to close',
    tip: 'Repair or remove your outdoor defenses', defenseList: 'Defense list', defenseListKey: 'Defense list ({0})'
  };

  var state = { data: emptyData(), isOpen: false, isLive: false, key: '', layout: null };
  var ui = null;
  var toastTimer = 0;
  var isBusy = false;
  var busyTimer = 0;
  var layoutVersion = 0;
  var hasReportedPageAccess = false;
  var hasReportedHudPlaced = false;
  var hasReportedLayoutPlaced = false;

  function emptyData() {
    return { note: null, locked: null, repairAll: 0, clearAll: 0, materials: [], defenses: [] };
  }

  function say(key) {
    var words = window.__sldefenseWords || ENGLISH;
    var text = words[key] || ENGLISH[key] || key;

    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));

    return text;
  }

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  // One press at a time: a double click would otherwise ask twice. Every press is answered by a status line, which
  // frees the buttons; the timer frees them anyway if that answer never comes.
  function sendAction(message) {
    if (isBusy) return;

    isBusy = true;
    clearTimeout(busyTimer);
    busyTimer = setTimeout(clearBusy, BUSY_TIMEOUT_MS);
    send(message);
  }

  function clearBusy() {
    isBusy = false;
    clearTimeout(busyTimer);
  }

  function el(tag, className, text) {
    var node = document.createElement(tag);

    if (className) node.className = className;
    if (text != null) node.textContent = text;

    return node;
  }

  function button(text, action, args, isEnabled, extraClass) {
    var node = el('button', 'dm-btn' + (extraClass ? ' ' + extraClass : ''), text);
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));
    if (!isEnabled) node.disabled = true;

    return node;
  }

  var CSS = [
    '#dm-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.4);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#eee;user-select:none}',
    '#dm-root *{box-sizing:border-box}',
    '.dm-plate{width:min(1040px,92vw);height:min(800px,86vh);display:flex;flex-direction:column;gap:12px;padding:18px 20px;border-radius:10px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(30,30,33,.92),rgba(18,18,20,.94));box-shadow:0 10px 40px rgba(0,0,0,.6)}',
    '.dm-head{display:flex;align-items:baseline;gap:14px}',
    '.dm-title{font-size:18px;font-weight:600;letter-spacing:1px;text-shadow:0 2px 6px rgba(0,0,0,.6)}',
    '.dm-sub{font-size:13px;color:rgba(238,238,238,.6);flex:1}',
    '.dm-btn.dm-x{width:30px;height:30px;padding:0;font-size:18px;line-height:28px}',
    '.dm-bar{display:flex;align-items:center;gap:10px;flex-wrap:wrap}',
    '.dm-mats{flex:1;display:flex;align-items:center;justify-content:flex-end;gap:8px;flex-wrap:wrap;font-size:13px;color:rgba(238,238,238,.7)}',
    '.dm-chip{display:flex;align-items:center;gap:6px;padding:3px 8px 3px 3px;border-radius:7px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);color:#eee;white-space:nowrap}',
    '.dm-chip.dm-short{border-color:rgba(229,57,53,.7)}',
    '.dm-num{font-weight:600;color:#EBA308}',
    '.dm-short .dm-num,.dm-miss{color:#ff8a80}',
    '.dm-lock{padding:8px 12px;border-radius:7px;border:1px solid rgba(255,193,7,.75);background:rgba(255,193,7,.12);color:#EBA308;font-size:14px}',
    '.dm-list{flex:1;min-height:0;overflow-y:auto;display:flex;flex-direction:column;gap:6px;padding:6px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);border-radius:8px}',
    '.dm-row{display:flex;align-items:center;gap:12px;padding:8px 10px;border-radius:7px;background:rgba(255,255,255,.04);border:1px solid transparent}',
    '.dm-row:hover{background:rgba(255,255,255,.06)}',
    '.dm-row.dm-broken{border-color:rgba(229,57,53,.45)}',
    '.dm-main{flex:1;min-width:0;display:flex;flex-direction:column;gap:5px}',
    '.dm-top{display:flex;align-items:center;gap:8px}',
    '.dm-name{font-size:14px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.dm-badge{flex:none;font-size:11px;padding:2px 7px;border-radius:5px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.8)}',
    '.dm-badge-broken{background:rgba(229,57,53,.28);color:#ff8a80}',
    '.dm-badge-damaged{background:rgba(255,193,7,.24);color:#EBA308}',
    '.dm-badge-queued{background:rgba(124,179,66,.24);color:#aed581}',
    '.dm-cond{display:flex;align-items:center;gap:10px;font-size:12px;color:rgba(238,238,238,.7)}',
    '.dm-track{width:180px;height:8px;border-radius:4px;background:rgba(255,255,255,.1);overflow:hidden}',
    '.dm-fill{height:100%;border-radius:4px;background:#7cb342}',
    '.dm-damaged .dm-fill{background:#EBA308}',
    '.dm-broken .dm-fill{background:#e53935}',
    '.dm-cost{display:flex;align-items:center;gap:6px;flex-wrap:wrap;font-size:12px;color:rgba(238,238,238,.7)}',
    '.dm-power{display:flex;align-items:center;gap:8px;flex-wrap:wrap;font-size:12px;color:rgba(238,238,238,.7)}',
    '.dm-power .dm-btn{font-size:12px;padding:4px 9px}',
    '.dm-badge-on{background:rgba(124,179,66,.24);color:#aed581}',
    '.dm-badge-off{background:rgba(255,255,255,.08);color:rgba(238,238,238,.6)}',
    '.dm-acts{flex:none;width:250px;display:flex;flex-direction:column;align-items:flex-end;gap:5px}',
    '.dm-btns{display:flex;gap:8px}',
    '.dm-why{font-size:12px;color:rgba(238,238,238,.55);text-align:right}',
    '.dm-icon{flex:none;display:flex;align-items:center;justify-content:center;border-radius:6px;background:rgba(255,255,255,.05);overflow:hidden;color:rgba(238,238,238,.7);font-weight:600}',
    '.dm-icon img{width:100%;height:100%;object-fit:contain}',
    '.dm-i44{width:44px;height:44px;font-size:18px}',
    '.dm-i22{width:22px;height:22px;font-size:11px}',
    '.dm-empty{padding:20px;color:rgba(238,238,238,.55);font-size:14px;text-align:center}',
    '.dm-btn{font:inherit;font-size:13px;color:#dde2f0;padding:7px 11px;border-radius:7px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(58,58,60,.9),rgba(38,38,40,.92));cursor:pointer;white-space:nowrap}',
    '.dm-btn:hover:not(:disabled){background:linear-gradient(180deg,rgba(72,66,48,.92),rgba(50,46,32,.95));border-color:rgba(255,193,7,.75)}',
    '.dm-btn.dm-danger:hover:not(:disabled){background:linear-gradient(180deg,rgba(84,44,42,.92),rgba(58,30,28,.95));border-color:rgba(229,57,53,.75)}',
    '.dm-btn:disabled{opacity:.4;cursor:default}',
    '.dm-foot{display:flex;font-size:13px;color:rgba(238,238,238,.7)}',
    '.dm-status{flex:1}',
    '#dm-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    root.id = 'dm-root';
    var plate = el('div', 'dm-plate');
    var head = el('div', 'dm-head');
    var sub = el('div', 'dm-sub');
    head.appendChild(el('div', 'dm-title', 'Defense Manager'));
    head.appendChild(sub);
    head.appendChild(button('\u00d7', 'close', null, true, 'dm-x'));

    var bar = el('div', 'dm-bar');
    var lock = el('div', 'dm-lock');
    var list = el('div', 'dm-list');
    var foot = el('div', 'dm-foot');
    var status = el('div', 'dm-status');
    var keys = el('div');
    foot.appendChild(status);
    foot.appendChild(keys);

    plate.appendChild(head);
    plate.appendChild(bar);
    plate.appendChild(lock);
    plate.appendChild(list);
    plate.appendChild(foot);
    root.appendChild(plate);

    var toast = el('div');
    toast.id = 'dm-toast';

    document.body.appendChild(root);
    document.body.appendChild(toast);

    root.addEventListener('click', onClick);
    // Clicks inside the plate must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so capture them.
    root.addEventListener('error', function (e) {
      var img = e.target;

      if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
    }, true);

    ui = { root: root, sub: sub, bar: bar, lock: lock, list: list, status: status, keys: keys, toast: toast };
  }

  // The style outlives a lost panel root, so a rebuild must not stack another copy of it.
  function addStyle() {
    if (document.getElementById('dm-style')) return;

    var style = el('style');
    style.id = 'dm-style';
    style.textContent = CSS;
    document.head.appendChild(style);
  }

  function onClick(e) {
    var target = e.target.closest('[data-act]');

    if (!target) return;

    e.stopPropagation();

    if (target.disabled) return;

    var act = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');

    // Opening or shutting the panel gets no status line back, so it must not hold the buttons busy.
    if (act === 'close' || act === 'toggle') {
      send(act);

      return;
    }

    sendAction(args ? act + '|' + args : act);
  }

  function icon(item, sizeClass) {
    var box = el('div', 'dm-icon ' + sizeClass);
    var letter = String(item.name || '?').charAt(0).toUpperCase();

    if (item.icon) {
      var img = el('img');
      img.setAttribute('data-letter', letter);
      img.src = item.icon;
      box.appendChild(img);
    } else {
      box.textContent = letter;
    }

    return box;
  }

  function render() {
    renderHead();
    renderBar();
    renderLock();
    renderList();
    renderKeys();
  }

  function renderHead() {
    var defenses = state.data.defenses;
    var broken = defenses.filter(function (d) { return d.state === 'broken'; }).length;
    var damaged = defenses.filter(function (d) { return d.state === 'damaged'; }).length;

    ui.sub.textContent = defenses.length === 0 ? '' : defenses.length + ' outdoor defenses: ' + broken + ' broken, ' + damaged + ' damaged';
  }

  // The two top buttons stay pressable with nothing left to line up: a press then says what is still queued.
  function renderBar() {
    var data = state.data;
    var isUnlocked = !data.locked;
    var mats = el('div', 'dm-mats');
    ui.bar.textContent = '';
    ui.bar.appendChild(button(say('repairAll', data.repairAll), 'repairall', null, isUnlocked));
    ui.bar.appendChild(button(say('clearAll', data.clearAll), 'clearbroken', null, isUnlocked, 'dm-danger'));

    if (data.materials.length > 0) mats.appendChild(el('span', '', say('materials')));

    data.materials.forEach(function (material) { mats.appendChild(materialChip(material)); });
    ui.bar.appendChild(mats);
  }

  // What is free after the repairs already queued, against what the repairs not yet queued would take.
  function materialChip(material) {
    var chip = el('div', 'dm-chip' + (material.free < material.need ? ' dm-short' : ''));
    chip.appendChild(icon(material, 'dm-i22'));
    chip.appendChild(el('span', '', material.name));
    chip.appendChild(el('span', 'dm-num', material.free + ' / ' + material.need));

    return chip;
  }

  function renderLock() {
    ui.lock.textContent = state.data.locked || '';
    ui.lock.style.display = state.data.locked ? 'block' : 'none';
  }

  function renderList() {
    var scroll = ui.list.scrollTop;
    var data = state.data;
    ui.list.textContent = '';

    if (data.defenses.length === 0) {
      ui.list.appendChild(el('div', 'dm-empty', data.note || say('empty')));

      return;
    }

    var fragment = document.createDocumentFragment();

    data.defenses.forEach(function (defense) { fragment.appendChild(row(defense)); });
    ui.list.appendChild(fragment);
    ui.list.scrollTop = scroll;
  }

  function row(defense) {
    var node = el('div', 'dm-row dm-' + defense.state);
    var main = el('div', 'dm-main');
    var top = el('div', 'dm-top');
    top.appendChild(el('div', 'dm-name', defense.name));
    top.appendChild(el('span', 'dm-badge dm-badge-' + defense.state, STATE_WORD[defense.state] ? say(STATE_WORD[defense.state]) : defense.state));

    if (defense.queued) top.appendChild(el('span', 'dm-badge dm-badge-queued', QUEUED_WORD[defense.queued] ? say(QUEUED_WORD[defense.queued]) : defense.queued));

    main.appendChild(top);
    main.appendChild(condition(defense));

    if (defense.cost.length > 0) main.appendChild(cost(defense.cost));
    if (defense.power) main.appendChild(power(defense));

    node.appendChild(icon(defense, 'dm-i44'));
    node.appendChild(main);
    node.appendChild(actions(defense));

    return node;
  }

  function condition(defense) {
    var box = el('div', 'dm-cond');
    var track = el('div', 'dm-track');
    var fill = el('div', 'dm-fill');
    var percent = defense.max > 0 ? Math.max(0, Math.min(100, Math.round(defense.cur * 100 / defense.max))) : 0;
    fill.style.width = percent + '%';
    track.appendChild(fill);
    box.appendChild(track);
    box.appendChild(el('span', '', defense.cur + ' / ' + defense.max));

    return box;
  }

  function cost(entries) {
    var box = el('div', 'dm-cost');
    box.appendChild(el('span', '', say('repairCost')));

    entries.forEach(function (entry) {
      var chip = el('div', 'dm-chip' + (entry.short > 0 ? ' dm-short' : ''));
      chip.appendChild(icon(entry, 'dm-i22'));
      chip.appendChild(el('span', 'dm-num', String(entry.count)));
      chip.appendChild(el('span', '', entry.name));

      if (entry.short > 0) chip.appendChild(el('span', 'dm-miss', say('short', entry.short)));

      box.appendChild(chip);
    });

    return box;
  }

  // An electrical defense's power: its switch as the game has it, the mode it runs in, and two buttons. Each button
  // names what it changes, this one defense or every defense of its type, and shows that setting as it stands.
  function power(defense) {
    var p = defense.power;
    var box = el('div', 'dm-power');
    box.appendChild(el('span', 'dm-badge ' + (p.on ? 'dm-badge-on' : 'dm-badge-off'), say(p.on ? 'powerOn' : 'powerOff')));
    box.appendChild(el('span', 'dm-mode', say('mode', p.mode)));

    if (!p.managed) box.appendChild(el('span', 'dm-miss', say('unmanaged')));

    box.appendChild(button(say('thisOne', p.label), 'power', defense.id, true));
    box.appendChild(button(say('every', defense.name, p.typeLabel), 'powertype', defense.id, true));

    return box;
  }

  // The web view draws no hover tips, so a button that would be refused says why underneath it.
  function actions(defense) {
    var box = el('div', 'dm-acts');
    var buttons = el('div', 'dm-btns');
    buttons.appendChild(button(say('repair'), 'repair', defense.id, defense.canRepair));
    buttons.appendChild(button(say('remove'), 'remove', defense.id, defense.canRemove, 'dm-danger'));
    box.appendChild(buttons);

    if (!defense.canRepair && defense.why) box.appendChild(el('div', 'dm-why', defense.why));
    if (!defense.canRemove && defense.removeWhy) box.appendChild(el('div', 'dm-why', defense.removeWhy));

    return box;
  }

  function renderKeys() {
    ui.keys.textContent = state.key ? state.key + ' / ' + say('escClose') : say('escClose');
  }

  function open(data) {
    build();
    state.data = data || state.data;
    state.isOpen = true;
    ui.status.textContent = '';
    ui.toast.style.display = 'none';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    state.data = data || state.data;

    if (state.isOpen) render();
  }

  function close() {
    state.isOpen = false;

    if (ui) ui.root.style.display = 'none';
  }

  function status(text) {
    build();
    clearBusy();

    if (state.isOpen) {
      ui.status.textContent = text;

      return;
    }

    // An empty status only clears the line; as a toast it would be an empty box.
    if (!text) {
      ui.toast.style.display = 'none';

      return;
    }

    ui.toast.textContent = text;
    ui.toast.style.display = 'block';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { ui.toast.style.display = 'none'; }, TOAST_MS);
  }

  // Whether a game the panel can manage is loaded, and the key that opens the panel ('' when none): the HUD button is
  // only shown in such a game, and both it and the panel name the key.
  function live(isLive, key) {
    state.isLive = !!isLive;
    state.key = key || '';
    layoutVersion++;

    if (ui && state.isOpen) renderKeys();

    patchPagesSafely();
  }

  // What the bar on the game's Layout screen draws from (the panel's own view), or null while Layout is not up.
  function layout(data) {
    state.layout = data || null;
    layoutVersion++;
    patchPagesSafely();
  }

  // The HUD and the Layout screen are same-origin iframes, so their documents can be reached from here; polled because
  // the game creates and drops those iframes whenever it likes.
  function patchPages() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      var src = frames[i].src || '';
      var isHud = /CoreUI1\.html/.test(src);

      if (!isHud && !/BuildTips\.html/.test(src)) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc) {
        if (!hasReportedPageAccess) send('diag|cannot reach a game page document (root ' + location.protocol + ', iframe ' + src + ')');
        hasReportedPageAccess = true;
        continue;
      }

      if (isHud) placeHudButton(doc);
      else placeLayoutBar(doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  function placeHudButton(doc) {
    var column = doc.querySelector('.vertical-tools');
    var existing = doc.querySelector('.' + HUD_CLASS);

    if (!column || !state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column && existing.getAttribute('data-key') === state.key) return;
    if (existing) existing.remove();

    column.insertBefore(hudButton(doc), column.firstChild);

    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
  }

  // The same markup as the game's bag button: its classes, an svg icon, and the hover tip its stylesheet shows. The
  // HUD page watches its body for new buttons and tells the game where they are, so a click on this one reaches it.
  function hudButton(doc) {
    var node = doc.createElement('button');
    node.className = 'ui-btn icon-sq interactive-btn has-entry-tip ' + HUD_CLASS;
    node.setAttribute('data-interactive', '');
    node.setAttribute('data-key', state.key);
    node.appendChild(shield(doc));
    node.appendChild(entryTip(doc));
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });

    return node;
  }

  function shield(doc) {
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var path = doc.createElementNS(SVG_NS, 'path');

    svg.setAttribute('class', 'icon-svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    path.setAttribute('d', SHIELD_PATH);
    svg.appendChild(path);

    return svg;
  }

  function entryTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var body = doc.createElement('div');

    tip.className = 'entry-tip';
    title.className = 'entry-tip-title';
    title.textContent = 'Defense Manager';
    body.className = 'entry-tip-body';
    body.textContent = say('tip');

    if (state.key) {
      var key = doc.createElement('span');
      key.className = 'entry-tip-key';
      key.textContent = state.key;
      title.appendChild(key);
    }

    tip.appendChild(title);
    tip.appendChild(body);

    return tip;
  }

  // Layout's own button look (its cancel button: dark plate, amber text and border, amber corner marks), so the bar
  // reads as part of that screen. The corner marks use the Layout page's own .corner rules.
  var LAYOUT_CSS = [
    '#dm-layout{position:fixed;top:24px;left:50%;transform:translateX(-50%);z-index:50;display:flex;flex-direction:column;align-items:center;gap:8px;pointer-events:none}',
    '#dm-layout .dm-lb-row{display:flex;gap:12px}',
    '#dm-layout .dm-lb-btn{position:relative;font-family:inherit;background-color:rgba(12,12,14,.8);border:1px solid rgba(235,163,8,.4);color:#EBA308;font-size:15px;font-weight:700;padding:10px 22px;cursor:pointer;letter-spacing:2px;white-space:nowrap;transition:background-color .15s,border-color .15s}',
    '#dm-layout .dm-lb-btn:hover:not(:disabled){background-color:rgba(235,163,8,.15);border-color:rgba(235,163,8,.7)}',
    '#dm-layout .dm-lb-btn:active:not(:disabled){background-color:rgba(235,163,8,.25)}',
    '#dm-layout .dm-lb-btn.dm-lb-danger:hover:not(:disabled){background-color:rgba(229,57,53,.18);border-color:rgba(229,57,53,.75);color:#ff8a80}',
    '#dm-layout .dm-lb-btn:disabled{opacity:.4;cursor:default}',
    '#dm-layout .dm-lb-note{position:relative;max-width:640px;text-align:center;padding:8px 18px;background-color:rgba(12,12,14,.75);border:1px solid rgba(235,163,8,.35);color:#EBA308;font-size:13px;font-weight:700;letter-spacing:1px;text-shadow:0 2px 8px rgba(0,0,0,.6)}'
  ].join('\n');

  // The bar is drawn again only when its view or the panel key changed, so the game's hover and click state on it is
  // not reset every poll. It goes away whenever Layout closes or the game is left.
  function placeLayoutBar(doc) {
    var existing = doc.getElementById(LAYOUT_ID);

    if (!state.layout || !state.isLive || !doc.body) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.getAttribute('data-version') === String(layoutVersion)) return;

    addLayoutStyle(doc);

    var bar = layoutBar(doc);

    if (existing) existing.parentNode.replaceChild(bar, existing);
    else doc.body.appendChild(bar);

    if (!hasReportedLayoutPlaced) send('diag|Layout bar placed on the Layout screen');
    hasReportedLayoutPlaced = true;
  }

  function addLayoutStyle(doc) {
    if (doc.getElementById(LAYOUT_STYLE_ID)) return;

    var style = doc.createElement('style');
    style.id = LAYOUT_STYLE_ID;
    style.textContent = LAYOUT_CSS;
    (doc.head || doc.body).appendChild(style);
  }

  // The same two buttons as the panel's top bar, plus one that opens the panel for the per-defense list. The row is
  // marked unity-interactive, as Layout marks its own buttons, so the page tells the game where it is and a click on
  // it, greyed buttons included, never falls through to the world behind. A greyed bar says why underneath it.
  function layoutBar(doc) {
    var data = state.layout;
    var isUnlocked = !data.locked;
    var bar = doc.createElement('div');
    var row = doc.createElement('div');
    var why = data.locked || data.note;

    bar.id = LAYOUT_ID;
    bar.setAttribute('data-version', String(layoutVersion));
    row.className = 'dm-lb-row unity-interactive';
    row.appendChild(layoutButton(doc, say('repairAll', data.repairAll), 'repairall', isUnlocked, ''));
    row.appendChild(layoutButton(doc, say('clearAll', data.clearAll), 'clearbroken', isUnlocked, 'dm-lb-danger'));
    row.appendChild(layoutButton(doc, state.key ? say('defenseListKey', state.key) : say('defenseList'), 'toggle', true, ''));
    row.addEventListener('click', onClick);
    bar.appendChild(row);

    if (why) {
      var note = doc.createElement('div');
      note.className = 'dm-lb-note';
      note.textContent = why;
      bar.appendChild(note);
    }

    return bar;
  }

  function layoutButton(doc, text, action, isEnabled, extraClass) {
    var node = doc.createElement('button');
    var corners = ['tl', 'tr', 'bl', 'br'];

    node.className = 'dm-lb-btn' + (extraClass ? ' ' + extraClass : '');
    node.setAttribute('data-act', action);

    if (!isEnabled) node.disabled = true;

    corners.forEach(function (corner) {
      var mark = doc.createElement('div');
      mark.className = 'corner ' + corner;
      node.appendChild(mark);
    });
    node.appendChild(doc.createTextNode(text));

    return node;
  }

  setInterval(patchPagesSafely, 400);

  window.__sldefense = { open: open, setData: setData, close: close, status: status, live: live, layout: layout };
  send('ready');
})();
