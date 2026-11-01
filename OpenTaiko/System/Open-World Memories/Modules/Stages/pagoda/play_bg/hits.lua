-- Player 1's hits as the monster takes them: on a flying note's landing, or at once when none flies.

local H = {}
H.__index = H

local P = 0
local QUEUE_MAX = 64
local FLIES = { perfect = true, great = true, good = true, roll = true, balloon = true }

function H.new(state)
    return setmetatable({
        seen = state.hitCount[P], landed = state.landCount[P], queue = {},
        fly = not state.simplemode and state.playerCount <= 2,
    }, H)
end

-- fn(big, judge) for each hit that reached the monster since the last poll
function H:poll(state, fn)
    local count, ring, q = state.hitCount[P], state.hitRing, self.queue
    for n = math.max(self.seen, count - ring), count - 1 do
        local slot = n % ring
        local judge = state.hitJudge[P][slot]
        if FLIES[judge] then
            local big = state.hitBig[P][slot] == true
            if not self.fly then
                fn(big, judge)
            else
                if #q >= QUEUE_MAX then table.remove(q, 1) end
                q[#q + 1] = { big = big, judge = judge }
            end
        end
    end
    self.seen = count
    if not self.fly then return end
    local lands = state.landCount[P]
    for _ = self.landed + 1, lands do
        local h = table.remove(q, 1)
        if h ~= nil then fn(h.big, h.judge) else fn(false, "good") end
    end
    self.landed = lands
end

return H
