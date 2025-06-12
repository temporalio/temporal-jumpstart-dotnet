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

    [Fact]
    public async Task PutPingAsync_ReturnsAccepted()
    {
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

    
    public class IC : ClientOutboundInterceptor
    {
        public IC() : base(null!)
        {
        }

       
    }

    public record MockWorkflowHandle<TWorkflow,TResult> : WorkflowHandle<TWorkflow,TResult>
    {
        public MockWorkflowHandle(ITemporalClient Client, string Id, string? RunId = null, string? ResultRunId = null, string? FirstExecutionRunId = null) : base(Client, Id, RunId, ResultRunId, FirstExecutionRunId)
        {
        }

        public override Task<TQueryResult> QueryAsync<TQueryResult>(string query, IReadOnlyCollection<object?> args, WorkflowQueryOptions? options = null)
        {
            return Task.FromResult<TQueryResult>(Result is TQueryResult ? (TQueryResult)Result : default);
        }

        public object Result { get; set; }
    }

    public record MockWorkflowHandle2<TWorkflow, TResult> : WorkflowHandle<TWorkflow, TResult>
    {
        public MockWorkflowHandle2() : base(null!, "") { }

        public MockWorkflowHandle2(ITemporalClient Client, string Id, string? RunId = null, string? ResultRunId = null, string? FirstExecutionRunId = null) : base(Client, Id, RunId, ResultRunId, FirstExecutionRunId)
        {
        }

        public override Task<TQueryResult> QueryAsync<TQueryResult>(string query, IReadOnlyCollection<object?> args, WorkflowQueryOptions? options = null) => ((WorkflowHandle)this).QueryAsync<TQueryResult>(query, args, options);
    }
    
    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        // System.Object[] args = new System.Object[0];
        // var qi = new QueryWorkflowInput(
        //     Id: "ping1",
        //     RunId: null,
        //     Query: "GetState",
        //     Args:null,
        //     Options: null,
        //     Headers: null
        //     );
        var mc = new Mock<ITemporalClient>(MockBehavior.Strict);
        
        // var handle2 = new Mock<MockWorkflowHandle2<Ping, string>>();
        // handle2.Setup(h => 
        //     h.QueryAsync<string>(wf => wf.GetState(), It.IsAny<WorkflowQueryOptions>()))
        //     .ReturnsAsync("pong");
        var handle = new MockWorkflowHandle<Ping,string>(mc.Object, "ping1");
        handle.Result = "pong";
        mc.Setup(c => c.GetWorkflowHandle<Ping>("ping1", null, null)).Returns(handle);

        // mc.Setup(c => c.OutboundInterceptor).Returns(interceptor.Object);
        // var handle = new WorkflowHandle<Ping, string>(mc.Object, "ping1", "run1", "run1");
        // mc.Setup(c => c.GetWorkflowHandle<Ping, string>("ping1", null, null)).Returns(handle);
        var sut = CreateController(mc.Object); 
        var result = await sut.GetOnboardingStatus("ping1");
        
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("pong", ok.Value);
        
    }
    // Helper method to extract the argument
    private static string ExtractArgumentFromExpression(Expression<Func<Ping, Task>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall && 
            methodCall.Arguments.Count > 0 &&
            methodCall.Arguments[0] is ConstantExpression constantExpr)
        {
            return constantExpr.Value?.ToString();
        }
        return null;
    }
}

