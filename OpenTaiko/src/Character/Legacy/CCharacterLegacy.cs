using System.Collections.Frozen;
using FDK;

namespace OpenTaiko;

class CCharacterLegacy : CCharacter {
	private class CCharacterAnimation {
		public CTexture?[] arTextures;
		public int[] arMotion;
		public double dbBeatLength = 1f;
		public string strFallbackAnimationName = "";

		public CCharacterAnimation(CTexture?[] textures, int[]? motion = null, double beat = 1.0f, string fallbackAnimationName = "") {
			arTextures = textures;

			if (motion == null) {
				arMotion = new int[textures.Length];
				for (int i = 0; i < textures.Length; i++) {
					arMotion[i] = i;
				}
			} else {
				arMotion = motion;
			}

			dbBeatLength = beat;
			strFallbackAnimationName = fallbackAnimationName;
		}
	}

	private CCharacterLegacyConfig Config;

	private CTexture? txPreview;
	private CTexture? txRender;

	private Dictionary<string, CCharacterAnimation> _dicAnimations = new Dictionary<string, CCharacterAnimation>();
	private FrozenDictionary<string, CCharacterAnimation>? dicAnimations;

	private CCharacterAnimation?[] arIdleAnimation = new CCharacterAnimation[5];
	private CCharacterAnimation?[] arActionAnimation = new CCharacterAnimation[5];
	private CCharacterAnimation?[] currentAnimation = new CCharacterAnimation[5];

	private bool bLoopLoopAnimation;
	private double[] fAnimation = new double[5];
	private double[] dbInterval = new double[5] { 1, 1, 1, 1, 1 };

	private string[] strCurrentAnimation = new string[5] { "", "", "", "", "" };

	private void CreateAnimationArray(string id, string dirName, int[]? motion = null, double beat = 1.0f, string fallbackAnimationName = "") {
		string dir = $"{_path}{Path.DirectorySeparatorChar}{dirName}";
		CTexture?[] textures = new CTexture[0];
		if (Directory.Exists(dir)) {
			string[] images = Directory.GetFiles(dir, "*.png");

			textures = new CTexture[images.Length];
			for (int i = 0; i < textures.Length; i++) {
				textures[i] = OpenTaiko.tテクスチャの生成($"{dir}{Path.DirectorySeparatorChar}{i}.png");
			}
		}

		CCharacterAnimation animation = new CCharacterAnimation(textures, motion, beat, fallbackAnimationName);
		_dicAnimations.Add(id, animation);
	}

	public CCharacterLegacy(string path, int i) : base(path, i) {
		var _str = "";
		OpenTaiko.Skin.LoadSkinConfigFromFile(path + @$"{Path.DirectorySeparatorChar}CharaConfig.txt", ref _str);
		Config = new CCharacterLegacyConfig(_str);

		txPreview = OpenTaiko.tテクスチャの生成($"{path}{Path.DirectorySeparatorChar}Normal{Path.DirectorySeparatorChar}0.png");
		txRender = OpenTaiko.tテクスチャの生成($"{path}{Path.DirectorySeparatorChar}Render.png");
	}

	public override void LoadStoryTextures() {
		base.LoadStoryTextures();
	}

	public override void LoadGeneralTextures() {
		base.LoadGeneralTextures();

		CreateAnimationArray(ANIM_GAME_NORMAL, "Normal", Config.Game_Motion_Normal, Config.Game_Beat_Normal, "");
		CreateAnimationArray(ANIM_GAME_CLEAR, "Clear", Config.Game_Motion_Clear, Config.Game_Beat_Clear, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_GAME_MAX, "Clear_Max", Config.Game_Motion_Clear_Max, Config.Game_Beat_Clear_Max, ANIM_GAME_CLEAR);
		CreateAnimationArray(ANIM_GAME_GOGO, "GoGo", Config.Game_Motion_GoGo, Config.Game_Beat_GoGo, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_GAME_GOGO_MAX, "GoGo_Max", Config.Game_Motion_GoGo_Max, Config.Game_Beat_GoGo_Max, ANIM_GAME_GOGO);
		CreateAnimationArray(ANIM_GAME_MISS, "Miss", Config.Game_Motion_Miss, Config.Game_Beat_Miss, "");
		CreateAnimationArray(ANIM_GAME_MISS_DOWN, "MissDown", Config.Game_Motion_MissDown, Config.Game_Beat_MissDown, ANIM_GAME_MISS);
		CreateAnimationArray(ANIM_GAME_10COMBO, "10combo", Config.Game_Motion_10combo, Config.Game_Beat_10combo, "");
		CreateAnimationArray(ANIM_GAME_10COMBO_MAX, "10combo_Max", Config.Game_Motion_10combo_Max, Config.Game_Beat_10combo_Max, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_GAME_CLEARED, "Cleared", Config.Game_Motion_Cleared, Config.Game_Beat_Cleared, ANIM_GAME_CLEAR_IN);
		CreateAnimationArray(ANIM_GAME_FAILED, "Failed", Config.Game_Motion_Failed, Config.Game_Beat_Failed, ANIM_GAME_CLEAR_OUT);
		CreateAnimationArray(ANIM_GAME_CLEAR_OUT, "Clearout", Config.Game_Motion_Clearout, Config.Game_Beat_Clearout, "");
		CreateAnimationArray(ANIM_GAME_CLEAR_IN, "Clearin", Config.Game_Motion_Clearin, Config.Game_Beat_Clearin, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_GAME_MAX_OUT, "Soulout", Config.Game_Motion_Soulout, Config.Game_Beat_Soulout, ANIM_GAME_CLEAR_OUT);
		CreateAnimationArray(ANIM_GAME_MAX_IN, "Soulin", Config.Game_Motion_Soulin, Config.Game_Beat_Soulin, ANIM_GAME_CLEAR_IN);
		CreateAnimationArray(ANIM_GAME_MISS_IN, "MissIn", Config.Game_Motion_MissIn, Config.Game_Beat_MissIn, "");
		CreateAnimationArray(ANIM_GAME_MISS_DOWN_IN, "MissDownIn", Config.Game_Motion_MissDownIn, Config.Game_Beat_MissDownIn, "");
		CreateAnimationArray(ANIM_GAME_RETURN, "Return", Config.Game_Motion_Return, Config.Game_Beat_Return, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_GAME_GOGOSTART, "GoGoStart", Config.Game_Motion_GoGoStart, Config.Game_Beat_GoGoStart, "");
		CreateAnimationArray(ANIM_GAME_GOGOSTART_CLEAR, "GoGoStart_Clear", Config.Game_Motion_GoGoStart_Clear, Config.Game_Beat_GoGoStart_Clear, ANIM_GAME_GOGOSTART);
		CreateAnimationArray(ANIM_GAME_GOGOSTART_MAX, "GoGoStart_Max", Config.Game_Motion_GoGoStart_Max, Config.Game_Beat_GoGoStart_Max, ANIM_GAME_GOGOSTART_CLEAR);
		CreateAnimationArray(ANIM_GAME_BALLOON_BREAKING, "Balloon_Breaking", Config.Game_Motion_Balloon_Breaking, Config.Game_Beat_Balloon_Breaking, ANIM_GAME_GOGO);
		CreateAnimationArray(ANIM_GAME_BALLOON_BROKE, "Balloon_Broke", Config.Game_Motion_Balloon_Broke, Config.Game_Beat_Balloon_Broke, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_GAME_BALLOON_MISS, "Balloon_Miss", Config.Game_Motion_Balloon_Miss, Config.Game_Beat_Balloon_Miss, ANIM_GAME_MISS);
		CreateAnimationArray(ANIM_GAME_KUSUDAMA_BREAKING, "Kusudama_Breaking", Config.Game_Motion_Kusudama_Breaking, Config.Game_Beat_Kusudama_Breaking, ANIM_GAME_BALLOON_BREAKING);
		CreateAnimationArray(ANIM_GAME_KUSUDAMA_IDLE, "Kusudama_Idle", Config.Game_Motion_Kusudama_Idle, Config.Game_Beat_Kusudama_Idle, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_GAME_KUSUDAMA_BROKE, "Kusudama_Broke", Config.Game_Motion_Kusudama_Broke, Config.Game_Beat_Kusudama_Broke, ANIM_GAME_BALLOON_BROKE);
		CreateAnimationArray(ANIM_GAME_KUSUDAMA_MISS, "Kusudama_Miss", Config.Game_Motion_Kusudama_Miss, Config.Game_Beat_Kusudama_Miss, ANIM_GAME_BALLOON_MISS);

		CreateAnimationArray(ANIM_MENU_NORMAL, "Menu_Loop", Config.Game_Motion_Menu_Loop, Config.Game_Beat_Menu_Loop, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_MENU_WAIT, "Menu_Wait", Config.Game_Motion_Menu_Wait, Config.Game_Beat_Menu_Wait, ANIM_MENU_NORMAL);
		CreateAnimationArray(ANIM_MENU_START, "Menu_Start", Config.Game_Motion_Menu_Start, Config.Game_Beat_Menu_Start, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_MENU_SELECT, "Menu_Select", Config.Game_Motion_Menu_Select, Config.Game_Beat_Menu_Select, ANIM_GAME_10COMBO_MAX);

		CreateAnimationArray(ANIM_ENTRY_NORMAL, "Title_Normal", Config.Game_Motion_Title_Normal, Config.Game_Beat_Title_Normal, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_ENTRY_JUMP, "Title_Entry", Config.Game_Motion_Title_Entry, Config.Game_Beat_Title_Entry, ANIM_GAME_10COMBO);

		CreateAnimationArray(ANIM_RESULT_NORMAL, "Result_Normal", Config.Game_Motion_Result_Normal, Config.Game_Beat_Result_Normal, ANIM_GAME_NORMAL);
		CreateAnimationArray(ANIM_RESULT_CLEAR, "Result_Clear", Config.Game_Motion_Result_Clear, Config.Game_Beat_Result_Clear, ANIM_GAME_10COMBO);
		CreateAnimationArray(ANIM_RESULT_FAILED_IN, "Result_Failed_In", Config.Game_Motion_Result_Failed_In, Config.Game_Beat_Result_Failed_In, ANIM_GAME_MISS_DOWN_IN);
		CreateAnimationArray(ANIM_RESULT_FAILED, "Result_Failed", Config.Game_Motion_Result_Failed, Config.Game_Beat_Result_Failed, ANIM_GAME_MISS_DOWN);

		dicAnimations = _dicAnimations.ToFrozenDictionary();
	}

	public override void DisposeStoryTextures() {
		base.DisposeStoryTextures();
	}

	public override void DisposeGeneralTextures() {
		base.DisposeGeneralTextures();

		foreach (var array in _dicAnimations.Values) {
			foreach (var texture in array.arTextures) {
				texture?.Dispose();
			}
		}
		_dicAnimations.Clear();
		dicAnimations = null;
	}

	public override void Dispose() {
		OpenTaiko.tDisposeSafely(ref txPreview);
		OpenTaiko.tDisposeSafely(ref txRender);
		base.Dispose();
	}

	public override void Update(int player) {
		base.Update(player);

		currentAnimation[player] = arActionAnimation[player] ?? arIdleAnimation[player];

		if (currentAnimation[player] != null) {
			fAnimation[player] += 1 / dbInterval[player] / (currentAnimation[player]?.dbBeatLength ?? 1) * (float)OpenTaiko.FPS.DeltaTime;
		}

		if (arActionAnimation[player] != null) {
			if (fAnimation[player] >= 1) {
				arActionAnimation[player] = null;
				fAnimation[player] = 0;
			}
		} else if (bLoopLoopAnimation) {
			if (fAnimation[player] >= 1) {
				fAnimation[player] -= (int)fAnimation[player];
			}
		}else {
			if (fAnimation[player] >= 1) {
				fAnimation[player] = 1;
			}
		}


	}

	public override void Draw(int player, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.Draw(player, x, y, scaleX, scaleY, opacity, color, flipX);

		if (flipX) scaleX *= -1;

		CTexture? texture = txPreview;
		if (currentAnimation[player] != null && currentAnimation[player].arMotion.Length >= 1) {
			int index = (int)(fAnimation[player] * currentAnimation[player].arMotion.Length);
			texture = currentAnimation[player].arTextures[currentAnimation[player].arMotion[Math.Min(index, currentAnimation[player].arMotion.Length - 1)]];
		}

		if (texture == null) return;

		float baseScale = (float)OpenTaiko.Skin.Resolution[1] / Config.Resolution[1];
		texture.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(baseScale * scaleX, baseScale * scaleY, 1.0f);
		texture.Opacity = opacity;

		switch (strCurrentAnimation[player]) {
			case ANIM_MENU_WAIT:
			case ANIM_MENU_START:
			case ANIM_MENU_NORMAL:
			case ANIM_MENU_SELECT:
			case ANIM_ENTRY_NORMAL:
			case ANIM_ENTRY_JUMP: {
					x += (Config.Menu_Offset[0] * texture.vcScaleRatio.X);
					y += (Config.Menu_Offset[1] * texture.vcScaleRatio.Y) - (texture.szTextureSize.Height * texture.vcScaleRatio.Y / 2.0f);
					break;
				}
			case ANIM_RESULT_NORMAL:
			case ANIM_RESULT_CLEAR:
			case ANIM_RESULT_FAILED_IN:
			case ANIM_RESULT_FAILED: {
					x += (Config.Result_Offset[0] * texture.vcScaleRatio.X);
					y += (Config.Result_Offset[1] * texture.vcScaleRatio.Y) - (texture.szTextureSize.Height * texture.vcScaleRatio.Y / 2.0f);
					break;
				}
			case ANIM_GAME_BALLOON_BREAKING:
			case ANIM_GAME_BALLOON_BROKE:
			case ANIM_GAME_BALLOON_MISS: {
					x += (Config.Game_Balloon_Offset[0] * texture.vcScaleRatio.X);
					y += (Config.Game_Balloon_Offset[1] * texture.vcScaleRatio.Y);
					break;
				}
			case ANIM_GAME_KUSUDAMA_BREAKING:
			case ANIM_GAME_KUSUDAMA_BROKE:
			case ANIM_GAME_KUSUDAMA_MISS:
			case ANIM_GAME_KUSUDAMA_IDLE: {
					x += (Config.Game_Kusudama_Offset[0] * texture.vcScaleRatio.X);
					y += (Config.Game_Kusudama_Offset[1] * texture.vcScaleRatio.Y);
					break;
				}
			default: {
					x += (Config.Game_Offset[0] * texture.vcScaleRatio.X);
					y += (Config.Game_Offset[1] * texture.vcScaleRatio.Y) + (texture.szTextureSize.Height * texture.vcScaleRatio.Y / 2.0f) - (0.2555f * Config.Resolution[1]);
					break;
				}
		}

		texture.t2D拡大率考慮中央基準描画(x, y);
		/*
		if (!flipX) {
			texture.t2D拡大率考慮中央基準描画(x, y);
		} else {
			texture.t2D拡大率考慮中央基準描画Mirrored(x, y);
		}
		*/
	}

	public override void DrawPreview(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawPreview(x, y, scaleX, scaleY, opacity, color, flipX);

		if (flipX) scaleX *= -1;

		if (txPreview == null) return;

		float baseScale = (float)OpenTaiko.Skin.Resolution[1] / Config.Resolution[1];
		txPreview.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(baseScale * scaleX, baseScale * scaleY, 1.0f);
		txPreview.Opacity = opacity;

		txPreview.t2D拡大率考慮中央基準描画(x, y);
		/*
		if (!flipX) {
			txPreview.t2D拡大率考慮中央基準描画(x, y);
		} else {
			txPreview.t2D拡大率考慮中央基準描画Mirrored(x, y);
		}
		*/
	}

	public override void DrawHeyaRender(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		base.DrawHeyaRender(x, y, scaleX, scaleY, opacity, color, flipX);

		if (flipX) scaleX *= -1;

		if (txRender == null) return;

		float baseScale = (float)OpenTaiko.Skin.Resolution[1] / Config.Resolution[1];
		txRender.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(baseScale * scaleX, baseScale * scaleY, 1.0f);
		txRender.Opacity = opacity;

		x += Config.Heya_Render_Offset[0] * txRender.vcScaleRatio.X;
		y += Config.Heya_Render_Offset[1] * txRender.vcScaleRatio.Y;

		txRender.t2D描画(x, y);
		/*
		if (!flipX) {
			txRender.t2D描画(x, y);
		} else {
			txRender.t2D描画(x, y);
		}
		*/
	}


	public override void SetLoopAnimation(int player, string animationType, bool loop = true) {
		base.SetLoopAnimation(player, animationType, loop);

		fAnimation[player] = 0.0f;
		bLoopLoopAnimation = loop;
		CCharacterAnimation? animation = GetAnimation(animationType);
		if (animation != null) {
			arIdleAnimation[player] = animation;
		}

		strCurrentAnimation[player] = animationType;
	}

	public override void PlayAnimation(int player, string animationType) {
		base.PlayAnimation(player, animationType);

		fAnimation[player] = 0.0f;
		arActionAnimation[player] = GetAnimation(animationType);

		strCurrentAnimation[player] = animationType;
	}

	public override void PlayVoice(string voiceType) {
		base.PlayVoice(voiceType);
	}

	public override void SetAnimationDuration(int player, double ms) {
		base.SetAnimationDuration(player, ms);

		dbInterval[player] = ms / 1000.0;
	}

	public override void SetAnimationCyclesToBPM(int player, double bpm) {
		base.SetAnimationCyclesToBPM(player, bpm);

		dbInterval[player] = 60.0 / Math.Abs(bpm);
	}

	private CCharacterAnimation? GetAnimation(string animationType) {
		if (dicAnimations == null) return null;
		if (!dicAnimations.ContainsKey(animationType)) return null;
		for (int i = 0; i < 5; i++) {
			if (dicAnimations[animationType].arMotion.Length != 0) {
				return dicAnimations[animationType];
			} else if (dicAnimations[animationType].strFallbackAnimationName != "") {
				animationType = dicAnimations[animationType].strFallbackAnimationName;
			} else {
				return null;
			}
		}
		return null;
	}
}
