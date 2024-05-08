// using Temporalio.Client;
//
// namespace Temporal.Curriculum.Starters.Clients;
//
// public class Clients
// {
//     private Clients()
//     {
//         
//     }
//     public static async Task<Clients> NewClients(ILoggerFactory loggerFactory)
//     {
//         var temporalClient = 
//             TemporalClient.ConnectAsync(new()
//             {
//                 TargetHost = "localhost:7233", LoggerFactory =loggerFactory,
//             });
//         var doneTC = await temporalClient;
//         return new Clients(doneTC);
//     }
//     
// }