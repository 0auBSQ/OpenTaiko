using System.Drawing;
using FDK;

namespace OpenTaiko;

public enum ETitleType { Title, Subtitle, Charter }

// Stub retained from the removed CStageSongSelect for types still used by active code.
// The full rendering class was removed; only the shared data types and utilities remain.
internal class CActSelect曲リスト {
	// Used by CSongDict for storing per-player score stats.
	public class CScorePad {
		public int[] ScoreRankCount = new int[7];
		public int[] CrownCount = new int[4];
	}

	// Recursive flat-list navigator used by CSong管理 and Refresh().
	public static (CSongListNode? node, int index) GetFromFlattenList(List<CSongListNode> list, bool useOpenFlag = false, int index = -1, bool wrap = true, CSongListNode? node = null) {
		int nowIndex = 0;
		for (int i = 0; i < list.Count; i++) {
			var e = list[i];
			if (!useOpenFlag || !e.bIsOpenFolder) {
				if (nowIndex == index || (node != null && e == node))
					return (e, nowIndex);
				++nowIndex;
			}
			if (e.nodeType == CSongListNode.ENodeType.BOX && (!useOpenFlag || e.bIsOpenFolder)) {
				var (n, idx) = GetFromFlattenList(e.childrenList, useOpenFlag, index - nowIndex, wrap: false, node: node);
				if (n != null)
					return (n, nowIndex + idx);
				nowIndex += idx;
			}
		}
		if (nowIndex == 0) return (null, 0);
		if (wrap) {
			index %= nowIndex;
			if (index < 0) index += nowIndex;
			return GetFromFlattenList(list, useOpenFlag, index, wrap: false, node: node);
		}
		return (null, nowIndex);
	}
}

// Stub retained from the removed CStage段位選択 for methods still called by gameplay/result stages.
internal class CActSelect段位リスト {
	private static CCachedFontRenderer? pfDanIconTitle = null;

	public static void RefleshSkin() {
		OpenTaiko.tDisposeSafely(ref pfDanIconTitle);
	}

	public static void tDisplayDanIcon(int count, float x, float y, int opacity, float scale, bool showFade = false) {
		pfDanIconTitle ??= HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_DanIconTitle_Size);

		string ex = count switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
		TitleTextureKey ttkTmp = new TitleTextureKey(count.ToString() + ex, pfDanIconTitle, Color.White, Color.Black, 1000);

		if (showFade && OpenTaiko.Tx.Dani_DanIcon_Fade != null) {
			OpenTaiko.Tx.Dani_DanIcon_Fade.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(scale, scale, 1.0f);
			OpenTaiko.Tx.Dani_DanIcon_Fade.Opacity = opacity;
			OpenTaiko.Tx.Dani_DanIcon_Fade.color4 = CConversion.ColorToColor4(OpenTaiko.Skin.DaniSelect_DanIcon_Color[Math.Min(count - 1, OpenTaiko.Skin.DaniSelect_DanIcon_Color.Length - 1)]);
			OpenTaiko.Tx.Dani_DanIcon_Fade.t2D拡大率考慮描画(CTexture.RefPnt.Left, x - ((OpenTaiko.Tx.Dani_DanIcon?.szTextureSize.Width / 2 ?? 0) * scale), y);
			OpenTaiko.Tx.Dani_DanIcon_Fade.Opacity = 255;
		}

		if (OpenTaiko.Tx.Dani_DanIcon == null) return;
		OpenTaiko.Tx.Dani_DanIcon.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(scale, scale, 1.0f);
		OpenTaiko.Tx.Dani_DanIcon.Opacity = opacity;
		OpenTaiko.Tx.Dani_DanIcon.color4 = CConversion.ColorToColor4(OpenTaiko.Skin.DaniSelect_DanIcon_Color[Math.Min(count - 1, OpenTaiko.Skin.DaniSelect_DanIcon_Color.Length - 1)]);
		OpenTaiko.Tx.Dani_DanIcon.t2D拡大率考慮描画(CTexture.RefPnt.Left, x, y);
		OpenTaiko.Tx.Dani_DanIcon.Opacity = 255;

		var ttx = TitleTextureKey.ResolveTitleTexture(ttkTmp);
		if (ttx != null) {
			ttx.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(scale, scale, 1.0f);
			ttx.Opacity = opacity;
			ttx.t2D拡大率考慮描画(CTexture.RefPnt.Left, x + ((OpenTaiko.Tx.Dani_DanIcon.szTextureSize.Width) * scale), y);
			ttx.Opacity = 255;
			ttx.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(1.0f, 1.0f, 1.0f);
		}
		OpenTaiko.Tx.Dani_DanIcon.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(1.0f, 1.0f, 1.0f);
	}
}
