# Temporal Jump Start -- Workflows
In this section, we will discuss code organization, testing and get into developing your first Temporal Workflow!

## Code Organization
Temporal is not prescriptive in the way you organize your code. Temporal encourage developers to organize their code using the standard practice of the particular language (SDK) you are using to develop your Workflows. We also support code organization specifics of language frameworks (like Spring Boot, Django, GoFrame, etc.) as well as different architectural design patterns (such as DDD, Hexagonal, Clean Architecture, etc.). The essence of code organization that Temporal focuses on is that it have a clear separation of concerns and structured to be highly maintainable, scalable and readable. In general, we suggest packages that separate ```Workflows```, ```Activities```, ```Workers```, ```Config```, ```Utils``` and tests will have a similar separation. This organization method will fit in easily with any of the above existing code organization patterns.

## Testing
Testing is an important part of any development process. The quality of your tests directly impact the quality of your final application and the ability to ensure it remains reliable as changes to the system are made over time. 

### Testing Culture
What is the testing culture like at your organziation or in your team? 
    -- Do you write your tests after development is complete? 
    -- Do you practice TDD (Test Driven Development)? 
    -- Do you not test at all? 
    -- Are your tests run as part of a Continous Integration pipeline?
    -- Do you perform unit tests, integration tests and end-to-end tests?
    -- Do you perform Black Box texting?

Most of the SDKs Temporal supports have popular test frameworks and we make a recommendation in our SDK testing documentation [Reference: SDK Docs](https://docs.temporal.io/develop/) on which framework is the best one to use with Temporal. Black Box testing is very simple to execute and is a useful way to find larger functional issues.  However, if you have long-running Workflows, you don't want to wait for hours, weeks or longer to complete the test, so the Temporal test framework provides an in-memory test server which supports time skipping. We recommend using this server for both end-to-end and integration tests with Workers. Another feature available with the Temporal test framework is replay testing. This feature ensures new Workflow Definition code is compatible with your event history (aka deterministic).

## Feedback Loop
The Feedback Loop in software testing is the iterative approach to developing software. Write a test, let it fail and then write the code to make the test pass. This methodology forces developers to both ensure your code is tested thoroughly and also that you are not writing extraneous code.

### Orchestration Test
The first step in the Feedback Loop is to write an Orchestration test.

## Orchestration
This is where we start get into the implementation of the Workflow Interface and its implementation. The Temporal SDKs are idiomatic so they each use the language-specific standards for implementation.  

| SDK Language | Workflow Definition | Doc Link |
| :------------ | :------------------- | :------- |
| Java         | Workflow Interface class with ```@WorkflowInterface``` annotation and an implementation class | [Develop a Workflow](https://docs.temporal.io/develop/java/core-application#develop-workflows) |
| Go           | Go exportable function with ```ctx workflow.Context``` as the first parameter | [Develop a Workflow](https://docs.temporal.io/develop/go/core-application#develop-workflows) |
| Python       | Python class with ```@workflow.defn``` decorator | [Develop a Workflow](https://docs.temporal.io/develop/python/core-application#develop-workflows) |
| .NET         | Workflow Class with ```[Workflow]``` attribute | [Develop a Workflow](https://docs.temporal.io/develop/dotnet/core-application#develop-workflow) |
| Typescript   | Just a Typescript function | [Develop a Workflow](https://docs.temporal.io/develop/typescript/core-application#develop-workflows) |
| PHP          | Workflow Interface class with ```#[WorkflowInterface]``` annotation and an implementation class | [Develop a Workflow](https://docs.temporal.io/develop/php/core-application#develop-workflows) |

## Dependencies in Workflows
What about my dependencies? Can I pass a Logger to my Workflow? I also need to connect to a database from my Workflow and call a third party API, how do I do that?  Workflows are meant to be where the overall steps of your application are specified and Activities are where any external dependencies need to be defined and used. We will cover Activities later in the class and how to define your dependencies there. 

However, you still want to log information at the Workflow level. Specifically for a logger, we have our own version of a logger that can be used inside a Workflow. The Temporal Logger keeps track of whether the current execution is a replay or not and will avoid having duplicate logging statements. The exact implementation differs per SDK, so check our SDK documentation. [Temporal SDK Documentation](https://docs.temporal.io/develop/)

We have mentioned previously that return parameters need to be Serializable, but beyond that we have recommendations that will make your design easier to maintain as well as maintaining Command-Query segregation. What does this mean? This means that Worfklows should have a single responsibility. They should either be responsible for performing actions in the system/mutating state (Command) or they should be responsible for getting data out of the system (Query). Command Workflows should not return parameters. Any changes to Workflow state should be retrieved via a Query into that Workflow.

Just like any application, Temporal Workflows may have exceptions or failures. An exception is any expected error which we should be able to recover from. However, a failure is an unexpected error which is not recoverable. A Temporal Workflow Execution will only fail if the encountered error is a Temporal Failure. All other errors only cause the Workflow Task to fail and will be retried. However, if a Workflow Execution raises an exception




