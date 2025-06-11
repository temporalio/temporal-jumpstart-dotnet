using System.Linq.Expressions;
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

namespace Onboardings.Api.Tests.Controllers;

public class PingsControllerV1Tests : TestBase
{
    private readonly DefaultHttpContext _context;
    private readonly Mock<ITemporalClient> _clientMock;
    private readonly PingsControllerV1 _controller;

    public PingsControllerV1Tests(ITestOutputHelper output) : base(output)
    {
        _context = new DefaultHttpContext();
        _context.Request.Scheme = "http";
        _context.Request.Host = new HostString("localhost");
        _clientMock = new Mock<ITemporalClient>(MockBehavior.Strict);
        _context.Features.Set<ITemporalClient>(_clientMock.Object);
        var accessor = new HttpContextAccessor { HttpContext = _context };
        var cfg = new TemporalConfig
        {
            Worker = new WorkerConfig { TaskQueue = "test" },
            Connection = new ConnectionConfig("default", "localhost")
        };
        var options = Options.Create(cfg);
        _controller = new PingsControllerV1(accessor, options, LoggerFactory)
        {
            ControllerContext = new ControllerContext { HttpContext = _context }
        };
    }

    [Fact]
    public async Task PutPingAsync_ReturnsAccepted()
    {
        _clientMock
            .Setup(c => c.StartWorkflowAsync<Ping>(It.IsAny<Expression<Func<Ping, Task>>>(), It.IsAny<WorkflowOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<WorkflowHandle>());

        var result = await _controller.PutPingAsync("ping1", new PutPing("hi"));

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal("http://localhost/v1/pings/ping1", accepted.Location);
    }

    [Fact]
    public async Task GetOnboardingStatus_ReturnsOk()
    {
        var handleMock = new Mock<WorkflowHandle>();
        handleMock
            .Setup(h => h.QueryAsync<string>(It.IsAny<Expression<Func<Ping, string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("pong");
        _clientMock
            .Setup(c => c.GetWorkflowHandle<Ping>("ping1", null))
            .Returns(handleMock.Object);

        var result = await _controller.GetOnboardingStatus("ping1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("pong", ok.Value);
    }
}
