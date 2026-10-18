<!-- modules/my-room.md -->

# 마이 룸

마이 룸은 `Global/Modules/Stages/`에 있는 전역 스테이지 `my_room`입니다. 플레이어의 방으로, 플레이어는 그 안을 돌아다니고, 미지의 탑, 금고, 코인 숍에서 얻은 것으로 가구를 놓고, 온라인 방문자에게 방을 엽니다. 어느 스킨에서나 열 수 있고, 데이터가 세이브 파일에 있으므로 모든 스킨이 같은 방을 보여 줍니다.

## 들어가기와 나가기

공유 문자열 `my_room_return`에 마이 룸이 돌아갈 스테이지를 설정한 뒤, `my_room`으로 나갑니다.

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

마이 룸은 들어올 때 이 문자열을 읽고 지웁니다. 비어 있으면 돌아갈 곳은 타이틀 화면(`_title`)입니다.

공유 문자열 `my_room_place`는 방에서 나갔다가 돌아오는 동안 누구의 방이었고 플레이어가 어디에 서 있었는지를 기억합니다. 플레이스홀더 매트릭스(방문할 스테이지를 나열하는 가구)와 포드(다른 스테이지를 여는 가구)는 둘 다 방 밖으로 이어집니다. 타이틀 화면이 이를 지우면(`""`로 설정하면) 다음 방문은 문 앞에서 시작합니다.

Open-World Memories는 예전 `Exit("stage", "myroom")` 호출과 엔진의 `"heya"` 나가기를 위해 전달용 스테이지 `myroom`을 유지합니다. 이 스테이지는 정식 릴리스에서 사라지므로, 새 코드는 `my_room`으로 나가야 합니다.

## 데이터

| 키 | 종류 | 내용 |
| --- | --- | --- |
| `my_room` | 세이브 문자열 | 방 자체로, JSON `{ v, room, jukebox }` 형식입니다(`sf:GetGlobalString("my_room")`). 세이브 자신의 슬롯에만 기록되며, 온라인 방문자나 빈 슬롯에는 기록되지 않습니다. |
| `.myroom_<id>` | 카운터 | 다른 스테이지가 주는 가구: 마이 룸은 다음 방문 때 이를 한 번 방의 재고에 더하고, 이미 가져간 것을 장부에 남깁니다. |
| `.myroom_held_<id>` | 카운터 | 세이브가 그 가구를 몇 개 소유하는지. 마이 룸이 최신으로 유지하며, 가구를 주는 스테이지도 즉시 올립니다. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | 카운터 | 방의 너비와 높이, 그리고 배치된 가구 수로, 세이브 목록에 표시하도록 마이 룸이 기록합니다(읽기만 하십시오). |
| `myroom_thumb_<id>` | 공유 텍스처 | 숍이나 보상 가구의 그림으로, 마이 룸이 시작할 때 만들어지며 구매와 보상 팝업이 보여 줍니다. |

자신의 스테이지에서 가구를 주려면 [Pagoda.Rewards](pagoda.md)와 [Vault.Chests](vault.md)처럼 두 카운터에 모두 1을 더하십시오. `my_room` 문자열이 설정되지 않은 동안에는 임시 마이그레이션이 Open-World Memories의 예전 저장소에서 방을 읽으며, 이는 정식 릴리스에서 제거됩니다.

## 바탕이 되는 것

- 메뉴와 대화 상자는 [UI.Pick](ui.md)을 통해 스킨의 UI 라이브러리를 씁니다. 라이브러리는 처음 쓸 때 정해지며(Open-World Memories에서는 PopUI, 아무것도 지정하지 않은 스킨에서는 SimpleUI), 코인 표시는 그 라이브러리의 코인 위젯입니다.
- 가구 이름(`lang/<code>/items.json`, 가구 id가 키)을 포함한 문자열은 자체 `lang/` 폴더에 아홉 개 언어로 있습니다.
- 가구 카탈로그는 스테이지 폴더의 `data/furniture.json`입니다.
- 플레이스홀더 매트릭스는 `Global/Modules/Stages/`에 있는 디버그 스테이지만 나열합니다. 이 디버그 스테이지들과 TV가 쓰는 `video_player` ROActivity도 전역 모듈입니다.
- 포드는 `Global/Modules/Stages/`에 스테이지 `mysterypod`가 있을 때, 또는 스킨 자체의 `mysterypod` 스테이지가 `onStart`에서 공유 문자열 `mysterypod_installed`를 `"1"`로 설정할 때 그 스테이지를 엽니다.
- 온라인 방문은 이동 전과 마찬가지로 방 id `"myroom"`과 그 페이로드의 버전 2를 씁니다.
