-- lang/ko.lua: the Korean dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "노콘"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "오늘의 인트로 노콘에 온 것을 환영한다!\n이 몸이 바로 사회자 노콘, 오늘은 내구 세션이다!"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "오늘의 도전자, {Player 1 name} 님이 음악 지식을 뽐낼 것이다!\n힘찬 응원과 행운을 보내 주자!"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "자, 퀴즈 시간이다!\n쇼~~~~타임!"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "오늘의 인트로 노콘에 온 것을 환영한다!\n이 몸이 바로 사회자 노콘, 오늘은 대결 세션이다!"
T["Let me present today's challengers..."] = "오늘의 도전자들을 이 몸이 소개하겠다..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "누가 이길 것인가? 곧 알게 될 것이다...\n힘찬 응원과 행운을 보내 주자!"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "정답, \"%s\"였다! 훌륭하기 그지없다!"
T["\"%s\" it was! Greeeeeeat!"] = "\"%s\"였다! 대다~~~~단하다!"
T["Yes, \"%s\"! Good answer!"] = "그렇다, \"%s\"! 좋은 답이다!"
T["It was \"%s\"! Very for real!"] = "\"%s\"였다! 진짜 리얼이다!"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "역시 \"%s\"! 캬~~~~!"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "똑똑해진 기분인가? 그 점수는 어떻게 저장할 셈이지, 명탐정?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "게임은 즐거운가? 좋다, 넌 영원히 여기 있을 테니까!"
T["You did beat my score? Wait until you reach the results first..."] = "이 몸의 점수를 넘었다고? 우선 결과 화면까지 가 보고 말해라..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "아쉽다! 지는 말에 걸 수는 없지!"
T["Come on, be for real!"] = "이봐, 진지하게 해라!"
T["So you prefer dogs? Sorry, we do not do that here!"] = "그러니까 개파라는 건가? 미안하지만 여기선 안 받는다!"
T["And that's a wrap! Time to end the show!"] = "자, 여기까지! 쇼는 끝이다!"
T["No one found it?... Really?..."] = "아무도 못 맞혔나?... 정말로?..."
T["And a skewer of loosers, chief!"] = "패배자 꼬치 한 접시 나왔습니다, 사장님!"
T["I have no words..."] = "이 몸, 할 말을 잃었다..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "%s 님, 또 한 번 승리다! 곡은 \"%s\"였다!"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s 님이 \"%s\" 한 곡으로 싹쓸이다! 무시무시하군!"
T["I always knew %s was the best! It was \"%s\"!"] = "이 몸은 처음부터 %s 님이 최고라는 걸 알고 있었다! 곡은 \"%s\"!"

-- UI and screen texts
T["Loading songs..."] = "곡을 불러오는 중..."
T["Round %d / %d"] = "라운드 %d / %d"
T["Game Over!"] = "게임 오버!"
T["Final Results!"] = "최종 결과 발표!"
T["Score: %d"] = "점수: %d"
T["High Scores"] = "최고 점수"
T["Decide: Play again   Cancel: Exit"] = "결정: 다시 하기   취소: 종료"
T["Pick the correct song!"] = "정답 곡을 골라라!"
T["Choose the song genre!"] = "곡 장르를 골라라!"
T["%s wins %d point!"] = "%s 님, %d점 획득!"
T["%s wins %d points!"] = "%s 님, %d점 획득!"
T["It's a wrap!"] = "여기까지!"
T["Let's set up the game show!"] = "게임 쇼를 준비하자!"
T["Mode"] = "모드"
T["Players"] = "플레이어 수"
T["Songs"] = "곡"
T["Round Count"] = "라운드 수"
T["The game can only be started if there are at least 5 available songs."] = "플레이할 수 있는 곡이 5곡 이상 있어야 게임을 시작할 수 있다."

return T
