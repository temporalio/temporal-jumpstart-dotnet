using System.Linq.Expressions;

namespace Onboardings.Api.Tests.Controllers;

public class ExtractArgumentValues
{
    public static object[] From(MethodCallExpression methodCall)
    {
        var arguments = new object[methodCall.Arguments.Count];
    
        for (int i = 0; i < methodCall.Arguments.Count; i++)
        {
            var arg = methodCall.Arguments[i];
        
            // Handle different types of expressions
            switch (arg)
            {
                case ConstantExpression constant:
                    arguments[i] = constant.Value;
                    break;
                case MemberExpression member:
                    arguments[i] = Expression.Lambda(member).Compile().DynamicInvoke();
                    break;
                default:
                    arguments[i] = Expression.Lambda(arg).Compile().DynamicInvoke();
                    break;
            }
        }
    
        return arguments;
    }
}