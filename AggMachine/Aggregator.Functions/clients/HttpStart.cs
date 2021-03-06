namespace Aggregator.Functions.Clients
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Aggregator.Events;
    using Aggregator.Logging;

    public static class HttpStart
    {
        [FunctionName("HttpStart")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            ClientLogger logger = new ClientLogger(log);
            string data = await req.ReadAsStringAsync();
            InventoryEvent incomingEvent;
            try
            {
                incomingEvent = JsonConvert.DeserializeObject<InventoryEvent>(data);
                if(incomingEvent.Type != InventoryEventType.OnHandUpdate && incomingEvent.Type != InventoryEventType.ShipmentUpdate)
                {
                    throw new Exception("Invalid event type. Please specify either 'onHandUpdate' or 'shipmentUpdate'.");
                }
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e.Message);
            }

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync<InventoryEvent>("StoreOrchestrator", incomingEvent);
            logger.LogManagmentUrls(starter, instanceId);
            logger.LogOrchestrationStarted(instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}