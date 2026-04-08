***

### 📄 Ink Canvas/Helpers/AutoUpdateHelper.cs

**问题 1：**
⚠️ Potential issue | 🔴 Critical

```csharp
        /// <summary>
        /// 更新服务基础地址。
        /// </summary>
        private const string UpdateServerBaseUrl = "http://8.134.100.248:8080";
```

**评论：**
Blocker: update package is fetched/executed without transport/integrity guarantees.

Line 22 uses plaintext HTTP for update delivery, and Lines 262-267 execute the downloaded binary directly. This enables MITM tampering and remote code execution through the updater path.

Please require HTTPS and verify installer integrity/authenticity (e.g., signed manifest hash and/or Authenticode) before execution.

*(Also applies to: 130-132, 262-267)*

---

**问题 2：**
⚠️ Potential issue | 🟠 Major

```csharp
        private static string statusFilePath = null;
```

**评论：**
`statusFilePath` shared static state is race-prone across concurrent update calls.

`DownloadSetupFileAndSaveStatus` mutates a process-wide `statusFilePath`; concurrent invocations can write/read the wrong version marker and corrupt status results.

**🔧 Proposed fix (make status path local and pass explicitly):**
```diff
-        private static string statusFilePath = null;
+        private static readonly object downloadStatusWriteLock = new object();

         public static async Task<bool> DownloadSetupFileAndSaveStatus(string version)
         {
             try
             {
-                statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");
+                string statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");

                 if (File.Exists(statusFilePath) && File.ReadAllText(statusFilePath).Trim().ToLower() == "true")
                 {
                     LogHelper.WriteLogToFile("AutoUpdate | Setup file already downloaded.");
                     return true;
                 }

                 string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                 string downloadUrl = $"{UpdateServerBaseUrl}/download/{setupFileName}";
                 string destinationPath = Path.Combine(updatesFolderPath, setupFileName);

                 LogHelper.WriteLogToFile($"AutoUpdate | Attempting download from: {downloadUrl} to {destinationPath}");

-                SaveDownloadStatus(false);
+                SaveDownloadStatus(statusFilePath, false);
                 await DownloadFile(downloadUrl, destinationPath);
-                SaveDownloadStatus(true);
+                SaveDownloadStatus(statusFilePath, true);

-        private static void SaveDownloadStatus(bool isSuccess)
+        private static void SaveDownloadStatus(string statusFilePath, bool isSuccess)
         {
             try
             {
-                if (statusFilePath == null)
+                if (string.IsNullOrWhiteSpace(statusFilePath))
                 {
                     LogHelper.WriteLogToFile("AutoUpdate | statusFilePath is null, cannot save download status.", LogHelper.LogType.Error);
                     return;
                 }

                 string directory = Path.GetDirectoryName(statusFilePath);
                 if (!Directory.Exists(directory))
                 {
                     Directory.CreateDirectory(directory);
                 }

-                File.WriteAllText(statusFilePath, isSuccess.ToString());
+                lock (downloadStatusWriteLock)
+                {
+                    File.WriteAllText(statusFilePath, isSuccess.ToString());
+                }
                 LogHelper.WriteLogToFile($"AutoUpdate | Saved download status ({isSuccess}) to {statusFilePath}");
             }
```
*(Also applies to: 122-123, 136-139, 219-236)*

***

### 📄 Ink Canvas/Helpers/LogHelper.cs

⚠️ Potential issue | 🟠 Major

```csharp
        /// <summary>
        /// 异常日志入口（当前保留空实现）。
        /// </summary>
        /// <param name="ex">异常对象。</param>
        public static void NewLog(Exception ex)
        {

        }
```

**评论：**
`NewLog(Exception)` silently drops exception logs.

On Line 30, the exception overload is a no-op, so callers can think they logged failures when nothing is persisted.

**🛠️ Proposed fix:**
```diff
         public static void NewLog(Exception ex)
         {
-
+            if (ex == null) return;
+            WriteLogToFile(ex.ToString(), LogType.Error);
         }
```

***

### 📄 Ink Canvas/Helpers/WinTabWindowsChecker.cs

⚠️ Potential issue | 🟡 Minor

```csharp
        /// <summary>
        /// 检查指定标题窗口是否存在。
        /// </summary>
        /// <param name="windowName">目标窗口标题或标题关键字。</param>
        /// <param name="matchFullName">是否要求完整标题匹配。</param>
        /// <returns>存在返回 <c>true</c>，否则返回 <c>false</c>。</returns>
```

**评论：**
返回值文档缺少 `windowName == null` 的行为说明。

目前在 Line 67 调用 `Contains(windowName)` 时，`windowName` 为 null 会抛异常，因此“否则返回 false”并不总是成立。建议补充参数约束或显式防御。

**🛠️ Proposed fix:**
```diff
 /// <summary>
 /// 检查指定标题窗口是否存在。
 /// </summary>
-/// <param name="windowName">目标窗口标题或标题关键字。</param>
+/// <param name="windowName">目标窗口标题或标题关键字，不能为空。</param>
 /// <param name="matchFullName">是否要求完整标题匹配。</param>
 /// <returns>存在返回 <c>true</c>，否则返回 <c>false</c>。</returns>
+/// <exception cref="ArgumentNullException"><paramref name="windowName"/> 为 <c>null</c>。</exception>
 public static bool IsWindowExisted(string windowName, bool matchFullName = true)
 {
+    ArgumentNullException.ThrowIfNull(windowName);
```

***

### 📄 Ink Canvas/MainWindow_cs/MW_Notification.cs

⚠️ Potential issue | 🟠 Major

```csharp
        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotificationAsync(notice, isShowImmediately);
        }
```

**评论：**
Don’t silently drop critical notifications when MainWindow is not available yet.

At Line [16], this becomes a no-op if the window is not initialized. That can hide crash notices from `Ink Canvas/App.xaml.cs` Line [45] (`App_DispatcherUnhandledException`). Add a fallback path (queue/log + replay) when no MainWindow is found.

***

### 📄 Ink Canvas/MainWindow_cs/MW_Timer.cs

⚠️ Potential issue | 🟡 Minor

```csharp
        /// 初始化与启动主定时器参数及事件绑定。
        /// </summary>
```

**评论：**
Docstring overstates behavior in `InitTimers`.

The summary says this method “initializes and starts” timers, but it only configures handlers/intervals and does not start them. Please adjust wording to avoid misleading readers.

***

### 📄 Ink Canvas/MainWindow.xaml.cs

⚠️ Potential issue | 🟡 Minor

```csharp
        /// 主窗口构造函数：初始化界面状态、事件绑定与基础设置加载。
        /// </summary>
```

**评论：**
Constructor doc includes behavior performed later in lifecycle.

The summary says the constructor loads base settings, but settings are loaded in `Window_Loaded` (Line 192). Consider narrowing constructor wording to initialization/binding only.

***

### 📄 update_server/server.py

⚠️ Potential issue | 🟡 Minor

```python
if __name__ == "__main__":
    logging.info("Starting FastAPI server with Uvicorn...")
    uvicorn.run("server:app", host="0.0.0.0", port=8080, reload=False, log_level="info")
```

**评论：**
Restrict server binding to 127.0.0.1 unless external access is required.

The server binds to `0.0.0.0`, exposing it to all network interfaces. For an update server, consider binding to `127.0.0.1` instead to prevent unintended external access. If external access is needed, document the reason and implement appropriate security controls (firewall rules, authentication, TLS).



***

### 📄 Ink Canvas/Helpers/AutoUpdateHelper.cs

**⚠️ Potential issue | 🔴 Critical (Lines 22, 130-132, 262-267)**
Blocker: update package is fetched/executed without transport/integrity guarantees.
Line 22 uses plaintext HTTP for update delivery, and Lines 262-267 execute the downloaded binary directly. This enables MITM tampering and remote code execution through the updater path.
Please require HTTPS and verify installer integrity/authenticity (e.g., signed manifest hash and/or Authenticode) before execution.

**⚠️ Potential issue | 🟠 Major (Lines 111-123, 136-139, 146-147, 219-236)**
Make download status path call-scoped (not static shared state).
Line 111 stores `statusFilePath` in shared static state, then Lines 136/138/146 use it later. Concurrent calls can overwrite each other’s status target and persist incorrect state. Also, Line 124 cache hit should validate installer existence, not only status text.

**Proposed fix:**
```diff
-        private static string statusFilePath = null;
+        // Removed shared mutable status path; keep it per call.

         public static async Task<bool> DownloadSetupFileAndSaveStatus(string version)
         {
             try
             {
-                statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");
+                string statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");
+                string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
+                string destinationPath = Path.Combine(updatesFolderPath, setupFileName);

-                if (File.Exists(statusFilePath) && File.ReadAllText(statusFilePath).Trim().ToLower() == "true")
+                if (File.Exists(statusFilePath)
+                    && File.Exists(destinationPath)
+                    && string.Equals(File.ReadAllText(statusFilePath).Trim(), "true", StringComparison.OrdinalIgnoreCase))
                 {
                     LogHelper.WriteLogToFile("AutoUpdate | Setup file already downloaded.");
                     return true;
                 }

-                string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                 string downloadUrl = $"{UpdateServerBaseUrl}/download/{setupFileName}";
-                string destinationPath = Path.Combine(updatesFolderPath, setupFileName);

                 LogHelper.WriteLogToFile($"AutoUpdate | Attempting download from: {downloadUrl} to {destinationPath}");

-                SaveDownloadStatus(false);
+                SaveDownloadStatus(statusFilePath, false);
                 await DownloadFile(downloadUrl, destinationPath);
-                SaveDownloadStatus(true);
+                SaveDownloadStatus(statusFilePath, true);

                 LogHelper.WriteLogToFile("AutoUpdate | Setup file successfully downloaded.");
                 return true;
             }
             catch (Exception ex)
             {
                 LogHelper.WriteLogToFile($"AutoUpdate | Error downloading setup file for version {version}: {ex.Message}", LogHelper.LogType.Error);
-                SaveDownloadStatus(false);
+                string statusFilePath = Path.Combine(updatesFolderPath, $"DownloadV{version}Status.txt");
+                SaveDownloadStatus(statusFilePath, false);
                 try
                 {
                     string setupFileName = $"Ink.Canvas.Artistry.V{version}.Setup.exe";
                     string destinationPath = Path.Combine(updatesFolderPath, setupFileName);
                     if (File.Exists(destinationPath))
@@
-        private static void SaveDownloadStatus(bool isSuccess)
+        private static void SaveDownloadStatus(string statusFilePath, bool isSuccess)
         {
             try
             {
-                if (statusFilePath == null)
+                if (string.IsNullOrWhiteSpace(statusFilePath))
                 {
                     LogHelper.WriteLogToFile("AutoUpdate | statusFilePath is null, cannot save download status.", LogHelper.LogType.Error);
                     return;
                 }
```

***

### 📄 Ink Canvas/Helpers/InkCanvasElementsHelper.cs

**⚠️ Potential issue | 🟠 Major (Lines 153-154)**
Protect `Activator.CreateInstance` from constructor-missing crashes.
Line 153 can throw for `FrameworkElement` types without a public parameterless constructor, which will break clone flows at runtime.

**Suggested fix:**
```diff
-                var clonedElement = (UIElement)Activator.CreateInstance(element.GetType());
+                UIElement clonedElement;
+                try
+                {
+                    clonedElement = (UIElement)Activator.CreateInstance(element.GetType());
+                }
+                catch
+                {
+                    return null;
+                }
```

**⚠️ Potential issue | 🟠 Major (Lines 206-211)**
Handle exceptions in async `Loaded` media bootstrap.
The async event handler is effectively `async void`; failures in `Play()`/`Pause()` can surface as unhandled UI-thread exceptions.

**Suggested fix:**
```diff
-            clonedMediaElement.Loaded += async (sender, args) =>
-            {
-                clonedMediaElement.Play();
-                await Task.Delay(100);
-                clonedMediaElement.Pause();
-            };
+            clonedMediaElement.Loaded += async (sender, args) =>
+            {
+                try
+                {
+                    clonedMediaElement.Play();
+                    await Task.Delay(100);
+                    clonedMediaElement.Pause();
+                }
+                catch
+                {
+                    // optionally log
+                }
+            };
```

**⚠️ Potential issue | 🟡 Minor (Lines 80-82, 111-113)**
Use collision-safe clone IDs to avoid history key overwrite.
Line 80 and Line 111 build names from millisecond timestamps plus a method-local counter. The counter resets each call, so rapid consecutive clone operations can generate duplicate keys and overwrite `ElementsInitialHistory` entries.

**Suggested fix:**
```diff
-                    string timestamp = "ele_" + DateTime.Now.ToString("ddHHmmssfff") + key.ToString();
-                    frameworkElement.Name = timestamp;
-                    ++key;
+                    frameworkElement.Name = $"ele_{Guid.NewGuid():N}";
```

***

### 📄 Ink Canvas/Helpers/MultiTouchInput.cs

**⚠️ Potential issue | 🟠 Major (Lines 17-20)**
Add index bounds validation in `GetVisualChild`.
Since `VisualChildrenCount` is fixed at 1, `GetVisualChild(int index)` must validate that `index == 0` and throw `ArgumentOutOfRangeException` for any other value. WPF's visual tree contract requires this—returning `Visual` for invalid indices violates the contract and will cause hard-to-debug failures when the framework or VisualTreeHelper interact with the visual tree.

**Proposed fix:**
```diff
 protected override Visual GetVisualChild(int index)
 {
-    return Visual;
+    if (index != 0)
+    {
+        throw new ArgumentOutOfRangeException(nameof(index));
+    }
+    return Visual;
 }
```

**⚠️ Potential issue | 🟡 Minor (Lines 108-114)**
Remove or implement the unimplemented implicit operator.
The public implicit operator `Stroke(StrokeVisual v)` currently throws `NotImplementedException` and is marked as a placeholder. While no usages of this operator were found in the codebase, having public API that guarantees runtime failure is a correctness risk. Either remove the operator or implement it by returning `v?.Stroke`, since `StrokeVisual` has a `Stroke` property of the correct type.

***

### 📄 Ink Canvas/MainWindow_cs/MW_BoardControls.cs

**⚠️ Potential issue | 🟠 Major (Lines 65-87)**
Fix restore source selection and remove silent failure path.
Line 69 always checks `CurrentWhiteboardIndex`, even when `isBackupMain` is true. Also, `catch { }` suppresses restore failures entirely, making state corruption invisible.

**Suggested fix:**
```diff
         private void RestoreStrokes(bool isBackupMain = false)
         {
             try
             {
-                if (TimeMachineHistories[CurrentWhiteboardIndex] == null) return; //防止白板打开后不居中
-                if (isBackupMain)
-                {
-                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
-                    foreach (var item in TimeMachineHistories[0])
-                    {
-                        ApplyHistoryToCanvas(item);
-                    }
-                }
-                else
-                {
-                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[CurrentWhiteboardIndex]);
-                    foreach (var item in TimeMachineHistories[CurrentWhiteboardIndex])
-                    {
-                        ApplyHistoryToCanvas(item);
-                    }
-                }
+                var history = isBackupMain
+                    ? TimeMachineHistories[0]
+                    : TimeMachineHistories[CurrentWhiteboardIndex];
+
+                if (history == null)
+                {
+                    timeMachine.ClearStrokeHistory();
+                    return;
+                }
+
+                timeMachine.ImportTimeMachineHistory(history);
+                foreach (var item in history)
+                {
+                    ApplyHistoryToCanvas(item);
+                }
             }
-            catch { }
+            catch (Exception)
+            {
+                // TODO: log exception
+            }
         }
```

**⚠️ Potential issue | 🟠 Major (Lines 151-166)**
Prevent deleting the last remaining page.
When only one page exists, Line 163 and Line 165 drive the state to `CurrentWhiteboardIndex = 0` and `WhiteboardTotalCount = 0`, which is an invalid whiteboard state.

**Suggested fix:**
```diff
         private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
         {
+            if (WhiteboardTotalCount <= 1) return;
+
             ClearStrokes(true);
             if (CurrentWhiteboardIndex != WhiteboardTotalCount)
             {
-                for (int i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
+                for (int i = CurrentWhiteboardIndex; i < WhiteboardTotalCount; i++)
                 {
                     TimeMachineHistories[i] = TimeMachineHistories[i + 1];
                 }
+                TimeMachineHistories[WhiteboardTotalCount] = null;
             }
             else
             {
                 CurrentWhiteboardIndex--;
             }
             WhiteboardTotalCount--;
             RestoreStrokes();
             UpdateIndexInfoDisplay();
         }
```

**⚠️ Potential issue | 🟠 Major (Lines 53-59)**
`ClearStrokes` now clears UI elements globally, which is unsafe for shared call sites.
Line [58] clears `inkCanvas.Children` for every caller. This affects non-pagination flows too (e.g., screenshot path in `Ink Canvas/MainWindow_cs/MW_FloatBarIcons.cs` Lines [1005-1018]) and can remove placed elements unexpectedly.

**Suggested fix:**
```diff
-        private void ClearStrokes(bool isErasedByCode)
+        private void ClearStrokes(bool isErasedByCode, bool clearElements = false)
         {
             _currentCommitType = CommitReason.ClearingCanvas;
             if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
             inkCanvas.Strokes.Clear();
-            inkCanvas.Children.Clear();
+            if (clearElements)
+            {
+                inkCanvas.Children.Clear();
+            }
             _currentCommitType = CommitReason.UserInput;
         }
```

***

### 📄 Ink Canvas/MainWindow_cs/MW_Notification.cs

**⚠️ Potential issue | 🟠 Major (Lines 14-17)**
Don’t silently drop critical notifications when MainWindow is not available yet.
At Line [16], this becomes a no-op if the window is not initialized. That can hide crash notices from `Ink Canvas/App.xaml.cs` Line [45] (`App_DispatcherUnhandledException`). Add a fallback path (queue/log + replay) when no MainWindow is found.

**⚠️ Potential issue | 🟠 Major (Lines 27-46)**
The `isShowImmediately` parameter is never used.
The method signature includes `bool isShowImmediately = true`, but the implementation ignores it entirely—the notification always shows immediately. The relevant code snippet in `App.xaml.cs:47-49` explicitly passes true, suggesting callers expect this parameter to affect behavior.
Either implement the intended behavior (e.g., queue notifications when `isShowImmediately = false`) or remove the unused parameter from the signature.

**🐛 Example implementation if immediate display should be conditional:**
```diff
 public async void ShowNotificationAsync(string notice, bool isShowImmediately = true)
 {
     try
     {
         ShowNotificationCancellationTokenSource.Cancel();
         ShowNotificationCancellationTokenSource = new CancellationTokenSource();
         var token = ShowNotificationCancellationTokenSource.Token;

         TextBlockNotice.Text = notice;
-        AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);
+        if (isShowImmediately)
+        {
+            AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);
+        }
+        else
+        {
+            // TODO: Implement queued/delayed notification display
+            AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);
+        }
```

*Alternatively, if the parameter was never intended to be used, remove it:*
```diff
-public static void ShowNewMessage(string notice, bool isShowImmediately = true)
+public static void ShowNewMessage(string notice)
 {
-    (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotificationAsync(notice, isShowImmediately);
+    (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotificationAsync(notice);
 }

-public async void ShowNotificationAsync(string notice, bool isShowImmediately = true)
+public async void ShowNotificationAsync(string notice)
```

***

### 📄 Ink Canvas/MainWindow_cs/MW_TouchEvents.cs

**⚠️ Potential issue | 🟠 Major (Lines 145-153)**
Use a strict all-empty check before global cache reset.
Line [148] uses `||`, so if any one cache hits zero, all touch caches are cleared. This can drop active multi-touch state mid-gesture. Use `&&` (all empty) for global reset, or clear only the finished touch id.

**Suggested fix:**
```diff
-                if (StrokeVisualList.Count == 0 || VisualCanvasList.Count == 0 || TouchDownPointsList.Count == 0)
+                if (StrokeVisualList.Count == 0 && VisualCanvasList.Count == 0 && TouchDownPointsList.Count == 0)
                 {
                     StrokeVisualList.Clear();
                     VisualCanvasList.Clear();
                     TouchDownPointsList.Clear();
                 }
```

***

### 📄 Ink Canvas/MainWindow_cs/MW_PPT.cs

**⚠️ Potential issue | 🟠 Major (Lines 109-110, 200-214)**
Guard `IsShowingRestoreHiddenSlidesWindow` with try/finally to avoid permanent lockout.
If `ShowDialog()` throws after Line [202], the flag may never reset, and Line [109] will block all future checks indefinitely.

**Suggested fix:**
```diff
                         if (isHaveHiddenSlide && !IsShowingRestoreHiddenSlidesWindow)
                         {
-                            IsShowingRestoreHiddenSlidesWindow = true;
-                            new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
-                                () =>
-                                {
-                                    foreach (Slide slide in slides)
-                                    {
-                                        if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue)
-                                        {
-                                            slide.SlideShowTransition.Hidden = Microsoft.Office.Core.MsoTriState.msoFalse;
-                                        }
-                                    }
-                                }).ShowDialog();
+                            IsShowingRestoreHiddenSlidesWindow = true;
+                            try
+                            {
+                                new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
+                                    () =>
+                                    {
+                                        foreach (Slide slide in slides)
+                                        {
+                                            if (slide.SlideShowTransition.Hidden == Microsoft.Office.Core.MsoTriState.msoTrue)
+                                            {
+                                                slide.SlideShowTransition.Hidden = Microsoft.Office.Core.MsoTriState.msoFalse;
+                                            }
+                                        }
+                                    }).ShowDialog();
+                            }
+                            finally
+                            {
+                                IsShowingRestoreHiddenSlidesWindow = false;
+                            }
                         }
```

***

### 📄 Ink Canvas/Helpers/LogHelper.cs

**⚠️ Potential issue | 🟠 Major (Lines 26-33)**
`NewLog(Exception)` silently drops exception logs.
On Line 30, the exception overload is a no-op, so callers can think they logged failures when nothing is persisted.

**🛠️ Proposed fix:**
```diff
         public static void NewLog(Exception ex)
         {
-
+            if (ex == null) return;
+            WriteLogToFile(ex.ToString(), LogType.Error);
         }
```

***

### 📄 Ink Canvas/Helpers/WinTabWindowsChecker.cs

**⚠️ Potential issue | 🟡 Minor (Lines 48-53)**
返回值文档缺少 `windowName == null` 的行为说明。
目前在 Line 67 调用 `Contains(windowName)` 时，`windowName` 为 null 会抛异常，因此“否则返回 false”并不总是成立。建议补充参数约束或显式防御。

**Proposed fix:**
```diff
 /// <summary>
 /// 检查指定标题窗口是否存在。
 /// </summary>
-/// <param name="windowName">目标窗口标题或标题关键字。</param>
+/// <param name="windowName">目标窗口标题或标题关键字，不能为空。</param>
 /// <param name="matchFullName">是否要求完整标题匹配。</param>
 /// <returns>存在返回 <c>true</c>，否则返回 <c>false</c>。</returns>
+/// <exception cref="ArgumentNullException"><paramref name="windowName"/> 为 <c>null</c>。</exception>
 public static bool IsWindowExisted(string windowName, bool matchFullName = true)
 {
+    ArgumentNullException.ThrowIfNull(windowName);
```

***

### 📄 Ink Canvas/MainWindow_cs/MW_Timer.cs

**⚠️ Potential issue | 🟡 Minor (Lines 25-26)**
Docstring overstates behavior in `InitTimers`.
The summary says this method “initializes and starts” timers, but it only configures handlers/intervals and does not start them. Please adjust wording to avoid misleading readers.

***

### 📄 Ink Canvas/MainWindow.xaml.cs

**⚠️ Potential issue | 🟡 Minor (Lines 21-22)**
Constructor doc includes behavior performed later in lifecycle.
The summary says the constructor loads base settings, but settings are loaded in `Window_Loaded` (Line 192). Consider narrowing constructor wording to initialization/binding only.

***

### 📄 update_server/server.py

**⚠️ Potential issue | 🟡 Minor**
Restrict server binding to 127.0.0.1 unless external access is required.
The server binds to `0.0.0.0`, exposing it to all network interfaces. For an update server, consider binding to `127.0.0.1` instead to prevent unintended external access. If external access is needed, document the reason and implement appropriate security controls (firewall rules, authentication, TLS).
