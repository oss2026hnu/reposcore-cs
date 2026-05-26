.PHONY: all docs synopsis clean-vars

PYTHON     := python3
J2RENDER   := $(PYTHON) tools/j2render.py

DOCS_TEMPLATE   := docs/README-template.md.j2
ROOT_TEMPLATE   := README-template.md.j2
DOCS_MD_FILES   := $(filter-out docs/README.md docs/README-template.md.j2,$(wildcard docs/*.md))

VARS_DIR        := vars
DOCLIST_JSON    := $(VARS_DIR)/doclist.json
SYNOPSIS_JSON   := $(VARS_DIR)/synopsis.json

## 전체 빌드: docs/README.md + README.md
all: docs

## vars/doclist.json 생성 (docs/*.md 탐색)
$(DOCLIST_JSON): $(DOCS_MD_FILES) $(DOCS_TEMPLATE)
	$(PYTHON) tools/update-doclist.py

## vars/synopsis.json 생성 (dotnet --help 캡처)
## Program.cs 또는 .csproj가 변경되면 재생성
$(SYNOPSIS_JSON): Program.cs reposcore-cs.csproj $(ROOT_TEMPLATE)
	$(PYTHON) tools/update-synopsis.py

## docs/README.md 빌드
docs/README.md: $(DOCLIST_JSON) $(DOCS_TEMPLATE)
	$(J2RENDER) $(DOCS_TEMPLATE) $(DOCLIST_JSON) -o docs/README.md

## 최상위 README.md 빌드
README.md: $(SYNOPSIS_JSON) $(ROOT_TEMPLATE)
	$(J2RENDER) $(ROOT_TEMPLATE) $(SYNOPSIS_JSON) -o README.md

## docs 빌드
docs: docs/README.md README.md

## README.md synopsis 섹션만 업데이트
synopsis: README.md

## vars/ 캐시 디렉토리 삭제 (강제 재빌드용)
clean-vars:
	rm -rf $(VARS_DIR)
