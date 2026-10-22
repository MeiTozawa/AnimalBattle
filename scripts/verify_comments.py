import os
import re
import sys

EXTENSIONS = {'.cs', '.cpp', '.h'}
EXCLUDE_DIRS = {'bin', 'obj', '.git', '.jj', '.vs'}

CHINESE_CALQUES = [
    '選人', '首領', '非托管', '時序', '原型', '原生', '前台', '後台', '前瞻',
    '多態', '全域', '破弃', '冷卻', '動效', '托管', '單例', '幀率', '解耦',
    '調試', '調用', '對象', '指針', '內存', '緩存'
]

MOJIBAKE_CHARS = ['繧', '繝', 'ｼ', '', 'ｪ', '縺', 'ゅ', '縲', '逅', '邏']

def check_file(path):
    violations = []
    with open(path, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()

    in_multiline = False
    for line_num, line in enumerate(lines, 1):
        comment_text = ""

        # Check multiline comments
        if in_multiline:
            if "*/" in line:
                idx = line.find("*/")
                comment_text += line[:idx]
                in_multiline = False
            else:
                comment_text += line
        else:
            if "/*" in line:
                idx = line.find("/*")
                end_idx = line.find("*/", idx + 2)
                if end_idx != -1:
                    comment_text += line[idx+2:end_idx]
                else:
                    comment_text += line[idx+2:]
                    in_multiline = True

        if "//" in line:
            c_idx = line.find("//")
            comment_text += " " + line[c_idx+2:]

        if not comment_text:
            continue

        # 1. Full-width brackets check
        if '（' in comment_text or '）' in comment_text:
            violations.append((line_num, "Full-width bracket found", line.strip()))

        # 2. Half-width brackets in natural language explanations check
        # Strip XML tags
        sanitized = re.sub(r'<[^>]+>', '', comment_text)
        # Strip standard Task Tags e.g. TODO(name, Task-ID), FIXME(name), etc.
        sanitized = re.sub(r'\b(TODO|FIXME|HACK|OPTIMIZE)\([^)]+\)', '', sanitized)
        # Flag if bracket contains natural language explanation (Japanese text inside or adjacent)
        if re.search(r'[\u3040-\u30ff\u4e00-\u9faf].*\([^)]*[\u3040-\u30ff\u4e00-\u9faf][^)]*\)', sanitized):
            violations.append((line_num, "Parenthetical explanation in comment", line.strip()))

        # 3. Chinese calques check
        for word in CHINESE_CALQUES:
            if word in comment_text:
                violations.append((line_num, f"Chinese calque '{word}' found", line.strip()))

        # 4. Mojibake characters check
        for ch in MOJIBAKE_CHARS:
            if ch in line and any(c in line for c in ['//', '/*', '*/', '*']):
                violations.append((line_num, f"Mojibake char '{ch}' found", line.strip()))
                break

    return violations

def main():
    root_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
    dirs_to_check = ['AnimalBattle', 'AnimalBattleCore']
    total_violations = 0

    for d in dirs_to_check:
        full_d = os.path.join(root_dir, d)
        for root, dirs, files in os.walk(full_d):
            dirs[:] = [dir_name for dir_name in dirs if dir_name not in EXCLUDE_DIRS]
            for file in files:
                ext = os.path.splitext(file)[1]
                if ext in EXTENSIONS:
                    path = os.path.join(root, file)
                    rel_path = os.path.relpath(path, root_dir)
                    v = check_file(path)
                    if v:
                        print(f"[{rel_path}]: {len(v)} violation(s)")
                        for line_num, reason, txt in v:
                            print(f"  L{line_num}: [{reason}] {txt}")
                        total_violations += len(v)

    if total_violations == 0:
        print("SUCCESS: All comments comply with project standards! No violations found.")
        sys.exit(0)
    else:
        print(f"FAILED: Found {total_violations} comment violation(s).")
        sys.exit(1)

if __name__ == '__main__':
    main()
