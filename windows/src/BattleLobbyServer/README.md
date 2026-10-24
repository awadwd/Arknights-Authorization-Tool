# 联机对战大厅服务端（BattleLobbyServer）

> 方舟通行证谷子查询工具 · 联机对战大厅服务端（开源自建）

## 定位

- **开源、玩家自建**：不做官方服务端，不收集任何账号/个人信息。
- 客户端（Windows 壳 / 手机 H5）连接本服务端后，可看到**在线玩家列表**，**邀请**任意在线玩家对战。
- 支持：昵称、在线大厅、邀请/接受/拒绝、房主踢人、房间对战（发牌/抽卡严格一致）。

## 技术

- .NET 10 控制台程序，`TcpListener` + **手写 WebSocket**（文本 JSON 帧）。
- 免管理员、免 `urlacl`（不使用 http.sys / HttpListener）。
- 默认端口 **23516**（与壳内置直连服务器 23515 区分）。

## 编译

```bash
cd src/BattleLobbyServer
dotnet build -c Release
# 单文件发布（任选平台）
dotnet publish -c Release -r win-x64 --self-contained false
dotnet publish -c Release -r linux-x64 --self-contained false
```

> 若本机 `dotnet build` 报 `Value cannot be null (Parameter 'path1')`，见下文「排障」。

## 运行

```bash
# 默认端口 23516
dotnet run --project src/BattleLobbyServer
# 或指定端口
dotnet run --project src/BattleLobbyServer -- 30000
```

启动后会打印本机局域网地址（如 `192.168.1.3:23516`）。

## 部署与联机

| 场景 | 做法 |
|---|---|
| 同一局域网 | 服务端跑在任一台机器，客户端填它的 `局域网IP:23516` |
| 公网联机 | 在路由器/云服务器做**端口转发**（TCP 23516 → 服务端机器 23516），客户端填公网 `IP:端口` |
| 云服务器 | 直接跑在云服务器上，客户端填公网地址 |

客户端（对战页 → 联机对战）：
1. 填昵称 + 服务器地址 → 「连接大厅」。
2. 大厅看到在线玩家 → 点「邀请」。
3. 对方接受后进入房间，房主「开始对战」/「踢出对方」。

## 协议（供二次开发）

文本 JSON 帧，字段名小写：

| 方向 | 消息 | 说明 |
|---|---|---|
| C→S | `{t:"hello", name}` | 注册昵称，进入大厅 |
| S→C | `{t:"welcome", id, name, lan}` | 返回自己 id 与服务器地址 |
| S→C | `{t:"list", players:[{id,name,busy}]}` | 在线列表（busy=对战中） |
| C→S | `{t:"invite", to}` | 邀请某人 |
| S→C | `{t:"invite", from, fromName}` | 收到邀请 |
| C→S | `{t:"invite_accept", from}` | 接受邀请 |
| C→S | `{t:"invite_decline", from}` | 拒绝邀请 |
| C→S | `{t:"invite_cancel", to}` | 取消邀请 |
| S→C | `{t:"room", roomId, role:"host"\|"guest", peerName}` | 进入房间 |
| C→S | `{t:"kick"}` | 房主踢人 |
| S→C | `{t:"kicked"}` / `{t:"kick_ok"}` | 被踢 / 踢出成功 |
| C→S | `{t:"cfg", super, cards:[{n,e,p,a}]}` | 房主下发牌库 |
| S→C | `{t:"start", super, total, host, guest}` | 开始对战 |
| C→S | `{t:"draw"}` | 抽一张 |
| S→C | `{t:"drew", by:"host"\|"guest", card, remaining, hostDrew, guestDrew}` | 抽卡结果广播 |
| C→S | `{t:"leave_room"}` | 离开房间 |
| S→C | `{t:"peer_left"}` | 对方离开 |

## 排障

**`dotnet build` 报 `Value cannot be null (Parameter 'path1')`**

本机 NuGet 还原依赖 `%APPDATA%` 环境变量定位用户配置。若该变量为空（可在 PowerShell 里
`echo $env:APPDATA` 验证），NuGet 还原会崩溃。修复：

```powershell
[Environment]::SetEnvironmentVariable('APPDATA','C:\Users\<你>\AppData\Roaming','User')
# 关掉当前终端重开，或重启机器后再试
```

## 协议实测

```bash
# 终端 1：启动服务端
dotnet run --project src/BattleLobbyServer
# 终端 2：跑协议测试（hello→list→invite→accept→room→cfg→draw→kick→peer_left）
python _test_lobby_server.py
```
