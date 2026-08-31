# -*- coding: utf-8 -*-
"""يبني masrouf.exe بلا تنزيل أيّ شيء.

يستعمل csc الموجود مع .NET Framework في كل ويندوز، فلا nuget ولا
حزمة. والأيقونة تُصنع هنا: صيغة ICO تقبل PNG داخلها كما هو، فلا
حاجة إلى مكتبة صور.
"""
import io
import os
import re
import struct
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CSC = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
PNG = os.path.join(ROOT, 'icon-192.png')
ICO = os.path.join(HERE, 'masrouf.ico')
CS = os.path.join(HERE, 'Masrouf.cs')
SETUP_SRC = os.path.join(HERE, 'Setup.cs')
INDEX = os.path.join(ROOT, 'index.html')


def app_version():
    """نسخة التطبيق كما بناها build_dist — لا رقمٌ يُكتب باليد."""
    s = io.open(INDEX, encoding='utf-8').read()
    m = re.search(r'var APP_VERSION = "([^"]*)"', s)
    return m.group(1) if m else '0.0.0-dev'


def with_version(src_path, ver):
    """نسخةٌ مؤقّتة من المصدر بعد حقن الرقم — الأصل يبقى بالعلامة."""
    t = io.open(src_path, encoding='utf-8').read()
    assert '__VER__' in t, 'لا علامة __VER__ في ' + src_path
    tmp = src_path + '.gen'
    io.open(tmp, 'w', encoding='utf-8').write(t.replace('__VER__', ver))
    return tmp
EXE = os.path.join(HERE, 'masrouf.exe')


def make_ico():
    png = io.open(PNG, 'rb').read()
    assert png[:8] == b'\x89PNG\r\n\x1a\n', 'ليست PNG'
    w, h = struct.unpack('>II', png[16:24])
    assert w <= 255 and h <= 255, 'ICO يخزّن المقاس في بايت واحد'
    head = struct.pack('<HHH', 0, 1, 1)              # محجوز · نوع=أيقونة · عدد=1
    entry = struct.pack('<BBBBHHII', w, h, 0, 0, 1, 32, len(png), 22)
    io.open(ICO, 'wb').write(head + entry + png)
    return w, h, len(png)


w, h, n = make_ico()
print('الأيقونة: %dx%d · %d بايت' % (w, h, n))

r = subprocess.run(
    # ‏/codepage:65001 ضروريّ: بدونه يقرأ csc المصدر بترميز النظام
    # فتتشوّه كل النصوص العربية — والاختصار يفشل باسمٍ لا يصلح ملفّاً.
    [CSC, '/nologo', '/target:winexe', '/optimize+', '/codepage:65001',
     '/win32icon:' + ICO, '/out:' + EXE,
     '/reference:System.dll', '/reference:System.Windows.Forms.dll',
     '/reference:System.Drawing.dll', CS],
    capture_output=True, text=True, encoding='utf-8', errors='replace')

if r.returncode != 0:
    print('فشل البناء:\n' + (r.stdout or '') + (r.stderr or ''))
    raise SystemExit(1)

print('بُني: %s · %d بايت' % (EXE, os.path.getsize(EXE)))

# ── المثبِّت: يحمل المشغّل والأيقونة داخله موردَين ──
SETUP_CS = os.path.join(HERE, 'Setup.cs')
SETUP = os.path.join(HERE, 'masrouf-setup.exe')

VER = app_version()
print('النسخة المحقونة: %s' % VER)
SETUP_GEN = with_version(SETUP_SRC, VER)

r2 = subprocess.run(
    [CSC, '/nologo', '/target:winexe', '/optimize+', '/codepage:65001',
     '/win32icon:' + ICO, '/out:' + SETUP,
     '/resource:' + EXE + ',app', '/resource:' + ICO + ',icon',
     '/reference:System.dll', '/reference:System.Windows.Forms.dll',
     '/reference:System.Drawing.dll', SETUP_GEN],
    capture_output=True, text=True, encoding='utf-8', errors='replace')

if r2.returncode != 0:
    print('فشل بناء المثبِّت:')
    print((r2.stdout or '') + (r2.stderr or ''))
    raise SystemExit(1)

try:
    os.remove(SETUP_GEN)
except OSError:
    pass
print('المثبِّت: %s · %d بايت · نسخة %s'
      % (SETUP, os.path.getsize(SETUP), VER))
