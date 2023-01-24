--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

inAnimeCounter = -20

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Base.png")
    for i = 0 , 18 do
        func:AddGraph("Down_1P/"..tostring(i)..".png")
        func:AddGraph("Down_2P/"..tostring(i)..".png")
    end
end

function update()
    inAnimeCounter = inAnimeCounter + (20 * deltaTime)
end

function draw()
    func:DrawGraph(0, 0, "Base.png")

    for i = 0 , 18 do
        pos = i - 9
        if 9 - math.abs(pos) <= inAnimeCounter then
            x = -32 + (71 * i)
            y = 536
    
            if pos <= battleState then
                func:DrawGraph(x, y, "Down_1P/"..tostring(i)..".png")
            else
                func:DrawGraph(x, y, "Down_2P/"..tostring(i)..".png")
            end
        end
    end
end
