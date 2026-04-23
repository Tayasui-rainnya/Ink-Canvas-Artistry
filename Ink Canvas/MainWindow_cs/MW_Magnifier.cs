using Ink_Canvas.Windows;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private Thread magnifierUiThread;
        private ScreenMagnifierWindow magnifierWindow;

        private void SymbolIconMagnifier_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode != 0)
            {
                return;
            }

            if (magnifierWindow == null)
            {
                OpenMagnifierWindow();
            }
            else
            {
                CloseMagnifierWindow();
            }
        }

        private void OpenMagnifierWindow()
        {
            if (magnifierWindow != null) return;

            IntPtr mainHandle = new WindowInteropHelper(this).Handle;
            using (var initSignal = new ManualResetEvent(false))
            {
                magnifierUiThread = new Thread(() =>
                {
                    var window = new ScreenMagnifierWindow(mainHandle);
                    magnifierWindow = window;
                    window.RequestClose += (_, __) => magnifierWindow = null;
                    initSignal.Set();
                    window.Show();
                    System.Windows.Threading.Dispatcher.Run();
                });

                magnifierUiThread.SetApartmentState(ApartmentState.STA);
                magnifierUiThread.IsBackground = true;
                magnifierUiThread.Start();
                initSignal.WaitOne();
            }
        }

        private void CloseMagnifierWindow()
        {
            ScreenMagnifierWindow window = magnifierWindow;
            if (window == null) return;

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (window.IsVisible)
                {
                    window.Close();
                }

                window.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background);
            }));
        }

        private void UpdateMagnifierToolButtonVisibility()
        {
            if (BtnToolsMagnifier == null) return;
            BtnToolsMagnifier.Visibility = currentMode == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
