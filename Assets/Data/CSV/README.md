# 대사 CSV 규칙

대사는 `id` 값으로 찾아옵니다. `ClueData`나 씬 데이터에서 이 `id`를 직접 참조하므로, 한 번 사용하기 시작한 ID는 가능하면 바꾸지 않는 것이 좋습니다.

## 필수 컬럼

```csv
id,speaker,text,portrait_key,emotion
```

- `id`: 스크립트에서 대사를 찾을 때 사용하는 고유 키입니다.
- `speaker`: 플레이어에게 표시할 이름입니다. 예: `책상`, `판사`, `???`
- `text`: 실제로 출력될 조사 문장 또는 캐릭터 대사입니다.
- `portrait_key`: 캐릭터 초상화를 찾을 때 사용할 선택 값입니다.
- `emotion`: 캐릭터 표정이나 상태를 나타내는 선택 값입니다. 예: `calm`, `angry`, `sad`

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

## 현재 책상 예시

`Desk` 오브젝트의 `ClueData`에는 아래처럼 연결하면 됩니다.

```text
First Investigation Dialogue Id = object.desk.office.first
Already Investigated Dialogue Id = object.desk.office.repeat
```

## 성향별 차이

플레이어 성향은 `Basic`, `Tendency1`, `Tendency2` 세 값으로 구분합니다.

- 사물 조사 대사는 `ClueData`의 `Disposition Overrides`에 성향별 대사 ID나 fallback 문장을 넣어 바꿀 수 있습니다.
- NPC 탐문 CSV는 `npc_inquiry_topics.csv`에 선택 컬럼 `disposition`을 추가하면 성향별 응답을 분리할 수 있습니다.
- `disposition`이 비어 있거나 `basic`이면 기본 응답으로 쓰이고, `Tendency1`, `Tendency2` 행이 있으면 현재 성향에 맞는 응답이 우선됩니다.

예시:

```csv
npc_id,keyword_id,disposition,response_dialogue_ids,fallback_response_text
witness,keyword.alibi,basic,npc.witness.alibi.basic,평범한 반응입니다.
witness,keyword.alibi,Tendency1,npc.witness.alibi.t1,성향 1에서 더 의심스럽게 받아들입니다.
witness,keyword.alibi,Tendency2,npc.witness.alibi.t2,성향 2에서 다른 단서를 떠올립니다.
```
