using FDK;
using Silk.NET.Maths;

namespace OpenTaiko;

class CCharacterLua : CCharacter {
	public CLuaCharacterScript Script { get; private set; }

	public CCharacterLua(string path, int i) : base(path, i) {
		Script = new CLuaCharacterScript(path, null, null, true);
		}

	public CCharacterLua(Info info) : base(info) {
		Script = new CLuaCharacterScript(info._path, null, null, true);
	}

	public override void Dispose() {
		base.Dispose();
		Script.Dispose();
	}

	public override void LoadAnimation(string animationType) {
		if (animationType == ANIM_NONE) return;
		Dictionary<string, int> animationCounts = animationLoadCounts;
		if (!animationCounts.ContainsKey(animationType)) animationCounts.Add(animationType, 0);

		animationCounts[animationType]++;

		if (animationCounts[animationType] == 1) {
			ImplLoadAnimation(animationType);
			bool available = AvailableAnimation(animationType, false);
			if (!available) {
				animationCounts[animationType]--;

				string alternative = GetAlternativeAnimation(animationType);
				LoadAnimation(alternative);
			}
		}

	}

	public override void DisposeAnimation(string animationType) {
		if (animationType == ANIM_NONE) return;

		bool available = AvailableAnimation(animationType, false);
		if (!available) {
			string alternative = GetAlternativeAnimation(animationType);
			DisposeAnimation(alternative);
			return;
		}

		Dictionary<string, int> animationCounts = animationLoadCounts;
		if (!animationCounts.ContainsKey(animationType)) animationCounts.Add(animationType, 1);

		animationCounts[animationType]--;

		if (animationCounts[animationType] == 0) {
			ImplDisposeAnimation(animationType);
			animationCounts.Remove(animationType);
		}
	}

	public override bool AvailableAnimation(string animationType, bool useAlternative = true) {
		if (animationType == ANIM_NONE) return false;
		for (int i = 0; i < 5; i++) {
			bool available = Script.AvailableAnimation(animationType);
			if (available) return true;
			if (!useAlternative) return false;

			animationType = GetAlternativeAnimation(animationType);
		}
		return false;
	}

	public override void SetAnimationDuration(string animationType, double duration) {
		Script.SetAnimationDuration(this.GetAnimation(animationType), duration);
	}

	public override void ResetAnimationCounter(string animationType) {
		Script.ResetAnimationCounter(this.GetAnimation(animationType));
	}

	//voice-------------
	public override void LoadVoice(string voice) {
		Dictionary<string, int> voiceCounts = voiceLoadCounts;
		if (!voiceCounts.ContainsKey(voice)) voiceCounts.Add(voice, 0);

		if (voiceCounts[voice] == 0) {
			ImplLoadVoice(voice);
		}

		voiceCounts[voice]++;
	}

	public override void DisposeVoice(string voice) {
		Dictionary<string, int> voiceCounts = voiceLoadCounts;
		if (!voiceCounts.ContainsKey(voice)) voiceCounts.Add(voice, 0);

		if (voiceCounts[voice] <= 0) return;
		voiceCounts[voice]--;

		if (voiceCounts[voice] == 0) {
			ImplDisposeVoice(voice);
			voiceCounts.Remove(voice);
		}
	}

	public override bool Update(string animationType, bool looping = true) {
		return Script.Update(OpenTaiko.FPS.DeltaTime, this.GetAnimation(animationType), looping);
	}

	public override void Draw(string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
		string resolvedAnimation = this.GetAnimation(animationType);
		string? contextType = (resolvedAnimation != animationType) ? animationType : null;
		Script.Draw(resolvedAnimation, x, y, scaleX, scaleY, opacity, new LuaColor(color ?? Color4.White), contextType, rotation, blendMode, wrapMode, gradientMap);
	}

	public override void DrawAtAnchor(string animationType, float x, float y, string anchor, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, float? clipW = null, float? clipH = null, float clipX = 0f, float clipY = 0f, float rotation = 0f, string? blendMode = null, string? wrapMode = null, LuaGradientMap? gradientMap = null) {
		string resolvedAnimation = this.GetAnimation(animationType);
		string? contextType = (resolvedAnimation != animationType) ? animationType : null;
		Script.DrawAtAnchor(resolvedAnimation, x, y, anchor, scaleX, scaleY, opacity, new LuaColor(color ?? Color4.White), contextType, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap);
	}

	public override LuaVector2 GetDrawSize(string animationType) {
		string resolvedAnimation = this.GetAnimation(animationType);
		return Script.GetDrawSize(resolvedAnimation);
	}

	public override (float x, float y) GetHeyaRenderOffset() {
		return Script.GetHeyaRenderOffset();
	}

	public override (float x, float y)? GetAIBattlePosition(int player, float charaScale = 1.0f) {
		return Script.GetAIBattlePosition(player, charaScale);
	}


	protected override void ImplLoadAnimation(string animationType) {
		Script.LoadAnimation(animationType);
	}

	protected override void ImplDisposeAnimation(string animationType) {
		Script.DisposeAnimation(animationType);
	}

	public override void PlayVoice(string voice) {
		Script.PlayVoice(voice);
	}

	protected override void ImplLoadVoice(string voice) {
		Script.LoadVoice(voice);
	}

	protected override void ImplDisposeVoice(string voice) {
		Script.DisposeVoice(voice);
	}

	//------------------
}
