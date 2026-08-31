# -*- coding: utf-8 -*-
"""يبني Masrouf.app للماك — من ويندوز، وهذا حدُّه.

حزمة ‎.app ليست ملفّاً ثنائياً بل مجلَّدٌ باتّفاقٍ معروف: Info.plist
ونصٌّ تنفيذيّ وأيقونة. فيمكن تركيبها هنا وضغطها مع حفظ صلاحية التنفيذ.

وما لا يمكن هنا فيُقال صراحةً:
  · ‎.dmg يحتاج hdiutil ولا وجود له إلا على ماك.
  · التوقيع والتوثيق (codesign / notarytool) يحتاجان ماك وحساب مطوّر.
    فبدونهما يعترض Gatekeeper، ويُفتح بالنقر الأيمن ← فتح أوّل مرة.
  · ولم يُختبَر على ماك — لا ماك هنا. البناء صحيح بحسب الاتّفاق
    المكتوب، لا بحسب تشغيلٍ رأيته.
"""
import io
import os
import struct
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PNG512 = os.path.join(ROOT, 'icon-512.png')
OUT = os.path.join(HERE, 'Masrouf-mac.zip')
URL = 'https://relifeg1.github.io/masrouf/'

LAUNCH = '''#!/bin/sh
# مصروف — يفتح التطبيق في نافذةٍ مستقلّة إن وُجد متصفّح يدعم ذلك،
# وإلا ففي المتصفّح الافتراضي. والتحديث يبقى تلقائياً من الموقع.
URL="%s"
for B in \\
  "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" \\
  "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge" \\
  "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser"
do
  if [ -x "$B" ]; then
    exec "$B" --app="$URL" --window-size=1280,900
  fi
done
exec open "$URL"
''' % URL

PLIST = '''<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>                <string>Masrouf</string>
  <key>CFBundleDisplayName</key>         <string>مصروف</string>
  <key>CFBundleIdentifier</key>          <string>io.github.relifeg1.masrouf</string>
  <key>CFBundleVersion</key>             <string>1.1</string>
  <key>CFBundleShortVersionString</key>  <string>1.1</string>
  <key>CFBundlePackageType</key>         <string>APPL</string>
  <key>CFBundleExecutable</key>          <string>masrouf</string>
  <key>CFBundleIconFile</key>            <string>masrouf</string>
  <key>LSMinimumSystemVersion</key>      <string>10.13</string>
  <key>NSHighResolutionCapable</key>     <true/>
</dict>
</plist>
'''


def icns(png_path):
    """ICNS يقبل PNG كما هو داخل إدخالٍ من نوع ic09 (512×512)."""
    png = io.open(png_path, 'rb').read()
    assert png[:8] == b'\x89PNG\r\n\x1a\n', 'ليست PNG'
    w, h = struct.unpack('>II', png[16:24])
    assert (w, h) == (512, 512), 'ic09 يريد 512×512 لا %dx%d' % (w, h)
    entry = b'ic09' + struct.pack('>I', 8 + len(png)) + png
    return b'icns' + struct.pack('>I', 8 + len(entry)) + entry, w, h


data, w, h = icns(PNG512)
print('الأيقونة: %dx%d · %d بايت' % (w, h, len(data)))

APP = 'Masrouf.app/Contents/'
files = [
    (APP + 'Info.plist', PLIST.encode('utf-8'), 0o644),
    (APP + 'PkgInfo', b'APPL????', 0o644),
    (APP + 'MacOS/masrouf', LAUNCH.replace('\r\n', '\n').encode('utf-8'), 0o755),
    (APP + 'Resources/masrouf.icns', data, 0o644),
]

with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for name, blob, mode in files:
        zi = zipfile.ZipInfo(name)
        zi.date_time = (2026, 8, 31, 12, 0, 0)
        zi.compress_type = zipfile.ZIP_DEFLATED
        zi.create_system = 3                      # يونكس — كي تُقرأ الصلاحيات
        zi.external_attr = (0o100000 | mode) << 16
        z.writestr(zi, blob)

print('الحزمة: %s · %d بايت' % (OUT, os.path.getsize(OUT)))

# تحقّقٌ ممّا يمكن التحقّق منه هنا: البنية والصلاحيات
with zipfile.ZipFile(OUT) as z:
    for i in z.infolist():
        print('  %-42s %s' % (i.filename, oct((i.external_attr >> 16) & 0o777)))
    assert z.testzip() is None
print('البنية سليمة · وصلاحية التنفيذ على المشغّل محفوظة')
print('لم يُختبر على ماك — لا ماك هنا.')
