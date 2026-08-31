/* مصروف — العملية الرئيسة
 *
 * ثلاثة قرارات تشرح هذا الملفّ:
 *
 * ١ · المحتوى من الموقع أوّلاً، ونسخةٌ مضمَّنة تُحمَّل حين لا شبكة.
 *     فأيّ تحسينٍ في التطبيق يصل فوراً بلا تنزيل مئة ميغابايت، ولا
 *     يُصدَر سطح المكتب إلا حين يتغيّر الهيكل نفسه.
 *
 * ٢ · التحديث يُنزَّل ولا يُثبَّت. يقف وينتظر ضغطة صاحبه، وقبلها
 *     يعرض عليه أن يحفظ نسخةً احتياطية — بحوار حفظٍ من النظام لا
 *     بتنزيل متصفّح، فيختار المجلّد والاسم.
 *
 * ٣ · لا nodeIntegration ولا وصولَ للصفحة إلى النظام. الجسر الوحيد
 *     preload، ولا يُصدِّر إلا ما يلزم.
 */

const { app, BrowserWindow, dialog, shell, Menu, ipcMain } = require('electron');
const { autoUpdater } = require('electron-updater');
const path = require('path');
const fs = require('fs');

const SITE = 'https://relifeg1.github.io/masrouf/';
const OFFLINE = path.join(__dirname, 'offline', 'index.html');
const LSKEY = 'khettati_v3';
const STATE = path.join(app.getPath('userData'), 'window.json');

let win = null;
let pendingUpdate = null;

/* ── حجم النافذة وموضعها يبقيان بين الجلسات ── */
function loadBounds() {
  try {
    const b = JSON.parse(fs.readFileSync(STATE, 'utf8'));
    if (b && b.width > 400 && b.height > 300) return b;
  } catch (e) { /* أوّل تشغيل */ }
  return { width: 1280, height: 900 };
}

function saveBounds() {
  if (!win || win.isDestroyed() || win.isMinimized()) return;
  try {
    fs.writeFileSync(STATE, JSON.stringify(win.getNormalBounds()), 'utf8');
  } catch (e) { /* لا يمنع الإغلاق */ }
}

/* ── النسخة الاحتياطية: تُقرأ من الصفحة وتُكتب حيث يختار ── */
async function backupToFile(reason) {
  if (!win || win.isDestroyed()) return false;
  let raw = null;
  try {
    raw = await win.webContents.executeJavaScript(
      `localStorage.getItem(${JSON.stringify(LSKEY)})`, true);
  } catch (e) { raw = null; }

  if (!raw) {
    await dialog.showMessageBox(win, {
      type: 'info', title: 'مصروف',
      message: 'لا توجد بيانات لحفظها بعد.',
      buttons: ['حسناً'], defaultId: 0
    });
    return false;
  }

  const d = new Date();
  const stamp = d.getFullYear() + '-'
    + String(d.getMonth() + 1).padStart(2, '0') + '-'
    + String(d.getDate()).padStart(2, '0');

  const res = await dialog.showSaveDialog(win, {
    title: reason || 'نسخة احتياطية',
    defaultPath: path.join(app.getPath('downloads'), `masrouf-${stamp}.json`),
    filters: [{ name: 'JSON', extensions: ['json'] }]
  });
  if (res.canceled || !res.filePath) return false;

  try {
    fs.writeFileSync(res.filePath, raw, 'utf8');
    return true;
  } catch (e) {
    await dialog.showMessageBox(win, {
      type: 'error', title: 'مصروف',
      message: 'تعذّر حفظ الملفّ:\n' + e.message,
      buttons: ['حسناً']
    });
    return false;
  }
}

/* ── التحديث: يُعرض ولا يُفرض ── */
async function offerUpdate(info) {
  if (!win || win.isDestroyed()) return;
  const ver = (info && info.version) ? info.version : '';
  const r = await dialog.showMessageBox(win, {
    type: 'info',
    title: 'نسخة جديدة جاهزة',
    message: 'نزلت نسخة جديدة' + (ver ? ` (${ver})` : '') + '.',
    detail: 'خذ نسخةً احتياطية قبل التحديث — دقيقةٌ تقيك يوماً.\n'
          + 'وبياناتك لا تُمسّ بالتحديث.',
    buttons: ['انسخ بياناتي ثم حدّث', 'حدّث بلا نسخة', 'لاحقاً'],
    defaultId: 0,
    cancelId: 2,
    noLink: true
  });

  if (r.response === 2) return;                 /* لاحقاً */
  if (r.response === 0) {
    const ok = await backupToFile('احفظ نسخةً احتياطية قبل التحديث');
    if (!ok) {
      const c = await dialog.showMessageBox(win, {
        type: 'question', title: 'مصروف',
        message: 'لم تُحفظ نسخة احتياطية.',
        buttons: ['حدّث على أي حال', 'إلغاء'],
        defaultId: 1, cancelId: 1, noLink: true
      });
      if (c.response !== 0) return;
    }
  }
  saveBounds();
  autoUpdater.quitAndInstall(false, true);
}

function wireUpdater() {
  if (!app.isPackaged) return;                  /* لا تحديث في التطوير */
  autoUpdater.autoDownload = true;
  autoUpdater.autoInstallOnAppQuit = false;     /* لا يُثبَّت خلسةً عند الإغلاق */
  autoUpdater.on('update-downloaded', (info) => {
    pendingUpdate = info;
    offerUpdate(info);
  });
  autoUpdater.on('error', () => { /* انقطاع شبكةٍ لا يُزعج أحداً */ });
  autoUpdater.checkForUpdates().catch(() => {});
  setInterval(() => { autoUpdater.checkForUpdates().catch(() => {}); }, 6 * 3600 * 1000);
}

/* ── القائمة: عربية ومختصرة ── */
function buildMenu() {
  const t = [
    {
      label: 'مصروف',
      submenu: [
        {
          label: 'احفظ نسخة احتياطية…',
          accelerator: 'CmdOrCtrl+S',
          click: () => backupToFile('نسخة احتياطية')
        },
        { type: 'separator' },
        {
          label: 'ابحث عن تحديث',
          click: () => {
            if (pendingUpdate) return offerUpdate(pendingUpdate);
            if (app.isPackaged) autoUpdater.checkForUpdates().catch(() => {});
          }
        },
        {
          label: 'افتح الموقع في المتصفّح',
          click: () => shell.openExternal(SITE)
        },
        { type: 'separator' },
        { role: 'quit', label: 'خروج' }
      ]
    },
    {
      label: 'عرض',
      submenu: [
        { role: 'reload', label: 'أعد التحميل' },
        { role: 'resetZoom', label: 'حجم أصلي' },
        { role: 'zoomIn', label: 'تكبير' },
        { role: 'zoomOut', label: 'تصغير' },
        { type: 'separator' },
        { role: 'togglefullscreen', label: 'ملء الشاشة' }
      ]
    }
  ];
  Menu.setApplicationMenu(Menu.buildFromTemplate(t));
}

function createWindow() {
  const b = loadBounds();
  win = new BrowserWindow({
    width: b.width, height: b.height, x: b.x, y: b.y,
    minWidth: 380, minHeight: 520,
    backgroundColor: '#16161a',
    title: 'مصروف',
    show: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  });

  win.once('ready-to-show', () => win.show());
  win.on('close', saveBounds);
  win.on('closed', () => { win = null; });

  /* روابط خارجية تُفتح في المتصفّح لا داخل التطبيق */
  win.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: 'deny' };
  });

  /* لا شبكة؟ النسخة المضمَّنة. وهي احتياطٌ لا أصل. */
  win.webContents.on('did-fail-load', (e, code, desc, url, isMain) => {
    if (!isMain) return;
    if (fs.existsSync(OFFLINE)) win.loadFile(OFFLINE);
  });

  win.loadURL(SITE);
}

app.whenReady().then(() => {
  buildMenu();
  createWindow();
  wireUpdater();
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

/* الصفحة تطلب نسخةً احتياطية عبر الجسر */
ipcMain.handle('masrouf:backup', () => backupToFile('نسخة احتياطية'));
