using System;
using UnityEngine;
using UnityEditor;

namespace BuiltinViewer
{
	public class BuiltinGUISkinViewer : EditorWindow
	{
		const float kItemHeight = 16f;

		EditorSkin m_skinType;
		GUISkin m_skin;

		string m_searchString = string.Empty;
		GUIStyle[] m_styles;
		GUIStyle m_selectedStyle;
		Vector2 m_scrollPosition;
		Rect m_scrollRect;

		bool m_sampleToggle;
		string m_sampleText = string.Empty;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Window/Builtin/BuiltinSkinView")]
		static void Open()
		{
			GetWindow<BuiltinGUISkinViewer>();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnEnable()
		{
			titleContent = new GUIContent("Skin見る造");
			minSize = new Vector2(250, 150);

			m_skin = EditorGUIUtility.GetBuiltinSkin(m_skinType);
		}

		void OnGUI()
		{
			DrawToolBar();

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope())
				{
					DrawGUIStyles();
				}

				if (m_selectedStyle != null)
				{
					using (new EditorGUILayout.VerticalScope(GUILayout.Width(150)))
					{
						GUILayout.Space(2f);
						DrawSample();
					}
				}
			}

			switch (Event.current.type)
			{
				case EventType.MouseDown:
					if (m_scrollRect.Contains(Event.current.mousePosition))
					{
						OnClicked(Event.current);
					}
					break;

				case EventType.keyDown:
					switch (Event.current.keyCode)
					{
						case KeyCode.UpArrow:
							PrevStyle();
							break;
						case KeyCode.DownArrow:
							NextStyle();
							break;
					}
					break;
			}
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void DrawToolBar()
		{
			GUI.Box(new Rect(0, 0, position.width, 16), GUIContent.none, "Toolbar");
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(8f);

				EditorGUI.BeginChangeCheck();
				m_skinType = (EditorSkin)EditorGUILayout.EnumPopup(m_skinType, "ToolbarPopup", GUILayout.Width(80));
				if (EditorGUI.EndChangeCheck())
				{
					m_skin = EditorGUIUtility.GetBuiltinSkin(m_skinType);
				}

				GUILayout.Space(4f);

				if (GUILayout.Button("出力", "toolbarbutton", GUILayout.Width(30)))
				{
					CreateBuiltinSkinAsset();
				}

				GUILayout.Space(8f);
				GUILayout.FlexibleSpace();
				GUILayout.Label("テクスチャで検索", EditorStyles.miniLabel, GUILayout.Width(80));
				m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField", GUILayout.MinWidth(30), GUILayout.MaxWidth(200));
				if (GUILayout.Button(GUIContent.none, "ToolbarSeachCancelButton"))
				{
					m_searchString = string.Empty;
					GUI.FocusControl(null);
				}

				GUILayout.Space(8f);
			}
		}

		void DrawGUIStyles()
		{
			m_scrollRect = GUILayoutUtility.GetRect(GUIContent.none, "ScrollView", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			m_styles = GetGUIStyles();

			var viewRect = new Rect(0, 0, m_scrollRect.width - 16f, m_styles.Length * kItemHeight);
			using (var scroll = new GUI.ScrollViewScope(m_scrollRect, m_scrollPosition, viewRect))
			{
				var itemPosition = viewRect;
				itemPosition.height = kItemHeight;

				for (int i = 0; i < m_styles.Length; ++i)
				{
					DrawGUIStyle(itemPosition, m_styles[i]);
					itemPosition.y += itemPosition.height;
				}

				m_scrollPosition = scroll.scrollPosition;
			}
		}

		void DrawGUIStyle(Rect itemPosition, GUIStyle style)
		{
			// 使われる GUIStyle を onActive にする方法がわからないので真似る
			// > 今はこれが精いっぱい...
			if (m_selectedStyle == style)
			{
				var selected = EditorGUIUtility.LoadRequired("selected") as Texture;
				GUI.DrawTexture(itemPosition, selected);
			}

			EditorGUI.LabelField(itemPosition, style.name);
		}

		void DrawSample()
		{
			var options = new GUILayoutOption[1] { GUILayout.ExpandWidth(true) };
			GUILayout.Label(m_selectedStyle.name, "ProjectBrowserTopBarBg", options);

			GUILayout.Label("Label", m_selectedStyle, options);
			GUILayout.Box("Box", options);
			GUILayout.Button("Button", options);
			m_sampleToggle = GUILayout.Toggle(m_sampleToggle, "Toggle", options);
			m_sampleText = GUILayout.TextField(m_sampleText, options);

			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Editor Label", m_selectedStyle, options);
			EditorGUILayout.LabelField("Editor", "Label", m_selectedStyle, options);
			m_sampleToggle = EditorGUILayout.Toggle("Editor Toggle", m_sampleToggle, options);
			m_sampleToggle = EditorGUILayout.Toggle(m_sampleToggle, options);
			m_sampleText = EditorGUILayout.TextField("Editor Text", m_sampleText, options);
			m_sampleText = EditorGUILayout.TextField(m_sampleText, options);
		}


		//------------------------------------------------------
		// events
		//------------------------------------------------------

		void OnClicked(Event ev)
		{
			var y = (ev.mousePosition.y - m_scrollRect.y + m_scrollPosition.y);
			var index = Mathf.FloorToInt(y / kItemHeight);
			if (index >= m_styles.Length)
				return;

			m_selectedStyle = m_styles[index];
			Repaint();
		}

		void NextStyle()
		{
			var index = Array.IndexOf(m_styles, m_selectedStyle);
			if (index >= 0 && index + 1 < m_styles.Length)
			{
				++index;
				m_selectedStyle = m_styles[index];

				m_scrollPosition.y = Mathf.Max(m_scrollPosition.y, (index + 1) * kItemHeight - m_scrollRect.height);
				Repaint();
			}
		}

		void PrevStyle()
		{
			var index = Array.IndexOf(m_styles, m_selectedStyle);
			if (index > 0)
			{
				--index;
				m_selectedStyle = m_styles[index];
				m_scrollPosition.y = Mathf.Min(m_scrollPosition.y, index * kItemHeight);
				Repaint();
			}
		}

		
		//------------------------------------------------------
		// skin
		//------------------------------------------------------

		void SetSkinType(EditorSkin skinType)
		{
			m_skinType = skinType;
			m_skin = EditorGUIUtility.GetBuiltinSkin(m_skinType);
		}

		GUIStyle[] GetGUIStyles()
		{
			return string.IsNullOrEmpty(m_searchString) ?
				m_skin.customStyles :
				Array.FindAll(m_skin.customStyles, i => Contains(i));
		}

		bool Contains(GUIStyle style)
		{
			return Contains(style.active, m_searchString) ||
				Contains(style.focused, m_searchString) ||
				Contains(style.hover, m_searchString) ||
				Contains(style.normal, m_searchString) ||
				Contains(style.onActive, m_searchString) ||
				Contains(style.onFocused, m_searchString) ||
				Contains(style.onHover, m_searchString) ||
				Contains(style.onNormal, m_searchString);
		}

		static bool Contains(GUIStyleState state, string str)
		{
			return state.background && state.background.name.Contains(str);
		}

		void CreateBuiltinSkinAsset()
		{
			var assetPath = string.Format("Assets/{0}.guiskin", m_skin.name);

			var guiSkin = Instantiate(m_skin);
			AssetDatabase.CreateAsset(guiSkin, assetPath);

			Selection.activeObject = guiSkin;

			EditorUtility.DisplayDialog("GUISkin出力",
				string.Format("GUISkinを出力しました\n\n{0}", assetPath),
				"OK");
		}
	}
}