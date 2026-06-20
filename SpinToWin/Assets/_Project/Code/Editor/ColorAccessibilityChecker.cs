using UnityEditor;
using UnityEngine;

namespace _Project.Code.Editor
{
    public class ColorAccessibilityChecker : EditorWindow
    {
        // Constants
        private const string WindowName = "WCAG Contrast Checker";
        private const string MenuItemName = "Accessibility/WCAG Contrast Checker";
        private const float WcagAaaThreshold = 7f;
        private const float WcagAAThreshold = 4.5f;
        private const int DefaultSpaceSize = 10;

        // Colors to manipulate
        private Color _c1 = Color.white;
        private Color _c2 = Color.black;

        // Helper styles to make it look pretty :)
        private GUIStyle _previewStyle;
        private GUIStyle _wcagStyle;
        private GUIStyle _passStyle;
        private GUIStyle _failStyle;
        private GUIStyle _contrastStyle;

        [MenuItem(MenuItemName)]
        public static void ShowWindow()
        {
            // Set up the window and don't let it get too small
            ColorAccessibilityChecker window = (ColorAccessibilityChecker)EditorWindow.GetWindow(typeof(ColorAccessibilityChecker), false, WindowName);
            window.minSize = new Vector2(230, 260);
            window.Show();
        }

        void OnGUI()
        {
            // Add some padding to the whole window (across the top)
            GUILayout.Space(DefaultSpaceSize);
            EditorGUILayout.BeginHorizontal();
            // Add some padding to the whole window (down the left)
            GUILayout.Space(DefaultSpaceSize);
            // Define the right column
            EditorGUILayout.BeginVertical(GUILayout.Width(210));
            // Draw the Color Pickers
            EditorGUILayout.BeginHorizontal();
            _c1 = EditorGUILayout.ColorField(new GUIContent(), _c1, false, false, false, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.Space(DefaultSpaceSize);
            _c2 = EditorGUILayout.ColorField(new GUIContent(), _c2, false, false, false, GUILayout.Width(100), GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();

            // Set up GUIStyle appropriately
            TryInitStyles();

            _previewStyle.normal.textColor = new Color(_c1.r, _c1.g, _c1.b, 1);
            _previewStyle.normal.background = MakePreviewBackground(new Color(_c2.r, _c2.g, _c2.b, 1));

            // Draw the preview
            GUILayout.Space(DefaultSpaceSize);
            EditorGUILayout.LabelField("Lorem Ipsum", _previewStyle, GUILayout.Height(30));

            GUILayout.Space(DefaultSpaceSize);

            float constrast = CalculateContrast(_c1, _c2);

            // WCAG Labels
            EditorGUILayout.BeginHorizontal();
            bool isAA = constrast > WcagAAThreshold;
            EditorGUILayout.LabelField("WCAG AA:", _wcagStyle, GUILayout.Width(100), GUILayout.Height(18));
            EditorGUILayout.LabelField(isAA ? "Pass" : "Fail", isAA ? _passStyle : _failStyle, GUILayout.Width(100), GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool isAaa = constrast > WcagAaaThreshold;
            EditorGUILayout.LabelField("WCAG AAA:", _wcagStyle, GUILayout.Width(100), GUILayout.Height(18));
            EditorGUILayout.LabelField(isAaa ? "Pass" : "Fail", isAaa ? _passStyle : _failStyle, GUILayout.Width(100), GUILayout.Height(18));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(DefaultSpaceSize);

            // Contrast Labels
            EditorGUILayout.LabelField("Contrast", _contrastStyle, GUILayout.Height(24));
            EditorGUILayout.LabelField($"{constrast.ToString("n2")}:1", _contrastStyle, GUILayout.Height(18));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // Calculates contrast based off of a Unity Color where R, G, B are 0-1
        private float CalculateContrast(Color c1, Color c2)
        {
            float relativeLuminancec1 = 0.2126f * GetChannelForRelativeLuminance(c1.r) + 0.7152f * GetChannelForRelativeLuminance(c1.g) + 0.0722f * GetChannelForRelativeLuminance(c1.b);
            float relativeLuminancec2 = 0.2126f * GetChannelForRelativeLuminance(c2.r) + 0.7152f * GetChannelForRelativeLuminance(c2.g) + 0.0722f * GetChannelForRelativeLuminance(c2.b);

            float l1 = Mathf.Max(relativeLuminancec1, relativeLuminancec2);
            float l2 = Mathf.Min(relativeLuminancec1, relativeLuminancec2);

            return (l1 + 0.05f) / (l2 + 0.05f);
        }

        // Makes sure the channel value is correctly calculated as per relative luminance guidelines
        private float GetChannelForRelativeLuminance(float f)
        {
            if (f < 0.03928f)
                return f / 12.92f;
            else
                return Mathf.Pow((f + 0.055f) / 1.055f, 2.4f);
        }

        // Creates all relevant styles, trying to keep OnGUI clean(ish)
        private void TryInitStyles()
        {
            if (_previewStyle == null)
            {
                _previewStyle = new GUIStyle(EditorStyles.boldLabel);
                _previewStyle.alignment = TextAnchor.MiddleCenter;
                _previewStyle.fontSize = 16; 
            }

            if (_wcagStyle == null)
            {
                _wcagStyle = new GUIStyle(EditorStyles.boldLabel);
                _wcagStyle.fontSize = 14;
            }

            if (_passStyle == null)
            {
                _passStyle = new GUIStyle(_wcagStyle);
                _passStyle.normal.textColor = Color.green;
            }

            if (_failStyle == null)
            {
                _failStyle = new GUIStyle(_wcagStyle);
                _failStyle.normal.textColor = Color.red;
            }

            if (_contrastStyle == null)
            {
                _contrastStyle = new GUIStyle(EditorStyles.boldLabel);
                _contrastStyle.alignment = TextAnchor.MiddleCenter;
                _contrastStyle.fontSize = 16;
            }
        }

        // Creates the background texture to help with the preview
        private Texture2D MakePreviewBackground(Color color)
        {
            Color[] colors = new Color[] { color };

            Texture2D result = new Texture2D(1, 1);
            result.SetPixels(colors);
            result.Apply();

            return result;
        }
    }
}
