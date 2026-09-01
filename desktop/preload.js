/* الجسر الوحيد بين الصفحة والنظام.
 *
 * لا يُصدَّر إلا ما تحتاجه الصفحة فعلاً. والصفحة تعمل بلا هذا الجسر
 * أصلاً — فهي موقعٌ يُفتح في أيّ متصفّح — فوجودُه يزيدها ولا يشترطها.
 * ولذلك يُفحص دائماً: window.masrouf && window.masrouf.backup
 */
const { contextBridge, ipcRenderer } = require('electron');

/* يُقرأ من الوسائط لا بنداءٍ غير متزامن: الصفحة ترسم نفسها فوراً،
   ورقمٌ يصل بعد الرسم لا يُعرض. */
let appVersion = '';
(process.argv || []).forEach(function(a){
  if (String(a).indexOf('--masrouf-version=') === 0)
    appVersion = String(a).slice('--masrouf-version='.length);
});

contextBridge.exposeInMainWorld('masrouf', {
  desktop: true,
  version: appVersion,
  /* حوار حفظٍ من النظام: يختار المستخدم المجلّد والاسم */
  backup: () => ipcRenderer.invoke('masrouf:backup'),
  /* والصفحة تحفظ ما تشاء بأيّ صيغة — لا JSON وحده */
  saveAs: (name, text) => ipcRenderer.invoke('masrouf:saveAs', String(name), String(text))
});
