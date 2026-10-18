<!-- modules/shop.md -->

# Shop

코인 숍의 로직입니다: 카탈로그, 그날의 재고, 상품 교체, 가격, 구매, 그리고 구매로 얻는 것. `Shop` 모듈은 아무것도 그리지 않습니다. 스킨은 그 위에 자신만의 숍을 그리고 자신만의 트랜지션을 고릅니다. 모든 스킨이 같은 규칙으로 같은 아이템을 팔며, 구매 내역은 세이브 파일에 기록됩니다. Open-World Memories의 `coin_shop` 스테이지(잡지와 그 주인)가 그런 클라이언트 중 하나입니다.

## 모듈 목록

| 모듈 | 내용 |
| --- | --- |
| `Shop.Daily` | 한 세이브의 그날 숍: 추첨, 재고, 품절된 칸, 상품 교체, 구매로, 방문에서 다음 방문까지 유지됩니다. |
| `Shop.Catalog` | 아이템: 모듈 옆에 있는 `Items.db3`의 `itempool` 테이블, 그리고 각 종류의 구매로 얻는 것. |
| `Shop.Seasonal` | 정해진 날짜에 큰 칸에 고정되는 추천 아이템. |
| `Shop.Popup` | `modal` ROActivity를 통한 선택 사항인 구매 팝업으로, 이를 원하는 스킨용입니다. |
| `Shop.Texts` | 모듈 자체의 문자열(`lang/<code>/shop.json`), 아이템 이름 포함. |

## Shop.Daily

숍은 JST 기준으로 매일 `major` 행에서 큰(추천) 칸 하나를, `regular` 행에서 일반 칸 네 개를 `PoolSize`로 가중치를 두어 추첨합니다. 큰 칸의 추첨에서 `empty` 행이 나오면 그날은 대신 일반 칸이 여섯 개가 됩니다. 시즌 항목이 큰 칸을 차지할 수도 있습니다. 상품 교체는 10코인이며 그날 교체할 때마다 두 배가 되고, 모든 칸의 재고를 다시 채웁니다.

| 함수 | 설명 |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | 세이브 슬롯 `index`의 오늘 숍: 오늘 이미 추첨했으면 불러오고, 아니면 추첨해서 저장합니다. `opts.store`는 로컬 저장소의 이름(`"shop_state"`), `opts.seasonal`은 추천 항목(`Shop.Seasonal`)을 지정합니다. |
| `shop.big`, `shop.normals[1..6]` | 아이템: 카탈로그 행에 `LocalizedName`, `Stock`, `SoldOut`을 더한 것. 빈 칸이면 nil. |
| `shop:item(slot)  -> item` | 칸 1~6, 또는 `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | 그 칸을 지금 살 수 없는 이유: `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | `qty`개(재고로 제한)를 삽니다: 지급하고, 가격을 지불하고, 재고를 줄이고, 저장합니다. |
| `shop:reroll()  -> true, or false and "poor"` | 교체 가격을 지불하고 새 재고를 추첨합니다. |
| `shop:rerollPrice()  -> number` | 다음 교체의 가격. |
| `shop:close()` | 저장소를 해제합니다. 화면을 닫을 때 호출하십시오. |

상태는 세이브 파일별로(그 `SaveUID`) 호출한 스테이지의 [DATABASE 로컬 저장소](../api/data.md)에 보관됩니다: 날짜, 교체 횟수, 품절 마스크, 각 칸의 아이템 코드와 재고. 따라서 두 스킨의 숍 스테이지는 서로 다른 일일 재고를 가지지만, 산 것은 공유됩니다.

## Shop.Catalog

`itempool` 행:

| 열 | 설명 |
| --- | --- |
| `Code` | 고유 코드: 마이 룸 가구는 `furn_<id>`, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | 구매로 얻는 것: `nameplate`(`RefInt`가 네임플레이트 id), `triggerable`(트리거 `RefText`를 설정), `counterable`(카운터 `RefText`를 수량만큼 올림), `empty`(그날 큰 칸이 비어 있음). |
| `Price`, `Name` | 코인 가격, 그리고 모듈에 그 코드의 문자열이 없을 때 쓰는 `LANG` JSON 이름. |
| `Slot`, `PoolSize` | `regular` 또는 `major`; 풀 안에서 그 행의 가중치. |
| `Condition` | 그 행이 제공되려면 설정되어 있어야 하는 트리거. |
| `StockMin`, `StockMax` | `StockMax`가 0보다 크면 한 개씩 사는 범위형 재고(페인트, 바닥재). |
| `OneTime` | 1: 단 한 번만 살 수 있고, 그 뒤로는 다시 제공되지 않습니다. |

| 함수 | 설명 |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | 행들로, 한 번만 읽습니다. |
| `Catalog.name(item)  -> string` | 아이템의 이름: 그 코드에 대한 모듈의 문자열, 없으면 행의 `Name`. |
| `Catalog.grant(save, item, qty)` | 구매를 종류에 따라 지급합니다(`Catalog.GRANTS`, 스킨이 자신만의 종류로 확장할 수 있음). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | 세이브가 고유 아이템을 소유했는지 여부; 그 세이브에 대해 그 행이 추첨될 수 있는지 여부. |
| `Catalog.furnitureId(code)  -> string` | `furn_<id>` 코드의 마이 룸 가구 id, 아니면 nil. |

## Shop.Seasonal과 Shop.Popup

`Shop.Seasonal`은 [almanac](calendar.md) 항목 `{ code, when, priority, sticky }`의 목록이며, `code`는 아이템 코드입니다. 항목이 일치하고 세이브가 그 아이템을 소유하지 않은 동안, 그 아이템이 큰 칸을 차지합니다. 스킨은 자신만의 목록을 `Daily.open`에 넘길 수 있습니다.

`Shop.Popup.show(item, qty, picture?)`는 `modal` ROActivity로 구매 팝업을 재생합니다(`picture`는 공유 텍스처 이름이며, 가구의 기본값은 `myroom_thumb_<id>`). 재생하는 동안 `Popup.update()`가 true를 반환하며, 그리기는 `Popup.draw()`, 정지는 `Popup.stop()`으로 합니다.

## 세이브 키

구매는 구매자의 세이브에 기록됩니다.

| 키 | 종류 | 내용 |
| --- | --- | --- |
| `.myroom_<id>` | 카운터 | 산 가구(`counterable` 행)로, 다음 방문 때 마이 룸에 넘겨집니다. |
| `.vault_key_count_<key>` | 카운터 | 산 금고 열쇠. |
| `<RefText>` | 트리거 | `triggerable` 행을 샀음. |
| `<RefText>_owned` | 트리거 | 1회 한정 행을 샀음: 다시는 제공되지 않습니다. |
| `.almanac_<code>` | 트리거 | 이 세이브에서 sticky 시즌 항목이 일치했음. |

네임플레이트와 코인은 세이브 자체의 메서드(`UnlockNameplate`, `SpendCoins`)를 거칩니다.

```lua
local Daily = require("Shop.Daily")
local shop

function activate()
    shop = Daily.open(slot)
end

local function buy(i)
    local ok, why = shop:buy(i, 1)
    if not ok then showRefusal(why) end
end

function deactivate()
    shop:close()
end
```
