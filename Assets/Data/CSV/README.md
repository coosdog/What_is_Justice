# 대사 CSV 규칙

대사는 `id` 값으로 찾아옵니다. `ClueData`나 씬 데이터에서 이 `id`를 직접 참조하므로, 한 번 사용하기 시작한 ID는 가능하면 바꾸지 않는 것이 좋습니다.

## 필수 컬럼

```csv
id,speaker,text,portrait_key,emotion,board_node_id,board_display_name,board_description,show_portraits,portrait_layout
```

- `id`: 스크립트에서 대사를 찾을 때 사용하는 고유 키입니다.
- `speaker`: 플레이어에게 표시할 이름입니다. 예: `책상`, `판사`, `???`
- `text`: 실제로 출력될 조사 문장 또는 캐릭터 대사입니다.
- `portrait_key`: 캐릭터 초상화를 찾을 때 사용할 선택 값입니다.
- `emotion`: 캐릭터 표정이나 상태를 나타내는 선택 값입니다. 예: `calm`, `angry`, `sad`
- `board_node_id`: 단서 연결 후보에 올릴 때 사용하는 고유 키입니다. 비워두면 연결 후보에 등록되지 않습니다.
- `board_display_name`: 단서 연결 후보 목록에 표시할 이름입니다. 비워두면 `{speaker}의 발언`으로 표시됩니다.
- `board_description`: 연결 후보 상세 설명입니다. 비워두면 `text`를 그대로 사용합니다.
- `show_portraits`: A/B 캐릭터 이미지 창을 표시할지 정합니다. 비워두면 `true`이며, 나레이션은 `false`로 둡니다.
- `portrait_layout`: 초상 표시 방식입니다. 비워두면 A/B 양쪽 표시, `single`/`left`/`player_only`는 A칸만 표시합니다.

## 캐릭터 일러스트 표시

대화창의 A/B 이미지 칸은 `portrait_key`로 제어합니다. 씬의 `GlobalGameSystems`에 붙은 `PortraitSpriteRegistry`에서 `portrait_key`와 스프라이트를 연결해두면, CSV에서는 같은 키만 적으면 됩니다.

예시:

```csv
id,speaker,text,portrait_key,emotion,board_node_id,board_display_name,board_description,show_portraits,portrait_layout
tutorial.opening.001,손주 올빼미,"나도 언젠가는 그런 탐정이 되고 싶다.",child_owl,hopeful,,,,true,single
tutorial.grandfather.001,할아버지 올빼미,"답을 맞히는 것보다 중요한 건 이유를 설명하는 눈이다.",old_owl,calm,,,,true,
system.office.narration.001,나레이션,"문 앞에서 종이가 떨어지는 소리가 났다.",narration,,,,,false,
```

- 손주 올빼미 독백처럼 A칸만 보여주고 싶으면 `portrait_layout`에 `single`을 넣습니다.
- 나레이션이나 시스템 문장은 `show_portraits`를 `false`로 둡니다.
- 새 캐릭터를 추가할 때는 `PortraitSpriteRegistry`에 키와 스프라이트를 추가한 뒤, CSV의 `portrait_key`에 그 키를 사용합니다.

## ID 작성 형식

ID는 점으로 구분해서 작성합니다.

```text
category.owner.location_or_context.purpose_or_index
```

예시:

```text
object.desk.office.first
object.desk.office.repeat
object.desk.courtroom.first
character.judge.intro.001
character.judge.case01.warning.001
```

## 카테고리

- `object`: 조사 가능한 오브젝트의 텍스트입니다.
- `character`: 캐릭터 대사입니다.
- `system`: 나중에 필요할 수 있는 UI 또는 시스템 메시지입니다.

## 조사 오브젝트 대사 규칙

조사 가능한 오브젝트는 아래 형식을 권장합니다.

```text
object.{object_name}.{place_or_variant}.first
object.{object_name}.{place_or_variant}.repeat
```

예시:

```text
object.desk.office.first
object.desk.office.repeat
object.desk.courtroom.first
object.desk.courtroom.repeat
```

이렇게 쓰면 책상이 여러 개 있어도 `desk2` 같은 애매한 이름을 쓰지 않아도 됩니다. 대신 `office`, `courtroom`, `archive`처럼 위치나 맥락으로 구분합니다.

## 캐릭터 대사 규칙

캐릭터 대사는 아래 형식을 권장합니다.

```text
character.{character_name}.{scene_or_topic}.{number}
```

예시:

```text
character.judge.intro.001
character.judge.case01.warning.001
character.detective.office.001
```

대사 번호는 `001`, `002`, `003`처럼 세 자리 숫자로 쓰는 것을 추천합니다. 대사가 많아졌을 때 정렬이 깔끔하게 유지됩니다.

## 단서 연결 후보 등록

모든 대사가 자동으로 단서 연결 보드에 올라가지는 않습니다. 기획자가 중요한 발언만 `board_node_id`를 채운 행으로 지정합니다.

예시:

```csv
id,speaker,text,portrait_key,emotion,board_node_id,board_display_name,board_description,show_portraits,portrait_layout
character.witness.alibi.001,목격자,"그 시간에는 복도에 없었습니다.",witness,nervous,dialogue.witness.alibi,목격자의 알리바이 발언,복도에 없었다고 주장한 발언,true,
```

이렇게 지정된 발언은 대화 로그에는 그대로 기록되고, 단서 연결 보드에는 `[발언] 목격자의 알리바이 발언`으로 표시됩니다.

## 나레이션 대사

캐릭터가 말하는 대사가 아니라 장면 설명이나 나레이션이라면 `show_portraits`를 `false`로 둡니다.

```csv
id,speaker,text,portrait_key,emotion,board_node_id,board_display_name,board_description,show_portraits,portrait_layout
system.office.narration.001,,"창밖으로 빗소리가 얇게 번진다.",narration,,,,,false,
```

`portrait_key`에 `none`, `off`, `narration`, `system`을 넣어도 A/B 이미지 창은 숨겨집니다.

## 챕터 오프닝 대사

챕터 시작 대사는 `ChapterOpeningDialoguePlayer`가 아래 prefix 규칙으로 자동 재생합니다.

```text
tutorial.opening.001
tutorial.opening.002
chapter1.opening.001
chapter1.opening.002
chapter2.opening.001
```

새 챕터를 만들 때는 별도 스크립트를 만들지 않고, 해당 챕터 prefix의 대사 행만 추가하면 됩니다. 씬에서 다른 prefix를 쓰고 싶다면 `ChapterOpeningDialoguePlayer`의 `Opening Dialogue Prefix` 값을 직접 지정합니다.

## 현재 책상 예시

`Desk` 오브젝트의 `ClueData`에는 아래처럼 연결하면 됩니다.

```text
First Investigation Dialogue Id = object.desk.office.first
Already Investigated Dialogue Id = object.desk.office.repeat
```

## 관찰모드별 차이

플레이어 관찰모드는 내부 값으로 `Basic`, `Tendency1`, `Tendency2`, `Tendency3`를 사용합니다.

- `Basic`: 기본 관찰. 일반 조사와 기본 탐문에 사용합니다.
- `Tendency1`: 예리한 시야. 조사 중 숨은 물리 단서를 발견하는 모드입니다.
- `Tendency2`: 미세한 청각. 탐문 중 말끝, 호흡, 망설임을 읽는 모드입니다.
- `Tendency3`: 침묵의 응시. 탐문 중 압박과 회피 반응을 읽는 모드입니다.

- 사물 조사 대사는 `ClueData`의 `Disposition Overrides`에 관찰모드별 대사 ID나 fallback 문장을 넣어 바꿀 수 있습니다.
- NPC 탐문 CSV는 `npc_inquiry_topics.csv`에 선택 컬럼 `disposition`을 추가하면 관찰모드별 응답을 분리할 수 있습니다.
- `disposition`이 비어 있거나 `basic`이면 기본 응답으로 쓰이고, `Tendency2`, `Tendency3` 행이 있으면 탐문 중 현재 관찰모드에 맞는 응답이 우선됩니다.
- 탐문에서 `Tendency1`은 부적합 모드로 처리됩니다. 조사에서 `Tendency2`, `Tendency3`도 부적합 모드로 처리됩니다.

예시:

```csv
npc_id,keyword_id,disposition,response_dialogue_ids,fallback_response_text
witness,keyword.alibi,basic,npc.witness.alibi.basic,평범한 반응입니다.
witness,keyword.alibi,Tendency2,npc.witness.alibi.hearing,미세한 청각으로 말끝의 흔들림을 듣습니다.
witness,keyword.alibi,Tendency3,npc.witness.alibi.gaze,침묵의 응시로 회피 반응을 끌어냅니다.
```
