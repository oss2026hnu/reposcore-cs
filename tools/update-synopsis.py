#!/usr/bin/env python3
"""
update-synopsis.py — CLI 도움말을 캡처하여 vars/synopsis.json을 생성합니다.

기존 tools/update-synopsis.py의 역할 중 "데이터 수집" 부분만 담당합니다.
실제 README.md 렌더링은 tools/j2render.py + README-template.md.j2 가 담당합니다.

사용법 (프로젝트 루트에서):
    python tools/update-synopsis.py

동작:
    1. dotnet run -- --help 로 CLI 도움말 텍스트 캡처
    2. vars/synopsis.json 생성

    Makefile이 이 JSON을 읽어 j2render.py로 README.md를 빌드합니다.
"""

import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROJECT_FILE = ROOT / "reposcore-cs.csproj"
VARS_DIR = ROOT / "vars"
OUTPUT_JSON = VARS_DIR / "synopsis.json"


def capture_cli_help() -> str:
    if not PROJECT_FILE.exists():
        raise FileNotFoundError(f"프로젝트 파일을 찾을 수 없습니다: {PROJECT_FILE}")

    candidates = [
        ["dotnet", "run", "--project", str(PROJECT_FILE), "--no-restore", "--", "--help"],
        ["dotnet", "run", "--project", str(PROJECT_FILE), "--", "--help"],
    ]

    last_error = None
    for command in candidates:
        proc = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
        output = ((proc.stdout or "") + (proc.stderr or "")).strip()
        if (proc.returncode == 0 or proc.returncode == 129) and output:
            return output
        last_error = output or f"dotnet returned exit code {proc.returncode}"

    raise RuntimeError("CLI 도움말을 생성하지 못했습니다:\n" + last_error)


def normalize(help_text: str) -> str:
    for marker in ["Usage:", "usage:"]:
        idx = help_text.find(marker)
        if idx != -1:
            return help_text[idx:].strip()
    return help_text.strip()


def main() -> None:
    print("[캡처] dotnet --help 실행 중...")
    raw = capture_cli_help()
    synopsis = normalize(raw)
    print(synopsis)

    VARS_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_JSON.write_text(
        json.dumps({"synopsis": synopsis}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"\n[생성] {OUTPUT_JSON}")


if __name__ == "__main__":
    main()
