local loopWidth = 492
local loopHeight = 288

local bgScrollX = 0
local bgScrollX2 = 0
local bgClearFade = { 0, 0, 0, 0, 0 }

local animeCounter1 = 0
local animeCounter2 = 0

function drawUp3rd_c(x, y, player)

    move = (animeCounter2 - 0.5) * 800

    if animeCounter2 < 1 then
        func:DrawGraph(x - move, y + move, "Clear_Up_3rd.png")
    end
    func:SetRotation(45, "Clear_Up_3rd.png")
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("1P_Up_1st.png");
    func:AddGraph("2P_Up_1st.png");
    func:AddGraph("1P_Up_3rd.png");
    func:AddGraph("2P_Up_3rd.png");
    func:AddGraph("Clear_Up_1st.png");
    func:AddGraph("Clear_Up_2nd.png");
    func:AddGraph("Clear_Up_3rd.png");
end

function update()
    bgScrollX = bgScrollX - (59 * deltaTime);
    bgScrollX2 = bgScrollX2 - (100 * deltaTime);
    animeCounter1 = animeCounter1 + (0.5 * deltaTime);
    animeCounter2 = animeCounter2 + (1 * deltaTime);


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

    if bgScrollX2 < -loopWidth * 2 then
        bgScrollX2 = 0;
    end

    if animeCounter2 > 2 then
        animeCounter2 = 0;
    end
end


function draw()
    if playerCount == 1 and p1IsBlue == false then

        func:SetOpacity(bgClearFade[1], "Clear_Up_1st.png");
        func:SetOpacity(bgClearFade[1], "Clear_Up_2nd.png");
        func:SetOpacity(bgClearFade[1], "Clear_Up_3rd.png");

        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "1P_Up_1st.png");
        func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "1P_Up_3rd.png");
        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "Clear_Up_1st.png");
        func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "Clear_Up_2nd.png");
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 0);
        end

    elseif playerCount == 1 and p1IsBlue == true then

        func:SetOpacity(bgClearFade[1], "Clear_Up_1st.png");
        func:SetOpacity(bgClearFade[1], "Clear_Up_2nd.png");
        func:SetOpacity(bgClearFade[1], "Clear_Up_3rd.png");

        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "2P_Up_1st.png");
        func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "2P_Up_3rd.png");
        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "Clear_Up_1st.png");
        func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "Clear_Up_2nd.png");
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 1);
        end

    elseif playerCount == 2 then

        for player = 0, playerCount - 1 do
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_1st.png");
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_2nd.png");
            func:SetOpacity(bgClearFade[player + 1], "Clear_Up_3rd.png");

            if player == 0 then
                func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "1P_Up_1st.png");
                func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "1P_Up_3rd.png");
                func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, loopHeight, "Clear_Up_1st.png");
                func:DrawRectGraph(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "Clear_Up_2nd.png");
                for i = 0, 5 do
                    drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 0);
                end
            else
                func:DrawRectGraph(0, 804, -bgScrollX, 0, 1920, loopHeight, "2P_Up_1st.png");
                func:DrawRectGraph(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "2P_Up_3rd.png");
                func:DrawRectGraph(0, 804, -bgScrollX, 0, 1920, loopHeight, "Clear_Up_1st.png");
                func:DrawRectGraph(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "Clear_Up_2nd.png");
                for i = 0, 5 do
                    drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 804, 1);
                end
            end
        end

    elseif playerCount == 3 then

        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, 288, "Clear_Up_1st.png");
        func:DrawRectGraph(0, 804, -bgScrollX, 0, 1920, 288, "Clear_Up_1st.png");
        func:DrawRectGraph(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349, "Clear_Up_2nd.png");
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 804, 0);
        end

    elseif playerCount == 4 then

        func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, 1080, "Clear_Up_1st.png");

    end
end
