using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

using Newtonsoft.Json;
using AWSTranscriberAPI.Services;
using AWSTranscriberAPI.Models;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSTranscriberAPI
{
    public class Functions
    {
        // This const is the name of the environment variable that the serverless.template will use to set
        // the name of the DynamoDB table used to store blog posts.
        const string DATABASENAME_ENVIRONMENT_VARIABLE_LOOKUP = "Database";
        const string CONNECTION_ENVIRONMENT_VARIABLE_LOOKUP = "ConnectionString";

        public const string ID_QUERY_STRING_NAME = "taskId";
        TaskService TaskService;


        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
            // Check to see if a table name was passed in through environment variables and if so 
            // add the table mapping.
            string ConnectionStr = System.Environment.GetEnvironmentVariable(CONNECTION_ENVIRONMENT_VARIABLE_LOOKUP);
            string Database = System.Environment.GetEnvironmentVariable(DATABASENAME_ENVIRONMENT_VARIABLE_LOOKUP);

            TaskService = new TaskService(ConnectionStr, Database);
                
        }

        /// <summary>
        /// Constructor used for testing passing in a preconfigured context.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        public Functions(string ConnectionStr, string Database)
        {
            if (!string.IsNullOrEmpty(ConnectionStr) && !string.IsNullOrEmpty(Database))
            {
                TaskService = new TaskService(ConnectionStr, Database);
            }

        }
        private string GetTaskIdParam(APIGatewayProxyRequest request)
        {
            string taskId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                taskId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                taskId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            return taskId;
        }

        public async Task<APIGatewayProxyResponse> GetTasksAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine("Getting tasks");

            var tasks = await TaskService.GetAllTasks();
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(tasks),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            return response;
            
        }
               
        /// <summary>
        /// A Lambda function that returns the task identified by taskId
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> GetTaskAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string taskId = GetTaskIdParam(request);
 
            if (string.IsNullOrEmpty(taskId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Getting task {taskId}");
            var task = await TaskService.GetTransTaskById(taskId);

            context.Logger.LogLine($"Found task: {task != null}");

            if (task == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(task),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that adds a task.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> AddTaskAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine($"Saving task");

            var newTask = JsonConvert.DeserializeObject<NewTask>(request?.Body);
            TransTask addTask = new TransTask
            {
                name = newTask.name,
                book = newTask.book,
                description = newTask.description,
                passageSet = newTask.passageSet,
                taskID = newTask.taskID
            };
            await TaskService.AddTransTask(addTask);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = addTask._id.ToString(),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
            return response;
        }

 
        public async Task<APIGatewayProxyResponse> UpdateTaskAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string taskId = GetTaskIdParam(request);
            if (string.IsNullOrEmpty(taskId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }
            //get the requested task
            var task = await TaskService.GetTransTaskById(taskId);

            context.Logger.LogLine($"Found task: {task != null}");

            if (task == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
            }

            var newTask = JsonConvert.DeserializeObject<NewTask>(request?.Body);
            TransTask updTask = new TransTask
            {
                _id = task._id,
                name = newTask.name,
                book = newTask.book,
                description = newTask.description,
                passageSet = newTask.passageSet,
                taskID = newTask.taskID
            };
            await TaskService.UpdateTransTask(taskId, updTask);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = updTask._id.ToString(),
                Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
            };
            return response;
        }

        /// <summary>
        /// A Lambda function that removes a task
        /// </summary>
        /// <param name="request"></param>
        public async Task<APIGatewayProxyResponse> RemoveTaskAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string taskId = null;
            if (request.PathParameters != null && request.PathParameters.ContainsKey(ID_QUERY_STRING_NAME))
                taskId = request.PathParameters[ID_QUERY_STRING_NAME];
            else if (request.QueryStringParameters != null && request.QueryStringParameters.ContainsKey(ID_QUERY_STRING_NAME))
                taskId = request.QueryStringParameters[ID_QUERY_STRING_NAME];

            if (string.IsNullOrEmpty(taskId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = $"Missing required parameter {ID_QUERY_STRING_NAME}"
                };
            }

            context.Logger.LogLine($"Deleting task with id {taskId}");
            await this.TaskService.RemoveTransTask(taskId);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK
            };
        }
    }
}
