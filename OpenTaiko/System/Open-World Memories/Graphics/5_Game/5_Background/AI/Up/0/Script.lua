--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local inAnimeCounter = -20

local animeCounter = 0

local nowAnimeFrame = 0

local maxAnimeFrame = 5

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Base.png")
	func:AddGraph("Frame.png")
    func:AddGraph("Down_1P/Default.png")
    func:AddGraph("Down_2P/Default.png")
    for i = 0 , 18 do
        --func:AddGraph("Down_1P/"..tostring(i)..".png")
        --func:AddGraph("Down_2P/"..tostring(i)..".png")
		func:AddGraph("Up_1P/"..tostring(i)..".png")
        func:AddGraph("Up_2P/"..tostring(i)..".png")
    end
	
	for i = 0 , maxAnimeFrame do
        func:AddGraph("Animation/"..tostring(i)..".png")
    end
end

function update()
    inAnimeCounter = inAnimeCounter + (20 * deltaTime)
	
	animeCounter = animeCounter + (10 * deltaTime)

    nowAnimeFrame = math.floor(animeCounter+0.5)

    if nowAnimeFrame > maxAnimeFrame then
        animeCounter = 0;
        nowAnimeFrame = 0;
    end
end

function draw()
    func:DrawGraph(0, 0, "Base.png")

    for i = 0 , 18 do
        pos = i - 9
        if 9 - math.abs(pos) <= inAnimeCounter then
		
			offset1 = -(math.sin((pos / 9.0) * math.pi) * 6)

            offset2 = 0

            if pos > 0 then
                offset2 = (math.cos((pos / 9.0) * math.pi) * 5)
            elseif pos < 0 then
                offset2 = -(math.cos((pos / 9.0) * math.pi) * 5)
            end

            up_x = 756 + (91 * pos) + offset1 + offset2;
            up_y = 57

			
            down_x = -32 + (106.5 * i)
            down_y = 804

            if pos <= battleState then
                --func:DrawGraph(x, y, "Down_1P/"..tostring(i)..".png")
				func:DrawGraph(up_x, up_y, "Up_1P/"..tostring(i)..".png")
                func:DrawGraph(down_x, down_y, "Down_1P/Default.png")
            else
                --func:DrawGraph(x, y, "Down_2P/"..tostring(i)..".png")
				func:DrawGraph(up_x, up_y, "Up_2P/"..tostring(i)..".png")
                func:DrawGraph(down_x, down_y, "Down_2P/Default.png")
            end
        end
    end
	
	func:DrawGraph(0, 0, "Frame.png")
	func:DrawGraph(0, 0, "Animation/"..tostring(nowAnimeFrame)..".png")
end
