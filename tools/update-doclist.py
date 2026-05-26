#!/usr/bin/env python3
"""
update-doclist.py — docs/ 폴더를 탐색하여 vars/doclist.json을 생성합니다.

기존 tools/update-readme.py의 역할을 담당합니다.
문자열 치환 방식 대신, 템플릿 변수 JSON을 생성하는 역할만 수행하며,
실제 렌더링은 tools/j2render.py + docs/README-template.md.j2 가 담당합니다.

사용법 (프로젝트 루트에서):
    python tools/update-doclist.py

동작:
    1. docs/ 폴더에서 README.md, README-template.md.j2 를 제외한 모든 .md 파일 탐색
    2. 각 파일의 첫 번째 H1(#) 헤더를 제목으로 추출
    3. 파일명 기준 알파벳 순으로 정렬
    4. vars/doclist.json 생성

    Makefile이 이 JSON을 읽어 j2render.py로 docs/README.md를 빌드합니다.
"""

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DOCS_DIR = ROOT / "docs"
VARS_DIR = ROOT / "vars"
OUTPUT_JSON = VARS_DIR / "doclist.json"

EXCLUDE = {"readme.md", "readme-template.md.j2", "readme-template.md"}


def extract_title(md_path: Path) -> str:
    try:
        with open(md_path, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line.startswith("# "):
                    return line[2:].strip()
    except (OSError, UnicodeDecodeError):
        pass
    return md_path.stem


def collect_docs(docs_dir: Path) -> list[dict]:
    entries = []
    for md_file in sorted(docs_dir.glob("*.md")):
        if md_file.name.lower() in EXCLUDE:
            continue
        title = extract_title(md_file)
        entries.append({"filename": md_file.name, "title": title})
    return entries


def main() -> None:
    if not DOCS_DIR.is_dir():
        raise FileNotFoundError(f"docs 폴더를 찾을 수 없습니다: {DOCS_DIR}")

    entries = collect_docs(DOCS_DIR)
    print(f"[탐색] {len(entries)}개 문서 발견:")
    for e in entries:
        print(f"  • {e['filename']}: {e['title']}")

    VARS_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_JSON.write_text(
        json.dumps({"docs": entries}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"[생성] {OUTPUT_JSON}")


if __name__ == "__main__":
    main()
