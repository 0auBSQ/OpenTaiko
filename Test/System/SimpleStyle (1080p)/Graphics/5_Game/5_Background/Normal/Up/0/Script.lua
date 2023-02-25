local loopWidth = 492
local loopHeight = 231

local bgScrollX = 0
local bgScrollX_3rd = 0
local bgScrollY = 0
local bgScrollY_3rd = 0
local bg3rdAnime = 0
local bgClearFade = { 0, 0 }

function drawUp1st(x, y, player)
    for i2 = -2, 1 do
        if player == 0 and p1IsBlue == false then
            func:DrawGraph(x, y + (loopHeight * i2) + bgScrollY, "1P_Up_1st.png");
            func:DrawGraph(x, y + (loopHeight * i2) + bgScrollY, "Clear_Up_1st.png");
        else
            func:DrawGraph(x, y + (loopHeight * i2) + bgScrollY, "2P_Up_1st.png");
            func:DrawGraph(x, y + (loopHeight * i2) + bgScrollY, "Clear_Up_1st.png");
        end
    end
end

function drawUp2nd(x, y, player)
    for i2 = -1, 2 do
        if player == 0 and p1IsBlue == false then
            func:DrawGraph(x, y + (loopHeight * i2) - bgScrollY, "1P_Up_2nd.png"); 
            func:DrawGraph(x, y + (loopHeight * i2) - bgScrollY, "Clear_Up_2nd.png");
        else
            func:DrawGraph(x, y + (loopHeight * i2) - bgScrollY, "2P_Up_2nd.png"); 
            func:DrawGraph(x, y + (loopHeight * i2) - bgScrollY, "Clear_Up_2nd.png");
        end
    end
end

function drawUp3rd(x, y, player)
    if player == 0 and p1IsBlue == false then
        func:DrawGraph(x - bgScrollX_3rd, y - bgScrollY_3rd, "1P_Up_3rd.png"); 
        func:DrawGraph(x - bgScrollX_3rd, y - bgScrollY_3rd, "Clear_Up_3rd.png");
    else
        func:DrawGraph(x - bgScrollX_3rd, y - bgScrollY_3rd, "2P_Up_3rd.png"); 
        func:DrawGraph(x - bgScrollX_3rd, y - bgScrollY_3rd, "Clear_Up_3rd.png");
    end
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    if playerCount <= 2 then
        func:AddGraph("1P_Up_1st.png");
        func:AddGraph("2P_Up_1st.png");
        func:AddGraph("1P_Up_2nd.png");
        func:AddGraph("2P_Up_2nd.png");
        func:AddGraph("1P_Up_3rd.png");
        func:AddGraph("2P_Up_3rd.png");
        
        func:AddGraph("Clear_Up_1st.png");
        func:AddGraph("Clear_Up_2nd.png");
        func:AddGraph("Clear_Up_3rd.png");
    else
        func:AddGraph("4PBG.png")
    end
end

function update()
    if playerCount <= 2 then
        bgScrollX = bgScrollX - (59 * deltaTime);
        bgScrollY = bgScrollY + (14 * deltaTime);
        bg3rdAnime = bg3rdAnime + (300 * deltaTime);
    
        for player = 0, playerCount - 1 do
            if isClear[player] then
                bgClearFade[player + 1] = bgClearFade[player + 1] + (2000 * deltaTime);
            else
                bgClearFade[player + 1] = bgClearFade[player + 1] - (2000 * deltaTime); 
            end
        
            if bgClearFade[player + 1] > 255 then
                bgClearFade[player + 1] = 255;
            end
            if bgClearFade[player + 1] < 0 then
                bgClearFade[player + 1] = 0;
            end
        end
    
        if bgScrollX < -loopWidth then
            bgScrollX = 0;
        end
        if bgScrollY > loopHeight then
            bgScrollY = 0;
        end
        if bg3rdAnime > 600 then
            bg3rdAnime = 0;
        end
    
        if bg3rdAnime < 270 then
            --speed
            bgScrollX_3rd = bg3rdAnime * 0.9258;
            bgScrollY_3rd = math.sin(bg3rdAnime * (math.pi / 270.0)) * 40.0;
        else
            bgScrollX_3rd = 250 + (bg3rdAnime - 270) * 0.24;
            if bg3rdAnime < 490 then
                bgScrollY_3rd = -math.sin((bg3rdAnime - 270) * (math.pi / 170.0)) * 15.0;
            else
                bgScrollY_3rd = -(math.sin(220 * (math.pi / 170.0)) * 15.0) + (((bg3rdAnime - 490) / 110) * (math.sin(220 * (math.pi / 170)) * 15.0));
            end
        end
    end
end


function draw()
    if playerCount <= 2 then
        for player = 0, playerCount - 1 do
    
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_1st.png");
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_2nd.png");
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_3rd.png");
        
            y = 0;
            if player == 1 then
                y = 804;
            end
            for i = 0, 5 do
                drawUp1st(bgScrollX + (loopWidth * i), y, player);
                drawUp2nd(bgScrollX + (loopWidth * i), y, player);
            end
            for i = 0, 5 do
                drawUp3rd(bgScrollX + (loopWidth * i), y, player);
            end
        end
    else
        func:DrawGraph(0, 0, "4PBG.png");
    end
end
