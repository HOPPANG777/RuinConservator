# -*- coding: utf-8 -*-
"""
Ruin Conservator Dialogue & Quest CSV 교보재 생성 스크립트.

DESIGNER_CSV_AUTHORING_SPEC.md 명세를 유일한 스키마 기준으로 사용해
기획자용 Excel 통합 문서(.xlsx)와 각 데이터 시트의 UTF-8 CSV 예시 파일을 생성한다.

생성물:
  - Docs/RuinConservator_CSV_Authoring_Guide.xlsx
  - Docs/CSV_Samples/**/*.csv
"""

import csv
import os

from openpyxl import Workbook
from openpyxl.comments import Comment
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.workbook.defined_name import DefinedName
from openpyxl.worksheet.datavalidation import DataValidation
from openpyxl.formatting.rule import FormulaRule
from openpyxl.worksheet.table import Table, TableStyleInfo

# --------------------------------------------------------------------------
# 출력 경로
# --------------------------------------------------------------------------
DOCS_DIR = os.path.dirname(os.path.abspath(__file__))
XLSX_PATH = os.path.join(DOCS_DIR, "RuinConservator_CSV_Authoring_Guide.xlsx")
CSV_ROOT = os.path.join(DOCS_DIR, "CSV_Samples")

BLANK_TEMPLATE_ROWS = 4  # 기획자 신규 입력용 빈 행 개수

# --------------------------------------------------------------------------
# 색상 / 서식 팔레트
# --------------------------------------------------------------------------
C_HDR_REQ = "1F4E78"   # 필수 헤더: 진한 파랑
C_HDR_OPT = "BDD7EE"   # 선택 헤더: 연한 파랑
C_HDR_COND = "8EAADB"  # 조건부 헤더: 중간 파랑
C_IDREF = "FFF9E3"     # ID/참조 셀: 연한 노랑
C_TEXT = "E2EFDA"      # 기획자 작성 문장 셀: 연한 초록
C_SAMPLE_TINT = "F2F2F2"

C_RED_FILL = "FFC7CE"
C_RED_FONT = "9C0006"
C_DUP_FILL = "FCE4D6"
C_DUP_FONT = "833C00"

FILL_HDR_REQ = PatternFill("solid", fgColor=C_HDR_REQ)
FILL_HDR_OPT = PatternFill("solid", fgColor=C_HDR_OPT)
FILL_HDR_COND = PatternFill("solid", fgColor=C_HDR_COND)
FILL_IDREF = PatternFill("solid", fgColor=C_IDREF)
FILL_TEXT = PatternFill("solid", fgColor=C_TEXT)

FONT_HDR_REQ = Font(name="맑은 고딕", bold=True, color="FFFFFF", size=11)
FONT_HDR_DARK = Font(name="맑은 고딕", bold=True, color="1F3864", size=11)
FONT_BODY = Font(name="맑은 고딕", size=11)

THIN = Side(style="thin", color="BFBFBF")
BORDER_THIN = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
DIVIDER = Side(style="medium", color="1F4E78")

ALIGN_HDR = Alignment(horizontal="center", vertical="center", wrap_text=True)
ALIGN_LEFT = Alignment(horizontal="left", vertical="center", wrap_text=False)
ALIGN_WRAP = Alignment(horizontal="left", vertical="center", wrap_text=True)
ALIGN_CENTER = Alignment(horizontal="center", vertical="center")

# --------------------------------------------------------------------------
# 컬럼 정의 헬퍼
#   req: "req"(필수) | "opt"(선택) | "cond"(조건부)
#   kind: id | ref | loc | asset | int | text | enum_action | enum_state | bool
# --------------------------------------------------------------------------
def col(name, req, kind, desc, example=""):
    return {"name": name, "req": req, "kind": kind, "desc": desc, "example": example}


IDREF_KINDS = {"id", "ref", "loc", "asset"}
DROPDOWN_KIND = {"enum_action": "ActionTypeList",
                 "enum_state": "QuestStateList",
                 "bool": "BoolList"}
WIDTHS = {"id": 24, "ref": 24, "loc": 34, "asset": 22, "int": 15,
          "text": 46, "enum_action": 26, "enum_state": 24, "bool": 18}

REQ_KOR = {"req": "필수", "opt": "선택", "cond": "조건부"}

# --------------------------------------------------------------------------
# 시트별 스키마 & 샘플 데이터 (명세 5~8장 그대로)
# --------------------------------------------------------------------------
SHEETS = []


def add_sheet(name, csv_rel, columns, rows, unique_groups, blurb):
    SHEETS.append({
        "name": name, "csv": csv_rel, "columns": columns,
        "rows": rows, "unique_groups": unique_groups, "blurb": blurb,
    })


add_sheet(
    "DialogueActors", "Dialogue/dialogue_actors.csv",
    [
        col("actor_id", "req", "id", "화자 고유 ID", "ACTOR_CURATOR"),
        col("display_name_key", "req", "loc", "화면에 표시할 화자 이름 키 (Localization 참조)", "actor.curator.name"),
        col("default_portrait_id", "opt", "id", "노드에서 표정을 지정하지 않았을 때 사용할 초상화 ID", "NORMAL"),
    ],
    [
        ["ACTOR_PLAYER", "actor.player.name", "NORMAL"],
        ["ACTOR_CURATOR", "actor.curator.name", "NORMAL"],
    ],
    [(1,)],  # actor_id 고유
    "화자 한 명의 기본 정보. 화면 표시 문장은 Localization 키로 연결한다.",
)

add_sheet(
    "DialoguePortraits", "Dialogue/dialogue_portraits.csv",
    [
        col("actor_id", "req", "ref", "소유 화자 ID (DialogueActors 참조)", "ACTOR_CURATOR"),
        col("portrait_id", "req", "id", "해당 화자 안에서 고유한 표정 ID", "WORRIED"),
        col("display_name_key", "opt", "loc", "표정에 따라 화자 표시명을 바꿀 때 사용", ""),
        col("sprite_id", "req", "asset", "런타임 에셋 Resolver가 사용할 Sprite ID", "SPR_CURATOR_WORRIED"),
    ],
    [
        ["ACTOR_PLAYER", "NORMAL", "", "SPR_PLAYER_NORMAL"],
        ["ACTOR_CURATOR", "NORMAL", "", "SPR_CURATOR_NORMAL"],
        ["ACTOR_CURATOR", "WORRIED", "", "SPR_CURATOR_WORRIED"],
        ["ACTOR_CURATOR", "HAPPY", "", "SPR_CURATOR_HAPPY"],
    ],
    [(1, 2)],  # (actor_id, portrait_id) 고유
    "화자의 표정/초상화 에셋 ID를 정의. Sprite 객체가 아니라 sprite_id만 저장한다.",
)

add_sheet(
    "DialogueGraphs", "Dialogue/dialogue_graphs.csv",
    [
        col("graph_id", "req", "id", "대화 그래프 고유 ID", "DLG_CURATOR_INTRO"),
        col("start_node_id", "req", "ref", "대화 시작 노드 ID (같은 graph_id의 DialogueNodes.node_id)", "NODE_START"),
        col("default_actor_id", "opt", "ref", "노드 화자가 비어 있을 때 사용할 기본 화자", "ACTOR_CURATOR"),
        col("left_actor_id", "opt", "ref", "대화 UI 왼쪽 참가자", "ACTOR_PLAYER"),
        col("right_actor_id", "opt", "ref", "대화 UI 오른쪽 참가자", "ACTOR_CURATOR"),
        col("background_id", "opt", "asset", "그래프 기본 대화창 배경 ID", "BG_MUSEUM_HALL"),
    ],
    [
        ["DLG_CURATOR_INTRO", "NODE_START", "ACTOR_CURATOR", "ACTOR_PLAYER", "ACTOR_CURATOR", "BG_MUSEUM_HALL"],
        ["DLG_CURATOR_IN_PROGRESS", "NODE_PROGRESS", "ACTOR_CURATOR", "ACTOR_PLAYER", "ACTOR_CURATOR", "BG_MUSEUM_HALL"],
        ["DLG_CURATOR_COMPLETE", "NODE_COMPLETE", "ACTOR_CURATOR", "ACTOR_PLAYER", "ACTOR_CURATOR", "BG_MUSEUM_HALL"],
    ],
    [(1,)],
    "대화 묶음의 시작점과 기본 화자, 화면 참가자를 정의한다.",
)

add_sheet(
    "DialogueNodes", "Dialogue/dialogue_nodes.csv",
    [
        col("graph_id", "req", "ref", "이 노드가 속한 그래프 ID (DialogueGraphs 참조)", "DLG_CURATOR_INTRO"),
        col("node_id", "req", "id", "그래프 안에서 고유한 노드 ID", "NODE_START"),
        col("actor_id", "opt", "ref", "이 노드에서 말하는 화자. 비우면 그래프 기본 화자 사용", "ACTOR_CURATOR"),
        col("portrait_id", "opt", "ref", "이 노드에서 사용할 화자의 표정 ID", "WORRIED"),
        col("background_id", "opt", "asset", "이 노드에서만 덮어쓸 배경 ID", ""),
        col("body_text_key", "req", "loc", "대화 본문 키 (Localization 참조)", "dialogue.curator.intro.001"),
        col("next_node_id", "opt", "ref", "선택지가 없을 때 이동할 다음 노드. 비우면 대화 종료", ""),
    ],
    [
        ["DLG_CURATOR_INTRO", "NODE_START", "ACTOR_CURATOR", "WORRIED", "", "dialogue.curator.intro.001", ""],
        ["DLG_CURATOR_INTRO", "NODE_ACCEPTED", "ACTOR_CURATOR", "NORMAL", "", "dialogue.curator.intro.accepted", ""],
        ["DLG_CURATOR_IN_PROGRESS", "NODE_PROGRESS", "ACTOR_CURATOR", "WORRIED", "", "dialogue.curator.progress.001", ""],
        ["DLG_CURATOR_COMPLETE", "NODE_COMPLETE", "ACTOR_CURATOR", "HAPPY", "", "dialogue.curator.complete.001", ""],
    ],
    [(1, 2)],  # (graph_id, node_id) 고유
    "한 행 = 대화창에 한 번 표시되는 대사 한 문단. 고유키는 (graph_id, node_id).",
)

add_sheet(
    "DialogueChoices", "Dialogue/dialogue_choices.csv",
    [
        col("graph_id", "req", "ref", "선택지가 속한 그래프 ID", "DLG_CURATOR_INTRO"),
        col("node_id", "req", "ref", "선택지가 속한 노드 ID", "NODE_START"),
        col("choice_order", "req", "int", "화면 표시 순서. 0부터 시작 권장, 같은 노드 내 중복 금지", "0"),
        col("choice_text_key", "req", "loc", "선택지 표시 문장 키 (Localization 참조)", "dialogue.curator.choice.accept"),
        col("next_node_id", "opt", "ref", "선택 후 이동할 같은 그래프의 노드 ID", "NODE_ACCEPTED"),
        col("action_type", "req", "enum_action", "선택 시 실행할 액션 종류 (드롭다운에서 선택)", "ACCEPT_QUEST"),
        col("action_id", "cond", "ref", "액션 대상 Flag ID 또는 Quest ID. Quest 액션이면 필수", "QUEST_RESTORE_MURAL"),
    ],
    [
        ["DLG_CURATOR_INTRO", "NODE_START", 0, "dialogue.curator.choice.accept", "NODE_ACCEPTED", "ACCEPT_QUEST", "QUEST_RESTORE_MURAL"],
        ["DLG_CURATOR_INTRO", "NODE_START", 1, "dialogue.curator.choice.decline", "", "END_DIALOGUE", ""],
        ["DLG_CURATOR_IN_PROGRESS", "NODE_PROGRESS", 0, "dialogue.curator.choice.refresh", "", "REFRESH_QUEST_PROGRESS", "QUEST_RESTORE_MURAL"],
        ["DLG_CURATOR_COMPLETE", "NODE_COMPLETE", 0, "dialogue.curator.choice.reward", "", "CLAIM_QUEST_REWARD", "QUEST_RESTORE_MURAL"],
    ],
    [(1, 2, 3)],  # (graph_id, node_id, choice_order) 고유
    "대화 노드에 표시할 선택지와 선택 직후 실행할 액션. 선택지 없는 노드는 행을 만들지 않는다.",
)

add_sheet(
    "Quests", "Quests/quests.csv",
    [
        col("quest_id", "req", "id", "퀘스트 고유 ID이자 저장 키. 출시 후 변경 금지", "QUEST_RESTORE_MURAL"),
        col("title_text_key", "req", "loc", "퀘스트 제목 키 (Localization 참조)", "quest.restore_mural.title"),
        col("description_text_key", "req", "loc", "퀘스트 설명 키 (Localization 참조)", "quest.restore_mural.description"),
        col("owner_npc_id", "opt", "ref", "퀘스트의 주요 NPC ID", "NPC_CURATOR"),
    ],
    [
        ["QUEST_RESTORE_MURAL", "quest.restore_mural.title", "quest.restore_mural.description", "NPC_CURATOR"],
    ],
    [(1,)],
    "퀘스트 한 개의 기본 정보. 목표/보상/소모 아이템은 별도 시트에서 연결한다.",
)

add_sheet(
    "QuestObjectives", "Quests/quest_objectives.csv",
    [
        col("quest_id", "req", "ref", "소속 퀘스트 ID (Quests 참조)", "QUEST_RESTORE_MURAL"),
        col("objective_order", "req", "int", "목표 표시 및 판정 순서. 같은 퀘스트 내 중복 금지", "0"),
        col("objective_id", "req", "id", "퀘스트 안에서 고유한 목표 ID", "OBJECTIVE_INSPECT_MURAL"),
        col("description_text_key", "req", "loc", "목표 설명 키 (Localization 참조)", "quest.restore_mural.objective.inspect"),
        col("required_progress_flag_id", "req", "id", "목표 판정에 사용할 Flag ID", "FLAG_MURAL_INSPECTED"),
        col("required_flag_value", "req", "bool", "목표 달성으로 인정할 Flag 값 (드롭다운)", "true"),
    ],
    [
        ["QUEST_RESTORE_MURAL", 0, "OBJECTIVE_INSPECT_MURAL", "quest.restore_mural.objective.inspect", "FLAG_MURAL_INSPECTED", "true"],
        ["QUEST_RESTORE_MURAL", 1, "OBJECTIVE_REPORT_DAMAGE", "quest.restore_mural.objective.report", "FLAG_MURAL_DAMAGE_REPORTED", "true"],
    ],
    [(1, 3), (1, 2)],  # (quest_id, objective_id) 및 (quest_id, objective_order) 고유
    "Progress Flag를 이용한 퀘스트 완료 목표. 퀘스트 하나에 여러 행 연결 가능.",
)

add_sheet(
    "QuestRewards", "Quests/quest_rewards.csv",
    [
        col("quest_id", "req", "ref", "소속 퀘스트 ID (Quests 참조)", "QUEST_RESTORE_MURAL"),
        col("reward_order", "req", "int", "보상 표시 및 적용 순서. 같은 퀘스트 내 중복 금지", "0"),
        col("reward_id", "req", "id", "퀘스트 안에서 고유한 보상 ID", "REWARD_COINS"),
        col("description_text_key", "req", "loc", "보상 설명 키 (Localization 참조)", "quest.restore_mural.reward.coins"),
        col("progress_flag_id", "opt", "id", "지급 시 변경할 Flag ID", "FLAG_CURATOR_TRUSTED"),
        col("flag_value", "cond", "bool", "Flag에 저장할 값. progress_flag_id가 있으면 필수", "true"),
        col("item_id", "opt", "ref", "지급할 인벤토리 Item ID", "ITEM_COIN"),
        col("item_amount", "cond", "int", "지급 수량. item_id가 있으면 1 이상 필수", "100"),
    ],
    [
        ["QUEST_RESTORE_MURAL", 0, "REWARD_REPUTATION", "quest.restore_mural.reward.reputation", "FLAG_CURATOR_TRUSTED", "true", "", ""],
        ["QUEST_RESTORE_MURAL", 1, "REWARD_COINS", "quest.restore_mural.reward.coins", "", "", "ITEM_COIN", 100],
    ],
    [(1, 3), (1, 2)],  # (quest_id, reward_id) 및 (quest_id, reward_order) 고유
    "보상 수령 시 적용할 Flag/아이템. 한 행에서 Flag와 아이템을 동시 지급 가능.",
)

add_sheet(
    "QuestConsumeItems", "Quests/quest_consume_items.csv",
    [
        col("quest_id", "req", "ref", "소속 퀘스트 ID (Quests 참조)", "QUEST_RESTORE_MURAL"),
        col("consume_order", "req", "int", "소모 처리 순서. 같은 퀘스트 내 중복 금지", "0"),
        col("item_id", "req", "ref", "자동 소모할 인벤토리 Item ID", "ITEM_REPAIR_KIT"),
        col("item_amount", "req", "int", "자동 소모할 수량. 1 이상", "1"),
    ],
    [
        ["QUEST_RESTORE_MURAL", 0, "ITEM_REPAIR_KIT", 1],
    ],
    [(1, 3), (1, 2)],
    "퀘스트 완료 확정 시 인벤토리에서 자동 소모할 아이템.",
)

add_sheet(
    "Localization", "Localization/localization.csv",
    [
        col("text_key", "req", "id", "문장 고유 키. 영문 소문자와 마침표 사용", "dialogue.curator.intro.001"),
        col("ko", "req", "text", "한국어 원문 (직접 작성)", "벽화가 심하게 손상됐어."),
        col("en", "opt", "text", "영어 번역 (직접 작성)", "The mural is badly damaged."),
        col("ja", "opt", "text", "일본어 번역 (직접 작성)", ""),
    ],
    [
        ["actor.player.name", "플레이어", "Player", ""],
        ["actor.curator.name", "큐레이터", "Curator", ""],
        ["dialogue.curator.intro.001", "벽화가 심하게 손상됐어.\\n조사를 도와주겠나?", "The mural is badly damaged.\\nWill you help investigate?", ""],
        ["dialogue.curator.intro.accepted", "고맙네. 먼저 손상된 부분을 자세히 살펴봐 줘.", "Thank you. Inspect the damaged area first.", ""],
        ["dialogue.curator.progress.001", "아직 조사 결과가 부족하네.", "We still need more findings.", ""],
        ["dialogue.curator.complete.001", "훌륭하군. 덕분에 복원 계획을 세울 수 있겠어.", "Excellent. Now we can plan the restoration.", ""],
        ["dialogue.curator.choice.accept", "도와드리겠습니다.", "I'll help.", ""],
        ["dialogue.curator.choice.decline", "지금은 어렵습니다.", "Not right now.", ""],
        ["dialogue.curator.choice.refresh", "조사 결과를 보고한다.", "Report the findings.", ""],
        ["dialogue.curator.choice.reward", "보상을 받는다.", "Receive the reward.", ""],
        ["quest.restore_mural.title", "손상된 벽화 조사", "Investigate the Damaged Mural", ""],
        ["quest.restore_mural.description", "박물관 홀의 손상된 벽화를 조사하고 큐레이터에게 보고하자.", "Inspect the damaged mural and report to the curator.", ""],
        ["quest.restore_mural.objective.inspect", "손상된 벽화를 조사한다.", "Inspect the damaged mural.", ""],
        ["quest.restore_mural.objective.report", "벽화의 손상 상태를 보고한다.", "Report the mural's condition.", ""],
        ["quest.restore_mural.reward.reputation", "큐레이터의 신뢰를 얻는다.", "Gain the curator's trust.", ""],
        ["quest.restore_mural.reward.coins", "보존 활동 지원금 100을 받는다.", "Receive 100 conservation funds.", ""],
    ],
    [(1,)],
    "화면에 표시되는 모든 문장을 키로 관리. 대사 줄바꿈은 \\n 문자로 표기한다.",
)

add_sheet(
    "NpcDialogueBindings", "Npc/npc_dialogue_bindings.csv",
    [
        col("npc_id", "req", "id", "NPC 고유 ID", "NPC_CURATOR"),
        col("default_graph_id", "req", "ref", "아무 조건도 맞지 않을 때 실행할 기본 Graph ID", "DLG_CURATOR_INTRO"),
        col("actor_override_id", "opt", "ref", "NPC별 화자 오버라이드 ID", "ACTOR_CURATOR"),
    ],
    [
        ["NPC_CURATOR", "DLG_CURATOR_INTRO", "ACTOR_CURATOR"],
    ],
    [(1,)],
    "[선택] NPC의 기본 대화와 화자 오버라이드를 CSV에서 관리할 때 사용.",
)

add_sheet(
    "NpcQuestDialogueRoutes", "Npc/npc_quest_dialogue_routes.csv",
    [
        col("npc_id", "req", "ref", "대상 NPC ID", "NPC_CURATOR"),
        col("route_order", "req", "int", "조건 검사 우선순위. 낮은 행부터 검사", "0"),
        col("quest_id", "req", "ref", "상태를 확인할 Quest ID", "QUEST_RESTORE_MURAL"),
        col("quest_state", "req", "enum_state", "일치해야 하는 Quest 상태 (드롭다운)", "QUEST_COMPLETED"),
        col("graph_id", "req", "ref", "조건이 맞을 때 실행할 Graph ID", "DLG_CURATOR_COMPLETE"),
    ],
    [
        ["NPC_CURATOR", 0, "QUEST_RESTORE_MURAL", "QUEST_COMPLETED", "DLG_CURATOR_COMPLETE"],
        ["NPC_CURATOR", 1, "QUEST_RESTORE_MURAL", "QUEST_IN_PROGRESS", "DLG_CURATOR_IN_PROGRESS"],
    ],
    [(1, 2)],
    "[선택] NPC가 퀘스트 상태에 따라 다른 대화를 실행하도록 정의. 완료 대화를 먼저 검사.",
)

# --------------------------------------------------------------------------
# CodeLists 데이터
# --------------------------------------------------------------------------
ACTION_TYPES = [
    ("NONE", "사용하지 않음 (action_id 비움)"),
    ("SET_PROGRESS_FLAG_TRUE", "Progress Flag를 true로 (action_id 필수)"),
    ("SET_PROGRESS_FLAG_FALSE", "Progress Flag를 false로 (action_id 필수)"),
    ("ACCEPT_QUEST", "퀘스트 수락 (action_id = Quest ID)"),
    ("REFRESH_QUEST_PROGRESS", "진행도 갱신 (action_id = Quest ID)"),
    ("COMPLETE_QUEST", "완료 확정 (action_id = Quest ID)"),
    ("CLAIM_QUEST_REWARD", "보상 수령 (action_id = Quest ID)"),
    ("END_DIALOGUE", "대화 종료 (next_node_id/action_id 비움)"),
]
QUEST_STATES = [
    ("QUEST_NOT_STARTED", "아직 수락 전"),
    ("QUEST_IN_PROGRESS", "진행 중"),
    ("QUEST_COMPLETED", "목표 달성/완료"),
    ("QUEST_REWARDED", "보상 수령 완료"),
]
BOOL_VALUES = [
    ("true", "참"),
    ("false", "거짓"),
]

# --------------------------------------------------------------------------
# 워크북 생성
# --------------------------------------------------------------------------
wb = Workbook()
wb.remove(wb.active)


def style_header_cell(cell, cdef):
    if cdef["req"] == "req":
        cell.fill = FILL_HDR_REQ
        cell.font = FONT_HDR_REQ
    elif cdef["req"] == "cond":
        cell.fill = FILL_HDR_COND
        cell.font = FONT_HDR_DARK
    else:
        cell.fill = FILL_HDR_OPT
        cell.font = FONT_HDR_DARK
    cell.alignment = ALIGN_HDR
    cell.border = BORDER_THIN
    memo = "{name}\n──────────\n{desc}\n필수 여부: {req}\n예시: {ex}".format(
        name=cdef["name"], desc=cdef["desc"], req=REQ_KOR[cdef["req"]],
        ex=(cdef["example"] if cdef["example"] != "" else "(비움)"))
    cm = Comment(memo, "CSV 교보재")
    cm.width = 300
    cm.height = 160
    cell.comment = cm


def build_data_sheet(ws, spec):
    columns = spec["columns"]
    ncols = len(columns)
    nsample = len(spec["rows"])

    # 헤더
    for ci, cdef in enumerate(columns, start=1):
        c = ws.cell(row=1, column=ci, value=cdef["name"])
        style_header_cell(c, cdef)
    ws.row_dimensions[1].height = 30

    # 샘플 데이터
    for ri, row in enumerate(spec["rows"], start=2):
        for ci, cdef in enumerate(columns, start=1):
            val = row[ci - 1]
            c = ws.cell(row=ri, column=ci, value=(val if val != "" else None))
            apply_body_style(c, cdef)

    # 빈 템플릿 행
    first_blank = 2 + nsample
    last_row = first_blank + BLANK_TEMPLATE_ROWS - 1
    for ri in range(first_blank, last_row + 1):
        for ci, cdef in enumerate(columns, start=1):
            c = ws.cell(row=ri, column=ci)
            apply_body_style(c, cdef)
    # 샘플/입력영역 구분 divider
    for ci in range(1, ncols + 1):
        c = ws.cell(row=first_blank, column=ci)
        b = c.border
        c.border = Border(left=b.left, right=b.right, bottom=b.bottom, top=DIVIDER)

    # 열 너비
    for ci, cdef in enumerate(columns, start=1):
        ws.column_dimensions[get_column_letter(ci)].width = WIDTHS[cdef["kind"]]

    # 틀 고정 + 표
    ws.freeze_panes = "A2"
    ref = "A1:{}{}".format(get_column_letter(ncols), last_row)
    tbl = Table(displayName="Tbl" + spec["name"], ref=ref)
    tbl.tableStyleInfo = TableStyleInfo(
        name="TableStyleLight8", showFirstColumn=False, showLastColumn=False,
        showRowStripes=False, showColumnStripes=False)
    ws.add_table(tbl)

    add_dropdowns(ws, columns, last_row)
    add_conditional_formatting(ws, spec, columns, last_row)


def apply_body_style(cell, cdef):
    cell.border = BORDER_THIN
    cell.font = FONT_BODY
    if cdef["kind"] in IDREF_KINDS:
        cell.fill = FILL_IDREF
        cell.number_format = "@"
        cell.alignment = ALIGN_LEFT
    elif cdef["kind"] == "text":
        cell.fill = FILL_TEXT
        cell.number_format = "@"
        cell.alignment = ALIGN_WRAP
    elif cdef["kind"] == "int":
        cell.number_format = "0"
        cell.alignment = ALIGN_CENTER
    else:  # enum / bool
        cell.number_format = "@"
        cell.alignment = ALIGN_CENTER


def add_dropdowns(ws, columns, last_row):
    for ci, cdef in enumerate(columns, start=1):
        if cdef["kind"] in DROPDOWN_KIND:
            letter = get_column_letter(ci)
            dv = DataValidation(type="list", formula1="=" + DROPDOWN_KIND[cdef["kind"]],
                                allow_blank=True, showDropDown=False)
            dv.error = "목록에 있는 값만 입력하세요."
            dv.errorTitle = "잘못된 입력"
            dv.prompt = "드롭다운에서 값을 선택하세요."
            ws.add_data_validation(dv)
            dv.add("{L}2:{L}{n}".format(L=letter, n=last_row))


def add_conditional_formatting(ws, spec, columns, last_row):
    red_fill = PatternFill("solid", fgColor=C_RED_FILL)
    red_font = Font(color=C_RED_FONT, bold=True)
    dup_fill = PatternFill("solid", fgColor=C_DUP_FILL)
    dup_font = Font(color=C_DUP_FONT, bold=True)

    # 필수 셀이 비어 있으면 빨간색
    for ci, cdef in enumerate(columns, start=1):
        if cdef["req"] == "req":
            L = get_column_letter(ci)
            rng = "{L}2:{L}{n}".format(L=L, n=last_row)
            ws.conditional_formatting.add(
                rng, FormulaRule(formula=["LEN(TRIM({L}2))=0".format(L=L)],
                                 fill=red_fill, font=red_font, stopIfTrue=False))

    # 중복 고유 키 강조
    for group in spec["unique_groups"]:
        letters = [get_column_letter(ci) for ci in group]
        first_L = letters[0]
        countifs = "COUNTIFS(" + ",".join(
            "${L}$2:${L}${n},${L}2".format(L=L, n=last_row) for L in letters) + ")"
        formula = "AND(${L}2<>\"\",{cf}>1)".format(L=first_L, cf=countifs)
        # 각 키 컬럼 전체에 적용
        for L in letters:
            rng = "{L}2:{L}{n}".format(L=L, n=last_row)
            ws.conditional_formatting.add(
                rng, FormulaRule(formula=[formula], fill=dup_fill, font=dup_font, stopIfTrue=False))


# --------------------------------------------------------------------------
# README 시트
# --------------------------------------------------------------------------
def build_readme(ws):
    ws.sheet_view.showGridLines = False
    ws.column_dimensions["A"].width = 3
    ws.column_dimensions["B"].width = 30
    ws.column_dimensions["C"].width = 95

    title_fill = PatternFill("solid", fgColor="1F4E78")
    sec_fill = PatternFill("solid", fgColor="D6E4F0")
    warn_fill = PatternFill("solid", fgColor="FDECEA")

    r = 2
    ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=3)
    t = ws.cell(row=r, column=2, value="Ruin Conservator  대화 & 퀘스트 CSV 작성 가이드")
    t.font = Font(name="맑은 고딕", bold=True, size=18, color="FFFFFF")
    t.fill = title_fill
    t.alignment = Alignment(horizontal="center", vertical="center")
    ws.cell(row=r, column=3).fill = title_fill
    ws.row_dimensions[r].height = 40
    r += 1
    ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=3)
    sub = ws.cell(row=r, column=2, value="이 파일은 기획자가 Excel에서 대화/퀘스트 데이터를 작성하고, 각 시트를 게임용 UTF-8 CSV로 내보내기 위한 작업 원본입니다.")
    sub.font = Font(name="맑은 고딕", size=11, italic=True, color="555555")
    sub.alignment = Alignment(wrap_text=True, vertical="center")
    ws.row_dimensions[r].height = 30
    r += 2

    def section(title):
        nonlocal r
        ws.merge_cells(start_row=r, start_column=2, end_row=r, end_column=3)
        c = ws.cell(row=r, column=2, value=title)
        c.font = Font(name="맑은 고딕", bold=True, size=13, color="1F3864")
        c.fill = sec_fill
        c.alignment = Alignment(vertical="center")
        ws.cell(row=r, column=3).fill = sec_fill
        ws.row_dimensions[r].height = 26
        r += 1

    def kv(key, val, fill=None):
        nonlocal r
        kc = ws.cell(row=r, column=2, value=key)
        kc.font = Font(name="맑은 고딕", bold=True, size=11, color="1F4E78")
        kc.alignment = Alignment(vertical="top", wrap_text=True)
        vc = ws.cell(row=r, column=3, value=val)
        vc.font = Font(name="맑은 고딕", size=11)
        vc.alignment = Alignment(vertical="top", wrap_text=True)
        if fill:
            kc.fill = fill
            vc.fill = fill
        lines = val.count("\n") + 1
        ws.row_dimensions[r].height = max(20, 16 * lines)
        r += 1

    section("1. 이 파일의 목적")
    kv("목적", "게임 기획자가 대화와 퀘스트 데이터를 직접 작성하기 위한 교보재 겸 템플릿입니다. "
       "샘플로 '손상된 벽화 조사' 퀘스트와 큐레이터 대화가 실제로 연결되어 들어 있습니다.")

    section("2. .xlsx 와 .csv 의 차이")
    kv(".xlsx (이 파일)", "여러 시트·색상·메모·드롭다운을 담는 작업 원본. 수정은 항상 이 파일에서 합니다.")
    kv(".csv", "시트 한 장의 표 데이터만 담는 게임 입력용 텍스트 파일. 시트마다 하나씩 내보냅니다.")
    kv("주의", "README, CodeLists, InvalidExamples 시트는 설명용이므로 CSV로 내보내지 않습니다.")

    section("3. 권장 작업 순서")
    kv("작업 순서",
       "1) DialogueActors, DialoguePortraits 작성\n"
       "2) Quests 작성\n"
       "3) QuestObjectives, QuestRewards, QuestConsumeItems 작성\n"
       "4) DialogueGraphs 작성\n"
       "5) DialogueNodes 작성\n"
       "6) DialogueChoices 작성\n"
       "7) Localization 문장 작성\n"
       "8) ID와 참조 관계 검사\n"
       "9) 각 데이터 시트를 UTF-8 CSV로 내보내기\n"
       "10) 게임 데이터 검증 도구 실행")

    section("4. 시트 목록과 내보낼 CSV 파일 이름")
    kv("① README", "설명용 · CSV 내보내기 안 함")
    kv("② CodeLists", "드롭다운 값 목록 · CSV 내보내기 안 함")
    kv("③ DialogueActors", "dialogue_actors.csv")
    kv("④ DialoguePortraits", "dialogue_portraits.csv")
    kv("⑤ DialogueGraphs", "dialogue_graphs.csv")
    kv("⑥ DialogueNodes", "dialogue_nodes.csv")
    kv("⑦ DialogueChoices", "dialogue_choices.csv")
    kv("⑧ Quests", "quests.csv")
    kv("⑨ QuestObjectives", "quest_objectives.csv")
    kv("⑩ QuestRewards", "quest_rewards.csv")
    kv("⑪ QuestConsumeItems", "quest_consume_items.csv")
    kv("⑫ Localization", "localization.csv")
    kv("⑬ NpcDialogueBindings", "npc_dialogue_bindings.csv (선택)")
    kv("⑭ NpcQuestDialogueRoutes", "npc_quest_dialogue_routes.csv (선택)")
    kv("⑮ InvalidExamples", "설명용 · CSV 내보내기 안 함")

    section("5. 색상 안내 (한눈에 보기)")
    kv("진한 파랑 헤더", "필수 입력 컬럼입니다.")
    kv("연한 파랑 헤더", "선택 입력 컬럼입니다.")
    kv("중간 파랑 헤더", "조건부 필수 컬럼입니다 (다른 값이 있으면 필수).")
    kv("연한 노랑 셀", "ID 또는 다른 시트를 참조하는 컬럼입니다. 철자·대소문자를 정확히 맞추세요.")
    kv("연한 초록 셀", "기획자가 직접 쓰는 화면 표시 문장(Localization ko/en/ja)입니다.")
    kv("굵은 파란 가로선", "그 위는 샘플 데이터, 아래는 직접 입력하는 영역입니다.")
    kv("빨간 셀", "필수 값이 비어 있다는 경고입니다. 값을 채우면 사라집니다.")
    kv("주황 셀", "고유해야 할 ID가 중복되었다는 경고입니다.")

    section("6. ID 변경 경고")
    kv("경고", "출시 후 quest_id 등 ID를 바꾸면 다른 시트의 참조와 세이브 데이터가 깨질 수 있습니다. "
       "ID는 화면에 안 보이므로 의미가 분명하게 처음에 잘 정하고, 되도록 바꾸지 마세요.", warn_fill)

    section("7. 빈 셀 규칙")
    kv("빈 셀", "값이 필요 없는 선택 컬럼은 셀을 완전히 비웁니다. "
       "'없음', 'null', '-', '0' 같은 임의 표기를 넣지 마세요. 필수 컬럼은 비울 수 없습니다.")

    section("8. Boolean / Enum 입력 규칙")
    kv("Boolean", "소문자 true 또는 false 만 사용합니다. TRUE / O / X / 1 / 0 / 예 / 아니오 는 안 됩니다.")
    kv("Enum", "action_type, quest_state 등은 CodeLists의 정확한 문자열만 사용합니다. 드롭다운으로 선택하세요.")
    kv("숫자", "순서·수량은 0 이상의 정수만 사용합니다. 같은 부모 안에서 순서 값은 중복되면 안 됩니다.")

    section("9. 쉼표 · 큰따옴표 · 줄바꿈 작성 예시")
    kv("쉼표", "문장에 쉼표가 있으면 CSV 저장 시 셀 전체가 자동으로 큰따옴표로 감싸집니다. (Excel이 처리)")
    kv("큰따옴표", "셀 안의 \" 는 CSV에서 \"\" 로 표현됩니다.")
    kv("줄바꿈", "대사 줄바꿈은 실제 줄바꿈 대신 문자 \\n 을 사용합니다.\n"
       "예: 잠깐, 그 유물은 만지면 안 돼!\\n먼저 장갑을 착용해.\n"
       "게임 로더가 \\n 을 실제 줄바꿈으로 변환합니다.")

    section("10. CSV로 내보내는 방법 (Excel)")
    kv("내보내기",
       "1) 내보낼 데이터 시트 탭을 선택합니다.\n"
       "2) 파일 > 다른 이름으로 저장 을 누릅니다.\n"
       "3) 파일 형식에서 'CSV UTF-8(쉼표로 분리)(*.csv)' 를 선택합니다.\n"
       "4) 이 가이드에 적힌 정확한 파일 이름으로 저장합니다.\n"
       "5) '현재 시트만 저장된다'는 Excel 경고는 정상입니다.")

    section("11. 내보낸 뒤 주의")
    kv("주의", "내보낸 CSV를 Excel로 다시 열어 저장하지 마세요. Excel이 ID나 형식을 자동 변환할 수 있습니다. "
       "수정은 항상 이 .xlsx 원본에서 하고 CSV를 다시 내보냅니다.", warn_fill)
    kv("색상 소실", "CSV에는 색상·메모·드롭다운이 저장되지 않습니다. 데이터 값만 남습니다.", warn_fill)

    section("12. 오류가 났을 때 체크리스트")
    kv("체크리스트",
       "□ 참조한 ID가 대상 시트에 실제로 존재하나요? (철자·대소문자 일치)\n"
       "□ 고유해야 할 ID가 중복되지 않았나요? (주황 셀 확인)\n"
       "□ 필수 셀이 비어 있지 않나요? (빨간 셀 확인)\n"
       "□ Boolean이 true/false 소문자인가요?\n"
       "□ action_type/quest_state가 목록의 정확한 값인가요?\n"
       "□ 순서 값(order)이 같은 부모 안에서 중복되지 않나요?\n"
       "□ item_id가 있으면 item_amount가 1 이상인가요?\n"
       "□ 한 셀에 여러 선택지/목표/보상을 몰아 쓰지 않았나요?")


# --------------------------------------------------------------------------
# CodeLists 시트
# --------------------------------------------------------------------------
def build_codelists(ws):
    ws.sheet_view.showGridLines = False
    hdr_fill = PatternFill("solid", fgColor="1F4E78")
    hdr_font = Font(name="맑은 고딕", bold=True, color="FFFFFF")
    val_fill = PatternFill("solid", fgColor="FFF9E3")
    desc_font = Font(name="맑은 고딕", size=10, color="555555")

    title = ws.cell(row=1, column=2, value="드롭다운 값 목록 (CodeLists) — 이 시트는 CSV로 내보내지 않습니다")
    title.font = Font(name="맑은 고딕", bold=True, size=13, color="1F3864")
    ws.merge_cells("B1:H1")

    blocks = [
        ("B", "C", "DialogueChoiceActionType (action_type)", ACTION_TYPES, "ActionTypeList"),
        ("E", "F", "QuestState (quest_state)", QUEST_STATES, "QuestStateList"),
        ("H", "I", "Boolean (flag 값)", BOOL_VALUES, "BoolList"),
    ]
    start_row = 3
    for vcolL, dcolL, header, values, name in blocks:
        hv = ws.cell(row=start_row, column=col_idx(vcolL), value=header)
        hv.fill = hdr_fill
        hv.font = hdr_font
        hv.alignment = Alignment(vertical="center")
        hd = ws.cell(row=start_row, column=col_idx(dcolL), value="설명")
        hd.fill = hdr_fill
        hd.font = hdr_font
        for i, (val, desc) in enumerate(values, start=start_row + 1):
            vc = ws.cell(row=i, column=col_idx(vcolL), value=val)
            vc.fill = val_fill
            vc.font = Font(name="맑은 고딕", bold=True)
            vc.border = BORDER_THIN
            vc.number_format = "@"
            dc = ws.cell(row=i, column=col_idx(dcolL), value=desc)
            dc.font = desc_font
            dc.border = BORDER_THIN
        # 이름 정의 (드롭다운 참조용)
        first = start_row + 1
        last = start_row + len(values)
        ref = "CodeLists!${L}${a}:${L}${b}".format(L=vcolL, a=first, b=last)
        wb.defined_names.add(DefinedName(name, attr_text=ref))

    for L, w in [("B", 30), ("C", 42), ("E", 26), ("F", 26), ("H", 14), ("I", 20)]:
        ws.column_dimensions[L].width = w


def col_idx(letter):
    from openpyxl.utils import column_index_from_string
    return column_index_from_string(letter)


# --------------------------------------------------------------------------
# InvalidExamples 시트
# --------------------------------------------------------------------------
INVALID_ROWS = [
    ("중복 Quest ID", "QUEST_RESTORE_MURAL 두 행", "한 행만 유지하거나 새 ID 부여"),
    ("존재하지 않는 시작 노드", "start_node_id=NODE_MISSING", "실제 Node ID 입력"),
    ("다른 그래프의 다음 노드 참조", "현재 그래프에 없는 next_node_id", "같은 Graph의 Node ID 사용"),
    ("존재하지 않는 Actor", "actor_id=ACTOR_UNKNOWN", "DialogueActors에 먼저 등록"),
    ("잘못된 Action Type", "AcceptQuest", "ACCEPT_QUEST (대문자·밑줄) 사용"),
    ("Quest 액션 ID 누락", "ACCEPT_QUEST 인데 action_id 빈 셀", "실제 Quest ID 입력"),
    ("END_DIALOGUE 충돌", "END_DIALOGUE 인데 next_node_id 입력", "next_node_id/action_id 비우기"),
    ("잘못된 Boolean", "TRUE, 1, O", "true 또는 false 사용"),
    ("잘못된 수량", "item_amount=0 또는 음수", "1 이상의 정수 사용"),
    ("Item ID 없이 수량만 입력", "item_id 빈 셀인데 item_amount=100", "둘 다 비우거나 Item ID 입력"),
    ("한 셀에 선택지 여러 개", "수락|거절", "DialogueChoices에 두 행으로 작성"),
    ("Localization Key 누락", "참조 키가 Localization에 없음", "해당 키와 문장 추가"),
    ("ID에 공백/한글 사용", "퀘스트 1", "QUEST_001 형태로 사용"),
]


def build_invalid(ws):
    ws.sheet_view.showGridLines = False
    title = ws.cell(row=1, column=2, value="잘못된 입력 예시 (InvalidExamples) — 실제 데이터 시트에는 절대 넣지 마세요")
    title.font = Font(name="맑은 고딕", bold=True, size=13, color="9C0006")
    ws.merge_cells("B1:D1")

    hdr_fill = PatternFill("solid", fgColor="C00000")
    hdr_font = Font(name="맑은 고딕", bold=True, color="FFFFFF")
    headers = ["오류 사례", "잘못된 예", "수정 방향"]
    for ci, h in enumerate(headers, start=2):
        c = ws.cell(row=3, column=ci, value=h)
        c.fill = hdr_fill
        c.font = hdr_font
        c.alignment = ALIGN_HDR
        c.border = BORDER_THIN
    ws.row_dimensions[3].height = 24

    err_fill = PatternFill("solid", fgColor="FDECEA")
    fix_fill = PatternFill("solid", fgColor="E7F4E4")
    for ri, (case, bad, fix) in enumerate(INVALID_ROWS, start=4):
        a = ws.cell(row=ri, column=2, value=case)
        a.fill = err_fill
        a.font = Font(name="맑은 고딕", bold=True, color="9C0006")
        b = ws.cell(row=ri, column=3, value=bad)
        b.fill = err_fill
        b.font = Font(name="Consolas", color="9C0006")
        d = ws.cell(row=ri, column=4, value=fix)
        d.fill = fix_fill
        d.font = Font(name="맑은 고딕", color="215C2E")
        for c in (a, b, d):
            c.border = BORDER_THIN
            c.alignment = Alignment(vertical="center", wrap_text=True)
    ws.column_dimensions["B"].width = 30
    ws.column_dimensions["C"].width = 40
    ws.column_dimensions["D"].width = 45


# --------------------------------------------------------------------------
# 시트 조립 (명세 순서)
# --------------------------------------------------------------------------
ws_readme = wb.create_sheet("README")
build_readme(ws_readme)

ws_codelists = wb.create_sheet("CodeLists")
build_codelists(ws_codelists)

for spec in SHEETS:
    ws = wb.create_sheet(spec["name"])
    build_data_sheet(ws, spec)

ws_invalid = wb.create_sheet("InvalidExamples")
build_invalid(ws_invalid)

wb.active = 0
wb.save(XLSX_PATH)
print("xlsx 저장:", XLSX_PATH)

# --------------------------------------------------------------------------
# CSV 내보내기 (UTF-8 BOM)
# --------------------------------------------------------------------------
csv_report = []
for spec in SHEETS:
    out_path = os.path.join(CSV_ROOT, spec["csv"].replace("/", os.sep))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    header = [c["name"] for c in spec["columns"]]
    with open(out_path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f, quoting=csv.QUOTE_MINIMAL, lineterminator="\n")
        w.writerow(header)
        for row in spec["rows"]:
            w.writerow(["" if v == "" else v for v in row])
    csv_report.append((spec["csv"], len(spec["rows"])))

print("\n=== CSV 내보내기 완료 ===")
for name, n in csv_report:
    print("  {:<44} 데이터 {}행".format(name, n))
print("\n총 시트: {} (데이터 {} + 설명 3)".format(len(SHEETS) + 3, len(SHEETS)))
