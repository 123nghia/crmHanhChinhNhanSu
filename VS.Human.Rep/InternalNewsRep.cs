using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class InternalNewsRep : RepositoryBase<InternalNewsItem>, IInternalNewsRep
    {
        public InternalNewsRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "InternalNews";
            sqlGetALl = "sp_InternalNews_getAll";
        }

        private async Task<int> AddAndReturnId(InternalNewsItem item)
        {
            var parameter = new
            {
                item.Title,
                item.Content,
                item.IsSendMail,
                item.CreatedBy
            };
            try
            {
                using (var con = GetConnection())
                {
                    var id = await con.ExecuteScalarAsync<decimal>(
                        "sp_InternalNews_insert",
                        parameter,
                        commandType: CommandType.StoredProcedure
                    );
                    return Convert.ToInt32(id);
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private async Task<bool> Update(InternalNewsItem item)
        {
            var parameter = new
            {
                item.Id,
                item.Title,
                item.Content,
                item.IsSendMail,
                item.UpdatedBy
            };
            return await ExecuteSQL("sp_InternalNews_update", parameter);
        }

        private async Task<bool> AddAttachments(int newsId, IEnumerable<InternalNewsAttachment> attachments, int userId)
        {
            if (attachments == null)
            {
                return true;
            }

            foreach (var attachment in attachments)
            {
                if (attachment == null || string.IsNullOrWhiteSpace(attachment.FilePath))
                {
                    continue;
                }

                var parameter = new
                {
                    NewsId = newsId,
                    attachment.FileName,
                    attachment.FilePath,
                    attachment.FileSize,
                    CreatedBy = userId
                };

                var added = await ExecuteSQL("sp_InternalNewsAttachment_insert", parameter);
                if (!added)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<List<InternalNewsAttachment>> GetAttachmentsByNewsId(int newsId)
        {
            using (var con = GetConnection())
            {
                var result = await con.QueryAsync<InternalNewsAttachment>(
                    "sp_InternalNewsAttachment_getAllByNewsId",
                    new { NewsId = newsId },
                    commandType: CommandType.StoredProcedure
                );
                return result.ToList();
            }
        }

        public async Task<bool> AddOrUpdate(InternalNewsItem item)
        {
            if (item.Id > 0)
            {
                var updated = await Update(item);
                if (!updated)
                {
                    return false;
                }

                if (item.Attachments != null && item.Attachments.Count > 0)
                {
                    var userId = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy;
                    return await AddAttachments(item.Id, item.Attachments, userId);
                }

                return true;
            }

            var newId = await AddAndReturnId(item);
            if (newId <= 0)
            {
                return false;
            }

            item.Id = newId;
            if (item.Attachments != null && item.Attachments.Count > 0)
            {
                return await AddAttachments(newId, item.Attachments, item.CreatedBy);
            }

            return true;
        }

        public async Task<BaseList> GetAll(InternalNewsRequest request)
        {
            return await GetBaseAll<InternalNewsIndexModel>(request, new
            {
                Token = request.Token ?? string.Empty,
                request.OrderBy,
                request.Page,
                request.Limit,
                request.From,
                request.To
            }, "sp_InternalNews_getAll");
        }

        public async Task<InternalNewsItem> GetById(int id)
        {
            var result = await GetFirstRecordBySql<InternalNewsItem>("sp_InternalNews_GetById", new { Id = id });
            if (result == null || result.Id <= 0)
            {
                return new InternalNewsItem { Id = -1 };
            }

            result.Attachments = await GetAttachmentsByNewsId(result.Id);
            return result;
        }

        public async Task<bool> Delete(int id)
        {
            return await DeleteBase(id, tableDelete: tableName);
        }
    }
}
