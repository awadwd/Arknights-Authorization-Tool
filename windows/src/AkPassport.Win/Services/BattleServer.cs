// 抽卡联机对战服务器：TcpListener + 手写 WebSocket（文本 JSON 帧）。
// 免管理员、免 urlacl：不使用 HttpListener（http.sys 需要端口 ACL）。
// 默认端口 23515；局域网直连 / 公网端口转发均可。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AkPassport.Win.Services;

public sealed class BattleServer : IDisposable
{
    public const int DefaultPort = 23515;
    private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    /// <summary>
    /// 单帧上限。协议里最大的消息是整副牌库（几十 KB），64 KB 足够；
    /// 超过一律断开 —— 否则一个「声称 2 GB」的帧头就能让服务端按长度开数组。
    /// </summary>
    private const int MaxPayloadBytes = 64 * 1024;

    /// <summary>同一来源 IP 的并发连接上限。开房时端口对局域网/公网开放，
    /// 没有这一层，一个扫描器就能把连接表刷满。</summary>
    private const int MaxConnectionsPerIp = 4;

    /// <summary>昵称长度上限（防止超长字符串被原样广播给对手）。</summary>
    private const int MaxNameLength = 24;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _perIp = new();

    // 房间状态（同一时刻只开一间房）
    private Client? _host;
    private Client? _guest;
    private List<JsonElement>? _deck;   // 房主洗好的牌库（原样 JSON 元素）
    private int _deckIdx;
    private bool _superMix;
    private bool _hostDrew, _guestDrew;
    private JsonElement? _lastHost, _lastGuest;

    private sealed class Client
    {
        public TcpClient Tcp = null!;
        public NetworkStream Stream = null!;
        public string Name = "";
        public string Role = "";   // host | guest
        public bool Alive;
    }

    public string? LanAddress { get; private set; }

    public void Start(int port = DefaultPort)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _listener = new TcpListener(IPAddress.IPv6Any, port);
        _listener.Server.DualMode = true;
        try
        {
            _listener.Start();
        }
        catch (SocketException)
        {
            // IPv6 双栈失败（罕见）退回 IPv4
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }
        LanAddress = ResolveLanIp(port);
        Diag.Log("BattleServer" + $"listening on *:{port}, lan={LanAddress}");
        _ = Task.Run(() => AcceptLoopAsync(ct), ct);
    }

    private static string? ResolveLanIp(int port)
    {
        try
        {
            using var udp = new UdpSocket();
            udp.Connect("223.5.5.5", 53);
            var ip = (udp.LocalEndPoint as IPEndPoint)?.Address;
            if (ip != null && !IPAddress.IsLoopback(ip)) return $"{ip}:{port}";
        }
        catch { }
        return null;
    }

    private sealed class UdpSocket : IDisposable
    {
        private readonly Socket _s = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public void Connect(string host, int port) => _s.Connect(host, port);
        public EndPoint? LocalEndPoint => _s.LocalEndPoint as EndPoint;
        public void Dispose() => _s.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient tcp;
            try { tcp = await _listener!.AcceptTcpClientAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch { break; }
            _ = Task.Run(() => HandleClientAsync(tcp, ct), ct);
        }
    }

    /// <summary>
    /// 连接是否来自本机回环地址。
    /// 用来判定「谁有资格当房主」：只有壳内 WebView（连 127.0.0.1:23515）才算，
    /// 局域网/公网连进来的手机一律只能是挑战者。
    /// </summary>
    private static bool IsLoopback(TcpClient tcp)
    {
        try
        {
            if (tcp.Client.RemoteEndPoint is not IPEndPoint ep) return false;
            var ip = ep.Address;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            return IPAddress.IsLoopback(ip);
        }
        catch
        {
            return false;
        }
    }

    private static string RemoteIp(TcpClient tcp)
    {
        try
        {
            if (tcp.Client.RemoteEndPoint is IPEndPoint ep)
            {
                var ip = ep.Address;
                if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
                return ip.ToString();
            }
        }
        catch { }
        return "?";
    }

    private async Task HandleClientAsync(TcpClient tcp, CancellationToken ct)
    {
        tcp.NoDelay = true;
        var stream = tcp.GetStream();
        var ip = RemoteIp(tcp);
        var counted = false;
        try
        {
            lock (_gate)
            {
                _perIp.TryGetValue(ip, out var existing);
                if (existing >= MaxConnectionsPerIp) return;      // 直接丢，不握手
                _perIp[ip] = existing + 1;
                counted = true;
            }

            var key = await HandshakeAsync(stream, ct);
            if (key == null) { tcp.Close(); return; }

            var client = new Client { Tcp = tcp, Stream = stream, Alive = true };
            // 角色分配【不能靠先到先得】：本机进程监听在 0.0.0.0，手机上「加入房间」
            // 随手填一个局域网 IP 也能连上来；若按「第一个就是房主」分配，手机就会收到
            // role=host，前端显示「房间已创建」——表现成手机也能开房。
            // 规则：
            //   · 只有回环地址（壳内 WebView 连 127.0.0.1）才有资格当房主；
            //   · 远端连接一律只能是挑战者，且必须已有房主在房，否则回 err/no_host 直接拒绝。
            bool fromLoopback = IsLoopback(tcp);
            bool isFull = false, noHost = false;
            lock (_gate)
            {
                if (fromLoopback)
                {
                    if (_host == null || _host.Alive == false) { _host = client; client.Role = "host"; }
                    else if (_guest == null) { _guest = client; client.Role = "guest"; }
                }
                else
                {
                    if (_host != null && _host.Alive && _guest == null) { _guest = client; client.Role = "guest"; }
                    else if (_host == null || _host.Alive == false) { noHost = true; }
                }
                isFull = client.Role == "";
            }
            if (noHost)
            {
                await SendAsync(stream, JsonSerializer.Serialize(new
                {
                    t = "err",
                    code = "no_host",
                    msg = "房主还没有开房，请让对方先点「创建房间」"
                }), ct);
                // 先半关（发 FIN）再等一小会儿：直接 Close 会让 Windows 发 RST，
                // 客户端可能连 err 帧都读不到，只看到一个「连接被重置」。
                try { tcp.Client.Shutdown(SocketShutdown.Send); } catch { }
                await Task.Delay(400, ct);
                return;
            }
            if (isFull)
            {
                await SendAsync(stream, JsonSerializer.Serialize(new { t = "full" }), ct);
                try { tcp.Client.Shutdown(SocketShutdown.Send); } catch { }
                await Task.Delay(400, ct);
                return;
            }
            var lan = LanAddress ?? $"127.0.0.1:{DefaultPort}";
            await SendAsync(stream, JsonSerializer.Serialize(new { t = "welcome", role = client.Role, lan }), ct);

            await ReadLoopAsync(client, ct);
        }
        catch { }
        finally
        {
            if (counted)
            {
                lock (_gate)
                {
                    if (_perIp.TryGetValue(ip, out var left))
                    {
                        if (left <= 1) _perIp.Remove(ip);
                        else _perIp[ip] = left - 1;
                    }
                }
            }
            OnClientLeft(tcp);
            try { tcp.Close(); } catch { }
        }
    }

    private static async Task<string?> HandshakeAsync(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[8192];
        var sb = new StringBuilder();
        while (!sb.ToString().Contains("\r\n\r\n"))
        {
            int n = await stream.ReadAsync(buf, ct);
            if (n <= 0) return null;
            sb.Append(Encoding.ASCII.GetString(buf, 0, n));
            if (sb.Length > 16384) return null;
        }
        var headers = sb.ToString();
        if (!headers.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase)) return null;
        string? wsKey = null;
        foreach (var line in headers.Split("\r\n"))
        {
            var idx = line.IndexOf(':');
            if (idx > 0 && line[..idx].Trim().Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                wsKey = line[(idx + 1)..].Trim();
        }
        if (wsKey == null) return null;
        using var sha = SHA1.Create();
        var accept = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(wsKey + WsGuid)));
        var resp = "HTTP/1.1 101 Switching Protocols\r\n" +
                   "Upgrade: websocket\r\nConnection: Upgrade\r\n" +
                   $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(resp), ct);
        return wsKey;
    }

    private async Task ReadLoopAsync(Client c, CancellationToken ct)
    {
        var buf = new byte[65536];
        while (!ct.IsCancellationRequested && c.Alive)
        {
            int n;
            try { n = await c.Stream.ReadAsync(buf, ct); }
            catch { break; }
            if (n <= 0) break;
            int pos = 0;
            while (pos + 2 <= n)
            {
                bool fin = (buf[pos] & 0x80) != 0;
                int opcode = buf[pos] & 0x0F;
                bool masked = (buf[pos + 1] & 0x80) != 0;
                int len = buf[pos + 1] & 0x7F;
                int hdr = 2;
                if (len == 126)
                {
                    if (pos + 4 > n) break;
                    len = (buf[pos + 2] << 8) | buf[pos + 3];
                    hdr = 4;
                }
                else if (len == 127)
                {
                    if (pos + 10 > n) break;
                    // 用 long 拼 64 位长度：按 int 截断会得到负数，
                    // new byte[负数] 直接抛异常，等于给了一个断开连接的开关
                    long big = ((long)buf[pos + 2] << 56) | ((long)buf[pos + 3] << 48)
                             | ((long)buf[pos + 4] << 40) | ((long)buf[pos + 5] << 32)
                             | ((long)buf[pos + 6] << 24) | ((long)buf[pos + 7] << 16)
                             | ((long)buf[pos + 8] << 8) | buf[pos + 9];
                    if (big < 0 || big > MaxPayloadBytes) { c.Alive = false; break; }
                    len = (int)big;
                    hdr = 10;
                }
                if (len > MaxPayloadBytes) { c.Alive = false; break; }
                int maskOff = pos + hdr;
                int dataOff = maskOff + (masked ? 4 : 0);
                if (dataOff + len > n) break;
                var payload = new byte[len];
                Array.Copy(buf, dataOff, payload, 0, len);
                if (masked)
                    for (int i = 0; i < len; i++) payload[i] ^= buf[maskOff + (i & 3)];
                pos = dataOff + len;

                if (opcode == 0x8) { c.Alive = false; break; }              // close
                else if (opcode == 0x9) { await SendFrameAsync(c.Stream, 0xA, payload, ct); } // ping→pong
                else if (opcode == 0x1 && fin)
                {
                    var text = Encoding.UTF8.GetString(payload);
                    try { await HandleMessageAsync(c, text, ct); }
                    catch (Exception ex) { Diag.Log("BattleServer" + "msg err: " + ex.Message); }
                }
            }
        }
        c.Alive = false;
    }

    private async Task HandleMessageAsync(Client c, string text, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var t = root.GetProperty("t").GetString();
        bool broadcastStart = false, reset = false;
        List<Client>? targets = null;
        string? outgoing = null;

        lock (_gate)
        {
            switch (t)
            {
                case "hello":
                    var rawName = root.TryGetProperty("name", out var nm) ? nm.GetString() ?? "玩家" : "玩家";
                    rawName = rawName.Trim();
                    c.Name = rawName.Length == 0
                        ? "玩家"
                        : (rawName.Length > MaxNameLength ? rawName[..MaxNameLength] : rawName);
                    var peer = c.Role == "host" ? _guest : _host;
                    if (peer != null && peer.Alive)
                        _ = SendAsync(peer.Stream, JsonSerializer.Serialize(new { t = "peer", name = c.Name, role = c.Role }), CancellationToken.None);
                    break;

                case "cfg": // 房主下发牌库
                    if (c.Role != "host") return;
                    _superMix = root.TryGetProperty("super", out var sp) && sp.GetBoolean();
                    // 深拷贝：JsonElement 依附于 using 的 JsonDocument，文档释放后视图失效
                    var cardsCopy = new List<JsonElement>();
                    foreach (var cardEl in root.GetProperty("cards").EnumerateArray())
                    {
                        using var tmp = JsonDocument.Parse(cardEl.GetRawText());
                        cardsCopy.Add(tmp.RootElement.Clone());
                    }
                    _deck = cardsCopy;
                    _deckIdx = 0; _hostDrew = false; _guestDrew = false;
                    _lastHost = null; _lastGuest = null;
                    broadcastStart = true;
                    break;

                case "draw":
                    {
                        if (_deck == null || _deckIdx >= _deck.Count) return;
                        bool byHost = c.Role == "host";
                        // 一人一回合只能抽一张（防止绕过 UI 连点）
                        if (byHost ? _hostDrew : _guestDrew) return;
                        var card = _deck[_deckIdx++];
                        if (byHost) { _hostDrew = true; _lastHost = card.Clone(); }
                        else { _guestDrew = true; _lastGuest = card.Clone(); }
                        outgoing = JsonSerializer.Serialize(new
                        {
                            t = "drew",
                            by = c.Role,
                            card = card,
                            remaining = _deck.Count - _deckIdx,
                            hostDrew = _hostDrew,
                            guestDrew = _guestDrew
                        });
                        targets = new List<Client> { _host!, _guest! };
                        // 双方都抽完这一轮即复位，允许下一轮继续抽（否则第二轮 draw 会被上面的守卫 return 卡死）
                        if (_hostDrew && _guestDrew) { _hostDrew = false; _guestDrew = false; }
                    }
                    break;

                case "reset":
                    // 只有房主能重开牌堆。之前谁都能发，等于房间里任何一个人
                    // （包括误连进来的扫描器）都能把正在打的对局重置掉。
                    if (c.Role != "host") return;
                    _deckIdx = 0; _hostDrew = false; _guestDrew = false;
                    _lastHost = null; _lastGuest = null;
                    reset = true;
                    break;
            }
        }

        if (broadcastStart && _guest != null && _guest.Alive)
        {
            var msg = JsonSerializer.Serialize(new
            {
                t = "start",
                super = _superMix,
                total = _deck!.Count,
                host = _host?.Name ?? "房主",
                guest = _guest.Name
            });
            var list = new List<Client> { _host!, _guest! };
            foreach (var x in list) if (x.Alive) await SendAsync(x.Stream, msg, CancellationToken.None);
        }
        else if (broadcastStart)
        {
            // 对方未加入也允许（不会发生：满员才开始）；忽略
        }
        if (outgoing != null && targets != null)
            foreach (var x in targets) if (x.Alive) await SendAsync(x.Stream, outgoing, CancellationToken.None);
        if (reset)
        {
            var msg = JsonSerializer.Serialize(new { t = "reset" });
            foreach (var x in new[] { _host, _guest }) if (x != null && x.Alive) await SendAsync(x.Stream, msg, CancellationToken.None);
        }
    }

    private void OnClientLeft(TcpClient tcp)
    {
        Client? left = null, other = null;
        bool wasMember = false;
        lock (_gate)
        {
            if (_host != null && ReferenceEquals(_host.Tcp, tcp)) { wasMember = true; left = _host; _host = null; other = _guest; _guest = null; }
            else if (_guest != null && ReferenceEquals(_guest.Tcp, tcp)) { wasMember = true; left = _guest; _guest = null; other = _host; }
            // 只有真正的房间成员离开才解散对局。
            // 被拒绝的远端（err/no_host）和被满员踢掉的连接也会走到这里，
            // 若不判 wasMember，随便来一个连不上的请求就会把房主正在打的牌局清空。
            if (wasMember)
            {
                _deck = null; _deckIdx = 0; _hostDrew = false; _guestDrew = false;
                _lastHost = null; _lastGuest = null;
            }
        }
        if (other != null && other.Alive)
            _ = SendAsync(other.Stream, JsonSerializer.Serialize(new { t = "peer_left" }), CancellationToken.None);
        if (left != null) Diag.Log("BattleServer" + $"left role={left.Role}");
    }

    private static async Task SendAsync(NetworkStream s, string text, CancellationToken ct)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        await SendFrameAsync(s, 0x1, payload, ct);
    }

    private static async Task SendFrameAsync(NetworkStream s, int opcode, byte[] payload, CancellationToken ct)
    {
        byte[] frame;
        if (payload.Length < 126)
        {
            frame = new byte[2 + payload.Length];
            frame[0] = (byte)(0x80 | opcode);
            frame[1] = (byte)payload.Length;
            payload.CopyTo(frame, 2);
        }
        else
        {
            frame = new byte[4 + payload.Length];
            frame[0] = (byte)(0x80 | opcode);
            frame[1] = 126;
            frame[2] = (byte)(payload.Length >> 8);
            frame[3] = (byte)payload.Length;
            payload.CopyTo(frame, 4);
        }
        await s.WriteAsync(frame, ct);
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _cts?.Dispose(); _cts = null; _listener = null;
        lock (_gate)
        {
            _perIp.Clear();
            _host = null;
            _guest = null;
            _deck = null;
            _deckIdx = 0;
            _hostDrew = false;
            _guestDrew = false;
            _lastHost = null;
            _lastGuest = null;
        }
    }

    /// <summary>当前房间是否有人（供外壳判断空闲后是否该关掉监听）。</summary>
    public bool HasClients
    {
        get
        {
            lock (_gate) return _host != null || _guest != null;
        }
    }

    public void Dispose() => Stop();
}
