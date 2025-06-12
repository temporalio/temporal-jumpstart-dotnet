using System.Linq.Expressions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Onboardings.Api.Controllers;
using Onboardings.Api.Messages;
using Onboardings.Domain.Clients.Temporal;
using Onboardings.Domain.Workflows;
using Temporalio.Client;
using Temporalio.Client.Interceptors;
using Xunit.Abstractions;

namespace Onboardings.Api.Tests.Controllers;

public class PingsControllerV1Tests : TestBase
{

    public PingsControllerV1Tests(ITestOutputHelper output) : base(output)
    {
        
    }

    private PingsControllerV1 CreateController(ITemporalClient clientMock)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        // var clientMock = new Mock<ITemporalClient>(MockBehavior.Strict);
        context.Features.Set<ITemporalClient>(clientMock);
        var accessor = new HttpContextAccessor { HttpContext = context };
        var cfg = new TemporalConfig
        {
            Worker = new WorkerConfig
            {
                TaskQueue = "test",
                Capacity = null,
                RateLimits = null,
                Cache = null
            },
            Connection = new ConnectionConfig("default", "localhost")
        };
        var options = Options.Create(cfg);
        var controller = new PingsControllerV1(accessor, options, LoggerFactory)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        return controller;
    }

    public class MockableClientOutboundInterceptor : ClientOutboundInterceptor
    {

        public MockableClientOutboundInterceptor(): base(null!) { }
    }
    [Fact]
    public async Task PutPingAsync_ReturnsAccepted()
    {
        // var mc = new Mock<ITemporalClient>();
        // var ic = new Mock<MockableClientOutboundInterceptor>();
        // var handle = new WorkflowHandleTestDouble<Ping, string>(mc.Object, "ping1");
        // mc.Setup(m => m.StartWorkflowAsync<Ping>(
        //     // wf => wf.ExecuteAsync("hi"),
        //     It.IsAny<Expression<Func<Ping, Task>>>(),
        //     It.IsAny<WorkflowOptions>())).
        //     Callback(Expression<Func<Ping,Task>>, WorkflowOptions(Expr)
        //     ReturnsAsync(handle);   
        //
        // mc.Setup(m =>
        //     m.StartWorkflowAsync<Ping>(wf => wf.ExecuteAsync("hi"),
        //         It.Is<WorkflowOptions>(o => o.TaskQueue == "test"))).ReturnsAsync(handle);
        //
        // ic.Setup(i => i.StartWorkflowAsync<Ping, string>(new StartWorkflowInput(
        //     )))
        var mockClient = new MockTemporalClient();
        var sut = CreateController(mockClient);
        var result = await sut.PutPingAsync("ping1", new PutPing("hi"));
        var arg = mockClient.CapturedCalls.First(c => c.MethodName == "ExecuteAsync").Arguments.First();
        var opts = mockClient.CapturedCalls.First(c => c.MethodName == "ExecuteAsync").Options;
        Assert.IsType<string>(arg);
        Assert.Equal(arg,"hi");
        Assert.Equal("test",opts?.TaskQueue);
        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal("http://localhost/v1/pings/ping1", accepted.Location);
    }
    
    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        var mc = new Mock<ITemporalClient>(MockBehavior.Strict);
        var handle = new WorkflowHandleTestDouble<Ping,string>(mc.Object, "ping1") { Result = "pong" };
        mc.Setup(c => c.GetWorkflowHandle<Ping>("ping1", null, null)).Returns(handle);
        var sut = CreateController(mc.Object); 
        var result = await sut.GetPingAsync("ping1");
        
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("pong", ok.Value);
        
    }
}