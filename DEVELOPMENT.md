# Development Guide 🛠

このドキュメントは **KoReadingABook** の開発者向けガイドです。
ビルド方法、技術スタック、アーキテクチャについて記述しています。

## 技術スタック 📚

| 項目 | 内容 |
| --- | --- |
| **Framework** | .NET 10.0 |
| **UI Framework** | WPF (Windows Presentation Foundation) |
| **Language** | C# 12 |
| **OS API** | Win32 API (P/Invoke) |

## プロジェクト構成 📂

- **MainWindow.xaml / .cs**
  - アプリケーションのメインUIとタイマー制御ロジック。
  - `DispatcherTimer` を2つ使用（ウィンドウ切替用、マウス移動用）。
- **WindowService.cs**
  - ウィンドウ操作の中核ロジック。
  - `EnumWindows` による列挙、フィルタリング（除外リスト、サイズチェック）。
  - `AttachThreadInput` と `SetWindowPos` を組み合わせた強力な最前面化処理（Foreground Lock Bypass）。
- **MouseService.cs**
  - マウス操作ロジック。
  - 三角関数による円運動座標の計算。
  - ユーザーのマウス介入検知（前回位置との偏差測定）。
- **NativeMethods.cs**
  - `user32.dll`, `kernel32.dll` などの Win32 API 定義集。

## ビルド方法 🏗

### 必要要件
- .NET 10.0 SDK
- Visual Studio 2022 (または VS Code + C# Dev Kit)

### コマンドラインでの実行・ビルド

```powershell
# 依存関係の復元
dotnet restore

# デバッグ実行
dotnet run

# リリースビルド（単一ファイル実行可能形式）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 実装のポイント 💡

### 最前面化の信頼性向上
Windowsの仕様により、バックグラウンドプロセスが最前面を奪うことは制限されています（Foreground Lock）。
本アプリでは以下の手法を組み合わせてこれを回避し、確実にウィンドウを前面に出しています：
1. **Altキー送信**: `keybd_event` でAltキーの入力をシミュレートし、OSのアイドル判定を解除。
2. **スレッド入力アタッチ**: `AttachThreadInput` で対象ウィンドウのスレッドと結合。
3. **TopMostトグル**: 一瞬だけ `HWND_TOPMOST` に設定してから解除することでZオーダーを強制変更。

### マウス介入の検知
グローバルフック（LowLevelMouseProc）は使用せず、定期的なポーリングで実装しています。
- プログラムが設定した座標 (`SetCursorPos`) と、次回タイマー時の現在座標 (`GetCursorPos`) を比較。
- 偏差が一定以上（50px）あれば「ユーザーが動かした」と判定し、10秒間のクールダウンに入ります。

## 除外リストの管理
`WindowService.cs` 内の `IsTargetWindow` メソッドで制御しています。
- **システム除外**: `Progman`, `Shell_TrayWnd`
- **アプリ除外**: `KoReadingABook` (自身), `設定`, `タスク マネージャー`
- **最小サイズ**: 幅/高さが 10px 未満のウィンドウは無視

---
*Maintained by Development Team*
