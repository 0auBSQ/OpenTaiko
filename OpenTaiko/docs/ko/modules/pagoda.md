<!-- modules/pagoda.md -->

# Pagoda

미지의 탑은 단위 층을 차례로 오르는 도전입니다. 각 층은 자신의 풀에서 곡 세 개(가끔 네 번째인 보라색 곡도)를 뽑고, 층이 올라갈수록 엄격해지는 시험 세 개가 있습니다. `Pagoda` 모듈은 규칙, 데이터, 세이브 키를 담고 있으며 아무것도 그리지 않습니다. 스킨은 그 위에 자신만의 탑 화면을 그리고, 진행 상황이 세이브 파일에 있으므로 모든 스킨이 하나의 진행 상황을 공유합니다. Open-World Memories의 `pagoda` 스테이지가 그런 클라이언트 중 하나입니다.

1~10층은 道一~道十, 11~20층은 扉一~扉十, 21층부터는 지붕인 穹天1, 穹天2 순으로 이름이 붙습니다. 이름은 단위의 칭호이며 모든 언어에서 같습니다.

## 모듈 목록

| 모듈 | 내용 |
| --- | --- |
| `Pagoda.Levels` | 층 번호에서 도출되는 모든 것: 곡 풀, 이름, 단위 플레이트의 테두리와 색, 시험 기준값, 보라색 곡의 확률. 순수한 조회입니다. |
| `Pagoda.Floor` | 층의 곡(추첨 결과)과 그 단위: 추첨 결과를 플레이트와 시험과 함께 `DANBUILDER`에 넘깁니다. |
| `Pagoda.Rewards` | 클리어로 얻는 것: 네임플레이트, 코인, 마이 룸 가구, 금고 열쇠. 이를 저장하고 `modal` ROActivity로 팝업을 보여 줍니다. |
| `Pagoda.Progress` | 영구 세이브 데이터(아래). |
| `Pagoda.Songs` | 탑이 곡을 뽑는 곡 목록과, 사운드트랙이 풀의 모든 항목을 갖추고 있는지에 대한 확인. |
| `Pagoda.Run` | 선택 사항: 도전과 연습의 흐름을 화면 없이 다룹니다. |
| `Pagoda.Texts` | 모듈 자체의 문자열(`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| 함수 | 설명 |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | 층의 풀: `{ id, diff }`(곡의 고유 id와 난이도 0~4)의 목록 `{ blue, green, red, purple }`. 자기 항목이 없는 층은 그 아래에서 가장 가까운 항목을 씁니다. |
| `Levels.load_pools()`, `Levels.clear_pools()` | `pools.json`을 읽습니다(한 번만). 곡을 다시 열거한 뒤에는 읽은 내용을 잊습니다. |
| `Levels.level_name(level)  -> string` | 층의 이름. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | 단위 플레이트의 테두리(0~4)와 색 `{ r, g, b }`. |
| `Levels.exam1_gauge(level)  -> red, gold` | 게이지 시험. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | 정확도 시험(1~20층), 또는 좋다 개수 시험(21층부터). |
| `Levels.exam3_base(level)  -> red, gold` | 보라색 곡에 따른 완화를 적용하기 전의 에구 개수 시험. |
| `Levels.purple_prob(level)  -> number` | 네 번째 곡으로 보라색 곡이 나올 확률(20층에는 항상 나옵니다). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | 추첨에 쓰이는 헬퍼. |

모듈 옆의 `pools.json`은 각 층 번호를 그 풀에 대응시킵니다.

```json
{
  "1": {
    "comment": "一題",
    "blue":   [ { "id": "<song unique id>", "diff": 0 } ],
    "green":  [ { "id": "<song unique id>", "diff": 0 } ],
    "red":    [ { "id": "<song unique id>", "diff": 0 } ],
    "purple": [ { "id": "<song unique id>", "diff": 0 } ]
  }
}
```

풀은 OpenTaiko 사운드트랙의 곡을 지정합니다. `Songs.count_missing_songs()`는 설치된 곡에 없는 항목(곡, 또는 그 난이도)을 로그에 남기고 세므로, 화면에서 플레이어에게 경고할 수 있습니다.

## Pagoda.Floor

| 함수 | 설명 |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | 파란색, 초록색, 빨간색 곡을 하나씩 섞인 순서로 뽑고, 층의 확률에 따라 보라색 곡을 더합니다. 같은 곡은 두 번 나오지 않습니다. `avoid`는 풀에 다른 곡이 있는 한 뽑지 않을 곡 id의 집합입니다. 추첨 결과는 `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`입니다. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | 풀이 허락하는 범위에서 다른 곡으로 다시 추첨합니다. `keepMystery`는 보라색 곡을 유지합니다. |
| `Floor.exams(level, hadPurple)  -> table` | 세 시험을 `{ type, red, gold, lessThan }`으로 반환합니다. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | 층에 맞게 `DANBUILDER`를 설정합니다: 제목, 플레이트, 장르(`opts.genre`, `opts.genreColor`), 곡, 시험. 곡을 하나도 추가하지 못하면 false이며, 그렇지 않으면 `DANBUILDER:Mount()`로 마운트하고 플레이합니다. |
| `Floor.speed(level)  -> number` | 층의 곡 속도(`CONFIG.SONGSPEED` 값): 穹天18 아래는 x1이고, 그 뒤로 올라갑니다. |

## Pagoda.Run

도전은 열려 있는 시작 층 중 하나에서 시작해 클리어할 때마다 한 층씩 오르고, 첫 패배에서 끝납니다. 연습은 도전에서 도달한 가장 높은 층(최소 11층)까지 어느 층이든 플레이할 수 있으며 아무것도 저장하지 않습니다.

| 함수 | 설명 |
| --- | --- |
| `Run.startOptions()  -> table` | 이 세이브에서 열려 있는 시작 층: 1, 6, 11은 항상, 16은 15층을 클리어한 뒤, 21은 20층을 클리어한 뒤. |
| `Run.new(start, songList?)  -> run` | 그 층에서 시작하는 도전. 필드: `level`(현재 층), `tower`(클리어로 표시되는 가장 높은 층), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | 현재 층의 곡으로, 플레이 전에 보여 줍니다. |
| `run:enter()` | 도전이 그 층에 도달했음을 기록합니다. 이제부터 연습에서 그 층을 고를 수 있습니다. |
| `run:buildDan(opts?)  -> bool` | 현재 층에 맞게 설정된 `DANBUILDER`. 마운트하고 플레이합니다. |
| `run:clear()  -> result` | 클리어 후: 보상을 지급하고 저장하며, 최고 층을 올리고, 한 층 오릅니다. `{ level, rewards, shrouded }`를 반환합니다. `rewards`는 `Rewards.grant`가 반환하는 목록이고, `shrouded`는 그 층이 탑보다 위에 있었는지 여부입니다. |
| `run:fail()` | 패배 후: 층을 기억하고, 플레이 횟수를 세고, 도전을 끝냅니다. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | 연습에서 고를 수 있는 층; 새로 추첨한 곡으로 연습 플레이용으로 설정된 `DANBUILDER`. |

## Pagoda.Rewards

| 함수 | 설명 |
| --- | --- |
| `Rewards.grant(level)  -> table` | 클리어 보상을 플레이어 1의 세이브에 지급하고 저장한 뒤, 보여 줄 팝업을 순서대로 반환합니다: `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.setup()` | 열쇠 그림을 한 번 공유합니다(`pagoda_reward_key_<id>`). 열쇠 팝업을 보여 주기 전에 호출하십시오. |
| `Rewards.show(r)  -> bool` | `modal` ROActivity로 보상 하나의 팝업을 시작합니다. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | 팝업을 진행하고(재생 중에는 true. 입력은 스스로 읽습니다), 그리고, 버립니다. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | 층의 보상 풀, 그리고 그 층의 첫 클리어로 얻는 네임플레이트. |

층을 처음 클리어하면 그 층의 네임플레이트(id 381~397, `Rewards.NAMEPLATES`에 나열된 층)를 얻습니다. 그다음 코인(범위 안의 금액, 50% 확률)과 층 풀의 각 항목이 각자의 확률로 추첨되므로(`Rewards.POOLS`, `Rewards.ROOF_POOLS`), 한 번의 클리어로 여러 가지를 얻을 수 있습니다. 비밀 금고가 닫혀 있는 동안 금고 열쇠는 코인이 되고, 한 번의 클리어에서 얻은 코인은 모두 팝업 하나로 묶입니다. 가구 팝업은 [마이 룸](my-room.md)이 제공하는 공유 텍스처 `myroom_thumb_<id>`를 보여 줍니다.

## 세이브 키

모든 것은 플레이어 1의 세이브(`GetSaveFile(0)`)에 보관됩니다.

| 키 | 종류 | 내용 |
| --- | --- | --- |
| `pagoda_highest_level` | 카운터 | 도전에서 클리어한 가장 높은 층. |
| `pagoda_highest_reached` | 카운터 | 도전에서 도달한 가장 높은 층으로, 연습은 여기까지의 층을 제공합니다. |
| `pagoda_plays` | 카운터 | 플레이한 도전 횟수(패배할 때 셉니다). |
| `pagoda_lost_<level>` | 트리거 | 그 층에서 도전에 패배했음. |
| `pagoda_story_first_seen` | 트리거 | 첫 스토리를 재생했음(예전 세이브에는 대신 카운터 `pagoda_story_seen`이 있을 수 있습니다). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | 트리거, 카운터 | 탑 플레이 동안 보관하는 플레이어 1의 모드 플래그로, 32비트씩 두 부분으로 나눕니다(`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | 카운터 | 예전 세이브의 번호를 한 번 다시 매기는 작업(`Progress.migrate_save()`)이 실행되었음. |
| `.myroom_<id>`, `.myroom_held_<id>` | 카운터 | 얻은 가구: 마이 룸에 넘길 것, 그리고 소유한 것. |
| `.vault_key_count_<key>` | 카운터 | 얻은 금고 열쇠로, [Vault.Keys](vault.md)를 통해 다룹니다. |

네임플레이트와 코인은 세이브 자체의 메서드(`UnlockNameplate`, `EarnCoins`)를 거칩니다.

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
Rewards.setup()
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
