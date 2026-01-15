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
        private const string DefaultCandidatePassword = "Vietstar@2024";



        public CandidateBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<bool> Add(CandidateAdd itemAdd)
        {
            var userName = itemAdd.UserName;
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = await BuildCandidateUserName(itemAdd);
            }

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
                IsEmployee = 0
            };
            item.Noted = itemAdd.Noted;
            item.Dob = itemAdd.Dob;
            item.IsActive = itemAdd.IsActive ?? 1;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(item);
        }


        public async Task<bool> ChangePassword(string password, int id)
        {
            var passwordNew = getMD5(password);

            return await _unitOfWork.CandidateRep.ChangePassword(passwordNew, id);
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

            return await _unitOfWork.CandidateRep.AddOrUpdate(item);
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

        public async Task<Candidate> Login(string userName, string password)
        {
            var passwordGen = getMD5(password);
            return await _unitOfWork.CandidateRep.Login(userName, passwordGen);

        }

        public async Task<Candidate> GetById(int id)
        {
            return await _unitOfWork.CandidateRep.GetById(id);

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

            var existingEmployee = await _unitOfWork.EmployeeRep.GetByUserName(candidate.UserName);
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
                UserName = candidate.UserName,
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
                newEmployee = await _unitOfWork.EmployeeRep.GetByUserName(candidate.UserName);
            }
            if (newEmployee == null || newEmployee.Id <= 0)
            {
                return null;
            }

            await _unitOfWork.EmployeeRep.UpdateCredentials(newEmployee.Id, candidate.UserName, candidate.Pass);

            candidate.IsEmployee = 1;
            candidate.EmployeeId = newEmployee.Id;
            candidate.IsActive = 0;
            candidate.UpdatedBy = GetUserId();

            var onboardStatus = await _unitOfWork.MasterDataRep.GetByName("Onboarded", 9);
            if (onboardStatus != null && int.TryParse(onboardStatus.Code, out var statusCode))
            {
                candidate.Status = statusCode;
            }

            await _unitOfWork.CandidateRep.AddOrUpdate(candidate);
            return newEmployee;
        }

        private async Task<string> BuildCandidateUserName(CandidateAdd itemAdd)
        {
            var baseUserName = EmployeeMapper.GenerateUserName(itemAdd.Email, itemAdd.Phone, itemAdd.Name);
            var finalUserName = baseUserName;
            var counter = 1;

            while (true)
            {
                var existingCandidate = await _unitOfWork.CandidateRep.GetByUserName(finalUserName);
                var existingEmployee = await _unitOfWork.EmployeeRep.GetByUserName(finalUserName);

                if ((existingCandidate == null || existingCandidate.Id <= 0) && (existingEmployee == null || existingEmployee.Id <= 0))
                {
                    break;
                }

                finalUserName = $"{baseUserName}{counter}";
                counter++;
                if (counter > 100) break;
            }

            return finalUserName;
        }

    }
}
