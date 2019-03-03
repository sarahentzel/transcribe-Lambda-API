using System.Collections.Generic;
using System.Linq;
using AWSTranscriberAPI.Models;
using MongoDB.Driver;
using System.Threading.Tasks;
using System;
using MongoDB.Bson;

namespace AWSTranscriberAPI.Services
{
    public interface ITaskService
    {
        IEnumerable<TransTask> GetTasksNow();

        Task<IEnumerable<TransTask>> GetAllTasks();

        //
        Task<TransTask> GetTransTaskById(string id);
        Task<IEnumerable<TransTask>> GetTransTask(string name);

        // query after multiple parameters
        //Task<IEnumerable<TransTask>> GetTransTask(string bodyText, DateTime updatedFrom, long headerSizeLimit);

        // add new task document
        Task<string> AddTransTask(TransTask item);

        // remove a single document / note
        Task<bool> RemoveTransTask(string id);

        // update just a single document / note
        Task<bool> UpdateTransTask(string id, TransTask updTask);

        // demo interface - full document update
        Task<bool> UpdateTransTaskDocument(string id, string body);

        // should be used with high cautious, only in relation with demo setup
        Task<bool> RemoveAllTransTasks();
    }/*
    public class TaskServiceSync
    {
            private readonly IMongoCollection<TransTask> _tasks;

            public TaskServiceSync(IOptions<Settings> settings)
            {
                var client = new MongoClient(settings.Value.ConnectionString);
                var database = client.GetDatabase(settings.Value.Database);
                _tasks = database.GetCollection<TransTask>("tasks");
            }
        public IEnumerable<TransTask> GetTransTasks()
        {

            try
            {
                return _tasks.Find(_ => true).Project<TransTask>("{name:1}").ToList();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }
    }
    */
    public class TaskService : ITaskService
    {
        private readonly IMongoCollection<TransTask> _tasks;

        public TaskService(string ConnectionString, string Database)
        {
            MongoClient client = new MongoClient(ConnectionString);
            var database = client.GetDatabase(Database);
            _tasks = database.GetCollection<TransTask>("tasks");
        }
        public IEnumerable<TransTask> GetTasksNow()
        {
            try
            {

                return _tasks.Find(_ => true).Project<TransTask>("{name:1,taskID:1}").ToList();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }


        public async Task<IEnumerable<TransTask>> GetAllTasks()
        {
            try
            {
                //return await _tasks
                //        .Find(_ => true).Project<TransTask>("{name:1,taskID:1}").ToListAsync();
                return await _tasks.Find(_ => true).ToListAsync();

            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        // query after Id or InternalId (BSonId value)
        //
        public async Task<TransTask> GetTransTaskById(string id)
        {
            try
            {
                return await _tasks
                                .Find(task => task.taskID == id || task._id==GetInternalId(id))
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        // query after body text, updated time, and header image size
        //
        public async Task<IEnumerable<TransTask>> GetTransTask(string name)
        {
            try
            {
                var query = _tasks.Find(task => task.name.Contains(name));

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        private ObjectId GetInternalId(string id)
        {
            ObjectId internalId;
            if (!ObjectId.TryParse(id, out internalId))
                internalId = ObjectId.Empty;

            return internalId;
        }

        public async Task<string> AddTransTask(TransTask item)
        {
            try
            {
                await _tasks.InsertOneAsync(item);
                return item._id.ToString();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<bool> RemoveTransTask(string id)
        {
            try
            {
                DeleteResult actionResult
                    = await _tasks.DeleteOneAsync(
                        Builders<TransTask>.Filter.Eq("taskID", id));

                return actionResult.IsAcknowledged
                    && actionResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<bool> UpdateTransTask(string id, TransTask item)
        {
            try
            {
                ReplaceOneResult actionResult
                    = await _tasks
                                    .ReplaceOneAsync(n => n.taskID.Equals(id) || n._id == GetInternalId(id)
                                            , item
                                            , new UpdateOptions { IsUpsert = true });
                return actionResult.IsAcknowledged
                    && actionResult.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        // Demo function - full document update
        public async Task<bool> UpdateTransTaskDocument(string id, string name)
        {
            var item = await GetTransTaskById(id) ?? new TransTask();
            item.name = name;
            //item.UpdatedOn = DateTime.Now;

            return await UpdateTransTask(id, item);
        }

        public async Task<bool> RemoveAllTransTasks()
        {
            try
            {
                DeleteResult actionResult
                    = await _tasks.DeleteManyAsync(new BsonDocument());

                return actionResult.IsAcknowledged
                    && actionResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }
 


    }
}