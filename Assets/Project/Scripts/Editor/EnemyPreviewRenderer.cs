using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Renders the local preview offscreen without installing a Unity package or starting a game scene.
internal sealed class EnemyPreviewRenderer : IDisposable
{
    private readonly string previewPath;
    private readonly string profile;
    private readonly string bridgeName;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ClientWebSocket socket = new();
    private readonly byte[] receiveBuffer = new byte[32 * 1024];
    private Process browser;
    private int nextId;
    private string session;

    public EnemyPreviewRenderer(string projectRoot, string folder, string bridgeName)
    {
        this.bridgeName = bridgeName;
        previewPath = Path.Combine(projectRoot, "Previews", folder, "index.html");
        profile = Path.Combine(projectRoot, "Temp", folder + "Preview", Guid.NewGuid().ToString("N"));
    }

    public async Task<T> StartAsync<T>()
    {
        if (!File.Exists(previewPath))
            throw new FileNotFoundException("Thiếu Previews/EnemyWater/index.html trong dự án.");
        string executable = FindBrowser();
        Directory.CreateDirectory(profile);
        var page = new Uri(previewPath);
        browser = Process.Start(new ProcessStartInfo(executable)
        {
            Arguments = "--headless=new --disable-gpu --disable-breakpad --disable-extensions --disable-background-networking " +
                "--disable-sync --disable-component-update " +
                "--no-first-run --no-default-browser-check --allow-file-access-from-files " +
                $"--remote-debugging-address=127.0.0.1 --remote-debugging-port=0 --user-data-dir=\"{profile}\" \"{page.AbsoluteUri}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            string portFile = Path.Combine(profile, "DevToolsActivePort");
            string[] endpoint;
            while ((endpoint = ReadEndpoint(portFile)).Length < 2)
            {
                if (browser == null || browser.HasExited)
                    throw new InvalidOperationException("Edge/Chrome đã thoát trước khi tải được preview.");
                await Task.Delay(80, timeout.Token);
            }
            var address = new Uri($"ws://127.0.0.1:{endpoint[0]}{endpoint[1]}");
            await socket.ConnectAsync(address, timeout.Token);

            Target pageTarget;
            while ((pageTarget = await FindPageAsync(page, timeout.Token)) == null)
                await Task.Delay(80, timeout.Token);
            string target = JsonUtility.ToJson(new AttachParameters { targetId = pageTarget.targetId });
            session = (await SendAsync("Target.attachToTarget", target, timeout.Token)).result.sessionId;

            while (await EvaluateStringAsync($"document.readyState === 'complete' && window.{bridgeName}?.ready ? 'ready' : ''", timeout.Token) != "ready")
                await Task.Delay(100, timeout.Token);
            return Decode<T>(await EvaluateStringAsync($"window.{bridgeName}.metadata()", timeout.Token));
        }
        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
        {
            throw new TimeoutException("Đã hết thời gian tải preview hệ Thủy. Hãy kiểm tra các tệp sprite trong dự án.");
        }
    }

    public async Task<T> EvaluateAsync<T>(string expression)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            return Decode<T>(await EvaluateStringAsync(expression, timeout.Token));
        }
        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
        {
            throw new TimeoutException("Bộ dựng hình hệ Thủy đã ngừng phản hồi.");
        }
    }

    private static T Decode<T>(string json) => JsonUtility.FromJson<T>(json);

    private async Task<Target> FindPageAsync(Uri page, CancellationToken token)
    {
        var reply = await SendAsync("Target.getTargets", "{}", token);
        return Array.Find(reply.result.targetInfos, target => target.type == "page" &&
            Uri.TryCreate(target.url, UriKind.Absolute, out var url) && url == page);
    }

    private async Task<string> EvaluateStringAsync(string expression, CancellationToken token)
    {
        string parameters = JsonUtility.ToJson(new EvaluationParameters { expression = expression });
        var reply = await SendAsync("Runtime.evaluate", parameters, token, session);
        if (!string.IsNullOrEmpty(reply.result.exceptionDetails?.text))
            throw new InvalidOperationException(reply.result.result?.description ?? reply.result.exceptionDetails.text);
        return reply.result.result?.value ?? "";
    }

    private async Task<Reply> SendAsync(string method, string parameters, CancellationToken token, string sessionId = null)
    {
        int id = ++nextId;
        string targetScope = sessionId == null ? "" : $",\"sessionId\":\"{sessionId}\"";
        string message = $"{{\"id\":{id.ToString(CultureInfo.InvariantCulture)},\"method\":\"{method}\",\"params\":{parameters}{targetScope}}}";
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, token);
        using var payload = new MemoryStream();
        while (true)
        {
            var frame = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), token);
            if (frame.MessageType == WebSocketMessageType.Close)
                throw new IOException("Trình dựng hình hệ Thủy đã đóng kết nối.");
            payload.Write(receiveBuffer, 0, frame.Count);
            if (!frame.EndOfMessage) continue;
            var reply = JsonUtility.FromJson<Reply>(Encoding.UTF8.GetString(payload.GetBuffer(), 0, (int)payload.Length));
            if (reply.id != id)
            {
                payload.SetLength(0);
                continue;
            }
            if (!string.IsNullOrEmpty(reply.error?.message))
                throw new InvalidOperationException(reply.error.message);
            return reply;
        }
    }

    private static string[] ReadEndpoint(string path)
    {
        if (!File.Exists(path)) return Array.Empty<string>();
        try { return File.ReadAllLines(path); }
        catch (IOException) { return Array.Empty<string>(); }
    }

    private static string FindBrowser()
    {
        var folders = new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.LocalApplicationData };
        var executables = new[] { "Microsoft/Edge/Application/msedge.exe", "Google/Chrome/Application/chrome.exe" };
        foreach (string executable in executables)
            foreach (var folder in folders)
            {
                string path = Path.Combine(Environment.GetFolderPath(folder), executable);
                if (File.Exists(path)) return path;
            }
        throw new FileNotFoundException("Cần Microsoft Edge hoặc Google Chrome trên Windows để dựng preview hệ Thủy.");
    }

    public void Dispose()
    {
        lifetime.Cancel();
        socket.Dispose();
        lifetime.Dispose();
        if (browser != null)
        {
            try { if (!browser.HasExited) browser.Kill(); }
            catch (InvalidOperationException) { }
            finally { browser.Dispose(); }
        }
        // Windows may keep profile files open briefly while Edge shuts down.
        _ = DeleteProfileAsync(profile);
    }

    private static async Task DeleteProfileAsync(string path)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
                return;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            await Task.Delay(250).ConfigureAwait(false);
        }
    }

    [Serializable] private sealed class Reply
    {
        public int id = 0;
        public ReplyBody result = null;
        public ErrorBody error = null;
    }

    [Serializable] private sealed class ReplyBody
    {
        public Target[] targetInfos = null;
        public string sessionId = null;
        public EvaluationResult result = null;
        public ErrorBody exceptionDetails = null;
    }

    [Serializable] private sealed class Target
    {
        public string targetId = null;
        public string url = null;
        public string type = null;
    }

    [Serializable] private sealed class AttachParameters
    {
        public string targetId;
        public bool flatten = true;
    }

    [Serializable] private sealed class EvaluationParameters
    {
        public string expression;
        public bool returnByValue = true;
    }

    [Serializable] private sealed class EvaluationResult
    {
        public string value = null;
        public string description = null;
    }

    [Serializable] private sealed class ErrorBody
    {
        public string message = null;
        public string text = null;
    }
}
