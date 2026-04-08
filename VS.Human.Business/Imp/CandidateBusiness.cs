using System.Linq;
using Microsoft.AspNetCore.Http;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class CandidateBusiness : BaseBusiness, ICandidateBusiness
    {
        private const string DefaultCandidatePassword = "Vietstar@2026";



        public CandidateBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<bool> Add(CandidateAdd itemAdd)
        {
            var duplicateCandidate = await FindDuplicateForCreate(itemAdd);
            if (duplicateCandidate != null && duplicateCandidate.Id > 0)
            {
                return false;
            }

            var userName = await BuildCandidateUserName(itemAdd);

            var passwordPlain = string.IsNullOrWhiteSpace(itemAdd.Pass) ? DefaultCandidatePassword : itemAdd.Pass;
            var passwordHash = getMD5(passwordPlain);

            var item = new Candidate()
            {
                Name = itemAdd.Name,
                Referrer = itemAdd.Referrer,
                Code = itemAdd.Code,
                DepartmentId = itemAdd.DepartmentId,
                Position = itemAdd.Position,
                CreateAt = DateTime.Now,
                CreatedBy = itemAdd.CreatedBy,
                CVLink = itemAdd.CVLink,
                Dob = itemAdd.Dob,
                Phone = itemAdd.Phone,
                Email = itemAdd.Email,
                Noted = itemAdd.Noted,
                Status = itemAdd.Status,
                Source = itemAdd.Source,
                IsActive = itemAdd.IsActive ?? 1,
                ManagerId = itemAdd.ManagerId,
                NationalId = itemAdd.NationalId,
                Address = itemAdd.Address,
                UserName = userName,
                Pass = passwordHash,
                IsEmployee = 0,
                ExpectedOnboardDate = itemAdd.ExpectedOnboardDate
            };
            item.Noted = itemAdd.Noted;
            item.Dob = itemAdd.Dob;
            item.IsActive = itemAdd.IsActive ?? 1;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(item);
        }

        public async Task<Candidate?> FindDuplicateForCreate(CandidateAdd item)
        {
            return await _unitOfWork.CandidateRep.FindDuplicateForCreate(
                item.Name,
                item.Phone,
                item.Email,
                item.Position,
                item.DepartmentId);
        }



        public async Task<bool> Update(CandidateDetailUpdate itemUpdate)
        {
            var item = new Candidate()
            {
                Name = itemUpdate.Name,


            };
            item.Referrer = itemUpdate.Referrer;
            item.UpdatedBy = GetUserId();
            item.Dob = itemUpdate.Dob;
            item.Phone = itemUpdate.Phone;
            item.ManagerId = itemUpdate.ManagerId;
            item.Id = itemUpdate.Id;
            item.DepartmentId = itemUpdate.DepartmentId;
            item.Position = itemUpdate.Position;
            item.CVLink = itemUpdate.CVLink;
            item.Status = itemUpdate.Status;
            item.StatusHuman = itemUpdate.StatusHuman;
            item.Name = itemUpdate.Name;
            item.Email = itemUpdate.Email;
            item.Noted = itemUpdate.Noted;
            item.UpdatedBy = GetUserId();
            item.NationalId = itemUpdate.NationalId;
            item.Address = itemUpdate.Address;
            item.ExpectedOnboardDate = itemUpdate.ExpectedOnboardDate;

            var result = await _unitOfWork.CandidateRep.AddOrUpdate(item);

            // Auto-update interview result if candidate status is Pass/Pending
            if (result && (itemUpdate.Status == 92 || itemUpdate.Status == 93))
            {
                var schedules = await _unitOfWork.ScheduleInterviewRep.GetAll(new ScheduleInterviewRquest 
                { 
                    RelId = itemUpdate.Id, 
                    Type = -1,
                    Limit = 20,
                    Page = 1
                });
                
                if (schedules?.Data != null && schedules.Data.Cast<object>().Any())
                {
                    var latest = schedules.Data.Cast<ScheduleInterviewIndexModel>()
                        .OrderByDescending(s => s.ScheduleDate ?? s.CreateAt)
                        .FirstOrDefault();

                    if (latest != null && (latest.InterviewResult == null || latest.InterviewResult == 0))
                    {
                        var scheduleEntity = await _unitOfWork.ScheduleInterviewRep.GetById(latest.Id);
                        if (scheduleEntity != null)
                        {
                            scheduleEntity.InterviewResult = 1; // Pass
                            scheduleEntity.UpdatedBy = GetUserId();
                            await _unitOfWork.ScheduleInterviewRep.AddOrUpdate(scheduleEntity);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<bool> UpdateProfile(CandidateProfileUpdate itemUpdate)
        {
            var existing = await _unitOfWork.CandidateRep.GetById(itemUpdate.CandidateId);
            if (existing == null || existing.Id <= 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(itemUpdate.Name))
            {
                existing.Name = itemUpdate.Name;
            }
            if (itemUpdate.Dob.HasValue)
            {
                existing.Dob = itemUpdate.Dob;
            }
            if (!string.IsNullOrWhiteSpace(itemUpdate.Phone))
            {
                existing.Phone = itemUpdate.Phone;
            }
            if (!string.IsNullOrWhiteSpace(itemUpdate.Email))
            {
                existing.Email = itemUpdate.Email;
            }
            if (!string.IsNullOrWhiteSpace(itemUpdate.CVLink))
            {
                existing.CVLink = itemUpdate.CVLink;
            }
            if (!string.IsNullOrWhiteSpace(itemUpdate.NationalId))
            {
                existing.NationalId = itemUpdate.NationalId;
            }
            if (!string.IsNullOrWhiteSpace(itemUpdate.Address))
            {
                existing.Address = itemUpdate.Address;
            }

            existing.UpdatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(existing);
        }
        public Task<bool> Delete(int id, bool reactive = false)
        {
            return _unitOfWork.CandidateRep.Delete(id, reactive);
        }


        public async Task<BaseList> GetAll(CandidateRequest request)
        {
            return await _unitOfWork.CandidateRep.GetAll(request);
        }

        public async Task<bool> HasViewAccess(int candidateId, int userId, string? roleCode)
        {
            return await _unitOfWork.CandidateRep.HasViewAccess(candidateId, userId, roleCode);
        }

        public async Task<bool> HasManageAccess(int candidateId, int userId, string? roleCode)
        {
            return await _unitOfWork.CandidateRep.HasManageAccess(candidateId, userId, roleCode);
        }

        public async Task<Candidate> Login(string userName, string password)
        {
            var passwordGen = getMD5(password);
            return await _unitOfWork.CandidateRep.Login(userName, passwordGen);

        }

        public async Task<bool> ChangePassword(string password, int id)
        {
            var existing = await _unitOfWork.CandidateRep.GetById(id);
            if (existing == null || existing.Id <= 0) return false;

            existing.Pass = getMD5(password);
            existing.UpdatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(existing);
        }

        public async Task<Candidate> GetById(int id)
        {
            return await _unitOfWork.CandidateRep.GetById(id);

        }

        public async Task<bool> ApprovePassInterview(int candidateId)
        {
            var candidate = await _unitOfWork.CandidateRep.GetById(candidateId);
            if (candidate == null || candidate.Id <= 0) return false;

            candidate.Status = 92; // Đậu phỏng vấn
            candidate.UpdatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(candidate);
        }

        public async Task<bool> ApprovePendingEmployee(int candidateId)
        {
            var candidate = await _unitOfWork.CandidateRep.GetById(candidateId);
            if (candidate == null || candidate.Id <= 0) return false;

            candidate.Status = 93; // Nhân viên chờ
            candidate.UpdatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(candidate);
        }

        public async Task<Employee?> Onboard(int candidateId)
        {
            var candidate = await _unitOfWork.CandidateRep.GetById(candidateId);
            if (candidate == null || candidate.Id <= 0)
            {
                return null;
            }

            if (candidate.IsEmployee.HasValue && candidate.IsEmployee.Value == 1)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(candidate.UserName) || string.IsNullOrWhiteSpace(candidate.Pass))
            {
                return null;
            }

            var resolvedUserName = await ResolveOnboardUserName(candidate);
            var existingEmployee = await _unitOfWork.EmployeeRep.GetByUserName(resolvedUserName);
            if (existingEmployee != null && existingEmployee.Id > 0)
            {
                return null;
            }

            var employee = new Employee()
            {
                FullName = candidate.Name,
                Phone = candidate.Phone,
                Email = candidate.Email,
                CVLink = candidate.CVLink,
                NationalId = candidate.NationalId,
                PermanentAddress = candidate.Address,
                Dob = candidate.Dob,
                UserName = resolvedUserName,
                Pass = candidate.Pass,
                RoleCode = "2",
                Status = 1,
                IsActive = 1,
                CreatedBy = GetUserId(),
                UpdatedBy = GetUserId(),
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            var created = await _unitOfWork.EmployeeRep.AddOrUpdate(employee);
            if (!created)
            {
                return null;
            }

            var newEmployee = await _unitOfWork.EmployeeRep.GetLastByEmailOrPhone(candidate.Email ?? string.Empty, candidate.Phone ?? string.Empty);
            if (newEmployee == null || newEmployee.Id <= 0)
            {
                newEmployee = await _unitOfWork.EmployeeRep.GetByUserName(resolvedUserName);
            }
            if (newEmployee == null || newEmployee.Id <= 0)
            {
                return null;
            }

            await _unitOfWork.EmployeeRep.UpdateCredentials(newEmployee.Id, resolvedUserName, candidate.Pass);

            candidate.UserName = resolvedUserName;
            candidate.IsEmployee = 1;
            candidate.EmployeeId = newEmployee.Id;
            candidate.IsActive = 0;
            candidate.UpdatedBy = GetUserId();

            // Update status to 94 (Official Employee)
            candidate.Status = 94;

            await _unitOfWork.CandidateRep.AddOrUpdate(candidate);
            return newEmployee;
        }

        private async Task<string> BuildCandidateUserName(CandidateAdd itemAdd)
        {
            return await BuildCandidateUserName(itemAdd.Name, itemAdd.Email, itemAdd.Phone);
        }

        private async Task<string> BuildCandidateUserName(string? fullName, string? email, string? phone, int ignoreCandidateId = 0)
        {
            var baseUserName = EmployeeMapper.GenerateUserName(email, phone, fullName);
            if (string.IsNullOrWhiteSpace(baseUserName))
            {
                baseUserName = "user" + DateTime.Now.Ticks;
            }

            var finalUserName = baseUserName;
            var counter = 2;

            while (true)
            {
                var existingCandidate = await _unitOfWork.CandidateRep.GetByUserName(finalUserName);
                var existingEmployee = await _unitOfWork.EmployeeRep.GetByUserName(finalUserName);

                var candidateTaken = existingCandidate != null && existingCandidate.Id > 0 && existingCandidate.Id != ignoreCandidateId;
                var employeeTaken = existingEmployee != null && existingEmployee.Id > 0;

                if (!candidateTaken && !employeeTaken)
                {
                    break;
                }

                finalUserName = $"{baseUserName}{counter}";
                counter++;
                // Safety break
                if (counter > 1000) break;
            }

            return finalUserName;
        }

        private async Task<string> ResolveOnboardUserName(Candidate candidate)
        {
            var currentUserName = candidate.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                return await BuildCandidateUserName(candidate.Name, candidate.Email, candidate.Phone, candidate.Id);
            }

            if (currentUserName.StartsWith("CA", StringComparison.OrdinalIgnoreCase)
                || EmployeeMapper.IsLegacyAutoGeneratedUserName(currentUserName, candidate.Name))
            {
                return await BuildCandidateUserName(candidate.Name, candidate.Email, candidate.Phone, candidate.Id);
            }

            return currentUserName;
        }

    }
}
