# Temporal Jump Start -- Starters
We will demonstrate programmatic start/execution of a Workflow in Temporal. We will also discuss decisions that need to be made before you start development. 

## Workflow Execution
There are several ways to execute a Temporal Workflow. What is the best way to execute your Workflow from a UX perspective based on your use case?
* Execute from an API endpoint (for integration with existing applications)
* Execute from the command line
* Integrate with your CI/CD system
* Containerized execution
* Can Serverless be involved?

## Retry Handling
* Unify SDK grpc retry handling (survey across Golang, Java, TypeScript, and Rust SDKs)
    * Cleary dissuade from DIY retry noodling at gRPC level

## Workflow Retries
Retrying at the Workflow level is not necessary in most cases. It is better to handle failures at the Activity or the Child Workflow level than to restart a Workflow from the beginning. This is why our Workflows do not retry by default and the default Retry Policy for an Activity is to retry an unlimited number of times. However, there are a few scenarios where a Workflow Retry might be applicable. For instance, if your Workflow is dealing with processing on a specific host and that host goes down, you might want to Retry on a different host. (Ex. https://github.com/temporalio/samples-java/blob/main/core/src/main/java/io/temporal/samples/fileprocessing/FileProcessingWorkflowImpl.java) 

## Forwarding Authz/n Best Practices
* Forwarding Authz/n best practices

## Workflow ID Reuse Policies
A specific Workflow Execution is uniquely identified by the combination of the Workflow ID, Run ID and Namespace. The Workflow ID Reuse Policy sets the rules by which a Workflow ID may be reused within Temporal. By default, a Workflow ID can be reused in the same Namespace if the previous Workflow Executions have a Closed status within the current Retention Period. The policy is called `Allow Duplicate`. The policy is set to `Allow Duplicate Failed Only` allows duplicate Workflow IDs in the case of previous Workflow Executions resulting in a Failed, Timed Out, Terminated or Cancelled Workflow Execution. If you do not want to allow a duplicate Workflow ID not matter the Closed status during the Retention Period, then use the `Reject Duplicate` policy setting. Setting the policy to `Terminate if Running` will terminate the existing Workflow Execution if another one with the same Workflow ID is started.

## API Design and Development Patterns
The first consideration to make is whether you want to use imperative (Command) or resource (Object) messaging for your API communication. Imperative messaging focuses on instructing a service to perform a specific action. Resource messaging (also known as RESTful messaging) uses messages to represent state changes to be made on specific objects. The choice between these messaging types has a large impact on the orchestration of your system. 
### Imperative messaging 
This messaging paradigm is typically more tightly-coupled since the sender needs to know what actions the receiver is able to perform. The orchestration engine has more control over the order of execution. The result is more centralized logic since the orchestrator needs to understand and manage the sequence of commands. The tightly-coupled nature of this messaging type can be less flexible with regard to making changes to the system which results in more difficulty in scaling these applications. Detailed error handling must be done at the orchestration layer adding complexity to this layer.
### Resource messaging (REST)
This approach is more loosely-coupled since the interaction is based on a shared understanding of the resource. The messages are requests to create, read, update or delete resources. The services have more control since they manage the state and behavior of their own resources. These characteristics allow for decentralized logic giving the system more flexibility where changes are easily made and error handling can be done at the service layer.


The next consideration is the API design itself. This is the contract you create to communicate with other parts of your application. This contract outlines the functionalities, data formats, protocols, and expectations that both the provider and consumer must adhere to when communicating with each other so it is crucial for the success of your application. A few of the questions to consider while designing your API are:

1. What is the purpose of the API and who are the consumers of it?
2. What is the specific functionality of the API (what operations should it provide)?
3. What data formats does the API need to support?
4. How will the API handle authentication and authorization?
5. Are there rate limiting/throttling considerations for the API?
6. How will your API be tested?

Once your API Design is solidified, the next decision point is which development pattern(s) you use. Which pattern(s) is/are correct for your use case?
### Entity
The Entity pattern is a design pattern used to encapsulate the data and behavior of a real-world concept and is typically used for long-running Workflows. A long-running process does not necessarily have a specific time threshold. A long-running Workflow is any process which involves complex or time-consuming operations (it could take anywhere from minutes to months to indefinitely). For example, a Workflow could represent a student enrollment and process tasks as long as the student is taking classes. Another example is of a shopping cart which runs until the checkout process has completed. [Reference: Temporal Blog Post](https://temporal.io/blog/very-long-running-workflows)
### Pipeline
The Pipeline pattern is a design pattern used for processing data through a series of steps and typically the output of one step is used as the input for the next step. The nature of this pattern allows for a natural separation of concerns making it flexible and easily scalable and testable. Examples of common use cases for the Pipeline pattern are data/image/video processing, infrastructure management, CI/CD pipelines and web middleware (to perform authn/z, logging, caching, etc on incoming requests)
### Saga
The Saga pattern is a design pattern in which a series of local transactions are used to maintain a consistent state across a series of microservices. Upon failure, all previous local transactions are rolled back by calling ```Compensating Actions```. This pattern is useful where distributed transactions are required and you still need to maintain the ACID properties. The most common use cases for the Saga pattern are Order Management (e-commerce), Reservation Booking and Supply Chain Management. [Reference: Temporal Blog Post](https://temporal.io/blog/saga-pattern-made-easy)

### API Publishing and Versioning
Another aspect of the API design to take into consideration is how you want to make your API available to consumers and how you want to version your API. It is critical to ensure your API is easily accessible to clients and maintains backward compatibility over time. Both Imperative and Resource-based APIs follow a similar approach to publishing versioning, but use different tooling to do so. RPC-based API protocols use IDL (Interface Definition Language) to define the contract of the service methods, data types and exceptions. REST APIs use standardized API description formats like OpenAPI or RAML to specify the API contract. As far as versioning is concerned, both API types support URI versioning and Header versioning:

   ### URI Versioning
   URI versioning is a strategy where the version of the API is included in the URL of the endpoint. \
   &nbsp;&nbsp;&nbsp;```RPC:  /v1/user.GetUser``` \
   &nbsp;&nbsp;&nbsp;```REST: /api/v1/users``` \
   This versioning strategy is easy for anyone to understand. It also is simple to route requests to the proper resource from a router or load balancer and allows for caching to be done based on the URI. However, it does require changing resource URIs when new versions are introduced, possibly breaking some dependencies.

   ### Header Versioning
   Header versioning is a strategy where the version of the API is included in a custom header sent with the call. \
   &nbsp;&nbsp;&nbsp;```RPC:  X-API-Version: 1``` \
   &nbsp;&nbsp;&nbsp;```REST: GET /api/users``` \
   &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;```X-API-Version: 1```
   This versioning strategy keeps the URI clean so client integrations are not impacted with changes and it allows for more flexibility in versioning at different levels of the API. However, it requires parsing and handling the custom version headers on both the client and server side. It is also more complex to provide caching and routing services for this versioning technique.
 





