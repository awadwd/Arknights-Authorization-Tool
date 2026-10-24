// 方舟通行证谷子查询工具 —— 联机对战大厅服务端（开源自建）
//
// 定位：开源、由玩家自行搭建。不做官方服务端、不收集任何账号信息。
// 部署：跑在任意一台机器（自己电脑 / 局域网主机 / 云服务器），
//       客户端连这台机器的 IP:端口 即可。公网联机需在路由器/云做端口转发。
//
// 协议：手写 WebSocket（文本 JSON 帧），免管理员、免 urlacl（不用 http.sys）。
// 默认端口 23516（与壳内置直连服务器 23515 区分）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BattleLobbyServer;

public sealed class LobbyServer : IDisposable
{
    public const int DefaultPort = 23516;
    private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly object _gate = new();

    // 大厅里的所有在线客户端（已 hello 注册的）
    private readonly Dictionary<string, Client> _online = new();   // id -> client
    private readonly Dictionary<string, Room> _rooms = new();      // roomId -> room
    private int _idSeq;

    public string? LanAddress { get; private set; }

    public void Start(int port = DefaultPort)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _listener = new TcpListener(IPAddress.IPv6Any, port);
        _listener.Server.DualMode = true;
        try { _listener.Start(); }
        catch (SocketException)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }
        LanAddress = ResolveLanIp(port);
        Log($"服务已启动：*:{port}" + (LanAddress != null ? $"，局域网地址 {LanAddress}" : ""));
        Log("这是开源自建服务端。客户端填本机 IP:端口 连接；公网联机请做端口转发。");
        _ = Task.Run(() => AcceptLoopAsync(ct), ct);
    }

    private static string? ResolveLanIp(int port)
    {
        try
        {
            using var udp = new UdpProbe();
            udp.Connect("223.5.5.5", 53);
            var ip = (udp.EndPoint as IPEndPoint)?.Address;
            if (ip != null && !IPAddress.IsLoopback(ip)) return $"{ip}:{port}";
        }
        catch { }
        return null;
    }

    private sealed class UdpProbe : IDisposable
    {
        private readonly Socket _s = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public void Connect(string h, int p) => _s.Connect(h, p);
        public EndPoint? EndPoint => _s.LocalEndPoint as EndPoint;
        public void Dispose() => _s.Dispose();
    }

    // ===== 客户端 =====
    private sealed class Client
    {
        public string Id = "";
        public string Name = "";
        public TcpClient Tcp = null!;
        public NetworkStream Stream = null!;
        public string? RoomId = null;
        public bool Alive = true;
        public DateTime Joined = DateTime.UtcNow;
    }

    // ===== 房间 =====
    private sealed class Room
    {
        public string Id = "";
        public string HostId = "";
        public string? GuestId = null;
        // 对战状态（房主发牌后开始）
        public List<JsonElement>? Deck;
        public int DeckIdx;
        public bool SuperMix;
        public bool HostDrew, GuestDrew;
        public JsonElement? LastHost, LastGuest;
        public bool Started;

        public Client? Host;
        public Client? Guest;
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

    private async Task HandleClientAsync(TcpClient tcp, CancellationToken ct)
    {
        tcp.NoDelay = true;
        var stream = tcp.GetStream();
        var client = new Client { Tcp = tcp, Stream = stream };
        try
        {
            var ok = await HandshakeAsync(stream, ct);
            if (!ok) { tcp.Close(); return; }
            await ReadLoopAsync(client, ct);
        }
        catch { }
        finally
        {
            OnClientLeft(client);
            try { tcp.Close(); } catch { }
        }
    }

    // ===== WebSocket 握手 / 帧（与壳内置服务器同逻辑）=====
    private static async Task<bool> HandshakeAsync(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[8192];
        var sb = new StringBuilder();
        while (!sb.ToString().Contains("\r\n\r\n"))
        {
            int n = await stream.ReadAsync(buf, ct);
            if (n <= 0) return false;
            sb.Append(Encoding.ASCII.GetString(buf, 0, n));
            if (sb.Length > 16384) return false;
        }
        var headers = sb.ToString();
        if (!headers.Contains("Upgrade: websocket", StringComparison.OrdinalIgnoreCase)) return false;
        string? wsKey = null;
        foreach (var line in headers.Split("\r\n"))
        {
            var idx = line.IndexOf(':');
            if (idx > 0 && line[..idx].Trim().Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                wsKey = line[(idx + 1)..].Trim();
        }
        if (wsKey == null) return false;
        using var sha = SHA1.Create();
        var accept = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(wsKey + WsGuid)));
        var resp = "HTTP/1.1 101 Switching Protocols\r\n" +
                   "Upgrade: websocket\r\nConnection: Upgrade\r\n" +
                   $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(resp), ct);
        return true;
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
                if (len == 126) { if (pos + 4 > n) break; len = (buf[pos + 2] << 8) | buf[pos + 3]; hdr = 4; }
                else if (len == 127) { if (pos + 10 > n) break; len = (int)((long)buf[pos + 6] << 24 | buf[pos + 7] << 16 | buf[pos + 8] << 8 | buf[pos + 9]); hdr = 10; }
                int maskOff = pos + hdr;
                int dataOff = maskOff + (masked ? 4 : 0);
                if (dataOff + len > n) break;
                var payload = new byte[len];
                Array.Copy(buf, dataOff, payload, 0, len);
                if (masked) for (int i = 0; i < len; i++) payload[i] ^= buf[maskOff + (i & 3)];
                pos = dataOff + len;

                if (opcode == 0x8) { c.Alive = false; break; }
                else if (opcode == 0x9) { await SendFrameAsync(c.Stream, 0xA, payload, ct); }
                else if (opcode == 0x1 && fin)
                {
                    var text = Encoding.UTF8.GetString(payload);
                    try { await HandleMessageAsync(c, text, ct); }
                    catch (Exception ex) { Log("消息处理错误: " + ex.Message); }
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

        switch (t)
        {
            case "hello":
                await OnHello(c, root);
                break;
            case "list":
                await Send(c, new { t = "list", players = SnapshotOnline(c) });
                break;
            case "invite":
                await OnInvite(c, root);
                break;
            case "invite_accept":
                await OnInviteAccept(c, root);
                break;
            case "invite_decline":
                await OnInviteDecline(c, root);
                break;
            case "invite_cancel":
                await OnInviteCancel(c, root);
                break;
            case "kick":
                await OnKick(c, root);
                break;
            // ---- 房间内对战（复用直连协议）----
            case "cfg":
                await OnCfg(c, root);
                break;
            case "draw":
                await OnDraw(c);
                break;
            case "reset":
                await OnReset(c);
                break;
            case "leave_room":
                await OnLeaveRoom(c);
                break;
        }
    }

    // ===== 大厅逻辑 =====
    private async Task OnHello(Client c, JsonElement root)
    {
        var name = root.TryGetProperty("name", out var nm) ? (nm.GetString() ?? "").Trim() : "";
        if (name.Length == 0) name = "玩家";
        if (name.Length > 16) name = name[..16];
        lock (_gate)
        {
            c.Name = name;
            c.Id = "u" + (++_idSeq);
            _online[c.Id] = c;
        }
        Log($"玩家加入：{name} ({c.Id})，在线 {_online.Count} 人");
        await Send(c, new { t = "welcome", id = c.Id, name = c.Name, lan = LanAddress ?? $"127.0.0.1:{DefaultPort}" });
        await Broadcast(new { t = "list", players = SnapshotOnline(null) });
    }

    private List<PlayerInfo> SnapshotOnline(Client? except)
    {
        lock (_gate)
        {
            return _online.Values
                .Where(x => except == null || x.Id != except.Id)
                .OrderBy(x => x.Joined)
                .Select(x => new PlayerInfo { Id = x.Id, Name = x.Name, Busy = x.RoomId != null })
                .ToList();
        }
    }

    private sealed class PlayerInfo
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("busy")] public bool Busy { get; set; }
    }

    private async Task OnInvite(Client c, JsonElement root)
    {
        var targetId = root.TryGetProperty("to", out var to) ? to.GetString() : null;
        Client? target;
        lock (_gate)
        {
            if (targetId == null || !_online.TryGetValue(targetId, out target)) target = null;
            if (target != null && target.RoomId != null) target = null; // 对方已在房间
            if (c.RoomId != null) target = null;                        // 自己已在房间
        }
        if (target == null)
        {
            await Send(c, new { t = "invite_fail", reason = "对方不在线或正忙" });
            return;
        }
        await Send(target, new { t = "invite", from = c.Id, fromName = c.Name });
    }

    private async Task OnInviteAccept(Client c, JsonElement root)
    {
        var fromId = root.TryGetProperty("from", out var f) ? f.GetString() : null;
        Client? host;
        lock (_gate)
        {
            if (fromId == null || !_online.TryGetValue(fromId, out host)) host = null;
            if (host != null && host.RoomId != null) host = null;
            if (c.RoomId != null) host = null;
        }
        if (host == null)
        {
            await Send(c, new { t = "invite_fail", reason = "邀请已失效" });
            return;
        }
        var room = CreateRoom(host, c);
        await Send(host, new { t = "room", roomId = room.Id, role = "host", peerName = c.Name });
        await Send(c, new { t = "room", roomId = room.Id, role = "guest", peerName = host.Name });
        await Broadcast(new { t = "list", players = SnapshotOnline(null) });
        Log($"房间 {room.Id}：{host.Name} vs {c.Name}");
    }

    private async Task OnInviteDecline(Client c, JsonElement root)
    {
        var fromId = root.TryGetProperty("from", out var f) ? f.GetString() : null;
        Client? host = null;
        lock (_gate) { if (fromId != null) _online.TryGetValue(fromId, out host); }
        if (host != null)
            await Send(host, new { t = "invite_declined", fromName = c.Name });
    }

    private async Task OnInviteCancel(Client c, JsonElement root)
    {
        var toId = root.TryGetProperty("to", out var to) ? to.GetString() : null;
        Client? target = null;
        lock (_gate) { if (toId != null) _online.TryGetValue(toId, out target); }
        if (target != null)
            await Send(target, new { t = "invite_cancelled", fromName = c.Name });
    }

    private async Task OnKick(Client c, JsonElement root)
    {
        var room = GetRoomOf(c);
        if (room == null || room.HostId != c.Id) return;
        var guest = room.Guest;
        if (guest == null) return;
        await Send(guest, new { t = "kicked" });
        await Send(c, new { t = "kick_ok" });
        RemoveRoom(room);
        await Broadcast(new { t = "list", players = SnapshotOnline(null) });
        Log($"房间 {room.Id}：房主踢出了 {guest.Name}");
    }

    private Room CreateRoom(Client host, Client guest)
    {
        lock (_gate)
        {
            var room = new Room
            {
                Id = "r" + (++_idSeq),
                HostId = host.Id,
                GuestId = guest.Id,
                Host = host,
                Guest = guest
            };
            _rooms[room.Id] = room;
            host.RoomId = room.Id;
            guest.RoomId = room.Id;
            return room;
        }
    }

    private void RemoveRoom(Room room)
    {
        lock (_gate)
        {
            _rooms.Remove(room.Id);
            if (room.Host != null) room.Host.RoomId = null;
            if (room.Guest != null) room.Guest.RoomId = null;
        }
    }

    private Room? GetRoomOf(Client c) => c.RoomId != null && _rooms.TryGetValue(c.RoomId, out var r) ? r : null;

    private async Task OnLeaveRoom(Client c)
    {
        var room = GetRoomOf(c);
        if (room == null) return;
        var other = room.HostId == c.Id ? room.Guest : room.Host;
        RemoveRoom(room);
        if (other != null && other.Alive)
            await Send(other, new { t = "peer_left" });
        await Send(c, new { t = "left_room" });
        await Broadcast(new { t = "list", players = SnapshotOnline(null) });
        Log($"房间 {room.Id}：{c.Name} 离开");
    }

    // ===== 房间内对战（复用直连协议语义）=====
    private async Task OnCfg(Client c, JsonElement root)
    {
        var room = GetRoomOf(c);
        if (room == null || room.HostId != c.Id) return;
        room.SuperMix = root.TryGetProperty("super", out var sp) && sp.GetBoolean();
        var cardsCopy = new List<JsonElement>();
        foreach (var cardEl in root.GetProperty("cards").EnumerateArray())
        {
            using var tmp = JsonDocument.Parse(cardEl.GetRawText());
            cardsCopy.Add(tmp.RootElement.Clone());
        }
        room.Deck = cardsCopy;
        room.DeckIdx = 0;
        room.HostDrew = false; room.GuestDrew = false;
        room.LastHost = null; room.LastGuest = null;
        room.Started = true;
        var msg = new
        {
            t = "start",
            super = room.SuperMix,
            total = room.Deck.Count,
            host = room.Host?.Name ?? "房主",
            guest = room.Guest?.Name ?? "挑战者"
        };
        foreach (var x in new[] { room.Host, room.Guest }) if (x != null && x.Alive) await Send(x, msg);
    }

    private async Task OnDraw(Client c)
    {
        var room = GetRoomOf(c);
        if (room == null || room.Deck == null || !room.Started) return;
        bool byHost = c.Id == room.HostId;
        if (room.DeckIdx >= room.Deck.Count) return;
        if (byHost ? room.HostDrew : room.GuestDrew) return; // 一回合一人一张
        var card = room.Deck[room.DeckIdx++];
        if (byHost) { room.HostDrew = true; room.LastHost = card.Clone(); }
        else { room.GuestDrew = true; room.LastGuest = card.Clone(); }
        var msg = new
        {
            t = "drew",
            by = byHost ? "host" : "guest",
            card = card,
            remaining = room.Deck.Count - room.DeckIdx,
            hostDrew = room.HostDrew,
            guestDrew = room.GuestDrew
        };
        foreach (var x in new[] { room.Host, room.Guest }) if (x != null && x.Alive) await Send(x, msg);
    }

    private async Task OnReset(Client c)
    {
        var room = GetRoomOf(c);
        if (room == null) return;
        room.DeckIdx = 0; room.HostDrew = false; room.GuestDrew = false;
        room.LastHost = null; room.LastGuest = null;
        var msg = new { t = "reset" };
        foreach (var x in new[] { room.Host, room.Guest }) if (x != null && x.Alive) await Send(x, msg);
    }

    private void OnClientLeft(Client c)
    {
        if (c.Id.Length == 0) return;
        var room = GetRoomOf(c);
        if (room != null)
        {
            var other = room.HostId == c.Id ? room.Guest : room.Host;
            RemoveRoom(room);
            if (other != null && other.Alive)
                _ = Send(other, new { t = "peer_left" });
            Log($"房间 {room.Id}：{c.Name} 掉线");
        }
        lock (_gate) { _online.Remove(c.Id); }
        _ = Broadcast(new { t = "list", players = SnapshotOnline(null) });
        Log($"玩家离开：{c.Name}，在线 {_online.Count} 人");
    }

    // ===== 发送工具 =====
    private static async Task Send(Client c, object obj)
    {
        try { await SendAsync(c.Stream, JsonSerializer.Serialize(obj), CancellationToken.None); }
        catch { }
    }

    private async Task Broadcast(object obj)
    {
        var text = JsonSerializer.Serialize(obj);
        List<Client> snapshot;
        lock (_gate) { snapshot = _online.Values.ToList(); }
        foreach (var x in snapshot)
            if (x.Alive) await SendAsync(x.Stream, text, CancellationToken.None);
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

    private static void Log(string msg)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        _cts?.Dispose(); _cts = null; _listener = null;
    }

    public void Dispose() => Stop();
}

public static class Program
{
    public static async Task Main(string[] args)
    {
        var port = LobbyServer.DefaultPort;
        if (args.Length >= 1 && int.TryParse(args[0], out var p) && p > 0 && p < 65536) port = p;

        Console.WriteLine("==============================================");
        Console.WriteLine(" 方舟通行证谷子查询工具 · 联机对战大厅服务端");
        Console.WriteLine(" 开源自建 · 不做官方服务端 · 不收集账号信息");
        Console.WriteLine("==============================================");

        using var srv = new LobbyServer();
        srv.Start(port);
        Console.WriteLine("按 Ctrl+C 或输入 q 回车退出。");
        Console.WriteLine();

        // 简单控制台：输入 q 退出
        while (true)
        {
            var line = await Task.Run(() => Console.ReadLine());
            if (line == null || line.Trim().ToLower() == "q") break;
        }
        srv.Stop();
        Console.WriteLine("服务已停止。");
    }
}
