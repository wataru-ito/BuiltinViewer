using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace BuiltinViewer
{
	public class BuiltinTextureViewer : EditorWindow
	{
		enum DrawType
		{
			RGB,
			Transparent,
			Alpha,
		}

		const float kItemSizeMin = 64f;
		const float kItemSizeMax = 128f;
		const float kScrollBarWidth = 16f;
		const float kPaddingMin = 16f;

		Texture[] m_textures;

		DrawType m_drawType;
		string m_searchString = string.Empty;
		Texture[] m_displayed;
		Texture m_selected;

		float m_itemSize = kItemSizeMin;
		int m_columnNum;
		int m_rawNum;
		Vector2 m_padding;
		Rect m_scrollRect;
		Vector2 m_scrollPosition;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Window/Builtin/BuiltinTextureViewer")]
		static void Open()
		{
			GetWindow<BuiltinTextureViewer>();
		}

		static AssetBundle GetBuiltinAssetBundle()
		{
			var info = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.Static | BindingFlags.NonPublic);
			return info.Invoke(null, new object[0]) as AssetBundle;
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnEnable()
		{
			titleContent = new GUIContent("ﾃｸｽﾁｬ見る造");
			minSize = new Vector2(200, 150);

			m_textures = GetBuiltinAssetBundle()
				.LoadAllAssets(typeof(Texture))
				.OfType<Texture>()
				.ToArray();
		}

		void OnGUI()
		{
			DrawSearchBar();
			DrawTextureList();
			DrawFooter();

			switch (Event.current.type)
			{
				case EventType.MouseDown:
					if (m_scrollRect.Contains(Event.current.mousePosition))
					{
						OnClicked(Event.current);
					}
					break;
			}
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void DrawSearchBar()
		{
			GUI.Box(new Rect(0, 0, position.width, 16), GUIContent.none, "Toolbar");
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(8f);

				m_drawType = (DrawType)EditorGUILayout.EnumPopup(m_drawType, "ToolbarPopup", GUILayout.Width(80));

				GUILayout.Space(8f);
				GUILayout.FlexibleSpace();

				m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField", GUILayout.MinWidth(80), GUILayout.MaxWidth(300));
				if (GUILayout.Button(GUIContent.none, "ToolbarSeachCancelButton"))
				{
					m_searchString = string.Empty;
					GUI.FocusControl(null);
				}

				GUILayout.Space(8f);
			}
		}

		void DrawTextureList()
		{
			m_scrollRect = GUILayoutUtility.GetRect(GUIContent.none, "ScrollView", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			m_displayed = GetTargetTextures();
			var viewRectWidth = m_scrollRect.width - kScrollBarWidth;

			m_columnNum = Mathf.FloorToInt((viewRectWidth - kPaddingMin) / (m_itemSize + kPaddingMin));
			m_rawNum = m_displayed.Length / m_columnNum + (m_displayed.Length % m_columnNum == 0 ? 0 : 1);
			m_padding.x = (viewRectWidth - m_itemSize * m_columnNum) / (m_columnNum + 1);
			m_padding.y = kPaddingMin;

			using (var scroll = new GUI.ScrollViewScope(m_scrollRect, m_scrollPosition,
				new Rect(0, 0, viewRectWidth, m_rawNum * (m_itemSize + m_padding.y) + m_padding.y)))
			{
				var top = m_scrollRect.y + m_scrollPosition.y;
				var bottom = top + m_scrollRect.height;
				var y = kPaddingMin;
				for (int i = 0; i < m_rawNum; ++i, y += m_itemSize + kPaddingMin)
				{
					if (y >= bottom || y + m_itemSize <= top) continue;

					DrawColumn(y, m_displayed, i * m_columnNum, m_columnNum, m_padding.x);
				}

				m_scrollPosition = scroll.scrollPosition;
			}
		}

		Texture[] GetTargetTextures()
		{
			return string.IsNullOrEmpty(m_searchString) ?
				m_textures :
				Array.FindAll(m_textures, i => i.name.Contains(m_searchString));
		}

		void DrawColumn(float y, Texture[] textures, int textureIndex, int count, float itemPadding)
		{
			var itemPosition = new Rect(itemPadding, y, m_itemSize, m_itemSize);

			count = Mathf.Min(count, textures.Length - 1 - textureIndex);
			for (int i = 0; i < count; ++i)
			{
				DrawTexture(itemPosition, textures[textureIndex + i]);
				itemPosition.x += itemPosition.width + itemPadding;
			}
		}

		void DrawTexture(Rect itemPosition, Texture texture)
		{
			// ProjectBrowserIconDropShadow の OnFocus が選択時青いテクスチャが設定されている
			// しかしどうすれば OnFocus 状態にできるのか？
			// わからないからとりあえず枠を描画する...orz
			if (texture == m_selected)
			{
				var r = itemPosition;
				r.x -= kPaddingMin * 0.5f;
				r.y -= kPaddingMin * 0.5f;
				r.width += kPaddingMin;
				r.height += kPaddingMin;
				GUI.Box(r, GUIContent.none, "ProjectBrowserTextureIconDropShadow");
			}

			GUI.SetNextControlName(texture.name);
			switch (m_drawType)
			{
				case DrawType.RGB:
					//GUI.Box(itemPosition, texture, "ProjectBrowserIconDropShadow"); 選択時の青表示これにしたい...
					GUI.DrawTexture(itemPosition, texture);
					break;

				case DrawType.Alpha:
					EditorGUI.DrawTextureAlpha(itemPosition, texture);
					break;

				case DrawType.Transparent:
					EditorGUI.DrawTextureTransparent(itemPosition, texture);
					break;
			}
		}

		void DrawFooter()
		{
			const float kPaddingL = 8f;
			const float kPaddingR = 16f;
			const float kSliderWidth = 64f;
			const float kButtonWidth = 40f;

			var itemPosition = GUILayoutUtility.GetRect(GUIContent.none, "Toolbar", GUILayout.ExpandWidth(true));
			GUI.Box(itemPosition, GUIContent.none, "Toolbar");

			var selectedName = m_selected ? m_selected.name : "";

			itemPosition.x = kPaddingL;
			itemPosition.width = 65;
			GUI.enabled = !string.IsNullOrEmpty(selectedName);
			if (GUI.Button(itemPosition, "Copy Name", "toolbarbutton"))
			{
				GUIUtility.systemCopyBuffer = selectedName;
			}
			GUI.enabled = true;

			GUI.enabled = m_selected is Texture2D;
			itemPosition.x += itemPosition.width;
			itemPosition.width = 40;
			if (GUI.Button(itemPosition, "Export", "toolbarbutton"))
			{
				ExportTexture(m_selected as Texture2D);
			}
			GUI.enabled = true;

			itemPosition.x += itemPosition.width + 4f;
			itemPosition.width = position.width - itemPosition.x - kSliderWidth - kPaddingR;
			EditorGUI.LabelField(itemPosition, selectedName);

			itemPosition.x = position.width - kSliderWidth - kPaddingR;
			itemPosition.width = kSliderWidth;
			m_itemSize = GUI.HorizontalSlider(itemPosition, m_itemSize, kItemSizeMin, kItemSizeMax);
		}

		void ExportTexture(Texture2D texture)
		{
			var path = EditorUtility.SaveFilePanel("Export Texture", Application.dataPath, texture.name, ".png");
			if (string.IsNullOrEmpty(path)) return;

			System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
		}

		//------------------------------------------------------
		// events
		//------------------------------------------------------

		void OnClicked(Event ev)
		{
			// 間の空白領域は選択と判定しない

			var y = (ev.mousePosition.y - m_scrollRect.y + m_scrollPosition.y);
			var blockHeight = m_itemSize + m_padding.y;
			var raw = Mathf.FloorToInt(y / blockHeight);
			if (y - blockHeight * raw <= m_padding.y)
				return;

			var x = (ev.mousePosition.x - m_scrollRect.x + m_scrollPosition.x);
			var blockWidth = m_itemSize + m_padding.x;
			var column = Mathf.FloorToInt(x / blockWidth);
			if (x - blockWidth * column <= m_padding.x)
				return;

			if (raw >= m_rawNum || column >= m_columnNum)
				return;

			m_selected = m_displayed[raw * m_columnNum + column];
			Selection.activeObject = m_selected;

			// どうすれば GUIStyle を OnFocused にできるんだ...
			//GUI.FocusControl(m_selected.name);
			//EditorGUI.FocusTextInControl(m_selected.name);

			Repaint();
		}
	}
}