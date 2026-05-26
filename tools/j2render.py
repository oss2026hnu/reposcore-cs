#!/usr/bin/env python3
"""
j2render.py — Jinja2 기반 범용 템플릿 렌더러

j2cli의 Python 3.12 비호환 문제를 우회하기 위해 jinja2 라이브러리를 직접 사용합니다.

사용법:
    python tools/j2render.py <template> <vars_json> -o <output>

    <template>   : Jinja2 문법이 적용된 .md.j2 템플릿 파일 경로
    <vars_json>  : 템플릿 변수를 담은 JSON 파일 경로 (없으면 빈 딕셔너리 사용)
    -o <output>  : 출력 파일 경로 (생략 시 stdout)

예시:
    python tools/j2render.py README-template.md.j2 vars/synopsis.json -o README.md
    python tools/j2render.py docs/README-template.md.j2 vars/doclist.json -o docs/README.md
"""

import argparse
import json
import sys
from pathlib import Path

try:
    from jinja2 import Environment, FileSystemLoader, StrictUndefined
except ImportError:
    print("오류: jinja2가 설치되어 있지 않습니다.", file=sys.stderr)
    print("설치: pip install jinja2 --break-system-packages", file=sys.stderr)
    sys.exit(1)


def render(template_path: Path, variables: dict, output_path: Path | None) -> None:
    env = Environment(
        loader=FileSystemLoader(str(template_path.parent)),
        keep_trailing_newline=True,
        undefined=StrictUndefined,
    )
    template = env.get_template(template_path.name)
    rendered = template.render(**variables)

    if output_path is None:
        print(rendered, end="")
    else:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(rendered, encoding="utf-8")
        print(f"[j2render] {output_path} 생성 완료")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Jinja2 템플릿을 렌더링하여 마크다운 파일을 생성합니다."
    )
    parser.add_argument("template", help="Jinja2 템플릿 파일 경로 (.md.j2)")
    parser.add_argument(
        "vars_json",
        nargs="?",
        default=None,
        help="템플릿 변수 JSON 파일 경로 (생략 시 빈 딕셔너리)",
    )
    parser.add_argument("-o", "--output", default=None, help="출력 파일 경로 (생략 시 stdout)")
    args = parser.parse_args()

    template_path = Path(args.template)
    if not template_path.exists():
        print(f"오류: 템플릿 파일을 찾을 수 없습니다: {template_path}", file=sys.stderr)
        sys.exit(1)

    variables: dict = {}
    if args.vars_json:
        vars_path = Path(args.vars_json)
        if not vars_path.exists():
            print(f"오류: 변수 JSON 파일을 찾을 수 없습니다: {vars_path}", file=sys.stderr)
            sys.exit(1)
        variables = json.loads(vars_path.read_text(encoding="utf-8"))

    output_path = Path(args.output) if args.output else None
    render(template_path, variables, output_path)


if __name__ == "__main__":
    main()
