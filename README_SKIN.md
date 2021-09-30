# How to reconstitute the skin

You can reconstitute the skin through the following steps :

## First step (Base Nijiiro skin)

- Clone the original https://github.com/4444fugu4444/TJAPlayer3-Develop-ReWrite 

- Following the `TJAPlayer3-Develop-ReWrite/Test/System` path, copy the `0.JpnNijiiro` folder and paste it in the `TJAPlayer3-Develop-BSQ/Test/System` folder

- (In the pasted directory) Delete the 2 `Graphics/6_Result/Result_Donchan*` folders

## Second step (First booster)

- Download and extract TaikoPlusEx from the https://tjadataba.se/shimyu/ website

- Copy and paste the following files/folders (Resize to the annoted size if necessary) (Left starts at `TaikoPlusEx`, Right at `TJAPlayer3-Develop-BSQ/Test/System/0.JpnNijiiro`)

```
Texture/Result/Flower => Graphics/6_Result/Flower
Texture/Result/Work => Graphics/6_Result/Work
Texture/Result/CrownEffect.png => Graphics/6_Result/CrownEffect.png (Resize 339x448)
Texture/Result/Effect.png => Graphics/6_Result/Effect.png 
Texture/Result/BackEffect.png => Graphics/6_Result/Shine.png
Texture/Result/ScoreRankEffect.png => Graphics/6_Result/ScoreRankEffect.png (Resize 1603x776)
Texture/Result/Cloud.png => Graphics/6_Result/Cloud.png (Resize 13200x1080)
Texture/Result/Speech_Bubble.png => Graphics/6_Result/Speech_Bubble.png (Resize 1432x690)
Texture/Result/Donchan/Clear_in => Graphics/6_Result/Result_Donchan_Clear
Texture/Result/Donchan/Failed_in => Graphics/6_Result/Result_Donchan_Failed_In
Texture/Result/Donchan/Failed_loop => Graphics/6_Result/Result_Donchan_Failed
Texture/Result/Donchan/Normal_loop => Graphics/6_Result/Result_Donchan_Normal

Texture/PetitChara.png => Graphics/5_Game/18_PuchiChara/0.png

Sound/Result/CrownIn.ogg => Sounds/ResultScreen/CrownIn.ogg
Sound/Result/RankIn.ogg => Sounds/ResultScreen/RankIn.ogg
Sound/Result/Donchan/Donchan_Clear.ogg => Sounds/ResultScreen/Donchan_Clear.ogg
Sound/Result/Donchan/Donchan_Miss.ogg => Sounds/ResultScreen/Donchan_Miss.ogg
```

## Third step (Second booster)

- Clone https://github.com/TJAPlayer3-Develop/TJAPlayer3-Develop

- As before, copy and paste the following files/folders (Left starts at `TJAPlayer3-Develop/Test/System/SimpleStyle`)

```
Graphics/5_Game/3_Mob => Graphics/5_Game/3_Mob
Graphics/5_Game/9_End => Graphics/5_Game/9_End (Merge the two folders, don't delete the existing files)

Sounds/Combo_1P => Sounds/Combo_1P
Sounds/Combo_2P => Sounds/Combo_2P
Sounds/Failed.ogg => Sounds/Failed.ogg
Sounds/Clear.ogg => Sounds/Clear.ogg
Sounds/Full combo.ogg => Sounds/Full combo.ogg
Sounds/Full combo 2P.ogg => Sounds/Full combo 2P.ogg
Sounds/Donda Full combo.ogg => Sounds/Donda Full combo.ogg
Sounds/Donda Full combo 2P.ogg => Sounds/Donda Full combo 2P.ogg
Sounds/SongSelect Chara.ogg => Sounds/SongSelect Chara.ogg
Sounds/DiffSelect.ogg => Sounds/DiffSelect.ogg
Sounds/SongDecide.ogg => Sounds/SongDecide.ogg
```

## Last step (Additional assets)

- For >= 11 stars charts, within the `3_SongSelect\Difficulty_Select` folder copy `Difficulty_Star.png` and paste it to `Difficulty_Star_Red.png`, you can use a color filter app online to tint it in red.

- Within the `1_Title` folder, extend the following assets by copying until `_5` 

```
ModeSelect_Bar_
ModeSelect_Bar_Chara_
ModeSelect_Bar_Text_
```

(Placeholders until the new modes will be implemented)