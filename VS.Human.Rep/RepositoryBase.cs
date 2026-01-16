using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class RepositoryBase<TModel> where TModel : BaseModel, new()
    {
        protected IDbConnection _connection { get; set; }
        protected readonly IConfiguration _configuration;
        protected int offset;
        protected string tableName = "Employees";
        protected string sqlGetALl = "sp_Employee_getAll";
        protected SqlContraint _Sql;
        public RepositoryBase(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(configuration.GetConnectionString("stringConnect7"));
            _Sql = SqlContraint.GetVariable();
        }
        protected IDbConnection GetConnection()
        {
            var con = new SqlConnection(_configuration.GetConnectionString("stringConnect7"));
            con.Open();
            return con;
        }

        protected async Task<IDbConnection> GetConnectionAsync()
        {
            var con = new SqlConnection(_configuration.GetConnectionString("stringConnect7"));
            await con.OpenAsync();
            return con;
        }

        public async Task<bool> Delete(int id)
        {
            return await DeleteBase(id, tableDelete: tableName);
        }

        public async Task<bool> DeleteReal(int id)
        {
            return await DeleteReal(id, tableDelete: tableName);
        }
        protected DynamicParameters AddOutputParam(string name, DbType type = DbType.Int32)
        {
            var p = new DynamicParameters();
            p.Add(name, dbType: type, direction: ParameterDirection.Output);
            return p;
        }
        protected DynamicParameters GetParams<T>(T model, string[] ignoreKey = null, string outputParam = "", DbType type = DbType.Int32)
        {
            var p = new DynamicParameters();
            if (!string.IsNullOrWhiteSpace(outputParam))
                p.Add(outputParam, dbType: type, direction: ParameterDirection.Output);
            var properties = model.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var key = prop.Name;
                var value = prop.GetValue(model);
                if (ignoreKey != null && ignoreKey.Contains(key))
                    continue;
                if (!string.IsNullOrWhiteSpace(outputParam))
                {
                    if (key.ToLower() == outputParam.ToLower())
                    {
                        continue;
                    }
                }
                if (!string.IsNullOrWhiteSpace(key))
                    p.Add(key, value);
            }
            return p;
        }
        protected void ProcessInputPaging(ref int page, ref int limit, out int offset)
        {
            page = page <= 0 ? 1 : page;
            if (limit <= 0)
                limit = 20;
            if (limit > 1000)
                limit = 100;
            offset = (page - 1) * limit;
        }

        public async Task<bool> ExecuteSQL(string sql = "",
            object parameter = null,
            CommandType commandType = CommandType.StoredProcedure)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return false;
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var affected = await _con.ExecuteAsync(sql, param: parameter, commandType: commandType);
                    // Stored procedures with SET NOCOUNT ON often return -1 even when successful
                    return affected != 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("--- ExecuteSQL error: " + e.Message);
                if (e.InnerException != null) Console.WriteLine("--- Inner: " + e.InnerException.Message);
                return false;
            }
        }


        public async Task<List<TIndexModel>> ExecuteSQL<TIndexModel>(string sql = "",
            object parameter = null,
            CommandType commandType = CommandType.StoredProcedure)
            where TIndexModel : BaseIndexModel

        {
            if (string.IsNullOrEmpty(sql))
            {
                return new List<TIndexModel>();
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = await _con.QueryAsync<TIndexModel>(sql, param: parameter, commandType: commandType);

                    if (result.Any())
                    {
                        return result.ToList();
                    }
                    return new List<TIndexModel>();
                }
            }
            catch (Exception e)
            {
                return new List<TIndexModel>();

            }



        }


        public async Task<TResult> GetData<TResult, TRequest, TindexModel>(
            TRequest request,
            string sql
            ) where TResult : BaseList, new()
            where TRequest : BaseRequest, new()
            where TindexModel : BaseIndexModel, new()
        {
            int page = request.Page;
            int limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out offset);
            try
            {
                using (var con = GetConnection())
                {
                    var result = await con.QueryAsync<TindexModel>(sql,
                    request, commandType: CommandType.StoredProcedure);

                    var fistElement = result.FirstOrDefault();
                    var totalRecord = 0;
                    if (fistElement != null)
                    {

                        totalRecord = 10;
                    }

                    var reponse = new TResult()
                    {
                        Total = totalRecord,
                        Data = result
                    };

                    return reponse;
                }
            }
            catch (Exception e)
            {
                return new TResult();
            }
        }


        public async Task<TResult> GetFirstRecordBySql<TResult, TRequest, TindexModel>(
           TRequest request,
           string sql
           ) where TResult : BaseList, new()
           where TRequest : BaseRequest, new()
           where TindexModel : BaseIndexModel, new()
        {
            int page = request.Page;
            int limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out offset);
            try
            {
                using (var con = GetConnection())
                {
                    var result = await con.QueryAsync<TindexModel>(sql,
                    request, commandType: CommandType.StoredProcedure);

                    var fistElement = result.FirstOrDefault();
                    var totalRecord = 0;
                    if (fistElement != null)
                    {

                        totalRecord = 10;
                    }

                    var reponse = new TResult()
                    {
                        Total = totalRecord,
                        Data = result
                    };

                    return reponse;
                }
            }
            catch (Exception e)
            {
                return new TResult();
            }
        }




        public async Task<T?> GetFirstRecordBySql<T>(string sql, object? parameters = null) where T : class
        {
            using var con = await GetConnectionAsync();  // ✅ just use `using`, not `await using`
            return await con.QueryFirstOrDefaultAsync<T>(
                sql,
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<TModel> GetById(int id)
        {
            var _baseTable = tableName;
            var sql = "SELECT * FROM " + "[" + _baseTable + "]" + " WHERE Id = @id";
            return await ExecuteSQL<TModel>(sql, new
            {
                Id = id
            });
        }
        public async Task<T> ExecuteSQL<T>(string sql = "",
            object parameter = null)
            where T : BaseModel, new()

        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = await _con.QuerySingleOrDefaultAsync<T>(sql, param: parameter);

                    if (result == null)
                    {
                        return new T()
                        {
                            Id = -1
                        };
                    }
                    return result;
                }
            }
            catch (Exception e)
            {

                return new T()
                {
                    Id = -1
                };

            }

        }


        public async Task<T> ExecuteSQL2<T>(string sql = "",
          object parameter = null)
          where T : class, new()

        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = await _con.QuerySingleOrDefaultAsync<T>(sql, param: parameter);

                    if (result == null)
                    {
                        return new T()
                        {

                        };
                    }
                    return result;
                }
            }
            catch (Exception e)
            {

                return new T()
                {

                };

            }

        }

        public async Task<List<T>> GetDataList<T>(string sql = "",
        object parameter = null)
        where T : class, new()
        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = await _con.QueryAsync<T>(sql, param: parameter);

                    if (result != null)
                    {
                        return result.ToList();
                    }
                    return new List<T>();
                }
            }
            catch (Exception e)
            {
                return new List<T>();

            }
        }


        public async Task<bool> ExecuteSQL(string sql = "",
         object parameter = null)

        {
            if (string.IsNullOrEmpty(sql))
            {
                return false;
            }
            if (parameter == null)
            {
                return false;
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var affected = await _con.ExecuteAsync(sql, param: parameter);
                    return affected != 0;
                }
            }
            catch (Exception e)
            {
                return false;
            }

        }

        public async Task<bool> DeleteBase(int id, int delete = 1)
        {
            var _baseTable = tableName;

            var sql = "UPDATE " + "[" + _baseTable + "]" + " SET Deleted= @del  WHERE Id = @id";
            return await ExecuteSQL(sql, new
            {
                Id = id,
                del = delete
            }, CommandType.Text);
        }
        public async Task<bool> DeleteBase(int id, int delete = 1, string tableDelete = "")
        {
            var _baseTable = string.IsNullOrEmpty(tableDelete) ? tableName : tableDelete;
            var sql = "UPDATE " + "[" + _baseTable + "]" + " SET Deleted= @del, UpdateAt = getdate()   WHERE Id = @id";
            return await ExecuteSQL(sql, new
            {
                Id = id,
                del = delete
            }, CommandType.Text);
        }

        public async Task<bool> DeleteReal(int id, int delete = 1, string tableDelete = "")
        {
            var _baseTable = string.IsNullOrEmpty(tableDelete) ? tableName : tableDelete;
            var sql = "delete from  " + "[" + _baseTable + "]" + "  WHERE Id = @id";
            return await ExecuteSQL(sql, new
            {
                Id = id
            }, CommandType.Text);
        }



        public async Task<BaseList> GetBaseAll<TIndexModel>(
             BaseRequest request,
             dynamic parameter,
             string sqlPro = ""


            ) where
            TIndexModel : BaseIndexModel
        {
            int page = request.Page;
            int limit = request.Limit;


            if (parameter == null)
            {
                parameter = new
                {
                    request.From,
                    request.To,
                    request.Limit,
                    request.Page,
                    request.Token,
                    request.OrderBy,
                    request.UserId

                };
            }
            else
            {
                //parameter.From = request.From;
                //parameter.To = request.To;
                //parameter.Limit = request.Limit;
                //parameter.Page = request.Page;
            }
            var sqlGet = sqlGetALl;
            if (!string.IsNullOrEmpty(sqlPro))
            {
                sqlGet = sqlPro;
            }
            
            try
            {
                using (var con = GetConnection())
                {
                    // Log để debug
                    System.Diagnostics.Debug.WriteLine($"Executing stored procedure: {sqlGet}");
                    System.Diagnostics.Debug.WriteLine($"Parameters: {System.Text.Json.JsonSerializer.Serialize(parameter)}");
                    
                    var result = await con.QueryAsync<TIndexModel>(sqlGet,
                        parameter as object, commandType: CommandType.StoredProcedure);

                    var fistElement = result.FirstOrDefault();
                    var totalRecord = 0;
                    if (fistElement != null)
                    {
                        totalRecord = fistElement.TotalRecord;
                    }

                    var reponse = new BaseList()
                    {
                        Total = totalRecord,
                        Data = result
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Query result: Total={totalRecord}, Count={result?.Count() ?? 0}");
                    
                    return reponse;
                }
            }
            catch (Exception e)
            {
                // Log lỗi chi tiết để debug
                var errorMsg = $"Error in GetBaseAll - SP: {sqlGet}, Error: {e.Message}";
                if (e.InnerException != null)
                {
                    errorMsg += $", InnerException: {e.InnerException.Message}";
                }
                
                System.Diagnostics.Debug.WriteLine(errorMsg);
                System.Diagnostics.Debug.WriteLine($"StackTrace: {e.StackTrace}");
                
                // Trả về empty list nhưng log lỗi để debug
                // Caller có thể check logs để biết lỗi
                Console.WriteLine($"[ERROR] {errorMsg}");
                
                return new BaseList()
                {
                    Data = new List<Object>(),
                    Total = 0,
                };
            }
        }

        public async Task<int> AddAndReturnIdAsync<T>(T entity, string tableName, string keyColumn,

             params string[] ignoredFields
            ) where T : class
        {

            if (ignoredFields.Length < 1 || !ignoredFields.Contains("id"))
            {
                ignoredFields.Append("id");
            }
            var properties = typeof(T).GetProperties()
                .Where(p => ignoredFields.Contains(p.Name)).ToList();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var query = new StringBuilder($"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY();");
            using (var con = GetConnection())
            {
                return await con.ExecuteScalarAsync<int>(query.ToString(), entity);

            }
        }

        public async Task<int> UpdateAsync<T>(T entity, string tableName, string keyColumn, params string[] ignoredFields)
        {


            var properties = typeof(T).GetProperties()
                                      .Where(p => p.Name != keyColumn && !ignoredFields.Contains(p.Name)) // Loại bỏ cột khóa chính và các trường bị bỏ qua
                                      .ToList();

            var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
            var query = new StringBuilder($"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @{keyColumn}");
            using (var con = GetConnection())
            {
                return await con.ExecuteAsync(query.ToString(), entity);

            }

        }


        public async Task<int> AddAsync<T>(T entity, string tableName, string keyColumn,

           params string[] ignoredFields
          ) where T : class
        {
            var properties = typeof(T).GetProperties().Where(p => p.Name != keyColumn
            && !ignoredFields.Contains(p.Name)
            ).ToList();
            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var query = new StringBuilder($"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY();");
            using (var con = GetConnection())
            {
                return await con.ExecuteScalarAsync<int>(query.ToString(), entity);
            }
        }

        public async Task<T> ExecuteSQLScalar<T>(string sql, object parameter = null)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return default;
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = await _con.ExecuteScalarAsync<T>(sql, param: parameter);
                    return result;
                }
            }
            catch (Exception)
            {
                return default;
            }
        }



        public List<TIndexModel> ExecuteSQLListSync<TIndexModel>(string sql = "",
            object parameter = null,
            CommandType commandType = CommandType.StoredProcedure)
            where TIndexModel : BaseIndexModel
        {
            if (string.IsNullOrEmpty(sql))
            {
                return new List<TIndexModel>();
            }
            if (parameter == null)
            {
                parameter = new DynamicParameters();
            }

            try
            {
                using (var _con = GetConnection())
                {
                    var result = _con.Query<TIndexModel>(sql, param: parameter, commandType: commandType);

                    if (result.Any())
                    {
                        return result.ToList();
                    }
                    return new List<TIndexModel>();
                }
            }
            catch (Exception e)
            {
                return new List<TIndexModel>();
            }
        }
    }
}
