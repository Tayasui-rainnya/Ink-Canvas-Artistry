using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private bool isApplyingGestureDefaults;

        /// <summary>
        /// 为旧版配置补齐分模式手势默认值，避免反序列化旧配置后出现空引用。
        /// </summary>
        private void InitializeGestureModeDefaults()
        {
            if (Settings.Gesture.AnnotationModeDefaults == null)
            {
                Settings.Gesture.AnnotationModeDefaults = new GestureModeDefaults();
            }

            if (Settings.Gesture.WhiteboardModeDefaults == null)
            {
                Settings.Gesture.WhiteboardModeDefaults = new GestureModeDefaults
                {
                    IsEnableMultiTouchMode = false,
                    IsEnableTwoFingerZoom = true,
                    IsEnableTwoFingerTranslate = true
                };
            }
        }

        /// <summary>
        /// 将当前模式的默认手势状态同步到运行时配置和对应的浮动工具栏控件。
        /// </summary>
        private void ApplyGestureDefaultsForCurrentMode()
        {
            InitializeGestureModeDefaults();
            isApplyingGestureDefaults = true;
            try
            {
                GestureModeDefaults defaults = GetGestureModeDefaults(currentMode == 1);
                Settings.Gesture.IsEnableMultiTouchMode = defaults.IsEnableMultiTouchMode;
                Settings.Gesture.IsEnableTwoFingerZoom = defaults.IsEnableTwoFingerZoom;
                Settings.Gesture.IsEnableTwoFingerTranslate = defaults.IsEnableTwoFingerTranslate;
                Settings.Gesture.IsEnableTwoFingerRotation = defaults.IsEnableTwoFingerRotation;

                ToggleSwitchEnableMultiTouchMode.IsOn = Settings.Gesture.IsEnableMultiTouchMode;
                ToggleSwitchEnableTwoFingerZoom.IsOn = Settings.Gesture.IsEnableTwoFingerZoom;
                ToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.Gesture.IsEnableTwoFingerTranslate;
                ToggleSwitchEnableTwoFingerRotation.IsOn = Settings.Gesture.IsEnableTwoFingerRotation;
                BoardToggleSwitchEnableMultiTouchMode.IsOn = Settings.Gesture.IsEnableMultiTouchMode;
                BoardToggleSwitchEnableTwoFingerZoom.IsOn = Settings.Gesture.IsEnableTwoFingerZoom;
                BoardToggleSwitchEnableTwoFingerTranslate.IsOn = Settings.Gesture.IsEnableTwoFingerTranslate;
                BoardToggleSwitchEnableTwoFingerRotation.IsOn = Settings.Gesture.IsEnableTwoFingerRotation;

                SetTwoFingerGestureControlsEnabled(!Settings.Gesture.IsEnableMultiTouchMode);
                if (Settings.Gesture.IsEnableMultiTouchMode != isInMultiTouchMode)
                {
                    BorderMultiTouchMode_MouseUp(null, null);
                }
            }
            finally
            {
                isApplyingGestureDefaults = false;
            }

            CheckEnableTwoFingerGestureBtnColorPrompt();
        }

        /// <summary>
        /// 处理任一模式中“多指书写”的切换，并清除与之互斥的双指手势。
        /// </summary>
        private void ToggleSwitchEnableMultiTouchMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded || isApplyingGestureDefaults) return;

            bool isWhiteboard = sender == BoardToggleSwitchEnableMultiTouchMode;
            GestureModeDefaults defaults = GetGestureModeDefaults(isWhiteboard);
            defaults.IsEnableMultiTouchMode = ((ToggleSwitch)sender).IsOn;
            if (defaults.IsEnableMultiTouchMode)
            {
                defaults.IsEnableTwoFingerZoom = false;
                defaults.IsEnableTwoFingerTranslate = false;
                defaults.IsEnableTwoFingerRotation = false;
            }

            ApplyGestureDefaultsForCurrentMode();
            SaveSettingsToFile();
        }

        /// <summary>
        /// 处理任一模式中的双指手势切换，并保存为该模式下次进入时的默认状态。
        /// </summary>
        private void ToggleSwitchEnableTwoFingerGesture_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded || isApplyingGestureDefaults) return;

            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            bool isWhiteboard = sender == BoardToggleSwitchEnableTwoFingerZoom ||
                                sender == BoardToggleSwitchEnableTwoFingerTranslate ||
                                sender == BoardToggleSwitchEnableTwoFingerRotation;
            GestureModeDefaults defaults = GetGestureModeDefaults(isWhiteboard);
            if (sender == ToggleSwitchEnableTwoFingerZoom || sender == BoardToggleSwitchEnableTwoFingerZoom)
            {
                defaults.IsEnableTwoFingerZoom = toggleSwitch.IsOn;
            }
            else if (sender == ToggleSwitchEnableTwoFingerTranslate || sender == BoardToggleSwitchEnableTwoFingerTranslate)
            {
                defaults.IsEnableTwoFingerTranslate = toggleSwitch.IsOn;
            }
            else
            {
                defaults.IsEnableTwoFingerRotation = toggleSwitch.IsOn;
            }

            ApplyGestureDefaultsForCurrentMode();
            SaveSettingsToFile();
        }

        /// <summary>
        /// 保存选中墨迹旋转的独立设置；该设置不属于模式默认手势。
        /// </summary>
        private void ToggleSwitchEnableTwoFingerRotationOnSelection_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded || isApplyingGestureDefaults) return;

            Settings.Gesture.IsEnableTwoFingerRotationOnSelection = ToggleSwitchEnableTwoFingerRotationOnSelection.IsOn;
            SaveSettingsToFile();
        }

        /// <summary>
        /// 返回指定模式的手势默认配置；白板为 <c>true</c>，屏幕批注为 <c>false</c>。
        /// </summary>
        private GestureModeDefaults GetGestureModeDefaults(bool isWhiteboard)
        {
            return isWhiteboard ? Settings.Gesture.WhiteboardModeDefaults : Settings.Gesture.AnnotationModeDefaults;
        }

        /// <summary>
        /// 在多指书写开启时禁用所有双指手势控件，防止保存冲突配置。
        /// </summary>
        private void SetTwoFingerGestureControlsEnabled(bool isEnabled)
        {
            ToggleSwitchEnableTwoFingerZoom.IsEnabled = isEnabled;
            ToggleSwitchEnableTwoFingerTranslate.IsEnabled = isEnabled;
            ToggleSwitchEnableTwoFingerRotation.IsEnabled = isEnabled;
            BoardToggleSwitchEnableTwoFingerZoom.IsEnabled = isEnabled;
            BoardToggleSwitchEnableTwoFingerTranslate.IsEnabled = isEnabled;
            BoardToggleSwitchEnableTwoFingerRotation.IsEnabled = isEnabled;
            TwoFingerGestureSimpleStackPanel.Opacity = isEnabled ? 1 : 0.5;
            BoardTwoFingerGestureSimpleStackPanel.Opacity = isEnabled ? 1 : 0.5;
        }
    }
}
