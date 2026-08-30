/* عامل الخدمة — يجعل التطبيق يعمل بلا إنترنت
 *
 * قاعدتان بحسب طبيعة الملفّ:
 *
 *   الصفحة نفسها  الشبكة أولاً، والمخزَّن احتياطاً. لأن التطبيق ملفّ
 *                 واحد يحمل الشيفرة كلها؛ فلو خُزّن أولاً لبقي
 *                 المستخدم على نسخةٍ قديمة لا يدري.
 *   ما لا يتغيّر   الأيقونات والخطوط: المخزَّن أولاً. توفيرٌ بلا خطر.
 *
 * ولا تُخزَّن بيانات المستخدم هنا إطلاقاً — هي في localStorage، ومسح
 * ذاكرة العامل لا يمسّها.
 */
var VERSION = 'masrouf-v1';
var SHELL = [
  './',
  './index.html',
  './manifest.webmanifest',
  './icon-192.png',
  './icon-512.png',
  './icon-maskable-512.png'
];

self.addEventListener('install', function (e) {
  e.waitUntil(
    caches.open(VERSION)
      .then(function (c) { return c.addAll(SHELL); })
      .then(function () { return self.skipWaiting(); })
      .catch(function () { /* ملفّ ناقص لا يمنع التثبيت */ })
  );
});

self.addEventListener('activate', function (e) {
  e.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(keys.map(function (k) {
        return k === VERSION ? null : caches.delete(k);
      }));
    }).then(function () { return self.clients.claim(); })
  );
});

function isFont(url) {
  return url.indexOf('fonts.googleapis.com') >= 0 ||
         url.indexOf('fonts.gstatic.com') >= 0;
}

self.addEventListener('fetch', function (e) {
  var req = e.request;
  if (req.method !== 'GET') return;

  var url = req.url;

  /* الخطوط وما لا يتغيّر: المخزَّن أولاً */
  if (isFont(url) || /\.(png|svg|webmanifest)$/.test(url)) {
    e.respondWith(
      caches.match(req).then(function (hit) {
        if (hit) return hit;
        return fetch(req).then(function (res) {
          var copy = res.clone();
          caches.open(VERSION).then(function (c) { c.put(req, copy); });
          return res;
        }).catch(function () { return hit; });
      })
    );
    return;
  }

  /* الصفحة: الشبكة أولاً كي تصل التحديثات، والمخزَّن حين لا شبكة */
  if (req.mode === 'navigate' || /index\.html$/.test(url) || url.replace(/[?#].*$/, '').endsWith('/')) {
    e.respondWith(
      fetch(req).then(function (res) {
        var copy = res.clone();
        caches.open(VERSION).then(function (c) { c.put('./index.html', copy); });
        return res;
      }).catch(function () {
        return caches.match('./index.html').then(function (hit) {
          return hit || new Response(
            '<meta charset="utf-8"><div style="font:16px system-ui;padding:2rem;direction:rtl">' +
            'لا اتّصال، ولا نسخة مخزَّنة بعد. افتح التطبيق مرّة واحدة وأنت متّصل.</div>',
            { headers: { 'Content-Type': 'text/html; charset=utf-8' } });
        });
      })
    );
  }
});
