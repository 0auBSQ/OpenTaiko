local time = 0

local bgClearFade = 0
local clearMultiplier = -1

local NormalBG = {
    x = 0,
    y = 0,
    w = 1920,
    h = 474
}
local ClearBG = {
    x = 0,
    y = 475,
    w = 1920,
    h = 474
}
local Planet = {
    x = 0,
    y = 950,
    w = 745,
    h = 474
}
local StarsL = {
    x = 746,
    y = 950,
    w = 407,
    h = 428
}
local StarsR = {
    x = 1154,
    y = 950,
    w = 461,
    h = 407
}
local Spaceship = {
    x = 1616,
    y = 950,
    w = 334,
    h = 424
}
local UFO = {
    x = 1921,
    y = 0,
    w = 259,
    h = 222
}
local Star = {
    x = 1922,
    y = 223,
    w = 32,
    h = 32
}
local Comet = {
    x = 1955,
    y = 223,
    w = 48,
    h = 48
}

function Draw(asset, x, y)
    func:DrawRectGraph(x, y + 540, asset.x, asset.y, asset.w, asset.h, "Assets.png")
end
function DrawCenter(asset, x, y)
    func:DrawGraphRectCenter(x, y + 540, asset.x, asset.y, asset.w, asset.h, "Assets.png")
end
function Rotate(angle)
    func:SetRotation(angle, "Assets.png")
end
function Scale(scale_x, scale_y)
    func:SetScale(scale_x, scale_y, "Assets.png")
end
function SimpleScale(scale)
    Scale(scale, scale)
end
function Opacity(opacity)
    func:SetOpacity(opacity, "Assets.png")
end
function Reset()
    Opacity(255)
    Rotate(0)
    SimpleScale(1)
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Assets.png");
end

function update()
    time = time + deltaTime

    clearMultiplier = (isClear[0]) and 1 or -1;

    bgClearFade = bgClearFade + (clearMultiplier * 1500 * deltaTime);

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end
end

function draw()
    Opacity(255);
    Scale(1, 1)
    Rotate(0)

    if bgClearFade < 255 then
        Draw(NormalBG, 0, 0);

        local startime = time % 16
        Rotate(500 * time)

        local function star(x, y, start)
            local length = startime - start
            if length > 1 or length < 0 then return end

            Opacity(128 * (math.sin(length * math.pi)))
            SimpleScale(0.5 + (0.75 * math.sin(length * (math.pi/2))))
            DrawCenter(Star, x, y)
        end

        star(1113, 160, 0)
        star(347, 389, 2)
        star(1401, 345, 4)
        star(687, 288, 6)
        star(1113, 160, 8)
        star(347, 389, 10)
        star(1401, 345, 12)
        star(687, 288, 14)

        star(1124, 76, 10.3)
        star(1024, 176, 10.6)
        star(924, 276, 11)
        star(824, 376, 11.3)
        
        Reset()

        local comet_length = startime - 10
        if comet_length > 0 and comet_length < 2 then
            comet_length = comet_length / 2
            local comet_x = 1200 - (600 * comet_length)
            local comet_y = -48 + (600 * comet_length)
            Draw(Comet, comet_x, comet_y)
        end
    end
    if bgClearFade > 0 then
        local fadeToOne = bgClearFade / 255;
        local fadeToZero = 1 - fadeToOne;
        local fadeSin = math.sin(fadeToOne * math.pi)
        local fadeSinHalf = math.sin(fadeToOne * (math.pi / 2))
        local fadeSinHalfToZero = math.sin(fadeToZero * (math.pi / 2))
        Opacity(bgClearFade);

        Scale(1, 1.05)
        --Draw(ClearBG, 0, (5 * math.sin(time)) - 5);
        Draw(ClearBG, 0, -5);
        SimpleScale(1)

        SimpleScale((1.2 - (0.2 * fadeToOne)) + (0.04 * (1 + math.sin(time * 2))))
        DrawCenter(Planet, 930.5, 237);
        SimpleScale(1)

        Draw(StarsL, 16 - (100 * fadeToZero), 15 + (10 * math.sin(time)));
        Draw(StarsR, 1428 + (100 * fadeToZero), 15 + (10 * math.sin(time)));
        Draw(Spaceship, 1480 + (75 * fadeSinHalfToZero), 50 + (20 * fadeSin) + (25 * math.sin(time)));
        Draw(UFO, 28 - (75 * fadeSinHalfToZero), 13 - (20 * fadeSin) + (25 * math.sin(time)));
    end
end
