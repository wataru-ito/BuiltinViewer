using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BuiltinViewer
{
	public class BuiltinWindowInfoViewer : EditorWindow
	{
		EditorWindow m_window;
		BindingFlags m_bindingFlags;
		Vector2 m_scrollPosition;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[MenuItem("Window/Builtin/BuiltinWindowInfoViewer")]
		static void Open()
		{
			GetWindow<BuiltinWindowInfoViewer>();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnEnable()
		{
			titleContent = new GUIContent("窓見る造");
		}

		void OnGUI()
		{
			if (!m_window)
			{
				EditorGUILayout.LabelField("Window not selected.");
				return;
			}

			EditorGUIUtility.labelWidth = 80f;

			EditorGUILayout.LabelField("Window Info");
			++EditorGUI.indentLevel;
			{
				EditorGUILayout.LabelField("titleContent", m_window.titleContent.text);
				EditorGUILayout.RectField("position", m_window.position);
				EditorGUILayout.Vector2Field("minSize", m_window.minSize);
				EditorGUILayout.Vector2Field("maxSize", m_window.maxSize);
			}
			--EditorGUI.indentLevel;


			EditorGUILayout.LabelField("Class Info");
			++EditorGUI.indentLevel;
			{
				var type = m_window.GetType();
				EditorGUILayout.LabelField(type.FullName);

				EditorGUILayout.LabelField("Assembly", type.Assembly.ToString());
				foreach (var attr in type.GetCustomAttributes(true))
				{
					EditorGUILayout.LabelField("Attribute", attr.ToString());
				}

				m_bindingFlags = (BindingFlags)EditorGUILayout.EnumMaskField("Methods", m_bindingFlags);
				using (var scroll = new EditorGUILayout.ScrollViewScope(m_scrollPosition, "box"))
				{
					foreach (var method in type.GetMethods(m_bindingFlags))
					{
						var sb = new System.Text.StringBuilder();
						foreach (var param in method.GetParameters())
						{
							if (sb.Length > 0) sb.Append(", ");
							sb.AppendFormat("{0} {1}", param.ParameterType.Name, param.Name);
						}
						EditorGUILayout.LabelField(string.Format("{0} {1} ({2})", method.ReturnType.Name, method.Name, sb.ToString()));
					}

					m_scrollPosition = scroll.scrollPosition;
				}
			}
			--EditorGUI.indentLevel;
		}

		void OnInspectorUpdate()
		{
			// Selection.selectionChanged みたいにフォーカス切り替わった時のイベントはないのかな？
			if (focusedWindow != m_window && focusedWindow != this)
			{
				m_window = focusedWindow;
			}
			
			Repaint();
		}
	}
}