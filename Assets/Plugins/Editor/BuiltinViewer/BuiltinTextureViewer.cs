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
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void DrawSearchBar()
		{
			GUI.Box(new Rect(0, 0, position.width, 16), GUIContent.none, EditorStyles.toolbar);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(8f);

				m_drawType = (DrawType)EditorGUILayout.EnumPopup(m_drawType, EditorStyles.toolbarPopup, GUILayout.Width(80));

				GUILayout.Space(8f);
				GUILayout.FlexibleSpace();

				m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField", GUILayout.MinWidth(80), GUILayout.MaxWidth(300)).ToLower();
				if (GUILayout.Button(GUIContent.none, "ToolbarSeachCancelButton"))
				{
					m_searchString = string.Empty;
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

			var viewRect = new Rect(0, 0, viewRectWidth, m_rawNum * (m_itemSize + m_padding.y) + m_padding.y);
			using (var scroll = new GUI.ScrollViewScope(m_scrollRect, m_scrollPosition, viewRect))
			{
				var displayRect = new Rect(0, m_scrollPosition.y, viewRect.width, viewRect.height);
				var y = kPaddingMin;
				for (int i = 0; i < m_rawNum; ++i, y += m_itemSize + kPaddingMin)
				{
					DrawColumn(ref displayRect, y, m_displayed, i * m_columnNum, m_columnNum, m_padding.x);
				}

				m_scrollPosition = scroll.scrollPosition;
			}

			var ev = Event.current;
			switch (ev.type)
			{
				case EventType.KeyDown:
					switch (ev.keyCode)
					{
						case KeyCode.RightArrow:
							MoveSelect(+1);
							ev.Use();
							break;
						case KeyCode.LeftArrow:
							MoveSelect(-1);
							ev.Use();
							break;
						case KeyCode.DownArrow:
							MoveSelect(m_columnNum);
							ev.Use();
							break;
						case KeyCode.UpArrow:
							MoveSelect(-m_columnNum);
							ev.Use();
							break;
					}
					break;
			}
		}

		void MoveSelect(int offset)
		{
			var index = Array.IndexOf(m_displayed, m_selected);
			if (index < 0) return;

			m_selected = m_displayed[Mathf.Clamp(index + offset, 0, m_displayed.Length - 1)];
		}

		Texture[] GetTargetTextures()
		{
			return string.IsNullOrEmpty(m_searchString) ?
				m_textures :
				Array.FindAll(m_textures, i => i.name.ToLower().Contains(m_searchString));
		}

		void DrawColumn(ref Rect displayRect, float y, Texture[] textures, int textureIndex, int count, float itemPadding)
		{
			var itemPosition = new Rect(itemPadding, y, m_itemSize, m_itemSize);

			count = Mathf.Min(count, textures.Length - 1 - textureIndex);
			for (int i = 0; i < count; ++i)
			{
				DrawTexture(ref displayRect, itemPosition, textures[textureIndex + i]);
				itemPosition.x += itemPosition.width + itemPadding;
			}
		}

		void DrawTexture(ref Rect displayRect, Rect itemPosition, Texture texture)
		{
			var ev = Event.current;
			var controlID = GUIUtility.GetControlID(FocusType.Passive);
			switch (ev.GetTypeForControl(controlID))
			{
				case EventType.Repaint:
					if (!itemPosition.Overlaps(displayRect)) break;

					if (texture == m_selected)
					{
						var style = GUI.skin.FindStyle("ProjectBrowserTextureIconDropShadow");
						var p = itemPosition;
						p.x -= 6f;
						p.y -= 6f;
						p.width += 12f;
						p.height += 12f;
						style.Draw(p, GUIContent.none, isHover: false, isActive: true, on: true, hasKeyboardFocus: false);
					}

					switch (m_drawType)
					{
						case DrawType.RGB:
							GUI.DrawTexture(itemPosition, texture);
							break;

						case DrawType.Alpha:
							EditorGUI.DrawTextureAlpha(itemPosition, texture);
							break;

						case DrawType.Transparent:
							EditorGUI.DrawTextureTransparent(itemPosition, texture);
							break;
					}
					break;

				case EventType.MouseDown:
					if (ev.button == 0 && itemPosition.Contains(ev.mousePosition))
					{
						m_selected = texture;
						Selection.activeObject = m_selected;
						ev.Use();
					}
					break;
			}
		}

		GUIContent kLabelCopyName = new GUIContent("Copy Name", "選択したテクスチャ名をコピーする");
		GUIContent kLabelExport = new GUIContent("Export", "選択したテクスチャを指定フォルダに出力");

		void DrawFooter()
		{
			const float kPaddingL = 8f;
			const float kPaddingR = 16f;
			const float kSliderWidth = 64f;

			var itemPosition = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.ExpandWidth(true));
			GUI.Box(itemPosition, GUIContent.none, EditorStyles.toolbar);

			var selectedName = m_selected ? m_selected.name : "";

			itemPosition.x = kPaddingL;
			itemPosition.width = 65;
			GUI.enabled = !string.IsNullOrEmpty(selectedName);
			if (GUI.Button(itemPosition, kLabelCopyName, EditorStyles.toolbarButton))
			{
				GUIUtility.systemCopyBuffer = selectedName;
			}
			GUI.enabled = true;

			GUI.enabled = m_selected is Texture2D;
			itemPosition.x += itemPosition.width;
			itemPosition.width = 40;
			if (GUI.Button(itemPosition, kLabelExport, EditorStyles.toolbarButton))
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
	}
}