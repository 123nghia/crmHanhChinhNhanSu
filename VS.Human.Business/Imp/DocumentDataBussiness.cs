using Microsoft.AspNetCore.Http;
using System;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class DocumentDataBussiness : BaseBusiness, IDocumentDataBussiness
    {
        public DocumentDataBussiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }


        public async Task<bool> AddOrUpdate(DocumentDataAddRequest item)
        {
            var relId = item.RelId;
            var dataDocument = item.Data;
            foreach (var item1 in dataDocument)
            {
                if (item1.Id <= 0)
                {
                    continue;
                }

                var current = await _unitOfWork.DocumentDataRep.GetById(item1.Id);
                if (current == null || current.Id <= 0)
                {
                    return false;
                }

                if (current.IsSignedInternal || current.IsSignatureRequested)
                {
                    var currentFile = (current.ValueFile ?? string.Empty).Trim();
                    var newFile = (item1.ValueFile ?? string.Empty).Trim();
                    var currentCode = (current.Code ?? string.Empty).Trim();
                    var newCode = (item1.Code ?? string.Empty).Trim();
                    var currentDisplayText = (current.DisplayText ?? string.Empty).Trim();
                    var newDisplayText = (item1.DisplayText ?? string.Empty).Trim();
                    if (!string.Equals(currentFile, newFile, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(currentCode, newCode, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(currentDisplayText, newDisplayText, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            foreach (var item1 in dataDocument)
            {
                var itemDocumentAdd = new DocumentData()
                {
                    Id = item1.Id,
                    Code = item1.Code,
                    DisplayText = item1.DisplayText,
                    ValueFile = item1.ValueFile,
                    RelId = relId,
                    dataType = item.DataType,
                    RelCode = "EMP",
                    CreatedBy = item.UserId,
                    UpdatedBy = item.UserId
                };
                await _unitOfWork.DocumentDataRep.AddOrUpdate(itemDocumentAdd);
            }
            return true;
        }
        public async Task<bool> Delete(int id)
        {
            var item = await _unitOfWork.DocumentDataRep.GetById(id);
            if (item != null && item.Id > 0 && (item.IsSignedInternal || item.IsSignatureRequested))
            {
                return false;
            }

            return await _unitOfWork.DocumentDataRep.Delete(id);
        }

        public async Task<BaseList> GetAll(DocumentDataRquest request)
        {
            return await _unitOfWork.DocumentDataRep.GetAll(request);
        }

        public async Task<BaseList> GetDocuments(int? parentId, int currentUserId, int page = 1, int limit = 20)
        {
            var request = new DocumentDataRquest
            {
                ParentId = parentId ?? -1,
                CurrentUserId = currentUserId,
                RelCode = "CLOUD",
                Page = page,
                Limit = limit
            };
            return await _unitOfWork.DocumentDataRep.GetAll(request);
        }

        public async Task<int> CreateFolder(string name, int? parentId, int userId)
        {
            var item = new DocumentData
            {
                DisplayText = name,
                ParentId = parentId,
                IsFolder = true,
                AccessLevel = 0,
                CreatedBy = userId,
                RelCode = "CLOUD",
                Code = "FOLDER"
            };
            var result = await _unitOfWork.DocumentDataRep.AddOrUpdate(item);
            return result ? 1 : 0;
        }

        public async Task<int> SaveFile(string name, string filePath, int? parentId, int userId)
        {
            var item = new DocumentData
            {
                DisplayText = name,
                ValueFile = filePath,
                ParentId = parentId,
                IsFolder = false,
                AccessLevel = 0,
                CreatedBy = userId,
                RelCode = "CLOUD",
                Code = "FILE"
            };
            var result = await _unitOfWork.DocumentDataRep.AddOrUpdate(item);
            return result ? 1 : 0;
        }

        public async Task<List<int>> GetShares(int documentId)
        {
            return await _unitOfWork.DocumentDataRep.GetShares(documentId);
        }

        public async Task<bool> UpdateShares(int documentId, List<int> userIds)
        {
            var existing = await _unitOfWork.DocumentDataRep.GetShares(documentId);
            foreach (var userId in existing)
            {
                if (!userIds.Contains(userId))
                {
                    await _unitOfWork.DocumentDataRep.RemoveShare(documentId, userId);
                }
            }
            foreach (var userId in userIds)
            {
                if (!existing.Contains(userId))
                {
                    await _unitOfWork.DocumentDataRep.AddShare(documentId, userId);
                }
            }
            return true;
        }

        public async Task<bool> UpdateAccessLevel(int documentId, int accessLevel, int userId)
        {
            var item = await _unitOfWork.DocumentDataRep.GetById(documentId);
            if (item == null || item.CreatedBy != userId) return false;

            item.AccessLevel = accessLevel;
            item.UpdatedBy = userId;
            return await _unitOfWork.DocumentDataRep.AddOrUpdate(item);
        }

        public async Task<DocumentData?> GetByToken(string token)
        {
            return await _unitOfWork.DocumentDataRep.GetByToken(token);
        }

        public async Task<bool> UpdateDisplayText(int id, string text, int userId)
        {
            var item = await _unitOfWork.DocumentDataRep.GetById(id);
            if (item == null || item.CreatedBy != userId) return false;

            item.DisplayText = text;
            item.UpdatedBy = userId;
            return await _unitOfWork.DocumentDataRep.AddOrUpdate(item);
        }

        public async Task<bool> RequestInternalSign(int id, int requestedBy, DateTime requestedAt)
        {
            var item = await _unitOfWork.DocumentDataRep.GetById(id);
            if (item == null || item.Id <= 0 || item.IsFolder || item.IsSignedInternal || item.IsSignatureRequested)
            {
                return false;
            }

            return await _unitOfWork.DocumentDataRep.RequestInternalSign(id, requestedBy, requestedAt);
        }

        public async Task<bool> SignInternal(int id, int signedBy, DateTime signedAt, string signatureHash, string fileHash, string? signNote, string signMethod, string? signedByUserNameSnapshot, string? signedByFullNameSnapshot, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImagePath, string? signatureIntentText, DateTime? termsAcceptedAt = null)
        {
            var item = await _unitOfWork.DocumentDataRep.GetById(id);
            if (item == null || item.Id <= 0 || item.IsFolder || item.IsSignedInternal || !item.IsSignatureRequested)
            {
                return false;
            }

            return await _unitOfWork.DocumentDataRep.SignInternal(id, signedBy, signedAt, signatureHash, fileHash, signNote, signMethod, signedByUserNameSnapshot, signedByFullNameSnapshot, signedIpAddress, signedUserAgent, signedFileArchivePath, signatureImagePath, signatureIntentText, termsAcceptedAt);
        }

        public async Task<BaseList> GetallRegional()
        {
            return await _unitOfWork.LocationRep.GetAll();
        }

        public async Task<DocumentData> GetById(int id)
        {
            return await _unitOfWork.DocumentDataRep.GetById(id);
        }
    }
}

