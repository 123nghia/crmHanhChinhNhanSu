using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class CandidateBusiness : BaseBusiness, ICandidateBusiness
    {



        public CandidateBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<bool> Add(CandidateAdd itemAdd)
        {
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
                IsActive = itemAdd.IsActive,
                ManagerId = itemAdd.ManagerId,
                NationalId = itemAdd.NationalId,
                Address = itemAdd.Address
            };
            item.Noted = itemAdd.Noted;
            item.Dob = itemAdd.Dob;
            item.IsActive = itemAdd.IsActive;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            return await _unitOfWork.CandidateRep.AddOrUpdate(item);
        }


        public async Task<bool> ChangePassword(string password, int id)
        {
            var passwordNew = getMD5(password);

            return await _unitOfWork.EmployeeRep.ChangePassword(passwordNew, id);
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
        public Task<bool> Delete(int id, bool reactive = false)
        {
            return _unitOfWork.EmployeeRep.Delete(id, reactive);
        }


        public async Task<BaseList> GetAll(CandidateRequest request)
        {
            return await _unitOfWork.CandidateRep.GetAll(request);
        }

        public async Task<Employee> Login(string userName, string password)
        {
            var passwordGen = getMD5(password);
            return await _unitOfWork.EmployeeRep.Login(userName, passwordGen);

        }

        public async Task<Candidate> GetById(int id)
        {
            return await _unitOfWork.CandidateRep.GetById(id);

        }
        public async Task<Employee> CheckDuplicate(string email, string phone)
        {
            return await _unitOfWork.EmployeeRep.CheckDuplicate(email, phone);
        }

    }
}
