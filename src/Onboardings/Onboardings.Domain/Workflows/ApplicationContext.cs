using System.Security.Principal;
using Microsoft.EntityFrameworkCore;

namespace Onboardings.Domain.Workflows;
// ApplicationContext represents all those fun global state things
// you can't use anymore because they presumed on a Thread, not a Task, as the context.
// AsyncLocal uses ExecutionContext under the hood.
// https://github.com/dotnet/runtime/blob/16b6369b7509e58c35431f05681a9f9e5d10afaa/src/libraries/System.Private.CoreLib/src/System/Threading/AsyncLocal.cs#L45
// So don't be scared by the static things here...
public static class ApplicationContext
{
    public static readonly AsyncLocal<IPrincipal> CurrentPrincipal = new();
    // This is put here for illustration purposes only (not used in the application)
    // ReSharper disable once UnusedMember.Global
    public static readonly AsyncLocal<DbContext> CurrentDbContext = new();
}