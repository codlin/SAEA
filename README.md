# ![Logo](/logo.jpg) SAEA.Socket

[![NuGet version (SAEA)](https://img.shields.io/nuget/v/SAEA.Sockets.svg?style=flat-square)](https://www.nuget.org/packages?q=saea)
[![License](https://img.shields.io/badge/license-Apache%202-4EB1BA.svg)](https://www.apache.org/licenses/LICENSE-2.0.html)


SAEA.Socket Is an IOCP high-performance sockets network framework, based on dotnet standard 2.0; SRC contains its use scenarios, such as large file transfer, websocket client and server, high-performance message queue, RPC, redis driver, httpserver, mqtt, MVC, DNS, message server, etc <br/>

SAEA.Socket是一个IOCP高性能sockets网络框架，基于dotnet standard 2.0；Src中含有其使用场景，例如大文件传输、websocket client and server、高性能消息队列、rpc、redis驱动、httpserver、MQTT、Mvc、DNS、消息服务器等<br/>

QQ群：788260487

- [√] IOCP
- [√] FileTransfer
- [√] FTP
- [√] MessageSocket
- [√] QueueSocket
- [√] MVC
- [√] RPC
- [√] Websocket
- [√] RedisDrive
- [√] MQTT
- [√] DNS

## Reference component

引用组件，可以在nuget中搜索saea，或者直接输入命令 
```
Install-Package SAEA.Sockets -Version 6.0.0.2
```
nuget url: https://www.nuget.org/packages?q=saea

<img src="https://github.com/yswenli/SAEA/blob/master/saea.nuget.png?raw=true" />

------

# Example

## SAEA.Sockets for custom protocol

### JT808 protocol

The test project is SAEA. Sockets Test, which demonstrates how to extend IContext, IUnpacker decoding and encoding to access SAEA. Sockets by using tripartite protocol (JT808).
测试项目为SAEA.SocketsTest，其中演示了使用三方协议（JT808）来如何扩展IContext、IUnpacker解码、编码的方式接入SAEA.Sockets

## FileTransfer

### saea.filesocket usage

```csharp
var fileTransfer = new FileTransfer(filePath);
fileTransfer.OnReceiveEnd += _fileTransfer_OnReceiveEnd;
fileTransfer.OnDisplay += _fileTransfer_OnDisplay;
fileTransfer.Start();
//send file
fileTransfer.SendFile(string fileName, string ip)
```

## FTP

### saea.ftp usage

#### saea.ftpclient

```csharp
var client = new FTPClient(ip, port, username, pwd);
client.Ondisconnected += _client_Ondisconnected;
client.Connect();
var path = client.CurrentDir();
client.Upload(filePath, (o, c) =>
{
	size = c;
	_loadingUserControl.Message = $"正在上传文件:{fileName},{(o * 100 / c)}%";
});
client.Download(fileName, Path.Combine(filePath, fileName), (o, c) =>
{
	_loadingUserControl.Message = $"正在下载文件:{fileName}，{(o * 100 / c)}%";
});
```
#### saea.ftpserver

```csharp

_serverConfig.IP = ip;
_serverConfig.Port = port;
FTPServerConfigManager.Save();

var ftpServer = new FTPServer(_serverConfig.IP, _serverConfig.Port, _serverConfig.BufferSize);
ftpServer.OnLog += _ftpServer_OnLog;
ftpServer.Start();

```

## QueueTest

### saea.queue server usage

```csharp
var server = new QServer();
server.Start();
```
### saea.queue producer usage

```csharp
var ipPort = "127.0.0.1:39654";
QClient producer = new QClient("productor_" + Guid.NewGuid().ToString("N"), ipPort);
producer.OnError += Producer_OnError;
producer.OnDisconnected += Client_OnDisconnected;
producer.Connect();
producer.Publish(topic, msg);
```

### saea.queue consumer usage

```csharp
var ipPort = "127.0.0.1:39654";
QClient consumer = new QClient("subscriber_" + Guid.NewGuid().ToString("N"), ipPort);
consumer.OnMessage += Subscriber_OnMessage;
consumer.OnDisconnected += Client_OnDisconnected;
consumer.Connect();
consumer.Subscribe(topic);
```

## WebSocket
### wsserver usage

```csharp
WSServer server = new WSServer();
server.OnMessage += Server_OnMessage;
server.Start();

private static void Server_OnMessage(string id, WSProtocal data)
{
    Console.WriteLine("WSServer 收到{0}的消息：{1}", ConsoleColor.Green, id, Encoding.UTF8.GetString(data.Content));
    server.Reply(id, data);
}
```
### wsclient usage

```csharp
WSClient client = new WSClient();
client.OnPong += Client_OnPong;
client.OnMessage += Client_OnMessage;
client.OnError += Client_OnError;
client.OnDisconnected += Client_OnDisconnected;
client.Connect();
client.Send("hello world!");
client.Ping();
client.Close();
```

## RedisTest

<a href="https://github.com/yswenli/WebRedisManager" target="_blank">https://github.com/yswenli/WebRedisManager</a>

### saea.redis usage

```csharp
 var cnnStr = "server=127.0.0.1:6379;passwords=yswenli";
 RedisClient redisClient = new RedisClient(cnnStr);
 redisClient.Connect();
 redisClient.GetDataBase(1).Set("key", "val");
 var val = redisClient.GetDataBase().Get("key");
```

## SAEA.MVC

<a href="https://github.com/yswenli/SAEA.Rested" target="_blank">https://github.com/yswenli/SAEA.Rested</a>

### saea.mvc init usage

```csharp

var mvcConfig = SAEAMvcApplicationConfigBuilder.Read();
SAEAMvcApplication mvcApplication = new SAEAMvcApplication(mvcConfig);
//设置默认控制器
mvcApplication.SetDefault("home", "index");
mvcApplication.SetDefault("index.html");
//限制
mvcApplication.SetForbiddenAccessList("/content/");
mvcApplication.SetForbiddenAccessList(".jpg");

mvcApplication.Start();
```

### saea.mvc controller usage

```csharp
[LogAtrribute]
public class HomeController : Controller
{          
	[Log2Atrribute]
	[HttpGet]
	[HttpPost]
	public ActionResult Index()
	{
		return Content("Hello,I'm SAEA.MVC！你好！");
	}
	
	public ActionResult Show()
	{
		var response = HttpContext.Response;
		response.ContentType = "text/html; charset=utf-8";
		response.Write("<h3>测试一下那个response对象使用情况！</h3>参考消息网4月12日报道外媒称，法国一架“幻影-2000”战机意外地对本国一家工厂投下了炸弹。据俄罗斯卫星网4月12日援引法国蓝色电视台报道，事故于当地时间10日发生在卢瓦尔省，当时两架法国空军的飞机飞过韦尔尼松河畔诺让市镇上空，一枚炸弹从其中一架飞机上掉了下来，直接掉在了佛吉亚公司的工厂里。与此同时，有两人受伤。一名目击者称，“起初是两架战机飞过，然后我们都听到了物体撞击的声音，声音相当响，甚至盖过了飞过的飞机的噪音。”法国空军代表称，掉在工厂里的炸弹是演习用的，里面没有装炸药，本来是要将它投到离兰斯市不远的靶场。这名代表称事件“非常非常罕见”，目前正进行调查。");
		response.End();
		return Empty();
	}
	
	public ActionResult GetModels(string version, BasePamars basePamars, PagedPamars pagedPamars)
	{
		return Content($"version:{version}  basePamars:{Serialize(basePamars)}  pagedPamars:{Serialize(pagedPamars)}");
	}	
	
	public ActionResult Download()
	{
		return File(HttpContext.Server.MapPath("/Content/Image/c984b2fb80aeca7b15eda8c004f2e0d4.jpg"));
	}

	[HttpPost]
	public ActionResult Upload(string name)
	{
		var postFiles = HttpContext.Request.PostFiles;
		return Content($"ok！name：{name}");
	}
}
```

## SAEA.RPC
### saea.rpc service usage

```csharp
var sp = new ServiceProvider();
sp.OnErr += Sp_OnErr;
sp.Start();

[RPCService]
public class HelloService
{
	public string Hello()
	{
		return "saea.rpc hello!"
	}
}
```

### saea.rpc client usage

```csharp
var url = "rpc://127.0.0.1:39654";
RPCServiceProxy cp = new RPCServiceProxy(url);
cp.OnErr += Cp_OnErr;
cp.HelloService.Hello();
```

## SAEA.Message

### saea.message server usage

```csharp
MessageServer server = new MessageServer(1024, 1000 * 1000, 30 * 60 * 1000);

server.OnDisconnected += Server_OnDisconnected;

server.Start();
```

### saea.message client usage

```csharp
var cc1 = new MessageClient();
cc1.OnPrivateMessage += Client_OnPrivateMessage;
cc1.Connect();

//私信
cc1.SendPrivateMsg(cc2.UserToken.ID, "你好呀,cc2！");

//订阅
cc1.Subscribe(channelName);

//发送频道消息
cc1.SendChannelMsg(channelName, "hello!");

//创建群组
cc1.SendCreateGroup(groupName);

//加入群组
cc2.SendAddMember(groupName);


//发送群消息
cc1.SendGroupMessage(groupName, "群主广播了！");

//退群
cc2.SendRemoveGroup(groupName);

```

## SAEA.MQTT

### saea.mqtt server usage

```csharp
var serverOptions = new MqttServerOptionsBuilder().Build();

server.ApplicationMessageReceived += Server_ApplicationMessageReceived;

await server.StartAsync(serverOptions)

private static void Server_ApplicationMessageReceived(object sender, MQTT.Event.MqttMessageReceivedEventArgs e)
{
    Console.ForegroundColor = ConsoleColor.DarkGreen;

    Console.WriteLine($"Server收到消息，ClientId:{e.ClientId}，{Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
}
```
### saea.mqtt client usage

```csharp
var client = factory.CreateMqttClient();

var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").Build();

client.ApplicationMessageReceived += Client_ApplicationMessageReceived;

await client.ConnectAsync(clientOptions);

client.SubscribeAsync("test/topic").GetAwaiter().GetResult();

client.PublishAsync("test/topic", "hello").GetAwaiter().GetResult();


private static void Client_ApplicationMessageReceived(object sender, MQTT.Event.MqttMessageReceivedEventArgs e)
{
    Console.ForegroundColor = ConsoleColor.Red;
	
    Console.WriteLine($"client:{e.ClientId}收到消息:{Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
}

```

## Instance screenshot

<img src="https://github.com/yswenli/SAEA/blob/master/FileSocketTest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/QueueSocketTest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/SAEA.MVC.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/SAEA.MVCTest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/SAEA.RedisTest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/SAEA.WebAPITest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/WebsocketTest.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/redis%20cluster%20test.png?raw=true" /><br/>
<img src="https://github.com/yswenli/SAEA/blob/master/rpc.png?raw=true" /><br/>

## More

WebRedisManager也是基于此的一款redis管理工具，具体可参见：https://www.cnblogs.com/yswenli/p/9460527.html git源码：https://github.com/yswenli/WebRedisManager

GFF一款仿QQ通信程序同样基于此，具体可参见:https://github.com/yswenli/GFF 
