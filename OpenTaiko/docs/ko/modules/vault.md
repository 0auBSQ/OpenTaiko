<!-- modules/vault.md -->

# Vault

비밀 금고에는 보물 상자 세 개가 있으며, 플레이어가 다른 곳(미지의 탑 클리어, 코인 숍)에서 얻은 열쇠로 엽니다. `Vault` 모듈은 열쇠, 상자의 보상 풀, 세이브 키를 담고 있으며 아무것도 그리지 않습니다. 스킨은 그 위에 자신만의 금고 화면을 그리고, 상태가 세이브 파일에 있으므로 모든 스킨이 하나의 금고를 공유합니다. Open-World Memories의 `secret_vault_rw` 스테이지가 그런 클라이언트 중 하나입니다.

## 모듈 목록

| 모듈 | 내용 |
| --- | --- |
| `Vault.Keys` | 열쇠 세 가지: 개수와 이름, 그리고 금고가 열려 있는지 여부. |
| `Vault.Chests` | 상자 세 개, 그 가중치 보상 풀, 보상 팝업. |
| `Vault.Texts` | 모듈 자체의 문자열(`lang/<code>/vault.json`). |

## Vault.Keys

열쇠는 번호나 id로 지정합니다: 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. 각 열쇠는 같은 번호의 상자를 엽니다.

| 함수 | 설명 |
| --- | --- |
| `Keys.count(key)  -> number` | 플레이어 1이 가진 그 열쇠의 개수. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | 개수를 설정합니다; `n`(기본값 1)을 더합니다. |
| `Keys.name(key)  -> string` | 게임 언어로 된 열쇠의 이름. |
| `Keys.vaultOpen()  -> bool` | 금고가 열려 있는지 여부: 스킨 고유의 금고 입구가 설정하는 트리거 `.vault_opened`. 닫혀 있는 동안 다른 모듈은 열쇠 대신 코인을 줍니다. |
| `Keys.index(key)  -> number` | 열쇠의 번호. |

## Vault.Chests

상자는 1이 나무, 2가 황금, 3이 플래티넘입니다. 상자를 열면 그 열쇠 하나를 쓰고 가중치 풀에서 보상 하나를 추첨합니다: 꽝(열쇠가 부러짐), 코인, 다음 상자의 열쇠, 플레이어에게 없는 네임플레이트(상자별로 id 361~368, 369~375, 376~380), 마이 룸 가구(이미 가진 가구는 확률의 5분의 1을 유지), 또는 사운드트랙의 `Secret Vault` 폴더에서 아직 얻지 않은 곡이며, 곡은 가장 높은 채보 레벨에 따라 상자로 나뉩니다.

| 함수 | 설명 |
| --- | --- |
| `Chests.enter()  -> bool` | 방문마다 한 번, 다른 무엇보다 먼저 호출합니다: 첫 방문의 무료 simple 열쇠를 주고(그때 true를 반환), 곡 풀을 만듭니다. |
| `Chests.roll(i)  -> reward` | 열쇠 하나로 상자 `i`를 열고(먼저 `keyCount`를 확인하십시오), 보상을 즉시 저장한 뒤 반환합니다. |
| `Chests.showReward(reward)  -> bool` | `modal` ROActivity로 보상 팝업을 시작합니다. 꽝과 열쇠에는 팝업이 없으므로 false. |
| `Chests.keyCount(i)  -> number` | 상자 `i`용으로 가진 열쇠 수. |
| `Chests.name(i)  -> string` | 게임 언어로 된 상자의 이름. |
| `Chests.stock(i)  -> left, total` | 상자에서 아직 얻을 수 있는 것(곡, 네임플레이트, 가구)의 남은 수와 전체 수. |
| `Chests.chestPool(i)  -> table` | 이 세이브 기준으로 현재 상자의 가중치 풀: `{ w, type, ... }` 항목. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | 상자의 곡; 곡의 가장 높은 채보 레벨. |

보상은 `{ type, openedChest, ... }`이며, `type`은 `"snap"`, `"coins"`(`amount`), `"key"`(`target`, 상자 번호), `"nameplate"`(`id`, `info`), `"furniture"`(`id`, `rarity`), `"song"`(`node`) 중 하나입니다. 팝업은 `modal` ROActivity와 같은 방식으로 동작합니다. `Pagoda.Rewards.update`와 `draw`처럼, `IsActive`인 동안 그 `Update`와 `Draw`를 호출하십시오. 가구 팝업은 [마이 룸](my-room.md)이 제공하는 공유 텍스처 `myroom_thumb_<id>`를 보여 줍니다.

## 세이브 키

모든 것은 플레이어 1의 세이브(`GetSaveFile(0)`)에 보관됩니다.

| 키 | 종류 | 내용 |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | 카운터 | 가진 열쇠. |
| `.vault_opened` | 트리거 | 금고가 열려 있음(스킨이 설정). |
| `.vault_first_key_given` | 트리거 | 첫 방문의 열쇠를 주었음. |
| `.vault_song_unlocked_<unique id>` | 트리거 | 그 `Secret Vault` 곡을 얻었음. |
| `.myroom_<id>`, `.myroom_held_<id>` | 카운터 | 얻은 가구: 마이 룸에 넘길 것, 그리고 소유한 것. |

네임플레이트와 코인은 세이브 자체의 메서드(`UnlockNameplate`, `EarnCoins`)를 거칩니다.

```lua
local Chests = require("Vault.Chests")

function activate()
    if Chests.enter() then showFreeKeyNotice = true end
end

local function open(i)
    if Chests.keyCount(i) < 1 then return end
    local reward = Chests.roll(i)
    if not Chests.showReward(reward) then playSnap(reward) end
end
```
