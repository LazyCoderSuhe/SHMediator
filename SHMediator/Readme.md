# <center> 使用方式

> # 请您等他升级到 2.0 版本后再使用 

# 安装 NuGet 包
```bash
dotnet add package SHMediator
```

# 注册服务
```csharp
using SHMediator;
using Microsoft.Extensions.DependencyInjection;
var services = new ServiceCollection();
services.AddSHMediator(typeof(Program).Assembly);
```



# 三种 接口

	1. IRequest       --  IRequestHandler
	2. IRequest<,>    --  IRequestHandler<,>
	3. INotification  --  INotificationHandler
## 1. IRequest -- IRequestHandler
```csharp
public class MyRequest : IRequest<MyResponse>
{
	public string Name { get; set; }
}
public class MyResponse
{
	public string Message { get; set; }
}
public class MyRequestHandler : IRequestHandler<MyRequest, MyResponse>
{
	public Task<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
	{
		return Task.FromResult(new MyResponse { Message = $"Hello, {request.Name}!" });
	}
}
// 使用
var mediator = serviceProvider.GetRequiredService<IMediator>();
var response = await mediator.Send(new MyRequest { Name = "World" });
Console.WriteLine(response.Message); // 输出: Hello, World!
```

## 2. INotification -- INotificationHandler

```csharp
public class MyNotify : INotification
{
	public string Name { get; set; }
}

public class MyNotify1Handlder : INotificationHandler<MyNotify>
{
	 Task Handle(TNotification notification)
	 {
	     Console.WriteLine("")
	 }
}
public class MyNotify2Handlder : INotificationHandler<MyNotify>
{
	 Task Handle(TNotification notification)
	 {
	     Console.WriteLine("")
	 }
}

```

更新内容

*  添加自动注册验证Validator功能
*  更新添加拦截器 并支持自定义


# 文档目录

- [使用方式](/docs/00使用方式.md)
- [拦截器](/docs/01拦截器.md)

