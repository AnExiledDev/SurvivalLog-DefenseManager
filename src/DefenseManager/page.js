// Defense Manager's web half. Runs in Root.html, the document every game page iframe sits in. Draws the panel over
// all of them in the warm gold look of the other mod panels: the home yard as a grid of squares with a side panel
// beside it, and tabs for power and settings. It also puts a button in the HUD's tool column (bottom left, above the
// phone and bag buttons), built like the game's own buttons there so it reads as part of the game. While the game's
// Layout screen is up it puts a bar of buttons at the top of that screen, built like Layout's own buttons. Talks to
// the plugin through vuplex.postMessage("slmod|defensemanager|<verb>") and is driven by window.__sldefense.
(function () {
  if (window.__sldefense) return;

  var PREFIX = 'slmod|defensemanager|';
  var HUD_CLASS = 'dm-hud-btn';
  // Defense Manager's place in the HUD's tool column when Exiled's Toolkit lays it out in columns of four.
  var DOCK_ORDER = '40';
  var LAYOUT_ID = 'dm-layout';
  var LAYOUT_STYLE_ID = 'dm-layout-style';
  var BUSY_TIMEOUT_MS = 5000;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A shield, drawn on the 24-unit grid the game's HUD icons use.
  var SHIELD_PATH = 'M12 2l8 3v6c0 5.25-3.4 9.74-8 11-4.6-1.26-8-5.75-8-11V5l8-3zm0 2.14L6 6.39V11c0 4.1 2.5 7.67 6 8.9 3.5-1.23 6-4.8 6-8.9V6.39l-6-2.25zM11 8h2v3h3v2h-3v3h-2v-3H8v-2h3z';
  var BOLT_PATH = 'M13 2L4 14h6l-1 8 9-12h-6z';
  var WRENCH_PATH = 'M21 6.5a5 5 0 0 1-6.6 4.7L6 19.6a2 2 0 0 1-2.8-2.8l8.4-8.4A5 5 0 0 1 17.5 3l-3 3 .6 2.9 2.9.6z';
  var LOCK_PATH = 'M7 11V8a5 5 0 0 1 10 0v3h1a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1v-8a1 1 0 0 1 1-1h1zm2 0h6V8a3 3 0 0 0-6 0v3z';
  var STATE_WORD = { broken: 'broken', damaged: 'damaged', intact: 'intact' };
  var QUEUED_WORD = { repair: 'repairQueued', remove: 'removeQueued' };
  // The modes in the order the panel lists them, by the names the plugin takes.
  var MODES = ['WaveOnly', 'Proximity', 'AlwaysOn', 'Default'];
  var MODE_WORD = { WaveOnly: 'modeWaveOnly', Proximity: 'modeProximity', AlwaysOn: 'modeAlwaysOn', Default: 'modeDefault' };
  // What the page sends for "no setting of its own" (PageCommand.NoMode).
  var NO_MODE = 'none';
  // The Settings tab's steppers, in the units and steps the plugin rounds to (SettingChange).
  var STEPS = {
    proximityrange: { step: 1, min: 1, max: 100 },
    crawlerdodge: { step: 0.05, min: 0, max: 1 },
    armoredcrawlerdodge: { step: 0.05, min: 0, max: 1 },
    attackrate: { step: 0.25, min: 0.25, max: 10 }
  };
  // Clicks the page answers itself: they change what the panel shows, not the game.
  var LOCAL = { tab: true, pick: true, row: true, cell: true, back: true };
  // Lists that keep their scroll when the panel is drawn again.
  var SCROLLERS = ['dm-rlist', 'dm-yard', 'dm-pcards', 'dm-stab'];
  // The plugin hands over the words in the game's language before every call (Texts.cs); English stands in until then.
  var ENGLISH = {
    broken: 'Broken', damaged: 'Damaged', intact: 'Intact', repairQueued: 'Repair queued', removeQueued: 'Removal queued',
    repairAll: 'Repair all ({0})', clearAll: 'Clear all broken ({0})', empty: 'No traps or barricades outside your home yet.',
    unmanaged: 'Not switched while broken or not working', repair: 'Repair', remove: 'Remove', escClose: 'Esc to close',
    tip: 'Repair or remove your outdoor defenses', defenseList: 'Defense list', defenseListKey: 'Defense list ({0})',
    title: 'DEFENSES', placed: '{0} placed, {1} of {2} squares yours', placedOnly: '{0} placed', tabYard: 'Yard',
    tabPower: 'Power', tabSettings: 'Settings', house: 'House', hint: 'Click a defense, a row number or a square',
    window: 'Window', door: 'Door', road: 'Road side', offGridLabel: 'Not on the grid', legEmpty: 'Yours, empty',
    legBuy: 'Not yours yet (Survival Points)', needsRepair: 'Needs repair', shortOf: 'Not enough for every repair: {0}',
    queuedCount: '{0} queued', allGood: 'All good', nothingNeeds: 'Nothing needs repair.', back: '< Back to repairs',
    where: 'Row {0}, square {1}', offGrid: 'Not on the yard grid', condition: 'Condition', nothingToRepair: 'Nothing to repair',
    onNow: 'On now', offNow: 'Off now', sameAsType: 'Same as every {0} ({1})', row: 'Row {0}', againstHouse: 'Against the house',
    rowOut1: '1 row out from the house', rowsOut: '{0} rows out from the house',
    rowInfo: 'Defenses: {0}. Squares yours: {1} of {2}.', rowPrice: 'The other squares cost {0} Survival Points each.',
    repairRow: 'Repair row {0} ({1})', emptySquare: 'Empty square', notYours: 'Not yours yet',
    buildHere: 'Build a defense here from the Layout screen.', unlockFor: 'Unlock it from the Layout screen for {0} Survival Points.',
    everyType: 'Every {0}', global: 'Global ({0})', ownList: 'Set on their own', followType: 'Follow the type',
    noneOwn: 'None. Pick one in the Yard tab to set it alone.', noPower: 'No powered defenses yet.',
    powerNote: 'Wave only: on during waves and while zombies are at the base. Proximity: on while a zombie is in range. Always on: never turns off. Default: the game\'s own power switch decides.',
    modeWaveOnly: 'Wave only', modeProximity: 'Proximity', modeAlwaysOn: 'Always on', modeDefault: 'Default',
    setControls: 'Controls', setCombat: 'Combat', setKey: 'Open the panel', setKeyDesc: 'Key that opens and closes this window.',
    keyNone: 'None', setLayout: 'Layout screen buttons',
    setLayoutDesc: 'Show Repair all, Clear all broken and Defense list on the Layout screen.',
    setMode: 'Default power mode', setModeDesc: 'Mode for powered defenses with no setting of their own.',
    setRange: 'Proximity range', setRangeDesc: 'How close a zombie must come to turn a Proximity defense on, 1 to 100 meters.',
    meters: '{0} m', setCrawler: 'Crawler dodge chance',
    setCrawlerDesc: 'Chance a Crawler Zombie dodges a Mechanical Chainsaw hit. The base game has 100%.',
    setArmored: 'Armored crawler dodge chance',
    setArmoredDesc: 'Chance an Armored Crawler Zombie dodges a Mechanical Chainsaw hit. The base game has 100%.',
    setRate: 'Device attack rate', setRateDesc: 'How often defense devices strike, 0.25 to 10. Takes effect at the next game start.',
    keyClose: '{0} or Esc to close', noSettings: 'Settings are not available right now.'
  };

  var state = { data: emptyData(), isOpen: false, isLive: false, key: '', layout: null, tab: 'yard', sel: null, row: null, cell: null };
  var ui = null;
  var isBusy = false;
  var busyTimer = 0;
  var layoutVersion = 0;
  var hasReportedPageAccess = false;
  var hasReportedHudPlaced = false;
  var hasReportedLayoutPlaced = false;

  function emptyData() {
    return { note: null, locked: null, repairAll: 0, clearAll: 0, materials: [], defenses: [], yard: null, global: null, settings: null };
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

  function add(parent) {
    for (var i = 1; i < arguments.length; i++) {
      if (arguments[i]) parent.appendChild(arguments[i]);
    }

    return parent;
  }

  // Marks anything as clickable: a click on it or inside it does this action.
  function act(node, action, args) {
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));

    return node;
  }

  function button(text, action, args, isEnabled, extraClass) {
    var node = act(el('button', 'dm-btn' + (extraClass ? ' ' + extraClass : ''), text), action, args);

    if (!isEnabled) node.disabled = true;

    return node;
  }

  function svg(path, className) {
    var node = document.createElementNS(SVG_NS, 'svg');
    var shape = document.createElementNS(SVG_NS, 'path');

    node.setAttribute('viewBox', '0 0 24 24');
    node.setAttribute('class', className || 'dm-svg');
    shape.setAttribute('d', path);
    shape.setAttribute('fill', 'currentColor');
    node.appendChild(shape);

    return node;
  }

  // A button with a wrench before its words, for the repair presses.
  function wrenchButton(text, action, args, isEnabled, extraClass) {
    var node = button(null, action, args, isEnabled, extraClass);

    add(node, svg(WRENCH_PATH), el('span', '', text));

    return node;
  }

  var CSS = [
    '#dm-root{--dm-accent:#FFC83A;--dm-glow:rgba(255,200,58,.35);--dm-text:#f0e8d8;--dm-text2:#cbbfa8;--dm-text3:#a89a82;--dm-text4:#7a6e5a;--dm-line:rgba(220,188,120,.2);--dm-soft:rgba(220,188,120,.11);--dm-card:rgba(70,58,44,.55);--dm-red:#e53935;--dm-red2:#ff8a80;--dm-green:#8cc56a;--dm-green2:#b5e09a;position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(10,6,2,.45);font-family:"Noto Sans SC","Microsoft YaHei","PingFang SC",sans-serif;color:var(--dm-text);user-select:none}',
    '#dm-root *{box-sizing:border-box}',
    '.dm-plate{position:relative;width:min(1600px,96vw);height:min(780px,92vh);display:flex;flex-direction:column;gap:12px;padding:18px 22px;border-radius:4px;border:1px solid rgba(220,188,120,.28);background:radial-gradient(ellipse at 15% 0%,rgba(150,100,40,.3),transparent 55%),linear-gradient(180deg,#3a3129 0%,#2b241d 55%,#241e18 100%);box-shadow:0 30px 90px rgba(0,0,0,.7),inset 0 1px 0 rgba(255,230,180,.08)}',
    '.dm-plate::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,var(--dm-accent) 50%,transparent 90%);opacity:.6}',
    '.dm-svg{width:15px;height:15px;flex:none}',
    '.dm-hd{display:flex;align-items:center;gap:16px}',
    '.dm-logo{width:40px;height:40px;flex:none;border-radius:4px;display:flex;align-items:center;justify-content:center;background:linear-gradient(180deg,#ffd25a,#e8a91c);border:1px solid #ffd36a;color:#2a1f10;box-shadow:0 0 14px rgba(255,200,58,.25)}',
    '.dm-logo .dm-svg{width:24px;height:24px}',
    '.dm-ttl{font-size:26px;letter-spacing:6px;color:#fff;line-height:1;white-space:nowrap}',
    '.dm-ttl small{display:block;font-size:12px;letter-spacing:1px;color:var(--dm-text3);margin-top:5px}',
    '.dm-sp{flex:1}',
    '.dm-have{display:flex;align-items:center;gap:8px;flex-wrap:wrap;justify-content:flex-end;min-width:0}',
    '.dm-chip{display:inline-flex;align-items:center;gap:6px;padding:3px 9px 3px 4px;border-radius:4px;background:rgba(20,14,8,.45);border:1px solid var(--dm-line);font-size:13px;white-space:nowrap}',
    '.dm-chip b{color:var(--dm-accent);font-weight:700}',
    '.dm-chip.dm-short{border-color:rgba(229,57,53,.7)}',
    '.dm-chip.dm-short b{color:var(--dm-red2)}',
    '.dm-tabs{display:flex;gap:8px}',
    '.dm-tab{padding:7px 18px;border-radius:4px;border:1px solid var(--dm-line);background:rgba(255,240,210,.05);font-size:14px;color:var(--dm-text2);cursor:pointer;display:flex;gap:7px;align-items:center;white-space:nowrap}',
    '.dm-tab:hover{border-color:rgba(255,200,58,.6);color:#fff}',
    '.dm-tab.dm-on{color:var(--dm-accent);font-weight:600;background:rgba(255,200,58,.13);border-color:var(--dm-accent);box-shadow:0 0 12px rgba(255,200,58,.18)}',
    '.dm-n{font-style:normal;font-size:11px;padding:0 6px;border-radius:7px;background:#D32F2F;color:#fff;font-weight:700}',
    '.dm-body{flex:1;min-height:0;display:flex;gap:12px}',
    '.dm-well{background:linear-gradient(180deg,rgba(58,48,38,.75),rgba(44,36,28,.75));border:1px solid var(--dm-line);border-radius:4px;min-height:0;overflow-y:auto}',
    '.dm-well::-webkit-scrollbar,.dm-rlist::-webkit-scrollbar{width:6px}',
    '.dm-well::-webkit-scrollbar-thumb,.dm-rlist::-webkit-scrollbar-thumb{background:rgba(255,200,58,.35);border-radius:3px}',
    '.dm-sect{font-size:15px;font-weight:700;letter-spacing:3px;color:var(--dm-accent);text-shadow:0 0 12px var(--dm-glow);display:flex;align-items:center;gap:10px;padding:14px 16px 6px;text-transform:uppercase}',
    '.dm-sect::before{content:"";width:4px;height:16px;background:var(--dm-accent);box-shadow:0 0 8px var(--dm-glow)}',
    '.dm-sect::after{content:"";flex:1;height:1px;background:linear-gradient(90deg,rgba(255,200,58,.25),transparent)}',
    '.dm-sect .dm-r{order:2;text-transform:none;letter-spacing:0;color:var(--dm-text4);text-shadow:none;font-size:12px;font-weight:400}',
    '.dm-btn{font:inherit;font-size:13px;color:var(--dm-text);padding:7px 12px;border-radius:4px;border:1px solid var(--dm-line);background:linear-gradient(180deg,rgba(92,78,60,.9),rgba(66,55,42,.92));cursor:pointer;white-space:nowrap;display:inline-flex;gap:6px;align-items:center}',
    '.dm-btn:hover:not(:disabled){border-color:var(--dm-accent);background:linear-gradient(180deg,rgba(110,90,58,.95),rgba(80,64,40,.95))}',
    '.dm-btn:disabled{opacity:.4;cursor:default}',
    '.dm-btn.dm-sm{font-size:12px;padding:4px 9px}',
    '.dm-btn.dm-x{width:30px;height:30px;padding:0;font-size:18px;justify-content:center}',
    '.dm-btn.dm-gold{color:#2a1f10;font-weight:700;letter-spacing:1px;border-color:#ffd36a;background:linear-gradient(180deg,#ffd25a,#e8a91c);box-shadow:0 0 14px rgba(255,200,58,.25)}',
    '.dm-btn.dm-gold:hover:not(:disabled){background:linear-gradient(180deg,#ffdf80,#f0b52a)}',
    '.dm-btn.dm-quiet{color:var(--dm-accent);font-weight:600;border-color:rgba(255,200,58,.55);background:rgba(255,200,58,.1)}',
    '.dm-btn.dm-quiet:hover:not(:disabled){background:rgba(255,200,58,.2)}',
    '.dm-btn.dm-danger{color:#ffcdd2}',
    '.dm-btn.dm-danger:hover:not(:disabled){background:linear-gradient(180deg,rgba(84,44,42,.92),rgba(58,30,28,.95));border-color:rgba(229,57,53,.75)}',
    '.dm-badge{flex:none;font-size:11px;padding:2px 7px;border-radius:3px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.8);white-space:nowrap;display:inline-block}',
    '.dm-b-broken{background:rgba(229,57,53,.28);color:var(--dm-red2)}',
    '.dm-b-damaged{background:rgba(255,193,7,.24);color:var(--dm-accent)}',
    '.dm-b-intact{background:rgba(124,179,66,.24);color:var(--dm-green2)}',
    '.dm-b-queued{background:rgba(100,181,246,.2);color:#90caf9}',
    '.dm-track{display:flex;height:8px;border-radius:4px;background:rgba(255,255,255,.1);overflow:hidden;flex:none}',
    '.dm-fill{height:100%;border-radius:4px;background:var(--dm-green)}',
    '.dm-fill.dm-damaged{background:var(--dm-accent)}',
    '.dm-fill.dm-broken{background:var(--dm-red)}',
    '.dm-ic{flex:none;display:inline-flex;align-items:center;justify-content:center;border-radius:4px;background:rgba(255,255,255,.05);overflow:hidden;color:rgba(238,238,238,.7);font-weight:600}',
    '.dm-ic img{width:100%;height:100%;object-fit:contain}',
    '.dm-i18{width:18px;height:18px;font-size:10px}',
    '.dm-i30{width:30px;height:30px;font-size:12px}',
    '.dm-i56{width:56px;height:56px;font-size:18px}',
    '.dm-i72{width:72px;height:72px;font-size:22px}',
    '.dm-yard{flex:1;min-width:0;display:flex;flex-direction:column;padding:14px 18px 12px}',
    '.dm-ytop{display:flex;align-items:baseline;gap:12px;margin-bottom:6px}',
    '.dm-house{font-size:12px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:var(--dm-accent);text-shadow:0 0 12px var(--dm-glow)}',
    '.dm-hint{margin-left:auto;font-size:12px;color:var(--dm-text3)}',
    '.dm-wall,.dm-grid{display:grid;column-gap:5px}',
    '.dm-wall{height:26px;border-bottom:3px solid rgba(255,200,58,.45);margin-bottom:8px;background:linear-gradient(180deg,rgba(150,100,40,.28),rgba(150,100,40,.05));border-radius:4px 4px 0 0}',
    '.dm-wf{grid-row:1;align-self:end;height:18px;border-radius:4px 4px 0 0;font-size:11px;letter-spacing:1px;text-transform:uppercase;display:flex;align-items:center;justify-content:center;overflow:hidden;white-space:nowrap}',
    '.dm-wf.dm-window{background:rgba(255,240,210,.08);color:var(--dm-text2);border:1px solid rgba(220,188,120,.35);border-bottom:none}',
    '.dm-wf.dm-door{background:rgba(255,200,58,.18);color:var(--dm-accent);border:1px solid rgba(255,200,58,.55);border-bottom:none}',
    '.dm-grid{row-gap:5px}',
    '.dm-rlab{display:flex;align-items:center;justify-content:center;position:relative;font-size:15px;font-weight:700;color:var(--dm-text2);border-radius:4px;cursor:pointer}',
    '.dm-rlab:hover{background:rgba(255,240,210,.07);color:#fff}',
    '.dm-rlab.dm-on{background:rgba(255,200,58,.13);color:var(--dm-accent);box-shadow:inset 0 0 0 1px var(--dm-accent)}',
    '.dm-rlab.dm-none{color:var(--dm-text4)}',
    '.dm-rlab i{position:absolute;top:-3px;right:-4px;font-style:normal;font-size:10px;font-weight:700;min-width:16px;height:16px;line-height:16px;text-align:center;padding:0 4px;border-radius:8px;background:#D32F2F;color:#fff}',
    // A square cell without aspect-ratio: its padding is a share of its width, so it is as tall as it is wide.
    '.dm-cell{position:relative;height:0;padding-bottom:100%;border-radius:4px;cursor:pointer;border:1px solid transparent}',
    '.dm-in{position:absolute;inset:0;display:flex;align-items:center;justify-content:center}',
    '.dm-cell.dm-buy{background:repeating-linear-gradient(135deg,rgba(150,52,30,.18) 0 6px,rgba(150,52,30,.07) 6px 12px);border-color:rgba(200,80,50,.18)}',
    '.dm-price{font-size:11px;font-weight:600;color:transparent}',
    '.dm-cell.dm-buy:hover .dm-price,.dm-cell.dm-buy.dm-sel .dm-price{color:var(--dm-red2)}',
    '.dm-cell.dm-empty{background:rgba(255,240,210,.07);border-color:rgba(220,188,120,.22)}',
    '.dm-cell.dm-def{background:linear-gradient(180deg,rgba(74,62,48,.85),rgba(56,46,35,.85));border-color:rgba(220,188,120,.32)}',
    '.dm-cell:hover{border-color:var(--dm-accent)}',
    '.dm-ci{width:78%;height:78%;background:none;font-size:14px}',
    '.dm-cell.dm-def img{filter:drop-shadow(0 2px 3px rgba(0,0,0,.6))}',
    '.dm-cell.dm-s-damaged{border-color:var(--dm-accent);box-shadow:inset 0 0 10px rgba(255,200,58,.25)}',
    '.dm-cell.dm-s-broken{border-color:rgba(229,57,53,.9);background:linear-gradient(180deg,rgba(96,36,26,.75),rgba(60,24,18,.75));box-shadow:inset 0 0 12px rgba(229,57,53,.35)}',
    '.dm-cell.dm-s-broken img{filter:grayscale(1) brightness(.55)}',
    '.dm-cell.dm-q{border-color:rgba(100,181,246,.8);box-shadow:none}',
    '.dm-cell.dm-inrow{box-shadow:0 0 0 1px rgba(255,200,58,.55);z-index:1}',
    '.dm-cell.dm-sel{box-shadow:0 0 0 2px var(--dm-accent),0 0 14px rgba(255,200,58,.3);z-index:1}',
    '.dm-cell .dm-track{position:absolute;left:5px;right:5px;bottom:4px;height:4px}',
    '.dm-pw{position:absolute;top:3px;right:3px;width:13px;height:13px;color:rgba(255,240,210,.3)}',
    '.dm-cell.dm-on .dm-pw{color:#ffd54f;filter:drop-shadow(0 0 3px rgba(255,213,79,.9))}',
    '.dm-qd{position:absolute;top:3px;left:3px;width:14px;height:14px;color:#90caf9}',
    '.dm-pw .dm-svg,.dm-qd .dm-svg{width:100%;height:100%}',
    '.dm-other{display:flex;align-items:center;gap:5px;flex-wrap:wrap;margin-top:10px}',
    '.dm-other .dm-olab{font-size:11px;letter-spacing:2px;text-transform:uppercase;color:var(--dm-text3);margin-right:6px}',
    '.dm-other .dm-cell{width:46px;padding-bottom:44px}',
    '.dm-road{height:34px;margin-top:8px;border-top:1px dashed var(--dm-line);display:flex;align-items:flex-end;justify-content:flex-end;font-size:11px;letter-spacing:2px;text-transform:uppercase;color:var(--dm-text4)}',
    '.dm-legend{display:flex;gap:18px;font-size:12px;color:var(--dm-text3);margin-top:auto;padding-top:10px;flex-wrap:wrap}',
    '.dm-legend span{display:flex;align-items:center;gap:6px}',
    '.dm-lg{width:14px;height:14px;border-radius:3px;display:inline-block}',
    '.dm-lg.dm-damaged{border:2px solid var(--dm-accent)}',
    '.dm-lg.dm-broken{border:2px solid rgba(229,57,53,.9);background:rgba(90,30,28,.6)}',
    '.dm-lg.dm-empty{background:rgba(255,240,210,.07);border:1px solid rgba(220,188,120,.22)}',
    '.dm-lg.dm-buy{background:repeating-linear-gradient(135deg,rgba(160,40,36,.3) 0 3px,rgba(160,40,36,.12) 3px 6px);border:1px solid rgba(229,57,53,.2)}',
    '.dm-note{padding:20px;color:var(--dm-text3);font-size:14px;text-align:center}',
    '.dm-side{width:380px;flex:none;display:flex;flex-direction:column;padding-bottom:12px;overflow:hidden}',
    '.dm-lock{margin:12px 16px 0;padding:8px 12px;border-radius:4px;border:1px solid rgba(255,193,7,.6);background:rgba(255,193,7,.1);color:#f3d38a;font-size:13px}',
    '.dm-big{display:flex;flex-direction:column;gap:8px;padding:4px 16px 10px}',
    '.dm-big .dm-btn{justify-content:center;font-size:15px;padding:11px 14px}',
    '.dm-warn{font-size:12px;color:#f3d38a}',
    '.dm-rlist{flex:1;min-height:0;overflow-y:auto;display:flex;flex-direction:column;gap:4px;padding:0 10px}',
    '.dm-rrow{display:flex;align-items:center;gap:10px;padding:7px 8px;border-radius:4px;background:var(--dm-card);border:1px solid var(--dm-soft);cursor:pointer}',
    '.dm-rrow:hover{background:rgba(84,70,52,.7);border-color:rgba(255,200,58,.4)}',
    '.dm-nm{flex:1;min-width:0;display:flex;flex-direction:column;gap:3px}',
    '.dm-nm b{font-size:13px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.dm-nm small,.dm-why{font-size:11px;color:var(--dm-text3)}',
    '.dm-rrow .dm-track{height:4px}',
    '.dm-cost{display:flex;gap:5px;flex-wrap:wrap}',
    '.dm-cost .dm-chip{font-size:11px;padding:1px 6px 1px 2px}',
    '.dm-sfoot{display:flex;align-items:center;gap:8px;padding:10px 16px 0;font-size:12px;color:var(--dm-text3)}',
    '.dm-calm{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--dm-text3)}',
    '.dm-calm .dm-svg{width:54px;height:54px;color:var(--dm-accent);opacity:.8}',
    '.dm-calm b{font-size:18px;color:var(--dm-text)}',
    '.dm-back{padding:12px 16px 2px;font-size:13px;color:var(--dm-text3);cursor:pointer}',
    '.dm-back:hover{color:var(--dm-accent)}',
    '.dm-ihd{display:flex;gap:14px;align-items:center;padding:10px 16px}',
    '.dm-ihd b{display:block;font-size:18px}',
    '.dm-ihd small{display:block;font-size:12px;color:var(--dm-text3);margin:2px 0 6px}',
    '.dm-rbig{width:56px;height:56px;flex:none;border-radius:4px;display:flex;align-items:center;justify-content:center;font-size:26px;font-weight:800;color:var(--dm-accent);background:rgba(255,200,58,.1);border:1px solid rgba(255,200,58,.45)}',
    '.dm-rbig.dm-sq{color:var(--dm-red2);background:rgba(255,240,210,.05);border-color:var(--dm-line)}',
    '.dm-rbig .dm-svg{width:22px;height:22px}',
    '.dm-hpl{display:flex;justify-content:space-between;padding:6px 16px 5px;font-size:13px;color:var(--dm-text3)}',
    '.dm-hpl b{color:var(--dm-text)}',
    '.dm-side>.dm-track{margin:0 16px;height:10px}',
    '.dm-side>.dm-cost{padding:8px 16px 0}',
    '.dm-acts{display:flex;gap:8px;padding:14px 16px 4px}',
    '.dm-acts .dm-btn:first-child{flex:1;justify-content:center;padding:9px 12px;font-size:14px}',
    '.dm-side>.dm-why,.dm-side>.dm-warn{padding:4px 16px}',
    '.dm-desc{font-size:13px;color:var(--dm-text2);line-height:1.5;padding:0 16px 6px}',
    '.dm-pcol{display:flex;flex-direction:column;gap:4px;padding:0 16px}',
    '.dm-opt{font:inherit;text-align:left;padding:8px 12px;border-radius:4px;font-size:13px;color:var(--dm-text2);background:var(--dm-card);border:1px solid var(--dm-soft);cursor:pointer}',
    '.dm-opt:hover{color:#fff;border-color:rgba(255,200,58,.5)}',
    '.dm-opt.dm-on{color:var(--dm-accent);background:rgba(255,200,58,.13);border-color:var(--dm-accent)}',
    '.dm-green{color:var(--dm-green2)}',
    '.dm-ptab{flex:1;min-width:0;display:flex;flex-direction:column;gap:12px}',
    '.dm-pcards{flex:1;min-height:0;overflow-y:auto;display:grid;grid-template-columns:repeat(auto-fill,minmax(520px,1fr));gap:12px;align-content:start}',
    '.dm-ptype{padding:6px 4px 14px;display:flex;flex-direction:column;gap:4px;overflow:visible}',
    '.dm-seg{display:flex;margin:0 16px;border:1px solid var(--dm-line);border-radius:4px;overflow:hidden}',
    '.dm-seg .dm-opt{flex:1;text-align:center;border:none;border-right:1px solid var(--dm-soft);border-radius:0;background:none;padding:9px 4px;color:var(--dm-text3)}',
    '.dm-seg .dm-opt:last-child{border-right:none}',
    '.dm-seg .dm-opt.dm-on{color:#2a1f10;background:var(--dm-accent);font-weight:700}',
    '.dm-orow{display:flex;align-items:center;gap:12px;margin:0 16px;padding:8px 12px;border-radius:4px;background:var(--dm-card);border:1px solid var(--dm-soft);font-size:13px}',
    '.dm-orow b{margin-left:auto;color:var(--dm-accent);font-weight:600}',
    '.dm-pnote{font-size:13px;color:var(--dm-text3);padding:0 4px}',
    '.dm-stab{flex:1}',
    '.dm-sgrid{display:grid;grid-template-columns:1fr 1fr;gap:18px;padding:6px 16px 16px}',
    '.dm-col{display:flex;flex-direction:column;gap:6px}',
    '.dm-col .dm-sect{padding-left:0}',
    '.dm-set{display:flex;align-items:center;gap:14px;padding:10px 14px;border-radius:4px;background:var(--dm-card);border:1px solid var(--dm-soft)}',
    '.dm-lbl{flex:1;min-width:0}',
    '.dm-lbl b{display:block;font-size:14px;font-weight:600}',
    '.dm-lbl small{display:block;font-size:12px;color:var(--dm-text3);margin-top:2px}',
    '.dm-set .dm-seg{margin:0;flex:none}',
    '.dm-set .dm-seg .dm-opt{padding:5px 11px;flex:none}',
    '.dm-kbd{font-size:14px;color:var(--dm-text2);border:1px solid rgba(220,188,120,.4);border-bottom-width:2px;border-radius:4px;padding:2px 10px}',
    '.dm-tog{width:38px;height:20px;padding:0;border-radius:10px;background:rgba(20,14,8,.5);border:1px solid var(--dm-line);position:relative;flex:none;cursor:pointer}',
    '.dm-tog::after{content:"";position:absolute;top:2px;left:2px;width:14px;height:14px;border-radius:50%;background:#a89a82}',
    '.dm-tog.dm-on{background:rgba(255,200,58,.35);border-color:var(--dm-accent)}',
    '.dm-tog.dm-on::after{left:20px;background:var(--dm-accent)}',
    '.dm-stepper{display:inline-flex;align-items:center;border:1px solid var(--dm-line);border-radius:4px;overflow:hidden;font-size:13px;flex:none}',
    '.dm-stepper button{font:inherit;padding:3px 9px;color:var(--dm-text3);cursor:pointer;background:rgba(255,240,210,.06);border:none}',
    '.dm-stepper button:hover:not(:disabled){color:var(--dm-accent)}',
    '.dm-stepper button:disabled{opacity:.4;cursor:default}',
    '.dm-stepper b{padding:3px 10px;color:var(--dm-accent);font-weight:600;min-width:58px;text-align:center}',
    '.dm-foot{display:flex;gap:14px;align-items:center;font-size:13px;color:var(--dm-text3);border-top:1px solid var(--dm-soft);padding-top:10px}',
    '.dm-status{flex:1;color:var(--dm-text2)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    root.id = 'dm-root';
    var plate = el('div', 'dm-plate unity-interactive');
    var head = el('div', 'dm-hd');
    var body = el('div', 'dm-body');
    var foot = el('div', 'dm-foot');
    var status = el('div', 'dm-status');
    var keys = el('div');

    add(foot, status, keys);
    add(plate, head, body, foot);
    root.appendChild(plate);

    document.body.appendChild(root);

    // Drawn at the player's UI size (Exiled's Toolkit). The full-screen mask always fills the screen, so the window on
    // it is what must fit.
    uiSize(root, { fit: plate });

    root.addEventListener('click', onClick);
    // Clicks inside the plate must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so capture them.
    root.addEventListener('error', function (e) {
      var img = e.target;

      if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
    }, true);

    ui = { root: root, head: head, body: body, status: status, keys: keys };
  }

  // Hands a panel to Exiled's Toolkit to be drawn at the player's UI size. Before the Toolkit's script has loaded it
  // waits in a list the Toolkit reads; without the Toolkit nothing happens.
  function uiSize(node, options) {
    options = Object.assign({ mod: 'DefenseManager' }, options);
    var toolkit = window.ExiledUi;

    if (toolkit) {
      toolkit.size(node, options);
      return;
    }

    var waiting = window.__exiledUiWait = window.__exiledUiWait || [];
    waiting.push([node, options]);
    if (waiting.length > 32) waiting.shift();
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

    var action = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');

    if (LOCAL[action]) {
      choose(action, args);
      render();

      return;
    }

    // Opening or shutting the panel gets no status line back, so it must not hold the buttons busy.
    if (action === 'close' || action === 'toggle') {
      send(action);

      return;
    }

    sendAction(args ? action + '|' + args : action);
  }

  // What the side panel shows: a defense, a row, a square, or with none of those picked the repair list. A second
  // click on what is already picked lets it go.
  function choose(action, args) {
    var was = { sel: state.sel, row: state.row, cell: state.cell };

    state.sel = null;
    state.row = null;
    state.cell = null;

    if (action === 'tab') state.tab = args;
    if (action === 'pick' && was.sel !== args) state.sel = args;
    if (action === 'row' && was.row !== Number(args)) state.row = Number(args);
    if (action === 'cell' && was.cell !== args) state.cell = args;
  }

  function icon(item, sizeClass) {
    var box = el('span', 'dm-ic ' + sizeClass);
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

  function track(defense) {
    var bar = el('div', 'dm-track');
    var fill = el('div', 'dm-fill dm-' + defense.state);
    var percent = defense.max > 0 ? Math.max(0, Math.min(100, Math.round(defense.cur * 100 / defense.max))) : 0;

    fill.style.width = percent + '%';
    bar.appendChild(fill);

    return bar;
  }

  function sect(text, right) {
    var node = el('div', 'dm-sect', text);

    if (right != null) node.appendChild(el('span', 'dm-r', String(right)));

    return node;
  }

  // ===== What the view says =====

  function defenses() {
    return state.data.defenses || [];
  }

  function byId(id) {
    return looked().ids[id] || null;
  }

  // What the drawing looks up per square and per row, worked out once per view instead of once per square: the square
  // at each place, the defense drawn there (the worst off when more than one stands there, the first of those on a
  // tie), each row's owned squares and waiting defenses, and the first defense with each id.
  var lookup = null;

  function looked() {
    var data = state.data;

    if (lookup && lookup.data === data) return lookup;

    var found = { data: data, squares: Object.create(null), worst: Object.create(null), owned: Object.create(null), waiting: Object.create(null), ids: Object.create(null) };
    var list = squares();
    var all = defenses();
    var i;

    for (i = 0; i < list.length; i++) {
      var square = list[i];
      var at = square.r + ':' + square.c;

      if (!(at in found.squares)) found.squares[at] = square;
      if (square.own) found.owned[square.r] = true;
    }

    for (i = 0; i < all.length; i++) {
      var defense = all[i];

      if (!(defense.id in found.ids)) found.ids[defense.id] = defense;
      if (!isOnGrid(defense)) continue;

      var place = defense.row + ':' + defense.col;
      var worst = found.worst[place];

      if (!worst || ratio(defense) < ratio(worst)) found.worst[place] = defense;
      if (needsRepair(defense)) found.waiting[defense.row] = (found.waiting[defense.row] || 0) + 1;
    }

    lookup = found;

    return found;
  }

  // Damaged and not yet queued for anything: what Repair all would take on.
  function needsRepair(defense) {
    return defense.state !== 'intact' && !defense.queued;
  }

  function worstFirst(list) {
    return list.slice().sort(function (a, b) { return ratio(a) - ratio(b); });
  }

  function ratio(defense) {
    return defense.max > 0 ? defense.cur / defense.max : 0;
  }

  function isOnGrid(defense) {
    return state.data.yard != null && defense.row != null && defense.col != null;
  }

  function where(defense) {
    return isOnGrid(defense) ? say('where', defense.row, defense.col) : say('offGrid');
  }

  function isLocked() {
    return !!state.data.locked;
  }

  function squares() {
    return state.data.yard ? state.data.yard.squares : [];
  }

  function squareAt(row, col) {
    return looked().squares[row + ':' + col] || null;
  }

  // The defense drawn on a square: the worst off when more than one stands there.
  function defenseAt(row, col) {
    return looked().worst[row + ':' + col] || null;
  }

  function modeWord(name) {
    return MODE_WORD[name] ? say(MODE_WORD[name]) : name;
  }

  // ===== Drawing =====

  // Squares and list rows from the last redraw, by place or defense id. A defense row the plugin sent unchanged is the
  // same object as before (see withHeldRows), so a node drawn from the same row with the same marks is put back as it
  // is instead of being drawn again. New words draw everything again.
  var drawn = {};
  var drawing = {};
  var drawnWords = null;

  function reused(key, owner, marks, make) {
    var kept = drawn[key];

    // The same key twice in one redraw (two rows with one id) gets a node of its own: one node cannot stand in two places.
    if (drawing[key]) return make();

    if (kept && kept.owner === owner && kept.marks === marks) {
      drawing[key] = kept;

      return kept.node;
    }

    var node = make();

    drawing[key] = { owner: owner, marks: marks, node: node };

    return node;
  }

  function render() {
    var words = window.__sldefenseWords || ENGLISH;

    if (words !== drawnWords) {
      drawnWords = words;
      drawn = {};
    }

    drawing = {};

    var scrolls = {};

    SCROLLERS.forEach(function (name) {
      var node = ui.body.querySelector('.' + name);

      if (node) scrolls[name] = node.scrollTop || 0;
    });

    if (state.tab !== 'yard' && state.tab !== 'power' && state.tab !== 'settings') state.tab = 'yard';
    if (state.sel != null && !byId(state.sel)) state.sel = null;

    renderHead();
    ui.body.textContent = '';

    if (state.tab === 'power') ui.body.appendChild(powerTab());
    else if (state.tab === 'settings') ui.body.appendChild(settingsTab());
    else add(ui.body, yard(), sidePanel());

    SCROLLERS.forEach(function (name) {
      var node = ui.body.querySelector('.' + name);

      if (node && scrolls[name]) node.scrollTop = scrolls[name];
    });

    drawn = drawing;
    renderKeys();
  }

  // The head draws only from the view, the tab and the words, so a click that picks a square or a row, which redraws
  // the body, leaves it as it is.
  var drawnHead = null;

  function renderHead() {
    var kept = drawnHead;

    if (kept && kept.head === ui.head && kept.data === state.data && kept.tab === state.tab && kept.words === drawnWords) return;

    var all = defenses();
    var owned = squares().filter(function (square) { return square.own; }).length;
    var ttl = el('div', 'dm-ttl', say('title'));
    var logo = add(el('div', 'dm-logo'), svg(SHIELD_PATH));
    var have = el('div', 'dm-have');
    var tabs = el('div', 'dm-tabs');
    var waiting = all.filter(needsRepair).length;

    ttl.appendChild(el('small', '', state.data.yard ? say('placed', all.length, owned, squares().length) : say('placedOnly', all.length)));

    (state.data.materials || []).forEach(function (material) {
      var chip = el('span', 'dm-chip' + (material.free < material.need ? ' dm-short' : ''));

      add(chip, icon(material, 'dm-i18'), el('span', '', material.name), el('b', '', material.free + ' / ' + material.need));
      have.appendChild(chip);
    });

    [['yard', 'tabYard', waiting], ['power', 'tabPower', 0], ['settings', 'tabSettings', 0]].forEach(function (tab) {
      var node = act(el('span', 'dm-tab' + (state.tab === tab[0] ? ' dm-on' : ''), say(tab[1])), 'tab', tab[0]);

      if (tab[2] > 0) node.appendChild(el('i', 'dm-n', String(tab[2])));

      tabs.appendChild(node);
    });

    ui.head.textContent = '';
    add(ui.head, logo, ttl, el('div', 'dm-sp'), have, tabs, button('X', 'close', null, true, 'dm-x'));
    drawnHead = { head: ui.head, data: state.data, tab: state.tab, words: drawnWords };
  }

  // ===== The yard =====

  function yard() {
    var data = state.data;
    var box = el('div', 'dm-yard dm-well');
    var top = el('div', 'dm-ytop');

    add(top, el('span', 'dm-house', say('house')), el('span', 'dm-hint', say('hint')));
    box.appendChild(top);

    if (defenses().length === 0 && !data.yard) {
      box.appendChild(el('div', 'dm-note', data.note || say('empty')));

      return box;
    }

    if (data.yard) add(box, wall(data.yard), grid(data.yard), el('div', 'dm-road', say('road')));

    add(box, offGrid());

    if (data.yard) box.appendChild(legend());

    return box;
  }

  function columns(yard) {
    return '34px repeat(' + yard.cols + ',minmax(0,1fr))';
  }

  // A door or window is three squares wide, the width of the sandbags that block it, centred on where it stands.
  function wall(yard) {
    var box = el('div', 'dm-wall');

    box.style.gridTemplateColumns = columns(yard);
    box.appendChild(el('div'));

    (yard.wall || []).forEach(function (feature) {
      var first = Math.max(1, Math.min(yard.cols - 2, Math.round(feature.at) - 1));
      var node = el('div', 'dm-wf dm-' + feature.kind, say(feature.kind));

      node.style.gridColumn = (first + 1) + ' / ' + (first + 4);
      box.appendChild(node);
    });

    return box;
  }

  function grid(yard) {
    var box = el('div', 'dm-grid');

    box.style.gridTemplateColumns = columns(yard);

    for (var row = 1; row <= yard.rows; row++) {
      box.appendChild(rowLabel(row));

      for (var col = 1; col <= yard.cols; col++) box.appendChild(cell(row, col));
    }

    return box;
  }

  function rowLabel(row) {
    var hasSquare = !!looked().owned[row];
    var waiting = looked().waiting[row] || 0;
    var node = act(el('div', 'dm-rlab' + (state.row === row ? ' dm-on' : '') + (hasSquare ? '' : ' dm-none'), String(row)), 'row', row);

    if (waiting > 0) node.appendChild(el('i', '', String(waiting)));

    return node;
  }

  function cell(row, col) {
    var key = row + ':' + col;
    var square = squareAt(row, col);
    var defense = defenseAt(row, col);
    var marks = (state.row === row ? ' dm-inrow' : '') + (state.cell === key ? ' dm-sel' : '');

    if (defense) return reused('c' + key, defense, marks + (state.sel === defense.id ? '|sel' : ''), function () { return defenseCell(defense, marks); });

    var own = !!(square && square.own);
    var price = square ? square.price : 0;

    return reused('c' + key, null, marks + '|' + own + '|' + price, function () { return emptyCell(key, own, price, marks); });
  }

  function emptyCell(key, own, price, marks) {
    var inner = el('div', 'dm-in');

    if (!own) {
      if (price > 0) inner.appendChild(el('span', 'dm-price', String(price)));

      return add(act(el('div', 'dm-cell dm-buy' + marks), 'cell', key), inner);
    }

    return add(act(el('div', 'dm-cell dm-empty' + marks), 'cell', key), inner);
  }

  function defenseCell(defense, marks) {
    var classes = 'dm-cell dm-def dm-s-' + defense.state + marks + (state.sel === defense.id ? ' dm-sel' : '') + (defense.queued ? ' dm-q' : '') + (defense.power && defense.power.on ? ' dm-on' : '');
    var inner = el('div', 'dm-in');

    inner.appendChild(icon(defense, 'dm-ci'));
    if (defense.power) inner.appendChild(add(el('i', 'dm-pw'), svg(BOLT_PATH)));
    if (defense.queued) inner.appendChild(add(el('i', 'dm-qd'), svg(WRENCH_PATH)));
    if (defense.state !== 'intact') inner.appendChild(track(defense));

    return add(act(el('div', classes), 'pick', defense.id), inner);
  }

  // Defenses the grid has no square for (or every defense when there is no grid), so each can still be picked.
  function offGrid() {
    var list = defenses().filter(function (defense) { return !isOnGrid(defense); });

    if (list.length === 0) return null;

    var box = el('div', 'dm-other');

    box.appendChild(el('span', 'dm-olab', say('offGridLabel')));
    list.forEach(function (defense) {
      box.appendChild(reused('o' + defense.id, defense, state.sel === defense.id ? 'sel' : '', function () { return defenseCell(defense, ''); }));
    });

    return box;
  }

  function legend() {
    var box = el('div', 'dm-legend');

    [['damaged', 'damaged'], ['broken', 'broken'], ['empty', 'legEmpty'], ['buy', 'legBuy']].forEach(function (entry) {
      box.appendChild(add(el('span'), el('i', 'dm-lg dm-' + entry[0]), el('span', '', say(entry[1]))));
    });

    return box;
  }

  // ===== The side panel =====

  function sidePanel() {
    var picked = state.sel != null ? byId(state.sel) : null;

    if (picked) return inspector(picked);
    if (state.row != null && state.data.yard) return rowPanel(state.row);
    if (state.cell != null && state.data.yard) return squarePanel(state.cell);

    return repairsPanel();
  }

  function side() {
    var box = el('div', 'dm-side dm-well');

    if (state.data.locked) box.appendChild(el('div', 'dm-lock', state.data.locked));

    return box;
  }

  function back() {
    return act(el('div', 'dm-back', say('back')), 'back');
  }

  function costChips(cost) {
    if (!cost || cost.length === 0) return null;

    var box = el('div', 'dm-cost');

    cost.forEach(function (entry) {
      box.appendChild(add(el('span', 'dm-chip' + (entry.short > 0 ? ' dm-short' : '')), icon(entry, 'dm-i18'), el('b', '', String(entry.count)), el('span', '', entry.name)));
    });

    return box;
  }

  // One defense in a list. Its Repair button stays quiet beside the one bright button of the panel. Where it stands
  // reads differently with and without a grid, so that is part of what it was drawn from.
  function repairRow(defense) {
    return reused('r' + defense.id, defense, state.data.yard ? 'grid' : 'nogrid', function () { return drawRepairRow(defense); });
  }

  function drawRepairRow(defense) {
    var node = act(el('div', 'dm-rrow'), 'pick', defense.id);
    var name = el('div', 'dm-nm');

    add(name, el('b', '', defense.name), el('small', '', where(defense)), track(defense));

    if (defense.state !== 'intact' && !defense.queued) add(name, costChips(defense.cost));
    if (!defense.canRepair && !defense.queued && defense.why) name.appendChild(el('span', 'dm-why', defense.why));

    add(node, icon(defense, 'dm-i30'), name);

    if (defense.queued) node.appendChild(el('span', 'dm-badge dm-b-queued', say(QUEUED_WORD[defense.queued] || defense.queued)));
    else if (defense.state === 'intact') node.appendChild(el('span', 'dm-badge dm-b-intact', say('intact')));
    else node.appendChild(button(say('repair'), 'repair', defense.id, defense.canRepair, 'dm-sm dm-quiet'));

    return node;
  }

  function repairsPanel() {
    var data = state.data;
    var box = side();
    var list = worstFirst(defenses().filter(needsRepair));
    var queued = defenses().filter(function (defense) { return defense.queued; }).length;
    var foot = el('div', 'dm-sfoot');

    add(foot, queued > 0 ? el('span', '', say('queuedCount', queued)) : null, el('span', 'dm-sp'), button(say('clearAll', data.clearAll), 'clearbroken', null, !isLocked() && data.clearAll > 0, 'dm-sm dm-danger'));

    if (list.length === 0) {
      var calm = el('div', 'dm-calm');

      add(calm, svg(SHIELD_PATH), el('b', '', say('allGood')), el('small', '', queued > 0 ? say('queuedCount', queued) : say('nothingNeeds')));

      return add(box, calm, foot);
    }

    var big = el('div', 'dm-big');
    var short = (data.materials || []).filter(function (material) { return material.free < material.need; });

    big.appendChild(wrenchButton(say('repairAll', data.repairAll), 'repairall', null, !isLocked(), 'dm-gold'));

    if (short.length > 0) {
      big.appendChild(el('small', 'dm-warn', say('shortOf', short.map(function (material) { return material.name + ' ' + material.free + ' / ' + material.need; }).join(', '))));
    }

    var rows = el('div', 'dm-rlist');

    list.forEach(function (defense) { rows.appendChild(repairRow(defense)); });

    return add(box, sect(say('needsRepair'), list.length), big, rows, foot);
  }

  function inspector(defense) {
    var box = side();
    var head = el('div', 'dm-ihd');
    var info = el('div');
    var condition = el('div', 'dm-hpl');
    var acts = el('div', 'dm-acts');

    add(info, el('b', '', defense.name), el('small', '', where(defense)), el('span', 'dm-badge dm-b-' + defense.state, say(STATE_WORD[defense.state] || defense.state)));

    if (defense.queued) info.appendChild(el('span', 'dm-badge dm-b-queued', say(QUEUED_WORD[defense.queued] || defense.queued)));

    add(head, icon(defense, 'dm-i72'), info);
    add(condition, el('span', '', say('condition')), el('b', '', defense.cur + ' / ' + defense.max));

    if (defense.queued) acts.appendChild(button(say(QUEUED_WORD[defense.queued] || defense.queued), 'repair', defense.id, false));
    else if (defense.state === 'intact') acts.appendChild(button(say('nothingToRepair'), 'repair', defense.id, false));
    else acts.appendChild(wrenchButton(say('repair'), 'repair', defense.id, defense.canRepair, 'dm-gold'));

    acts.appendChild(button(say('remove'), 'remove', defense.id, defense.canRemove, 'dm-danger'));
    add(box, back(), head, condition, track(defense));

    if (defense.state !== 'intact' && !defense.queued) add(box, costChips(defense.cost));

    box.appendChild(acts);

    if (!defense.canRepair && !defense.queued && defense.state !== 'intact' && defense.why) box.appendChild(el('div', 'dm-why', defense.why));
    if (!defense.canRemove && defense.removeWhy) box.appendChild(el('div', 'dm-why', defense.removeWhy));
    if (defense.power) power(box, defense);

    return box;
  }

  // A powered defense's own setting: follow its type, or one of the modes. The pick shows what is set now.
  function power(box, defense) {
    var p = defense.power;
    var list = el('div', 'dm-pcol');
    var now = el('span', p.on ? 'dm-green' : '', say(p.on ? 'onNow' : 'offNow'));
    var heading = sect(say('tabPower'));

    heading.appendChild(add(el('span', 'dm-r'), now));
    list.appendChild(option(say('sameAsType', defense.name, p.typeMode ? p.typeMode.label : ''), 'power', defense.id + '|' + NO_MODE, p.own == null));

    MODES.forEach(function (name) { list.appendChild(option(modeWord(name), 'power', defense.id + '|' + name, p.own === name)); });

    add(box, heading, p.managed ? null : el('div', 'dm-why', say('unmanaged')), list);
  }

  function option(text, action, args, isOn) {
    return act(el('button', 'dm-opt' + (isOn ? ' dm-on' : ''), text), action, args);
  }

  function rowPanel(row) {
    var box = side();
    var yardData = state.data.yard;
    var list = defenses().filter(function (defense) { return isOnGrid(defense) && defense.row === row; }).sort(function (a, b) { return a.col - b.col; });
    var waiting = worstFirst(list.filter(needsRepair));
    var inRow = squares().filter(function (square) { return square.r === row; });
    var mine = inRow.filter(function (square) { return square.own; }).length;
    var prices = inRow.filter(function (square) { return !square.own && square.price > 0; }).map(function (square) { return square.price; });
    var head = el('div', 'dm-ihd');
    var info = el('div');
    var desc = say('rowInfo', list.length, mine, yardData.cols);
    var rows = el('div', 'dm-rlist');

    add(info, el('b', '', say('row', row)), el('small', '', row === 1 ? say('againstHouse') : row === 2 ? say('rowOut1') : say('rowsOut', row - 1)));
    add(head, el('div', 'dm-rbig', String(row)), info);

    if (prices.length > 0) desc += ' ' + say('rowPrice', distinct(prices).join(' / '));

    add(box, back(), head, el('p', 'dm-desc', desc));

    if (waiting.length > 0) box.appendChild(add(el('div', 'dm-big'), wrenchButton(say('repairRow', row, waiting.length), 'repairrow', row, !isLocked(), 'dm-gold')));

    (waiting.length > 0 ? waiting : list).forEach(function (defense) { rows.appendChild(repairRow(defense)); });

    return add(box, rows);
  }

  function distinct(values) {
    return values.filter(function (value, i) { return values.indexOf(value) === i; }).sort(function (a, b) { return a - b; });
  }

  function squarePanel(key) {
    var box = side();
    var at = key.split(':').map(Number);
    var square = squareAt(at[0], at[1]);
    var isMine = !!(square && square.own);
    var head = el('div', 'dm-ihd');
    var info = el('div');
    var mark = el('div', 'dm-rbig dm-sq');

    if (!isMine) mark.appendChild(svg(LOCK_PATH));

    add(info, el('b', '', say(isMine ? 'emptySquare' : 'notYours')), el('small', '', say('where', at[0], at[1])));
    add(head, mark, info);

    return add(box, back(), head, el('p', 'dm-desc', isMine ? say('buildHere') : say('unlockFor', square ? square.price : '?')));
  }

  // ===== The Power tab =====

  function powerTab() {
    var box = el('div', 'dm-ptab');
    var cards = el('div', 'dm-pcards');
    var types = [];
    var byType = {};

    defenses().forEach(function (defense) {
      if (!defense.power) return;

      if (!byType[defense.type]) {
        byType[defense.type] = [];
        types.push(defense.type);
      }

      byType[defense.type].push(defense);
    });

    if (types.length === 0) cards.appendChild(el('div', 'dm-note', say('noPower')));

    types.forEach(function (type) { cards.appendChild(typeCard(byType[type])); });

    return add(box, cards, el('div', 'dm-pnote', say('powerNote')));
  }

  // Every defense of one type: the type's own setting, and the ones set on their own with a way back to the type's.
  function typeCard(list) {
    var first = list[0];
    var p = first.power;
    var global = state.data.global;
    var card = el('div', 'dm-ptype dm-well');
    var head = el('div', 'dm-ihd');
    var seg = el('div', 'dm-seg');
    var own = list.filter(function (defense) { return defense.power.own != null; });

    add(head, icon(first, 'dm-i56'), add(el('div'), el('b', '', first.name), el('small', '', say('placedOnly', list.length))));
    seg.appendChild(option(say('global', global ? global.label : ''), 'powertype', first.id + '|' + NO_MODE, p.typeOwn == null));

    MODES.forEach(function (name) { seg.appendChild(option(modeWord(name), 'powertype', first.id + '|' + name, p.typeOwn === name)); });

    add(card, head, sect(say('everyType', first.name)), seg, sect(say('ownList'), own.length));

    if (own.length === 0) card.appendChild(el('p', 'dm-desc', say('noneOwn')));

    own.forEach(function (defense) {
      card.appendChild(add(el('div', 'dm-orow'), el('span', '', where(defense)), el('b', '', modeWord(defense.power.own)), button(say('followType'), 'power', defense.id + '|' + NO_MODE, true, 'dm-sm')));
    });

    return card;
  }

  // ===== The Settings tab =====

  function settingsTab() {
    var s = state.data.settings;
    var box = el('div', 'dm-stab dm-well');

    if (!s) return add(box, el('div', 'dm-note', say('noSettings')));

    var grid = el('div', 'dm-sgrid');
    var left = el('div', 'dm-col');
    var right = el('div', 'dm-col');
    var modes = el('div', 'dm-seg');
    var toggle = act(el('button', 'dm-tog' + (s.layout ? ' dm-on' : '')), 'set', 'layoutcontrols|' + (s.layout ? '0' : '1'));

    MODES.forEach(function (name) { modes.appendChild(option(modeWord(name), 'set', 'defaultmode|' + name, s.mode === name)); });

    add(left,
      sect(say('setControls')),
      setRow('setKey', 'setKeyDesc', el('span', 'dm-kbd', s.key || say('keyNone'))),
      setRow('setLayout', 'setLayoutDesc', toggle),
      sect(say('tabPower')),
      setRow('setMode', 'setModeDesc', modes),
      setRow('setRange', 'setRangeDesc', stepper('proximityrange', s.range, function (v) { return say('meters', v); })));
    add(right,
      sect(say('setCombat')),
      setRow('setCrawler', 'setCrawlerDesc', stepper('crawlerdodge', s.crawler, percent)),
      setRow('setArmored', 'setArmoredDesc', stepper('armoredcrawlerdodge', s.armored, percent)),
      setRow('setRate', 'setRateDesc', stepper('attackrate', s.rate, function (v) { return v + 'x'; })));

    return add(box, add(grid, left, right));
  }

  function percent(value) {
    return Math.round(value * 100) + '%';
  }

  function setRow(nameKey, descKey, control) {
    var label = add(el('div', 'dm-lbl'), el('b', '', say(nameKey)), el('small', '', say(descKey)));

    return add(el('div', 'dm-set'), label, control);
  }

  // Minus and plus send the value one step along; the plugin keeps it in range and redraws the tab with what it saved.
  function stepper(name, value, format) {
    var range = STEPS[name];
    var box = el('span', 'dm-stepper');
    var down = stepped(value - range.step);
    var up = stepped(value + range.step);

    return add(box,
      button('-', 'set', name + '|' + down, value > range.min),
      el('b', '', format(value)),
      button('+', 'set', name + '|' + up, value < range.max));
  }

  // Two decimals at most, so 0.1 + 0.05 goes out as 0.15 and not 0.15000000000000002.
  function stepped(value) {
    return Math.round(value * 100) / 100;
  }

  function renderKeys() {
    ui.keys.textContent = state.key ? say('keyClose', state.key) : say('escClose');
  }

  // A defense row the page was last sent unchanged comes as its id alone; it is the row the page already holds, so the
  // one in the view shown now takes its place. An id with no such row (a view the page never got) is left out and the
  // whole view asked for again.
  function withHeldRows(data) {
    var list = data && data.defenses;

    if (!list) return data;

    var held = looked().ids;
    var isMissing = false;

    for (var i = 0; i < list.length; i++) {
      if (typeof list[i] !== 'string') continue;

      if (list[i] in held) list[i] = held[list[i]];
      else isMissing = true;
    }

    if (isMissing) {
      data.defenses = list.filter(function (row) { return typeof row !== 'string'; });
      send('resend');
    }

    return data;
  }

  function open(data) {
    build();
    state.data = withHeldRows(data) || state.data;
    state.isOpen = true;
    state.sel = null;
    state.row = null;
    state.cell = null;
    ui.status.textContent = '';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    state.data = withHeldRows(data) || state.data;

    if (state.isOpen) render();
  }

  function close() {
    state.isOpen = false;

    if (ui) ui.root.style.display = 'none';
  }

  function status(text) {
    build();
    clearBusy();

    // With the panel closed the plugin shows the line as an Exiled's Toolkit toast instead.
    if (state.isOpen) ui.status.textContent = text;
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

    placedHud = null;
    placedBar = null;

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

      if (isHud) placeHudButton(frames[i], doc);
      else placeLayoutBar(frames[i], doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // What this script last put on the game's pages, with the iframe and document it went into. The 400 ms check looks
  // for iframes only when one of them is gone or out of date; outside a game, with nothing placed, it does nothing.
  var placedHud = null;
  var placedBar = null;

  function checkPages() {
    try {
      if (!state.isLive && !placedHud && !placedBar) return;
      if (isHudPlaced() && (state.layout ? isBarPlaced() : !placedBar)) return;

      patchPages();
    } catch (e) {}
  }

  // A node in an iframe the game dropped, or in a document the iframe has since replaced, still reads as connected to
  // its own document, so the iframe and its document are checked too.
  function isStillIn(placed) {
    return placed.frame.isConnected && placed.frame.contentDocument === placed.doc;
  }

  function isHudPlaced() {
    var placed = placedHud;

    return !!placed && state.isLive && isStillIn(placed) && placed.column.isConnected && placed.node.parentNode === placed.column && placed.node.getAttribute('data-key') === state.key;
  }

  function isBarPlaced() {
    var placed = placedBar;

    return !!placed && state.isLive && !!state.layout && isStillIn(placed) && placed.node.isConnected && placed.node.getAttribute('data-version') === String(layoutVersion);
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  function placeHudButton(frame, doc) {
    var column = doc.querySelector('.vertical-tools');
    var existing = doc.querySelector('.' + HUD_CLASS);

    if (!column || !state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column && existing.getAttribute('data-key') === state.key) {
      placedHud = { frame: frame, doc: doc, column: column, node: existing };

      return;
    }

    if (existing) existing.remove();

    var node = hudButton(doc);

    node.setAttribute('data-etk-dock', DOCK_ORDER);
    column.insertBefore(node, column.firstChild);
    placedHud = { frame: frame, doc: doc, column: column, node: node };

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
  function placeLayoutBar(frame, doc) {
    var existing = doc.getElementById(LAYOUT_ID);

    if (!state.layout || !state.isLive || !doc.body) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.getAttribute('data-version') === String(layoutVersion)) {
      placedBar = { frame: frame, doc: doc, node: existing };

      return;
    }

    addLayoutStyle(doc);

    var bar = layoutBar(doc);

    if (existing) existing.parentNode.replaceChild(bar, existing);
    else doc.body.appendChild(bar);

    placedBar = { frame: frame, doc: doc, node: bar };

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

  setInterval(checkPages, 400);

  window.__sldefense = { open: open, setData: setData, close: close, status: status, live: live, layout: layout };
  send('ready');
})();
