local time = 0

local bgClearFade = 0
local clearMultiplier = -1

local BG = {
    x = 0,
    y = 0,
    w = 1920,
    h = 474
}
local BGTrees = {
    x = 0,
    y = 475,
    w = 1920,
    h = 474
}
local BGFog = {
    x = 0,
    y = 1425,
    w = 1920,
    h = 474
}
local BGClear = {
    x = 0,
    y = 950,
    w = 1920,
    h = 474
}
local Candies = {
    x = 2360,
    y = 422,
    w = 391,
    h = 148
}
local Lollipop = {
    x = 2919,
    y = 0,
    w = 134,
    h = 244
}
local Slime = {
    x = 1921,
    y = 0,
    w = 656,
    h = 350
}
local GhostTop = {
    x = 2578,
    y = 0,
    w = 340,
    h = 421
}
local GhostBottom = {
    x = 1921,
    y = 351,
    w = 438,
    h = 182
}
local BannerTop = {
    x = 0,
    y = 1900,
    w = 1920,
    h = 53
}
local BannerBottom = {
    x = 0,
    y = 1954,
    w = 1920,
    h = 53
}
local CatBack = {
    x = 2752,
    y = 475,
    w = 487,
    h = 474
}
local CatEyes = {
    x = 2496,
    y = 571,
    w = 253,
    h = 283
}
local CatFront = {
    x = 3240,
    y = 475,
    w = 440,
    h = 474
}
local CatAlt = {
    x = 3193,
    y = 0,
    w = 487,
    h = 474
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

-- function for lazy people (me)
function DrawRepeat(asset, x, y, x_offset)
    func:DrawRectGraph(x, y + 540, asset.x + x_offset, asset.y, asset.w, asset.h, "Assets.png");
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
    Scale(1, 1);
    Rotate(0);

    if bgClearFade < 255 then
        Draw(BG, 0, 0);
        DrawRepeat(BGFog, 0, 0, time * 80)
        Draw(BGTrees, 0, 0);
    end
    if bgClearFade > 0 then
        local fadeToOne = bgClearFade / 255;
        local fadeToZero = 1 - fadeToOne;
        local fadeSinHalfToZero = math.sin(fadeToZero * (math.pi / 2))

        local bounce = time % 2.5
        local candybounce = 0
        if bounce >= 0 and bounce <= 0.5 then
            candybounce = 15 * math.sin((bounce) * math.pi * 2)
        end
        if bounce >= 0.5 and bounce <= 1 then
            candybounce = 10 * math.sin((bounce - 0.5) * math.pi * 2)
        end

        Opacity(bgClearFade);
        DrawRepeat(BGClear, 0, 0, time * 50)

        Scale(1, 1 + (0.1 * math.sin(time)))
        Draw(Slime, -5, 0)
        SimpleScale(1)
        Draw(Candies, 23, 335 - candybounce)
        Draw(Lollipop, 21, 248 - (candybounce * 1.25))

        Draw(GhostTop, 104 - (50 * fadeSinHalfToZero), 45 + (20 * math.sin(time * 2)))
        Draw(GhostBottom, 387 - (25 * fadeSinHalfToZero), 292 + 10 + (10 * math.sin(time * 2)))

        DrawRepeat(BannerTop, 0, -1, time * 100)
        DrawRepeat(BannerBottom, 0, 421, time * -100)

        if (bgClearFade == 255) then
            Draw(CatBack, 1433, 0)
            Draw(CatEyes, 1582 + ((time % 4 >= 2) and 50 or 0), 101)
            Draw(CatFront, 1480, 0)
        else
            Draw(CatAlt, 1433 + (50 * fadeSinHalfToZero), 0)
        end

    end
end
