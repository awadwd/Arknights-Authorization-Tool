#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""推送前密钥扫描。

用途：这个仓库是 **公开** 的，任何一次 git push 都可能把凭据永久留在历史里。
发布前跑一遍，命中「高危」直接中止，别等出事再 force-push 洗历史。

用法：
    python scripts/secret_scan.py            # 扫当前工程（工作目录）
    python scripts/secret_scan.py D:/some/repo
    python scripts/secret_scan.py --staged   # 只扫 git 暂存区（更快的推送前关卡）

退出码：0 = 干净（或有已知可接受项）；1 = 命中高危，别推。
"""

import os
import re
import subprocess
import sys

# 这些目录里不可能有源码级凭据，扫了只会拖慢
SKIP_DIRS = {
    '.git', 'node_modules', 'unpackage', '.hbuilderx', 'dist', 'dist_h5_check',
    'dist_h5_music', 'dist_h5_music2', 'dist_h5_music3', 'dist_h5_music4',
    'dist_h5_v19', '_trash', '.workbuddy',
}
SKIP_PREFIX = ('_archive_backups_', '_backup_', '_check_', '_probe_')

# 文本文件才读，二进制跳过
TEXT_EXT = {
    '.js', '.mjs', '.cjs', '.ts', '.vue', '.json', '.py', '.md', '.txt', '.yml',
    '.yaml', '.xml', '.cs', '.xaml', '.csproj', '.swift', '.sh', '.ps1', '.bat',
    '.css', '.scss', '.html', '.properties', '.gradle', '.kt', '.uts', '.env',
}

# ── 高危：命中就中止 ────────────────────────────────────────────────────────
HIGH = [
    ('私钥文件', re.compile(r'$^'), 'file'),  # 由文件名规则处理，这里占位
    ('RSA/EC 私钥块',
     re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----')),
    ('Android keystore 密码赋值',
     re.compile(r'(?i)(storePassword|keyPassword|keystore\.password)\s*[=:]\s*["\']?[^\s"\',;]{4,}')),
    ('GitHub token',
     re.compile(r'\bgh[pousr]_[A-Za-z0-9]{30,}')),
    ('OpenAI 风格 key',
     re.compile(r'\bsk-[A-Za-z0-9]{32,}')),
    ('AWS Access Key',
     re.compile(r'\bAKIA[0-9A-Z]{16}\b')),
    ('腾讯云 SecretId/Key',
     re.compile(r'\bAKID[0-9A-Za-z]{13,}\b')),
    ('Slack token', re.compile(r'\bxox[baprs]-[A-Za-z0-9-]{10,}')),
    ('身份证号',
     re.compile(r'(?<![0-9])[1-9][0-9]{5}(?:19|20)[0-9]{2}(?:0[1-9]|1[0-2])(?:0[1-9]|[12][0-9]|3[01])[0-9]{3}[0-9Xx](?![0-9])')),
]

# 证书类文件：出现在仓库里本身就是问题
HIGH_FILE_EXT = {
    '.jks': 'Android keystore',
    '.keystore': 'Android keystore',
    '.p12': 'PKCS#12 证书',
    '.pfx': 'PKCS#12 证书',
    '.pem': 'PEM 证书/私钥',
    '.key': '私钥文件',
    '.mobileprovision': 'iOS 描述文件',
}

# ── 中危：报出来但要人判断 ──────────────────────────────────────────────────
MEDIUM = [
    ('手机号', re.compile(r'(?<![0-9])1[3-9][0-9]{9}(?![0-9])')),
    ('疑似 API Key/token 赋值',
     re.compile(r'(?i)\b(api[_-]?key|app[_-]?key|appkey|access[_-]?token|secret[_-]?key)'
                r'\s*[=:]\s*["\'][A-Za-z0-9_\-]{16,}["\']')),
    ('PushPlus token 赋值',
     re.compile(r'(?i)pushplus[^\n]{0,40}[0-9a-f]{32}')),
]

# 已知的「客户端内置密钥」：装成 APK 也能被扒出来，删了功能就废。
# 不算新增泄露，但每次扫描都提醒一遍，等哪天挪到服务端了再从这里删掉。
KNOWN_CLIENT_KEYS = {
    'MmwufMcFLOAfbeOEXfLovtNoclgmxSMxaYwosDTCkLwYcwrqgVjLzSFztKtTZaZZiMDtSbFQDKdmXCogThbVLCMWycxWuLkXRUftezkPVOYZKzgYkxlQUMLLIThqqXen':
        '腾讯云 ADP AppKey（Claw 模式）',
    'j2igdTjEliGF84QH75irkWLc51mpmP26':
        '腾讯元器 AppKey（普通模式）',
}

# 这些是文档/示例里的占位写法，别误报
NOISE = re.compile(
    r'(?i)(your[_-]?key|xxx+|placeholder|example|<[^>]*key[^>]*>|'
    r'\*\*\*|password\s*不入库|见本地凭据)')


def iter_files(root, staged):
    """产出 (绝对路径, 相对路径)。

    --staged 走 git diff --cached，只扫将要提交的文件，速度快。
    """
    if staged:
        out = subprocess.run(
            ['git', 'diff', '--cached', '--name-only', '--diff-filter=ACMR'],
            cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
        for rel in out.stdout.splitlines():
            rel = rel.strip()
            p = os.path.join(root, rel.replace('/', os.sep))
            if os.path.isfile(p):
                yield p, rel
        return

    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames
                       if d not in SKIP_DIRS and not d.startswith(SKIP_PREFIX)]
        for name in filenames:
            p = os.path.join(dirpath, name)
            rel = os.path.relpath(p, root).replace(os.sep, '/')
            if any(part in SKIP_DIRS for part in rel.split('/')):
                continue
            yield p, rel


def look(rel):
    ext = os.path.splitext(rel)[1].lower()
    return ext if ext in TEXT_EXT else None


def scan(root, staged):
    hits_high, hits_med, known = [], [], []

    for path, rel in iter_files(root, staged):
        ext = os.path.splitext(rel)[1].lower()
        base = os.path.basename(rel)

        # 证书文件本身
        if ext in HIGH_FILE_EXT and not base.endswith('.example' + ext):
            hits_high.append((rel, 0, '证书/私钥文件：' + HIGH_FILE_EXT[ext], base))

        if not look(rel):
            continue

        try:
            with open(path, 'r', encoding='utf-8', errors='replace') as fh:
                lines = fh.read().splitlines()
        except OSError:
            continue

        for idx, line in enumerate(lines, 1):
            if len(line) > 4000:      # 压缩产物里的超长行，跳过
                continue

            # 已知客户端密钥：单独归类，不算新增泄露
            for key, label in KNOWN_CLIENT_KEYS.items():
                if key in line:
                    known.append((rel, idx, label))
                    break

            for label, rx in HIGH[1:]:
                if rx.search(line) and not NOISE.search(line):
                    hits_high.append((rel, idx, label, line.strip()[:160]))

            for label, rx in MEDIUM:
                m = rx.search(line)
                if not m or NOISE.search(line):
                    continue
                frag = m.group(0)
                # 已知 key 已经归到「已知」里，别重复报中危
                if any(k in line for k in KNOWN_CLIENT_KEYS):
                    continue
                hits_med.append((rel, idx, label, frag[:120]))

    return hits_high, hits_med, known


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('-')]
    staged = '--staged' in sys.argv

    root = os.path.abspath(args[0]) if args else os.getcwd()
    if not os.path.isdir(root):
        print('目录不存在：' + root)
        return 2

    print('扫描目录：' + root + ('（仅暂存区）' if staged else ''))
    high, med, known = scan(root, staged)

    if known:
        print('\n[已知·客户端内置密钥] 以下密钥会随 APK 一起分发，装到手机上本来就能被提取；')
        print('  仓库里出现不算「新泄露」，但建议找机会挪到服务端 / 轮换：')
        for rel, ln, label in known[:10]:
            print('  · %s:%d  %s' % (rel, ln, label))

    if med:
        print('\n[中危] 需要人判断（多为误报，比如示例 / 文档占位）：')
        for rel, ln, label, frag in med[:40]:
            print('  · %s:%d  %s → %s' % (rel, ln, label, frag))
        if len(med) > 40:
            print('  … 其余 %d 条省略' % (len(med) - 40))

    if high:
        print('\n[高危] 命中 %d 处，**不要推送**：' % len(high))
        for rel, ln, label, frag in high[:40]:
            print('  · %s:%d  %s → %s' % (rel, ln, label, frag))
        print('\n处理完再跑一次；已经推上去的话，先轮换凭据，再决定要不要洗历史。')
        return 1

    print('\n[干净] 没有命中高危项，可以推送。')
    return 0


if __name__ == '__main__':
    sys.exit(main())
