using NLog;

namespace NLogArchiveTest;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        // コマンドライン引数で設定ファイルを切り替え
        // 引数なし: 本番用設定 (nlog.config)
        // 引数 "test": テスト用設定 (nlog.test.config)
        var isTestMode = args.Length > 0 && args[0].Equals("test", StringComparison.OrdinalIgnoreCase);

        var configFile = isTestMode ? "nlog.test.config" : "nlog.config";
        LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(configFile);

        Console.WriteLine("===========================================");
        Console.WriteLine(" NLog アーカイブ機能 PoC テスト");
        Console.WriteLine("===========================================");
        Console.WriteLine($"設定ファイル: {configFile}");
        Console.WriteLine($"モード: {(isTestMode ? "テスト（分単位アーカイブ、3ファイル保持）" : "本番（日次アーカイブ、31日保持）")}");
        Console.WriteLine();

        if (isTestMode)
        {
            Console.WriteLine("【テストモード】");
            Console.WriteLine("- 1分ごとにログファイルがアーカイブされます");
            Console.WriteLine("- 最大3ファイルまで保持され、古いファイルは削除されます");
            Console.WriteLine("- 5分間ログを出力してアーカイブ動作を確認します");
            Console.WriteLine();
            Console.WriteLine("Ctrl+C で終了できます");
            Console.WriteLine("-------------------------------------------");

            await RunTestMode();
        }
        else
        {
            Console.WriteLine("【本番モード】");
            Console.WriteLine("- 日次でログファイルがアーカイブされます");
            Console.WriteLine("- maxArchiveDays=31 で31日間保持されます");
            Console.WriteLine();
            Console.WriteLine("サンプルログを出力します...");
            Console.WriteLine("-------------------------------------------");

            RunProductionSample();
        }

        LogManager.Shutdown();
    }

    static async Task RunTestMode()
    {
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromMinutes(5);
        var logInterval = TimeSpan.FromSeconds(2);
        var counter = 0;

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n終了します...");
            LogManager.Shutdown();
            Environment.Exit(0);
        };

        while (DateTime.Now - startTime < duration)
        {
            counter++;

            // 様々なログレベルで出力
            Logger.Debug($"[{counter}] Debug message - 詳細なデバッグ情報");
            Logger.Info($"[{counter}] Info message - 一般的な情報");

            if (counter % 3 == 0)
            {
                Logger.Warn($"[{counter}] Warning message - 警告");
            }

            if (counter % 5 == 0)
            {
                Logger.Error($"[{counter}] Error message - エラー発生");
            }

            // logs/archive フォルダの状態を表示
            ShowArchiveStatus();

            await Task.Delay(logInterval);
        }

        Console.WriteLine("\n-------------------------------------------");
        Console.WriteLine("テスト完了！logs/archive フォルダを確認してください");
        ShowArchiveStatus();
    }

    static void RunProductionSample()
    {
        Logger.Info("アプリケーション開始");
        Logger.Info("設定を読み込みました");
        Logger.Debug("デバッグ情報（本番モードでは出力されません）");
        Logger.Warn("これは警告メッセージです");
        Logger.Error("これはエラーメッセージです");
        Logger.Info("アプリケーション終了");

        Console.WriteLine("\n-------------------------------------------");
        Console.WriteLine("ログ出力完了！");
        Console.WriteLine("logs/app.log ファイルを確認してください");
        Console.WriteLine();
        Console.WriteLine("【本番環境への適用時のポイント】");
        Console.WriteLine("nlog.config の以下の設定が31日後削除を実現します:");
        Console.WriteLine("  - archiveEvery=\"Day\"     : 日次でアーカイブ");
        Console.WriteLine("  - maxArchiveDays=\"31\"    : 31日経過後に自動削除");
    }

    static void ShowArchiveStatus()
    {
        var archiveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "archive");

        if (!Directory.Exists(archiveDir))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] アーカイブフォルダはまだ作成されていません");
            return;
        }

        var files = Directory.GetFiles(archiveDir, "*.log")
                            .Select(f => new FileInfo(f))
                            .OrderBy(f => f.CreationTime)
                            .ToList();

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] アーカイブファイル数: {files.Count}");
        foreach (var file in files)
        {
            Console.WriteLine($"  - {file.Name} ({file.Length} bytes, {file.CreationTime:HH:mm:ss})");
        }
    }
}
