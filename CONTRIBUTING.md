# Contributing Guide

本プロジェクトへようこそ。Azure Functions (.NET 8 / Isolated Worker) を用いたサーバーレス機能群です。開発環境の準備から Issue / Pull Request の基本ルールを以下にまとめます。

---
## 1. プロジェクト前提
- ランタイム: Azure Functions v4 (Isolated Worker)
- ターゲットフレームワーク: .NET 8
- 言語: C# 12
- ローカル実行: Azure Functions Core Tools

---
## 2. 開発環境セットアップ
### 2-1. 必要ツール
| ツール | 目的 | 参考 |
|--------|------|------|
| .NET 8 SDK | ビルド / 実行 | https://dotnet.microsoft.com/ |
| Azure Functions Core Tools v4 | Functions ローカル実行 | https://learn.microsoft.com/azure/azure-functions/functions-run-local |
| Azure CLI | 認証 / デプロイ | https://learn.microsoft.com/cli/azure/ |
| (任意) Visual Studio / VS Code | 開発 | VS Code は Azure Functions 拡張推奨 |

### 2-2. リポジトリ取得
```
git clone <your-fork-or-this-repo-url>
cd function-onelake
```

### 2-3. 依存復元
```
dotnet restore
```

### 2-4. Azure CLI ログイン
```
az login
```
必要に応じてサブスクリプションを選択:
```
az account set --subscription <SUBSCRIPTION_ID_OR_NAME>
```

### 2-5. local.settings.json 作成
`local.settings.json` は Git 管理外です (秘匿情報を含む可能性があるため)。以下のテンプレートをプロジェクトルート (csproj と同階層) に作成してください。
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ONELAKE_DFS_FILE_URL": "https://<workspace>.dfs.fabric.microsoft.com/<path>",
    "SQL_ENDPOINT": "sql.<workspace>.fabric.microsoft.com",
    "SQL_DATABASE": "<database-name>"
  }
}
```

### 2-6. 設定キー一覧
| キー | 説明 | 例 |
|------|------|----|
| ONELAKE_DFS_FILE_URL | OneLake 内の Data Lake / ファイル参照 URL | https://contoso.dfs.fabric.microsoft.com/Files/sample.csv |
| SQL_ENDPOINT | Fabric SQL Endpoint ホスト名 | sql.contoso.fabric.microsoft.com |
| SQL_DATABASE | 接続先データベース名 | AnalyticsDb |

### 2-7. ローカル実行
```
func start
```
または (デバッグ用):
```
dotnet build
func start
```
`http://localhost:7071/api/Function1` をブラウザ / curl で確認。

---
## 3. ブランチ運用 (推奨)
- main: 常にデプロイ可能 / 安定
- feature/*: 機能追加
- fix/*: バグ修正
- chore/*: 設定 / メンテナンス

---
## 4. Issue 作成ルール
Issue テンプレは以下 3 ブロックを基本とします。
1. 背景 (なぜ必要か / 現状の課題)
2. 要件 (箇条書きで機能 / 変更範囲)
3. 受け入れ条件 (テスト観点 / 完了条件 / 想定リクエスト例 など)

追加推奨: スクリーンショット / 参考リンク / 想定非機能要件。

---
## 5. Pull Request ルール
| 項目 | ルール |
|------|--------|
| 単位 | 1 PR = 1 機能 / 1 論理的変更 (肥大化させない) |
| Issue 紐付け | `Closes #<Issue番号>` を PR 説明に含める |
| コミット | 意味のある粒度 / Imperative で英語推奨 (例: Add X, Fix Y) |
| テンプレ | `.github/pull_request_template.md` (存在する場合) を利用 |
| レビュー | 最低 1 名承認 (自動テスト導入後はグリーン必須) |
| 動作確認 | ローカル実行で基本動作を検証し、再現手順を記載 |

### PR 説明推奨構成
- Summary
- Related Issue: `Closes #123`
- Changes
- How to Test
- Screenshots / Logs (必要に応じ)
- Notes / Breaking Changes

---
## 6. コーディング指針 (最小)
- ログ: `ILogger` で構造化メッセージを使用 (`_logger.LogInformation("Processing {File}", fileName);` など)
- 設定値: 環境依存値は環境変数 / local.settings.json Values から取得
- 例外: 予期しない例外は捕捉せず失敗させ、必要なら再スロー前にログ

---
## 7. セキュリティ / シークレット
- API キー / 接続文字列はコミット禁止
- サンプルはダミー値を使用
- `local.settings.json` を誤ってコミットしない (既に .gitignore に含まれているか確認)

---
## 8. よくあるトラブル
| 現象 | 対処 |
|------|------|
| `func start` で Storage 関連エラー | `AzureWebJobsStorage` の値がローカル開発用か確認 (`UseDevelopmentStorage=true`) |
| 実行ポート競合 | `--port 7072` などで変更 |
| 認証失敗 | `az login` 後に `az account show` でサブスクリ確認 |

---
## 9. リリース (将来拡張用)
CI/CD 追加後に: ビルド / テスト / デプロイ手順をここに追記。

---
何か不足があれば Issue を作成してください。Happy Coding!
