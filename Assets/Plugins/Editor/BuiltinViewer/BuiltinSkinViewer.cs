using System;
using UnityEngine;
using UnityEditor;

namespace BuiltinViewer
{
	public class BuiltinGUISkinViewer : EditorWindow
	{
		readonly GUIContent kExportContent = new GUIContent("出力", "現在のGUISkinをアセットとして出力します");

		enum SkinType
		{
			Game,
			Inspector,
			Scene,
			Current, // ProSkinは取得できないのでGUI.skinを参照する
		}

		enum SearchType
		{
			StyleName,
			TextureName,
		}


		SkinType m_skinType;
		SearchType m_searchType;
		string m_searchString = "";
		GUISkin m_skin;
		GUIStyle[] m_styles;

		GUIStyle m_selectedStyle;
		Vector2 m_scrollPosition;

		bool m_guiInitialized;
		GUIStyle m_labelStyle;
		GUIStyle m_sampleTitleStyle;

		const float kSampleWidthMin = 150;
		const float kSampleWidthMax = 300;
		float m_sampleWidth = 150f;
		float m_sampleEditBeganX;
		float m_sampleEditBeganValue;
		bool m_sampleToggle;
		string m_sampleText = "Text";
		int m_sampleValue;

		bool m_sampleHover = true;
		bool m_sampleActive;
		bool m_sampleOn;
		bool m_sampleKeyboard;


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
			
			m_guiInitialized = false; // コンパイルが走るたびにOnEnable()は呼ばれる
		}

		void OnGUI()
		{
			if (!m_guiInitialized)
			{
				InitGUI();
			}

			DrawToolbar();

			var position = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			position.width -= m_sampleWidth;
			DrawStylelist(position);

			position.x += position.width;
			position.width = m_sampleWidth;
			DrawSample(position);
		}


		//------------------------------------------------------
		// skin/style
		//------------------------------------------------------

		void InitGUI()
		{
			m_skin = GetSkin(m_skinType);
			m_styles = GetGUIStyles();

			m_labelStyle = GUI.skin.FindStyle("PR Label");
			m_sampleTitleStyle = GUI.skin.FindStyle("OL Titlemid");
			
			if (m_labelStyle == null || m_sampleTitleStyle == null)
			{
				EditorGUILayout.HelpBox("BuiltinStyle not found.", MessageType.Error);
				return;
			}

			m_guiInitialized = true;
		}

		static GUISkin GetSkin(SkinType skinType)
		{
			switch (skinType)
			{
				case SkinType.Game: return EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game);
				case SkinType.Inspector: return EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
				case SkinType.Scene: return EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
				case SkinType.Current: return GUI.skin;
				default:
					Debug.LogWarningFormat("Unknown skinType[{0}]", skinType);
					goto case SkinType.Game;
			}
		}


		//------------------------------------------------------
		// toolbar
		//------------------------------------------------------

		void DrawToolbar()
		{
			GUI.Box(new Rect(0, 0, position.width, EditorGUIUtility.singleLineHeight), GUIContent.none, EditorStyles.toolbar);

			const float kPadding = 8f;
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUI.BeginChangeCheck();
				GUILayout.Space(kPadding);

				DrawSkinField();
				
				GUILayout.FlexibleSpace();
				
				DrawSeachField();

				GUILayout.Space(kPadding);
				if (EditorGUI.EndChangeCheck())
				{
					m_styles = GetGUIStyles();
				}
			}
		}

		void DrawSkinField()
		{
			EditorGUI.BeginChangeCheck();
			m_skinType = (SkinType)EditorGUILayout.EnumPopup(m_skinType, EditorStyles.toolbarPopup, GUILayout.Width(80));
			if (EditorGUI.EndChangeCheck())
			{
				m_skin = GetSkin(m_skinType);
				Selection.activeObject = m_skin;
			}

			EditorGUILayout.ObjectField(m_skin, typeof(GUISkin), false);

			if (GUILayout.Button(kExportContent, EditorStyles.toolbarButton, GUILayout.Width(30)))
			{
				CreateBuiltinSkinAsset();
			}
		}

		void DrawSeachField()
		{
			m_searchType = (SearchType)EditorGUILayout.EnumPopup(m_searchType, EditorStyles.toolbarPopup, GUILayout.Width(90));
			m_searchString = GUILayout.TextField(m_searchString, "ToolbarSeachTextField", GUILayout.MinWidth(30), GUILayout.MaxWidth(200)).ToLower();
			if (GUILayout.Button(GUIContent.none, "ToolbarSeachCancelButton"))
			{
				m_searchString = string.Empty;
				GUI.FocusControl(null);
			}
		}

		GUIStyle[] GetGUIStyles()
		{
			if (string.IsNullOrEmpty(m_searchString))
				return m_skin.customStyles;

			return m_searchType == SearchType.StyleName ?
				Array.FindAll(m_skin.customStyles, i => i.name.ToLower().Contains(m_searchString)) :
				Array.FindAll(m_skin.customStyles, i => IsTarget(i));
		}

		bool IsTarget(GUIStyle style)
		{
			return IsTarget(style.active) ||
				IsTarget(style.focused) ||
				IsTarget(style.hover) ||
				IsTarget(style.normal) ||
				IsTarget(style.onActive) ||
				IsTarget(style.onFocused) ||
				IsTarget(style.onHover) ||
				IsTarget(style.onNormal);
		}

		bool IsTarget(GUIStyleState state)
		{
			return state.background && state.background.name.ToLower().Contains(m_searchString);
		}

		void CreateBuiltinSkinAsset()
		{
			var filePath = EditorUtility.SaveFilePanel("GUISkin出力", "Assets/", m_skin.name, "guiskin");
			if (string.IsNullOrEmpty(filePath)) 
				return;

			if (!filePath.StartsWith(Application.dataPath))
			{
				EditorUtility.DisplayDialog("GUISkin出力",
					"場所は Assets/以下 でお願いします",
					"選びなおす");

				EditorApplication.delayCall += CreateBuiltinSkinAsset;
				return;
			}

			var assetPath = filePath.Substring(Application.dataPath.Length - 6);

			var guiSkin = Instantiate(m_skin);
			AssetDatabase.CreateAsset(guiSkin, assetPath);

			Selection.activeObject = guiSkin;

			EditorUtility.DisplayDialog("GUISkin出力",
				string.Format("GUISkinを出力しました\n\n{0}", assetPath),
				"OK");
		}


		//------------------------------------------------------
		// stylelist
		//------------------------------------------------------

		void DrawStylelist(Rect position)
		{
			var viewRect = new Rect(0, 0, position.width - GUI.skin.verticalScrollbar.fixedWidth, EditorGUIUtility.singleLineHeight * m_styles.Length);
			using (var scroll = new GUI.ScrollViewScope(position, m_scrollPosition, viewRect, false, true))
			{
				for (int i = 0; i < m_styles.Length; ++i)
				{
					GUIStyleField(new Rect(0, EditorGUIUtility.singleLineHeight * i, viewRect.width, EditorGUIUtility.singleLineHeight), m_styles[i]);
				}

				m_scrollPosition = scroll.scrollPosition;
			}

			var ev = Event.current;
			switch (ev.type)
			{
				case EventType.KeyDown:
					switch (ev.keyCode)
					{
						case KeyCode.UpArrow:
							if (PrevStyle())
							{
								ev.Use();
							}
							break;
						case KeyCode.DownArrow:
							if (NextStyle(position.height))
							{
								ev.Use();
							}
							break;
					}
					break;
			}
		}

		void GUIStyleField(Rect position, GUIStyle style)
		{
			var controlID = GUIUtility.GetControlID(FocusType.Passive);
			var ev = Event.current;
			switch (ev.GetTypeForControl(controlID))
			{
				case EventType.Repaint:
					m_labelStyle.Draw(position, new GUIContent(style.name), controlID, m_selectedStyle == style);
					break;

				case EventType.MouseDown:
					if (position.Contains(ev.mousePosition) && ev.button == 0)
					{
						m_selectedStyle = style;
						ev.Use();
						GUI.FocusControl("");
					}
					break;

				case EventType.ContextClick:
					if (position.Contains(ev.mousePosition))
					{
						ShowStyleContextMenu(style);
						ev.Use();
					}
					break;
			}
		}

		void ShowStyleContextMenu(GUIStyle style)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Copy Name"), false, () => EditorGUIUtility.systemCopyBuffer = style.name);
			menu.ShowAsContext();
		}

		bool NextStyle(float displayHeight)
		{
			if (m_styles.Length == 0) return false;

			var index = Array.IndexOf(m_styles, m_selectedStyle);
			if (index >= m_styles.Length - 1) return false;
			
			++index;
			m_selectedStyle = m_styles[index];
			m_scrollPosition.y = Mathf.Max(m_scrollPosition.y, EditorGUIUtility.singleLineHeight * (index + 1) - displayHeight);

			return true;
		}

		bool PrevStyle()
		{
			if (m_styles.Length == 0) return false;
			
			var index = Array.IndexOf(m_styles, m_selectedStyle);
			if (index <= 0) return false;
			
			--index;
			m_selectedStyle = m_styles[index];
			m_scrollPosition.y = Mathf.Min(m_scrollPosition.y, EditorGUIUtility.singleLineHeight * (index));

			return true;
		}


		//------------------------------------------------------
		// sample
		//------------------------------------------------------

		void DrawSample(Rect position)
		{
			OperateSampleFieldWidth(new Rect(position.x, position.y, 4, position.height));

			position.height = EditorGUIUtility.singleLineHeight;

			DrawSampleHeader(ref position);
			
			if (m_selectedStyle == null)
				return;
			
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = position.width * 0.5f;

			DrawSample(ref position); 
			position.y += position.height;
			
			DrawGUISample(ref position); 			
			position.y += position.height;
			
			DrawEditorGUISample(ref position);
			position.y += position.height;
			
			EditorGUIUtility.labelWidth = labelWidth;
		}

		void OperateSampleFieldWidth(Rect position)
		{
			var edgeRect = new Rect(position.x, position.yMin, 3f, position.height);

			var controlID = GUIUtility.GetControlID(FocusType.Passive);
			var ev = Event.current;
			switch (ev.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (edgeRect.Contains(ev.mousePosition) && GUIUtility.hotControl == 0)
					{
						GUIUtility.hotControl = controlID;
						m_sampleEditBeganValue = m_sampleWidth;
						m_sampleEditBeganX = ev.mousePosition.x;
						ev.Use();
						GUI.FocusControl("");
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						m_sampleWidth = Mathf.Clamp(m_sampleEditBeganValue + (m_sampleEditBeganX - ev.mousePosition.x), kSampleWidthMin, kSampleWidthMax);
						GUI.changed = true;
						ev.Use();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
						ev.Use();
					}
					break;
			}

			// 領域指定じゃなくて完全にマウスカーソルを変える方法ないかな？
			EditorGUIUtility.AddCursorRect(GUIUtility.hotControl == controlID ? new Rect(0, 0, Screen.width, Screen.height) : edgeRect,
				MouseCursor.ResizeHorizontal);
		}

		void DrawSampleHeader(ref Rect position)
		{
			EditorGUI.LabelField(position, new GUIContent(m_selectedStyle == null ? "---" : m_selectedStyle.name), m_sampleTitleStyle);
			position.y += position.height;

			var btnRect = position;
			btnRect.width *= 0.25f;
			m_sampleHover = GUI.Toggle(btnRect, m_sampleHover, "Hover", EditorStyles.toolbarButton); 
			btnRect.x += btnRect.width;
			m_sampleActive = GUI.Toggle(btnRect, m_sampleActive, "Active", EditorStyles.toolbarButton); 
			btnRect.x += btnRect.width;
			m_sampleOn = GUI.Toggle(btnRect, m_sampleOn, "On", EditorStyles.toolbarButton); 
			btnRect.x += btnRect.width;
			m_sampleKeyboard = GUI.Toggle(btnRect, m_sampleKeyboard, "Keyboard", EditorStyles.toolbarButton); 
			btnRect.x += btnRect.width;
	
			position.y += position.height;
		}

		void DrawSample(ref Rect position)
		{
			switch (Event.current.type)
			{
				case EventType.Repaint:
					var itemPosition = position;
					itemPosition.y += 8f;
					itemPosition.x += 4f;
					itemPosition.width -= 8f;
					m_selectedStyle.Draw(itemPosition, new GUIContent("Sample"), m_sampleHover, m_sampleActive, m_sampleOn, m_sampleKeyboard);
					break;
			}

			position.y += position.height;
		}

		void DrawGUISample(ref Rect position)
		{
			EditorGUI.LabelField(position, new GUIContent("GUI"), m_sampleTitleStyle);
			position.y += position.height;
			GUI.Label(position, "Label", m_selectedStyle);
			position.y += position.height;
			GUI.Box(position, "Box", m_selectedStyle);
			position.y += position.height;
			m_sampleToggle = GUI.Toggle(position, m_sampleToggle, "Toggle", m_selectedStyle);
			position.y += position.height;
			m_sampleText = GUI.TextField(position, m_sampleText, m_selectedStyle);
			position.y += position.height;
		}

		void DrawEditorGUISample(ref Rect position)
		{
			EditorGUI.LabelField(position, new GUIContent("EditorGUI"), m_sampleTitleStyle);
			position.y += position.height;
			EditorGUI.LabelField(position, "Label", m_selectedStyle);
			position.y += position.height;
			EditorGUI.LabelField(position, "Label", "Label2", m_selectedStyle);
			position.y += position.height;
			m_sampleValue = EditorGUI.Popup(position, "Popup", m_sampleValue, new string[] { "Sample1", "Sample2", }, m_selectedStyle);
			position.y += position.height;
			m_sampleToggle = EditorGUI.Foldout(position, m_sampleToggle, "Foldout", m_selectedStyle);
			position.y += position.height;
			m_sampleToggle = EditorGUI.Toggle(position, "Toggle", m_sampleToggle, m_selectedStyle);
			position.y += position.height;
			m_sampleText = EditorGUI.TextField(position, "Text", m_sampleText, m_selectedStyle);
			position.y += position.height;
			m_sampleValue = EditorGUI.IntField(position, "Int", m_sampleValue);
			position.y += position.height;
		}
	}
}