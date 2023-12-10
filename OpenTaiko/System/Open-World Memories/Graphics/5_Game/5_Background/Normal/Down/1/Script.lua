local loopWidth = 1920

local bgClearFade = 0

local bgScrollX = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Down.png");
    func:AddGraph("Down_Clear.png");
	func:AddGraph("Down_Scroll_1.png");
	func:AddGraph("Down_Scroll_2.png");
	func:AddGraph("Down_Scroll_3.png");
	func:AddGraph("Down_Scroll_4.png");
	func:AddGraph("Down_Scroll_5.png");
end

function update()
    if isClear[0] then
        bgClearFade = bgClearFade + (2000 * deltaTime);
    else
        bgClearFade = bgClearFade - (2000 * deltaTime);
    end

    bgScrollX = bgScrollX + (100 * deltaTime);

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

    if bgScrollX > loopWidth then
        bgScrollX = 0;
    end
end

function draw()
    func:SetOpacity(bgClearFade, "Down_Clear.png");
	func:SetOpacity(bgClearFade, "Down_Scroll_1.png");
	func:SetOpacity(bgClearFade, "Down_Scroll_2.png");
	func:SetOpacity(bgClearFade, "Down_Scroll_3.png");
	func:SetOpacity(bgClearFade, "Down_Scroll_4.png");
	func:SetOpacity(bgClearFade, "Down_Scroll_5.png");

    func:DrawGraph(0, 540, "Down.png");
    func:DrawGraph(0, 540, "Down_Clear.png");

    for i = 0, 2 do
		for j = 5, 1, -1 do
			func:DrawGraph(0 + (loopWidth * i) - (bgScrollX * (6 - j) % loopWidth), 540, "Down_Scroll_" .. j .. ".png");
		end
    end
end
