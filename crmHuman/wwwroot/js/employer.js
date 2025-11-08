
function openPageMember(id = -1) {

    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/GroupPage?handler=FormMember&id=' + id,

        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');
            loadData(id);

        },
        error: function (jqXHR, exception) {
           
        },
        complete: function () {

        },
    });
}

function loadData(groupId) {
   
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/GroupPage?handler=ListDataMember&groupId=' + groupId,

        success: function (data) {

            $("#DataListView").empty();
            $("#DataListView").append(data);
       

        },
        error: function (jqXHR, exception) {
           
        },
        complete: function () {

        },
    });
}

function OpenChangPassword(id = -1, router = "employee") {

    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/' + router + '?handler=FormChangePassword&id=' + id,

        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');

        },
        error: function (jqXHR, exception) {
       
        },
        complete: function () {

        },
    });
}
function openFormEdit(id = -1, router = "employee") {
  
    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",
  
        url: '/' + router + '?handler=FormEdit&id='+id,
     
        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show'); 
      

            

        },
        error: function (jqXHR, exception) {
           
        },
        complete: function () {
            getAllGroup();
            getAllPartner();
            getAllJob();
            loadDataPartner();

            $('.my-select').selectpicker();
            loadCombobx();
        },
    });
}



function openFormAssignee(id = -1, router = "OrderAssignee") {
  
    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",
  
        url: '/' + router + '?handler=openForm&id='+id,
        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);

            const sonucModal= document.getElementById('formModal');
            const modalEl = new bootstrap.Modal(sonucModal);
            modalEl.show();
            // setTimeout(() => {
            //     $('#formModal').modal('show'); 
            // }, 2000);
           
         },
        error: function (jqXHR, exception) {

        },
        complete: function () {
            
         },
    });
}
function login() {


    var userName = $("#txtUserName").val();
    var password = $("#txtPassword").val();
    if (userName == "") {
        addError("txtUserName", "yêu cầu nhập tên đăng nhập");
        return;
    }
    else {
        removeError("txtUserName");
    }
    if (password == "") {
        addError("txtPassword", "Yêu cầu nhập mật khẩu");
        return;
    }
    else {
        removeError("txtUserName");
    }
   
    var dataRequest = {
        UserName: userName,
        Password: password
    };

    submitForm("formLogin");
    return;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/login?handler=Login',
        data: dataRequest,
        success: function (data) {
            if (data.success == true) {
                window.location.href = "/";
            }
         
        },
        error: function (jqXHR, exception) {
          
        },
        complete: function () {
           
        },
    });


}
function updateEmployInfo(idEmp) {

    var updateName = getValueControl("txtUpdateName");
    var updateNote = getValueControl("txtUpdateNote");
      var updatePhone = getValueControl("txtPhoneInput");
    var dayOfBirth = getValueControl("txtDayOfBirthInput");
    removeAllEror("formUpdateEmployee");

    if (updateName == "") {
        addError("txtUpdateName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtUpdateName");
    }
    
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/employee?handler=UpdateInfo',
        data: {
          
            FullName: updateName,
            Id: idEmp,
            Dob:  dayOfBirth,
            Phone : updatePhone,
            Noted: updateNote
           
      
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function changePassword(reloadPage =true) {
    

    var currentPassword = getValueControl("txtcurrentPassword");
    var newPassword = getValueControl("txtnewPassword");
    var renewPassword = getValueControl("txtrenewPassword");
    removeAllEror("updateChangePassword");

  

    if (newPassword == "") {
        addError("txtnewPassword", "yêu cầu nhập mật khẩu mới");
        return;
    }
    else {
        removeError("txtnewPassword");
    }

    if (renewPassword == "") {
        addError("txtrenewPassword", "yêu cầu nhập lại mật khẩu mới");
        return;
    }
    else {
        removeError("txtrenewPassword");
    }

    if (newPassword != renewPassword) {
        addError("txtrenewPassword", "Hai mật khẩu không trùng khớp");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/employee?handler=ChangePassword',
        data: {

            newPassword: newPassword
           


        },
        success: function (data) {

            if (reloadPage == true) {
                successAdd(1);
            }
            else {
                openAlertAndClosePopup(1);
            }
           
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function changePassword2(reloadPage = false, idEmp, reset = false) {


    var currentPassword = getValueControl("txtcurrentPassword");
    var newPassword = getValueControl("txtnewPassword");
    var renewPassword = getValueControl("txtrenewPassword");
    removeAllEror("updateChangePassword");
    if (reset == false) {

        if (newPassword == "") {
            addError("txtnewPassword", "yêu cầu nhập mật khẩu mới");
            return;
        }
        else {
            removeError("txtnewPassword");
        }

        if (renewPassword == "") {
            addError("txtrenewPassword", "yêu cầu nhập lại mật khẩu mới");
            return;
        }
        else {
            removeError("txtrenewPassword");
        }

        if (newPassword != renewPassword) {
            addError("txtrenewPassword", "Hai mật khẩu không trùng khớp");
        }
    }
    else {
        newPassword = "Vietstar@2021";
    }


    

    var isReset = reset;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/employee?handler=ChangePassword',
        data: {

            newPassword: newPassword,
            id: idEmp,
            resetPass: isReset



        },
        success: function (data) {

            if (reloadPage == true) {
                successAdd(1);
            }
            else {
                openAlertAndClosePopup(1);
            }

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function SaveEmployee(idEmp) {

    var code = getValueControl("txtUserName");
    var fullNametext = getValueControl("txtFullName");
    var dobcb = getValueControl("dob");
    var dateOnboardTemp = getValueControl("dateOnboard");
    var txtLineCodeTemp = getValueControl("txtLineCode");
    var phoneText = getValueControl("txtPhone");
    var passText = getValueControl("txtPass");
    var roleCodeText = getValueControl("txtRoleCode");
    var txtColorCode = getValueControl("txtColor");
    var cbStatusWork = getValueControl("cbStatusWork");


    var isActiveCb = 1;
    var txtNotedText = getValueControl("txtNoted");
    removeAllEror("mainForm");

    if (fullNametext == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (phoneText == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }

    if (idEmp < 0) {
        if (passText == "") {
            addError("txtPass", "Yêu cầu nhập mật khẩu");
            return;
        }
        else {
            removeError("txtPass");
        }
    }
   

    if (roleCodeText == "") {
        addError("txtRoleCode", "Yêu cầu chọn thông tin vai trò");
        return;
    }
    else {
        removeError("txtRoleCode");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/employee?handler=AddEmployeee',
        data: {
            UserName: code,
            FullName: fullNametext,
            phone: phoneText,
            Id: idEmp,
            Onboard: dateOnboardTemp,
            LineCode: txtLineCodeTemp,
            RoleCode: roleCodeText,
            Noted: txtNotedText,
            ColorCode :txtColorCode,
            Pass: passText,
            StatusWork: cbStatusWork,
            Dob: dobcb, 
            IsActive: isActiveCb
        },
        success: function (data) {
        
            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
           
        }
    });
}
function SaveGroup(idEmp) {
    var fullNametext = getValueControl("txtName");
    var managerId = getValueControl("cbManagerid");
    var isActiveCb = getValueControl("isActive");
    var txtNotedText = getValueControl("txtNoted");
    removeAllEror("mainForm");
    if (fullNametext == "") {
        addError("txtName", "yêu cầu nhập tên nhóm");
        return;
    }
    else {
        removeError("txtName");
    }
    if (isActiveCb == "") {
        addError("isActive", "Yêu cầu chọn trạng thái");
        return;
    }
    else {
        removeError("isActive");
    }

    if (managerId == "") {
        addError("cbManagerid", "Yêu cầu chọn trưởng nhóm");
        return;
    }
    else {
        removeError("cbManagerid");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/GroupPage?handler=Add',
        data:
        {
            Name: fullNametext,
            ManagerId: managerId,
            Id: idEmp,
            IsActive: isActiveCb
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function openFormGroupEdit(id = -1) {

    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET", 

        url: '/GroupPage?handler=FormEdit&id=' + id,

        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');
        },
        error: function (jqXHR, exception) {
            
        },
        complete: function () {

        },
    });
}
function changePage(pageNumber) {

    var urlcurent = new URL(window.location.href);
    urlcurent.searchParams.set('page', pageNumber);
    window.location.href = urlcurent.toString();

}
var isHasInteract = true;
function deleteRecord(idEmp, routerInput) { 
    isHasInteract = false;
   
   
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: routerInput  +'?handler=delete',
        data: {

            Id: idEmp,

        },
        success: function (data) {

            successAdd(idEmp, true);
            isHasInteract = true;
        },
        error: function (jqXHR, exception) {
            showErrorDelete(jqXHR);
            isHasInteract = true;
        },
        complete: function () {
            isHasInteract = true;

        }
    });
}


function deleteRecord2(idEmp, routerInput) { 
    isHasInteract = false;
   
   
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: routerInput  +'?handler=reactive',
        data: {

            Id: idEmp,

        },
        success: function (data) {

            khoiphucXoa(idEmp, true);
            isHasInteract = true;
        },
        error: function (jqXHR, exception) {
            showErrorDelete(jqXHR);
            isHasInteract = true;
        },
        complete: function () {
            isHasInteract = true;

        }
    });
}
function deleteGroupMember(idEmp, groupId = -1, routerInput = "/GroupPage") {
    isHasInteract = false;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: routerInput + '?handler=deleteMember',
        data: {

            Id: idEmp,

        },
        success: function (data) {

            successDeleteMember(idEmp, true, groupId);
            isHasInteract = true;
        },
        error: function (jqXHR, exception) {
            showErrorDelete(jqXHR);
            isHasInteract = true;
        },
        complete: function () {
            isHasInteract = true;

        }
    });
}


function changeResult(idEmp, routerInput = "order") {

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
          confirmButton: "btn btn-success",
          cancelButton: "btn btn-danger"
        },
        buttonsStyling: false
      });
      swalWithBootstrapButtons.fire({
        title: "Kết quả ứng tuyển",
        text: "Bạn nhấn nút để kết thúc ứng tuển",
        icon: "warning",
        showCancelButton: true,
        showConfirmButton: false,
        confirmButtonText: "Onboard",
        cancelButtonText: "Kết thúc ứng tuyển",
        reverseButtons: true,
         denyButtonText: `Đóng`
      }).then((result) => {
        if (result.isConfirmed) {
            changeResultTD(idEmp,1,swalWithBootstrapButtons );
         
        } else if (
          /* Read more about handling dismissals below */
          result.dismiss === Swal.DismissReason.cancel
        ) {
             changeResultTD(idEmp,2,swalWithBootstrapButtons );
         
        }
      });

  

}


function changeApply(idEmp) {

   
    var  cvlink1 = getValueControl("inputCvlink");

    var  cbJobId1 = getValueControl("cbJobId1");

    if( cbJobId1  < 1  )
    {
        addError("cbJobId1", "Yêu cầu Vị trí Việc làm");
        return;
    }
        

    if( cvlink1 ==""  ||  cvlink1 =="" )
    {
         addError("inputCvlink", "Yêu cầu có file CV");
         return;
    }

    var cbtinhthanh = getValueControl("idProvinces");
    var schoolText = getValueControl("txtSchoolText");
    var idExperenceValue = getValueControl("idExperence");
    var idRankLevelValue = getValueControl("idRankLevel");
    var idGenderValue = getValueControl("idGender");
    var idIntroductionValue = getValueControl("txtIntroduction");
    var genderInput =  getValueControl("idGender");
    var cbdobInput = getValueControl("dob");
    var emailInput = getValueControl("txtEmail");

    if(cbtinhthanh == "" || cbtinhthanh =="-1" ||
    schoolText == "" || 
    idExperenceValue == "" || idExperenceValue =="-1" ||
    idRankLevelValue == "" || idRankLevelValue =="-1" ||
    idGenderValue == "" || idGenderValue =="-1" ||
    idIntroductionValue == "" || idIntroductionValue =="-1" ||
    cbdobInput =="" || cbdobInput == null ||
    emailInput == "" ||  emailInput == null ||
    genderInput == "" || genderInput =="-1")
    {
        Swal.fire({
        icon: "error",
        title: "Điền đầy đủ thông tin",
        text: "vui lòng cập nhật khu vực ứng tuyển, ngày sinh, Email,  trường học, trình độ, kinh nghiệm, giới tính, mục tiêu nghề nghiệp sau khi cập nhật, quay lại thao tác đẩy", 
        footer: 'yêu cầu nghiệp vụ'
        });
        return;

    }
    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
          confirmButton: "btn btn-success",
          cancelButton: "btn btn-danger"
        },
        buttonsStyling: false
      });
      swalWithBootstrapButtons.fire({
        title: "Đẩy qua trang danh sách ứng tuyển",
        text: "Bạn nhấn nút  để xác nhận",
        icon: "warning",
        showCancelButton: true,
        showConfirmButton: true,
        confirmButtonText: "Đẩy qua",
        cancelButtonText: "Bỏ qua",
        reverseButtons: true,
         denyButtonText: `Đóng`
      }).then((result) => {
        if (result.isConfirmed) {
                pushCase(idEmp,1,swalWithBootstrapButtons );
         
        } else if (
          /* Read more about handling dismissals below */
          result.dismiss === Swal.DismissReason.cancel
        ) {
            //  changeResultTD(idEmp,2,swalWithBootstrapButtons );
         
        }
      });

  

}
function changeReturnOrderAlert(idEmp) {

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
          confirmButton: "btn btn-success",
          cancelButton: "btn btn-danger"
        },
        buttonsStyling: false
      });
      swalWithBootstrapButtons.fire({
        title: "Đẩy qua trang danh sách khai thác lại",
        text: "Bạn nhấn nút  để xác nhận",
        icon: "warning",
        showCancelButton: true,
        showConfirmButton: true,
        confirmButtonText: "Đẩy qua",
        cancelButtonText: "Bỏ qua",
        reverseButtons: true,
         denyButtonText: `Đóng`
      }).then((result) => {
        if (result.isConfirmed) {
            changeReturnOrder(idEmp,1,swalWithBootstrapButtons );
         
        } else if (
          /* Read more about handling dismissals below */
          result.dismiss === Swal.DismissReason.cancel
        ) {
            //  changeResultTD(idEmp,2,swalWithBootstrapButtons );
         
        }
      });

  

}


function pushCVCTV(idEmp) {

    const swalWithBootstrapButtons = Swal.mixin({
        customClass: {
          confirmButton: "btn btn-success",
          cancelButton: "btn btn-danger"
        },
        buttonsStyling: false
      });
      swalWithBootstrapButtons.fire({
        title: "Đẩy qua đối tác",
        text: "Bạn nhấn nút  để xác nhận",
        icon: "warning",
        showCancelButton: true,
        showConfirmButton: true,
        confirmButtonText: "Đẩy qua",
        cancelButtonText: "Bỏ qua",
        reverseButtons: true,
         denyButtonText: `Đóng`
      }).then((result) => {
        if (result.isConfirmed) {
            pushCaseCTV(idEmp,1,swalWithBootstrapButtons );
         
        } else if (
          /* Read more about handling dismissals below */
          result.dismiss === Swal.DismissReason.cancel
        ) {
            //  changeResultTD(idEmp,2,swalWithBootstrapButtons );
         
        }
      });

  

}
function pushCaseCTV(idEmp,result,swalWithBootstrapButtons) {

    

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=pushCaseCTV',
        data:
        {
            Id: idEmp
        },
        success: function (data) {

            if(data.success == false)
                {
                    Swal.fire({
                        icon: "error",
                        title: "Oops...",
                        text: "Có lỗi sảy ra!",
                        footer: ''
                      });
                 
                }
        
                swalWithBootstrapButtons.fire({
                    title: "Đã chuyển qua thành công!",
                    text: ".",
                    icon: "success"
                  });
          

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

   
}

function changeReturnOrder(idEmp,result,swalWithBootstrapButtons) {

    

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=changeReturnOrder',
        data:
        {
            Id: idEmp
        },
        success: function (data) {

            if(data.success == false)
                {
                    Swal.fire({
                        icon: "error",
                        title: "Oops...",
                        text: "Có lỗi sảy ra!",
                        footer: ''
                      });
                 
                }
        
            if(result ==1)
                {
                    swalWithBootstrapButtons.fire({
                        title: "Đã chuyển qua thành công!",
                        text: "Danh sách ứng tuyển.",
                        icon: "success"
                      });
                }
             else  {
               
             }
          

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

   
}
function pushCase(idEmp,result,swalWithBootstrapButtons) {

    

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=ChangeStatusApply',
        data:
        {
            OrderId: idEmp
        },
        success: function (data) {

            if(data.success == false)
                {
                    Swal.fire({
                        icon: "error",
                        title: "Oops...",
                        text: "Có lỗi sảy ra!",
                        footer: ''
                      });
                 
                }
        
            if(result ==1)
                {
                    swalWithBootstrapButtons.fire({
                        title: "Đã chuyển qua thành công!",
                        text: "Danh sách ứng tuyển.",
                        icon: "success"
                      });
                }
             else  {
                // swalWithBootstrapButtons.fire({
                //     title: "Ghi nhận kết quả tuyển dụng",
                //     text: "Done",
                //     icon: "error"
                //   });
             }
          

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

   
}
function changeResultTD(idEmp,result,swalWithBootstrapButtons) {

    

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=ChangeResult',
        data:
        {
            Result: result,
            Id: idEmp
        },
        success: function (data) {

            if(data.success == false)
                {
                    Swal.fire({
                        icon: "error",
                        title: "Oops...",
                        text: "Có lỗi sảy ra!",
                        footer: ''
                      });
                 
                }
        
            if(result ==1)
                {
                    swalWithBootstrapButtons.fire({
                        title: "Ghi nhận kết quả tuyển dụng!",
                        text: "Onboard.",
                        icon: "success"
                      });
                }
             else  {
                swalWithBootstrapButtons.fire({
                    title: "Ghi nhận kết quả tuyển dụng",
                    text: "Done",
                    icon: "error"
                  });
             }
          

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

   
}

function confirmDelete(idEmp, routerInput = "employee") {
    var urlrouter = "/" + routerInput;
    if (isHasInteract == false) {
        return;
    }
    isHasInteract = false;
    if (idEmp < 1) {
        isHasInteract = true;
        return;    
    }
    

    Swal.fire({
        title: "Bạn chắc chắn xoá?",
        text: "Cân nhắc trước khi nhấn nút Xoá!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Xoá!"
    }).then((result) => {
        if (result.isConfirmed) {
            deleteRecord(idEmp, routerInput);
        }
        else {
            isHasInteract = true;
        }
    }); 
}

function reActive(idEmp, routerInput = "employee") {
    var urlrouter = "/" + routerInput;
    if (isHasInteract == false) {
        return;
    }
    isHasInteract = false;
    if (idEmp < 1) {
        isHasInteract = true;
        return;    
    }
    

    Swal.fire({
        title: "Bạn chắc chắn thao tác?",
        text: "Active lại nhân viên!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "khôi phục!"
    }).then((result) => {
        if (result.isConfirmed) {
            deleteRecord2(idEmp, routerInput);
        }
        else {
            isHasInteract = true;
        }
    }); 
}



function SaveMember(groupId) {
 
    var isActiveCb = getValueControl("cbmemberId");

    if (isActiveCb == "-1")
        isActiveCb = "";
   
    removeAllEror("mainFormMember");
    if (isActiveCb == "") {
        addError("cbmemberId", "yêu cầu chọn thành viên để thêm vào nhóm");
        return;
    }
    else {
        removeError("cbmemberId");
    }
  
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/GroupPage?handler=AddMember',
        data:
        {
            GroupId: groupId,
            MemberId: isActiveCb
        },
        success: function (data) {

            successAddMember(groupId, false, groupId);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}




function openFormCommon(id = -1, controller = "partner", type ="") {

    var dataString = "id=" + id;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/' + controller + '?handler=FormEdit&id=' + id,

        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');



        },
        error: function (jqXHR, exception) {
            debugger;
        },
        complete: function () {

        },
    });
}



function SavePartner(idEmp) {

    var code = getValueControl("txtCode");
    var fullNametext = getValueControl("txtFullName");
    var isActiveCb = getValueControl("isActive");
    var txtNotedText = getValueControl("txtNoted");
    var txtShortNameValue = getValueControl("txtShortName");
    var txtTaxCodeValue  = getValueControl("txtTaxCode");

    var parrentChild = document.getElementById("lisItemAdd")
    var allInputList =  parrentChild.querySelectorAll("input");

    var arrayList = [];

   
    for (let index = 0; index < allInputList.length; index++) {
        const intpuText = allInputList[index];
        var textValueTemp = intpuText.value;
        var idElement = intpuText.getAttribute("customvalue");
        var itemAdd = {
            Text : textValueTemp ,
            Id:  idElement,
            RelId: idEmp,
            Type : "1"
        };
        arrayList.push(itemAdd);
    }



    var parrentChild2 = document.getElementById("listProject")
    var allInputList2 =  parrentChild2.querySelectorAll("input");

    var arrrayProject = [];

   
    for (let index = 0; index < allInputList2.length; index++) {
        const intpuText = allInputList2[index];
        var textValueTemp = intpuText.value;
        var idElement = intpuText.getAttribute("customvalue");
        var itemAdd = {
            Text : textValueTemp ,
            Id:  idElement,
            RelId: idEmp,
            Type : "2"
        };
        arrrayProject.push(itemAdd);
    }



    removeAllEror("mainForm");
 

    
    if (fullNametext == "") {
        addError("txtFullName", "yêu cầu nhập tên đối tác");
        return;
    }
    else {
        removeError("txtFullName");
    }
      if (isActiveCb == "") {
        addError("isActiveCb", "Yêu cầu chọn trạng thái");
        return;
    }
    else {
        removeError("isActiveCb");
    }

    var bodyResquest = { 
            Name: fullNametext,
            addressList: arrayList,
            projectList: arrrayProject,
            Id: idEmp,
            ShortName: txtShortNameValue,
            TaxCode: txtTaxCodeValue,
            Noted: txtNotedText,
            IsActive: isActiveCb
     };

   
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/partner?handler=Add',
        data:bodyResquest,
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function SaveMasterData2(idEmp, namecontroller = "nganhnghe") {

    var code = getValueControl("txtCode");
    var fullNametext = getValueControl("txtFullName");
    var isActiveCb = getValueControl("isActive");
    var txtNotedText = getValueControl("txtNoted");
    var txtTypeDataText = getValueControl("txtTypeData");
    
    var txtExtraText = getValueControl("txtExtra");
    var applyFor = getValueControl("applyFor");
    removeAllEror("mainForm");

    if (fullNametext == "") {
        addError("txtFullName", "yêu cầu nhập tên đối tác");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (isActiveCb == "") {
        addError("isActiveCb", "Yêu cầu chọn trạng thái");
        return;
    }
    else {
        removeError("isActiveCb");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/' + namecontroller +'?handler=Add',
        data: {

            Name: fullNametext,

            Id: idEmp,
            Noted: txtNotedText,
            Extra: txtExtraText,
            applyFor: applyFor,
            typeData:  txtTypeDataText,

            IsActive: isActiveCb
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function SaveMasterData(idEmp, namecontroller = "nganhnghe") {

    var code = getValueControl("txtCode");
    var fullNametext = getValueControl("txtFullName");
    var isActiveCb = getValueControl("isActive");
    var txtNotedText = getValueControl("txtNoted");


    removeAllEror("mainForm");

    if (fullNametext == "") {
        addError("txtFullName", "yêu cầu nhập tên đối tác");
        return;
    }
    else {
        removeError("txtFullName");
    }



    if (isActiveCb == "") {
        addError("isActiveCb", "Yêu cầu chọn trạng thái");
        return;
    }
    else {
        removeError("isActiveCb");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/' + namecontroller +'?handler=Add',
        data: {

            Name: fullNametext,

            Id: idEmp,

            Noted: txtNotedText,

            IsActive: isActiveCb
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function saveJob(idEmp) {


    var fullNametext = getValueControl("txtName");
    var txtWarrantyValue = getValueControl("txtWarrantydate");

    var cbField = getValueControl("cbField");
    var cbCareerId = getValueControl("cbCareerId");
    var txtNotedText = getValueControl("txtNoted");

    var contentText = getValueControl("txtContent");
    var shortDesText = getValueControl("txtShortDes");
    var cbisActive = getValueControl("cbisActive");
    var inputfileValue = getValueControl("txtinputfile");
    var partnerIdValue = getValueControl("cbpartnerId4");
    var projectIdValue = getValueControl("cbProject");

    removeAllEror("mainForm");

    if (fullNametext == "") {
        addError("txtName", "yêu cầu nhập tiêu đề việc làm");
        return;
    }
    else {
        removeError("txtName");
    }

   

    if (cbField == "") {
        addError("cbField", "Yêu cầu nhập lĩnh vực");
        return;
    }
    else {
        removeError("cbField");
    }

    if (txtWarrantyValue == "" || txtWarrantyValue < 1) {
        addError("txtWarrantydate", "Yêu cầu nhập số ngày bảo hành cho job");
        return;
    }
    else {
        removeError("txtWarrantydate");
    }



    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/job?handler=Add',
        data: {
             Name: fullNametext,
            Field: cbField,
            Id: idEmp,
            CareerId: "1",
            Noted: txtNotedText,
            Content: contentText,
            ShortDes: shortDesText,
            IsActive: cbisActive,
            WarrantyDate : txtWarrantyValue,
            Inputfile : inputfileValue,
            partnerId:  partnerIdValue,
            projectId: projectIdValue
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function SaveCandidatenew(idEmp) {

    var txtFullName = getValueControl("txtFullName");
    var txtPhone = getValueControl("txtPhone");
    var dobcb = getValueControl("dob");
    var txtNoted = getValueControl("txtNotedCand");

    var txtEmail = getValueControl("txtEmail");
    var txtSourceCode = 0;
    if(txtSourceCode =="")
        {
            txtSourceCode = 0;
        }


    var txtShortDes = getValueControl("txtNotedCand");

    var cbisActive = 1;

    var fileCvLinkInput = getValueControl("inputCvlink1");

    removeAllEror("mainForm");

    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (txtPhone == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }
    


    if (cbisActive == "") {
        addError("cbisActive", "yêu cầu nhập trạng thái");
        return;
    }
    else {
        removeError("cbisActive");
    }


    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/candidate?handler=Add',
        data: {
           
            Phone: txtPhone,
            Name: txtFullName,
            Email: txtEmail,
            Id: idEmp,
            Dob: dobcb,
            AvatarLink: "",
            CVLink: fileCvLinkInput,
            ShortDes:"",
            Noted: txtNoted ,
            Source : 0,
            IsActive: 1
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function SaveCandidate(idEmp) {

    var txtFullName = getValueControl("txtFullName");
    var txtPhone = getValueControl("txtPhone");
    var dobcb = getValueControl("dob");
    var txtNoted = getValueControl("txtNoted");
    var txtEmail = getValueControl("txtEmail");
    var txtSourceCode = getValueControl("cbSource");

    if(txtSourceCode =="")
        {
            txtSourceCode = 0;
        }


    var txtShortDes = getValueControl("txtShortDes");

    var cbisActive = getValueControl("cbisActive");

    var fileCvLinkInput = getValueControl("inputCvlink1");
    var avatarFileInput = getValueControl("avatarFile");
    removeAllEror("mainForm");

    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (txtPhone == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }
    


    if (cbisActive == "") {
        addError("cbisActive", "yêu cầu nhập trạng thái");
        return;
    }
    else {
        removeError("cbisActive");
    }


    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/candidate?handler=Add',
        data: {
           
            Phone: txtPhone,
            Name: txtFullName,
            Email: txtEmail,
            Id: idEmp,
            Dob: dobcb,
            AvatarLink: avatarFileInput,
            CVLink: fileCvLinkInput,
            ShortDes: txtShortDes,
            Noted: txtNoted ,
            Source : txtSourceCode,
            IsActive: cbisActive
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function saveOrderMarketting(idEmp) {
     //var ProjectIdValue = getValueControl("cbProject");
    var cbcandidateId = getValueControl("cbcandidateId");
    var cbJobId = getValueControl("cbJobId");
    // var cbPartnerId = getValueControl("cbpartnerId2");
    var txtShortDes = getValueControl("txtShortDes");
    var txtNoted = getValueControl("txtNoted");
    var txtSourceCode = getValueControl("txtNoted");
    var cvLink = getValueControl("inputCvlink");
    removeAllEror("mainForm2");
    if (cbcandidateId == "") {
        addError("cbcandidateId", "Chọn thông tin ứng cử viên");
        return;
    }
    else {
        removeError("cbcandidateId");
    }
    if (cbJobId == "") {
        addError("cbJobId", "Yêu cầu chọn thông tin vị trí");
        return;
    }
    else {
        removeError("cbJobId");
    }
   
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=AddMarketting',
        data: {
            CandidateId: cbcandidateId,
            JobId: cbJobId,
            CVLink: cvLink,
            ShortDes: txtShortDes,
            Noted: txtNoted, 
            id: idEmp
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function assingeeOrder(idEmp) {
   
   var asssigneeId = getValueControl("cbAssingeeId");
   removeAllEror("mainForm2");
   if (asssigneeId == "") {
       addError("cbAssingeeId", "Chọn thông tin TC");
       return;
   }
   else {
       removeError("cbAssingeeId");
   }
     $.ajax({
       headers: {
           "RequestVerificationToken":
               $('input[name="__RequestVerificationToken"]').val()
       },
       type: "POST",
       datatype: "JSON",
       url: '/OrderAssignee?handler=Assingee',
       data: {
            Assignee: asssigneeId,
           id: idEmp
       },
       success: function (data) {

           successAdd(idEmp);
       },
       error: function (jqXHR, exception) {
           showError(jqXHR);
       },
       complete: function () {

       }
   });
}


function saveOrder(idEmp) {
    var ProjectIdValue = getValueControl("cbProject");
    var cbcandidateId = getValueControl("cbcandidateId");
    var cbJobId = getValueControl("cbJobId1");
    var cbPartnerId = getValueControl("cbpartnerId2");
    var txtShortDes = getValueControl("txtShortDes");
    var txtNoted = getValueControl("txtNoted");
    var cvLink = getValueControl("inputCvlink");
    removeAllEror("mainForm2");
    if (cbcandidateId == "") {
        addError("cbcandidateId", "Chọn thông tin ứng cử viên");
        return;
    }
    else {
        removeError("cbcandidateId");
    }
    if (cbJobId == "") {
        addError("cbJobId", "Yêu cầu chọn thông tin vị trí");
        return;
    }
    else {
        removeError("cbJobId");
    }
 
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=Add',
        data: {
         
            CandidateId: cbcandidateId,
            JobId: cbJobId, 
            ProjectId:  ProjectIdValue,
            PartnerId : cbPartnerId,
            CVLink: cvLink,
            ShortDes: txtShortDes,
            Noted: txtNoted, 
            id: idEmp

        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function saveOrder2(idEmp) {

    var cbcandidateId = getValueControl("cbcandidateId");
    var cbJobId = getValueControl("cbJobId1");
    var phoneNumber = getValueControl("txtPhoneCall");
    var txtShortDes = getValueControl("txtShortDes");
    var txtNoted = getValueControl("txtNoted");
    var cvLink = getValueControl("inputCvlink");
    removeAllEror("mainForm2");
    if (cbcandidateId == "") {
        addError("cbcandidateId", "Chọn thông tin ứng cử viên");
        return;
    }
    else {
        removeError("cbcandidateId");
    }
    if (cbJobId == "") {
        addError("cbJobId", "Yêu cầu chọn thông tin vị trí");
        return;
    }
    else {
        removeError("cbJobId");
    }
   
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=Add',
        data: {
         
            CandidateId: cbcandidateId,
            JobId: cbJobId, 
            CVLink: cvLink,
            ShortDes: txtShortDes,
            PhoneNumber: phoneNumber,
            Noted: txtNoted, 
            id: idEmp

        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function saveInfoCV(idEmp, saveungtuyen =false) {

    var cbtinhthanh = getValueControl("idProvinces");
    var schoolText = getValueControl("txtSchoolText");
    var idExperenceValue = getValueControl("idExperence");
    var idRankLevelValue = getValueControl("idRankLevel");
    var idGenderValue = getValueControl("idGender");
    var idIntroductionValue = getValueControl("txtIntroduction");
    var cbcandidateId = getValueControl("txtcanddiateInfo")
    var fullName = getValueControl("txtFullName");
    var dobTextBox = getValueControl("dob");
    var phoneInputText = getValueControl("txtPhoneCall");
    var txtEmail= getValueControl("txtEmail");
    var txtShortDes = getValueControl("txtShortDes");
    var txtNoted = getValueControl("txtNoted");
    var cvLink = getValueControl("inputCvlink");
    var notedCan = getValueControl("txtNoted");
    var cbJobId = getValueControl("cbJobId1");

    removeAllEror("mainForm2");
    if (fullName == "") {
        addError("txtFullName", "Điền thông tin họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (phoneInputText == "") {
        addError("txtPhoneCall", "Điền thông tin số điện thoại");
        return;
    }
    else {
        if(    phoneInputText.match(/\d/g).length  != 10    )
        {
            addError("txtPhoneCall", "Số điện thoại không hợp lệ");
            return;
        }
        else 
        {
            removeError("txtPhoneCall");
        }
    }

    if(saveungtuyen ==true)
    {
        if (cbtinhthanh == "" || cbtinhthanh =="-1") {
            addError("idProvinces", "Điền thông tin tỉnh thành khu vực ứng tuyển");
            return;
        }
        else {
            removeError("idProvinces");
        }
    
        if (schoolText == "" || schoolText =="-1") {
            addError("txtSchoolText", "Trường học bắt buộc phải nhập");
            return;
        }
        else {
            removeError("txtSchoolText");
        }
     
        if (idRankLevelValue == "" || idRankLevelValue =="-1") {
            addError("idRankLevel", "Chọn trình độ tương ứng với ứng cử viên");
            return;
        }
        else {
            removeError("idRankLevel");
        }
    
    
        if (idExperenceValue == "" || idExperenceValue =="-1") {
            addError("idExperence", "Chưa chọn mức giói tính");
            return;
        }
        else {
            removeError("idExperence");
        }
    
    
        if (idExperenceValue == "" || idExperenceValue =="-1") {
            addError("idExperence", "chọn  mức kinh nghiệm của ứng viên");
            return;
        }
        else {
            removeError("idExperence");
        }
        if (idGenderValue == "" || idGenderValue =="-1") {
            addError("idGender", "Chưa chọn giới tính");
            return;
        }
        else {
            removeError("idGender");
        }
        if (idIntroductionValue == "" || idIntroductionValue =="-1") {
            addError("txtIntroduction", "Chưa điền thông tin mục tiêu nghề nghiệp");
            return;
        }
        else {
            removeError("txtIntroduction");
        }
    }

  

    var bodyData = {
        PhoneNumber: phoneInputText,
        FulName: fullName,
        Email: txtEmail,
        Dob: dobTextBox,
        RequestId: cbcandidateId,
        JobId: cbJobId ,
        ShortDes : txtShortDes ,
        CVLink: cvLink,
        Noted: txtNoted ,
        regional :  cbtinhthanh,
        schoolName :  schoolText,
        experience :  idExperenceValue,
        rankLevel :  idRankLevelValue,
        Gender :  idGenderValue,
        Introduction :idIntroductionValue
    };

      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/order?handler=AddInfo',
        data: bodyData ,
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function saveCanddiateOrder(idEmp) {

    var cbcandidateId = getValueControl("cbcandidateId");
    var txtFullName = getValueControl("txtFullName");
    var dobCan = getValueControl("dob");
    var txtReferrerInput = getValueControl("txtReferrer");
    var phoneNumber = getValueControl("txtPhone");
    var EmailText = getValueControl("txtEmail");
    var txtShortDes = "";
    var txtNotedCand = getValueControl("txtNotedCand");
     var cvLinkInput = getValueControl("inputCvlink");
     var cbDepartmentIdInput = getValueControl("cbDepartmentId");
     var cbPositionInput = getValueControl("cbPosition");

     var cbManagerId = getValueControl("cbManager");
    // var statusAplly = getValueControl("statusAplly");
    // var txtShortDesOrder = getValueControl("txtShortDesOrder");
    removeAllEror("mainForm");
    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (phoneNumber == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }
  
    
        var bodyRequest = {
        Name: txtFullName,
        Dob: dobCan,
        Phone: phoneNumber,
        ManagerId: cbManagerId,
        CandidateId: idEmp,
        ShortDes: txtShortDes,
        CVLink: cvLinkInput,
        // statusAplly: statusAplly,
        Email: EmailText,
        Referrer      : txtReferrerInput,
        NotedCan: txtNotedCand, 
        DepartmentId:  cbDepartmentIdInput,
        Position:  cbPositionInput,
        ShortDesOrder :txtNotedCand
        };
   console.log(bodyRequest);
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/Candidate?handler=Add',
        data:  bodyRequest,
        success: function (data) {
            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
        }
    });
}


function saveCanddiateDetail(idEmp) {

    var cbcandidateId = getValueControl("inputId");
    var txtFullName = getValueControl("txtFullName");
    var dobCan = getValueControl("dob");
    var phoneNumber = getValueControl("txtPhone");
    var EmailText = getValueControl("txtEmail");
    var txtShortDes = "";
    var txtNotedCand = getValueControl("txtNotedCand");
     var cvLinkInput = getValueControl("inputCvlink");
     var cbDepartmentIdInput = getValueControl("cbDepartmentId");
     var cbPositionInput = getValueControl("cbPosition");
     var cbManagerInput = getValueControl("cbManager");
     var cbStatusHumanInput = getValueControl("cbStatusHuman");
     var txtStatusInput = getValueControl("cbStatus");
     var txtReferrerInput = getValueControl("txtReferrer");
    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (phoneNumber == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }
    var bodyRequest = {
        CandidateId: cbcandidateId,
        ShortDes: txtShortDes,
        CVLink: cvLinkInput,
        // statusAplly: statusAplly,
        Phone: phoneNumber,
        Email: EmailText,
        Name: txtFullName,
        Dob: dobCan,
        Noted: txtNotedCand, 
        DepartmentId:  cbDepartmentIdInput,
        Position:  cbPositionInput,
        ShortDesOrder :txtNotedCand,
        ManagerId : cbManagerInput,
        StatusHuman :cbStatusHumanInput,
        Status  : txtStatusInput,
        Referrer : txtReferrerInput
    };

  debugger;
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/CandidateDetail?handler=Update',
        data:  bodyRequest,
        success: function (data) {
            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
        }
    });
}

function saveImpact(orderCode) {
    var cbPartnerId2Value = getValueControl("cbpartnerId3");
    var cbSelectStatus1 = getValueControl("cbSelectStatus");
    var dateFromVal1 = getValueControl("dateFrom");
    var txtTimer1 = getValueControl("txtTimer");
    var txtPlace1 = getValueControl("txtPlace");
    var txtNotedExtra1 = getValueControl("txtNotedExtra");
 
    var radioOther =   document.getElementById('checkDefaultAddress').checked;

    if(radioOther == true)
    {

        txtPlace1 = $('#cbAddress :selected').text(); 
    } 



    removeAllEror("formSecond");

    

    if (cbSelectStatus1 == "") {
        addError("cbSelectStatus", "Chưa chọn tình trạng trạng thái hồ sơ");
        return;
    }
    else {
        removeError("cbSelectStatus");

    }

  

    if(cbSelectStatus1 == 1 )
    {

        if (dateFromVal1 == "") {
            addError("dateFrom", "Chưa chọn thông tin ngày");
            return;
        }
        else {
            removeError("dateFrom");
    
        }


        if (txtTimer1 == "") {
            addError("txtTimer", "Chưa chọn thông tin giờ ");
            return;
        }
        else {
            removeError("txtTimer");
    
        }

        
    }
    if (isBlank(txtNotedExtra1)) {
        addError("txtNotedExtra", "Chưa có thông tin ghi chú ");
        return;
    }
    else {
        removeError("txtNotedExtra");

    }
    

if( cbSelectStatus1 == 47)
    {
        if (isBlank(dateFromVal1)) {
            addError("dateFrom", "Cung cấp ngày phỏng vấn");
            return;
        }
        else {
            removeError("dateFrom");
    
        }
    }


    if( cbSelectStatus1 == 12)
    {
        if (isBlank(dateFromVal1)) {
            addError("dateFrom", "Cung cấp ngày Onboard");
            return;
        }
        else {
            removeError("dateFrom");
    
        }
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/OrderDetail?handler=Add',
        data: {
            OrderCode: orderCode,
            Noted: txtNotedExtra1,
            dateFrom:  dateFromVal1,
            txtTimer: txtTimer1,
            NewStatus: cbSelectStatus1, 
            txtPlace : txtPlace1,
            radioOtherAdress:  radioOther,
            PartnerId: cbPartnerId2Value
        },
        success: function (data) {
          
            successAddImpact(orderCode);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function openFormModal()
{
    $('#exampleModalCenter').modal('show'); 
}

function closeForm()
{
    $('#exampleModalCenter').modal('hide'); 
} 

function closeFormUser()
{
    $('#dataUser').modal('hide'); 
} 


function UploadImage1() {


    var fileInput = document.getElementById("fileImport");
 
    if (fileInput.files.length < 1)
        return;
    var fileAccess = fileInput.files[0];
    var formData = new FormData();
    formData.append('FileRequest', fileAccess);

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        processData: false,  // tell jQuery not to process the data
        contentType: false,
        url: '/Candidate?handler=ImportSource',
        data: formData ,
        success: function (data, reponse) {
               
               Swal.fire({
                    position: "center",
                    icon: "success",
                    title: "import thành công",
                    showConfirmButton: false,
                    timer: 2000
                }).then((result) => {
                    // loadData(groupId);
                });
            
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

}

function  exportFileDashboard () {
    
    Swal.fire({
        title: 'Đang chuẩn bị thông tin file '
        });
        Swal.showLoading();

        var fromDate = getValueControl("fromDate");
        var endDate = getValueControl("toDate");
        var jobId = getValueControl("cbjob");
        var statusId = getValueControl("cbstatus2");
        $.ajax({
            headers: {
                "RequestVerificationToken":
                    $('input[name="__RequestVerificationToken"]').val()
            },
            type: "POST",
            url: '/FileReport?handler=FileDashboard',

            data: {
                From: fromDate,
                To:  endDate,
                job: jobId, 
                status: statusId
            },
            success: function (data) {

                    Swal.fire({
                        title: "File báo cáo đã sẵn sàng",
                        text: "nhấn nút tải xuống để tải file về!",
                        icon: "warning",
                        showCancelButton: true,
                        confirmButtonColor: "#3085d6",
                        cancelButtonColor: "#d33",
                        confirmButtonText: "Tải file xuống"
                    }).then((result) => {
                        if (result.isConfirmed && data.success==true) {

                            var link = document.createElement("a");
                            var fileName= "reportDashboard_"+ new Date().getTime() + ".xlsx";
                            link.setAttribute('download', fileName);
                            link.href = data.linkResult;
                            document.body.appendChild(link);
                            link.click();
                            link.remove(); 
                        }
                    });

             },
            error: function (jqXHR, exception) {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "Something went wrong!",
                    footer: '<a href="#">Why do I have this issue?</a>'
                  });
            },
            complete: function () {
    
            },
        });

       
}


function isVietnamesePhoneNumber(number) {
    
    return /([\+84|84|0]+(3|5|7|8|9|1[2|6|8|9]))+([0-9]{8})\b/.test(number);
  }

  function validateEmail(email) {
    var re = /\S+@\S+\.\S+/;
    return re.test(email);
  }
function saveOrderCandidateMarketting(idEmp,waysave= false) {
    //var ProjectIdValue = getValueControl("cbProject");
   var cbcandidateId = getValueControl("cbcandidateId");
    var fullName = getValueControl("txtFullName");

    var birthDay = getValueControl("dob");

    var emailCand = getValueControl("txtEmail");

    var phoneCand = getValueControl("txtPhone");

    var cbSource  = getValueControl("cbSource");

   var txtNotedCand = getValueControl("txtNoted");

   var cvLink = getValueControl("inputCvlink");

   var cbJobId = getValueControl("cbJobId");

   // var cbPartnerId = getValueControl("cbpartnerId2");
   var txtShortDes = getValueControl("txtShortDes");
   var txtNotedOrder = getValueControl("txtNotedOrder");
   var txtSourceCode = getValueControl("txtNoted");

   var idProvincesValue = getValueControl("idProvinces");
  
   removeAllEror("mainForm");
   if (cbcandidateId == "") {
       addError("cbcandidateId", "Chọn thông tin ứng cử viên");
       return;
   }
   else {
       removeError("cbcandidateId");
   }

    if (fullName == "") {
            addError("txtFullName", "Chưa chọn tên");
            return;
    }
    else {
            removeError("txtFullName");
    }


    if( phoneCand == "" )
    {
        addError("txtPhone", "Số điện thoại bắt buộc nhập");

    
        return;
    }
   
    else
    {

        if (phoneCand.length < 10) {
            addError("txtPhone", "Số điện thoại không hợp lệ");
            return;
        }

        if( phoneCand.length > 0)
        {
            if(!isVietnamesePhoneNumber(phoneCand))
            {
                addError("txtPhone", "Số điện thoại không đúng format, vui lòng kiểm tra lại");
                return;
             
            }
            else 
            {
                removeError("txtPhone");
            }
        }

        if( emailCand.length > 0)
        {
            if(!validateEmail(emailCand))
            {
                addError("txtEmail", "Email không đúng format, vui lòng kiểm tra lại");
                return;
            }
            else 
            {
                removeError("txtEmail");
            }
        }
     }


    if( waysave ==true)
    {
        if( cbJobId == ""  || cbJobId == "-1")
            {
                addError("cbJobId", "Chưa chọn thông tin vị trí việc làm");
                return;
            }
            else 
            { 
                // removeError("cbJobId");
                // if( cvLink =="" &&  txtShortDes =="" )
                // {
                //     addError("inputCvlink", "Chưa đính kèm CV hoặc link tài liệu");
                //     return;
                // }
                // else {
                //     removeError("inputCvlink");
                //     removeError("txtShortDes");
                // }
        
            }

    }
    else {
        cbJobId  = -1;
    }


    if( idProvincesValue == "" || idProvincesValue == "-1")
    {
        addError("idProvinces", "Chọn khu vực ứng tuyển");
        return;
    }

    
   
     $.ajax({
       headers: {
           "RequestVerificationToken":
               $('input[name="__RequestVerificationToken"]').val()
       },
       type: "POST",
       datatype: "JSON",
       url: '/candidate?handler=AddCandidateOrderMarketting',
       data: {
            Id: cbcandidateId,
           Name: fullName,
           Dob: birthDay,
           Email: emailCand,
           PhoneNumber: phoneCand,
           Source: cbSource,
           SaveOrder: waysave,
           CVLink: cvLink,
           JobId: cbJobId,
           Document: txtShortDes,
           NotedOrder: txtNotedOrder,
           NotedCan: txtNotedCand, 
           regional: idProvincesValue,
           id: idEmp
       },
       success: function (data) {

           successAdd(idEmp);
       },
       error: function (jqXHR, exception) {
        debugger;
           showError(jqXHR);
       },
       complete: function () {

       }
   });
}


document.addEventListener("DOMContentLoaded", () => {

    // const config = {
    //     type: 'pie',
    //     data: data,
    //   };
    //   const data = {
    //     labels: [
    //       'Red',
    //       'Blue',
    //       'Yellow'
    //     ],
    //     datasets: [{
    //       label: 'My First Dataset',
    //       data: [300, 50, 100],
    //       backgroundColor: [
    //         'rgb(255, 99, 132)',
    //         'rgb(54, 162, 235)',
    //         'rgb(255, 205, 86)'
    //       ],
    //       hoverOffset: 4
    //     }]
    //   };

    //     const ctx = document.getElementById('myChart');

    //     new Chart(ctx,config);
});

function OpenPageOrder()
{
    window.location.href ="/OrderDetailNew?OrderId=-1";
}

function editOnboard(){
   

}



function SaveOnboard(idEmp) {

    var onboarDay = getValueControl("dateOnboard");
    var cbOnboarded = getValueControl("cbOnboarded");

    if( cbOnboarded ==1)
    {
        if(onboarDay =='')
        {
            return;
        }
    }
     $.ajax({
        headers: {
                "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/OrderOnboard?handler=Update',
        data: {
           
            OrderId: idEmp,
            Result:  cbOnboarded,
            DateOnboard: onboarDay
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function openFormModalChart(itemDAta)
{
   renderChartCV(itemDAta);
    $('#exampleModalCenter').modal('show'); 
}

function opneFormActiveUser()
{ 
     $('#dataUser').modal('show'); 

}




// function SaveMasterData2(idEmp, namecontroller = "nganhnghe") {

//     var code = getValueControl("txtCode");
//     var fullNametext = getValueControl("txtFullName");
//     var isActiveCb = getValueControl("isActive");
//     var txtNotedText = getValueControl("txtNoted");
//     var txtTypeDataText = getValueControl("txtTypeData");

//     removeAllEror("mainForm");

//     if (fullNametext == "") {
//         addError("txtFullName", "yêu cầu nhập tên đối tác");
//         return;
//     }
//     else {
//         removeError("txtFullName");
//     }



//     if (isActiveCb == "") {
//         addError("isActiveCb", "Yêu cầu chọn trạng thái");
//         return;
//     }
//     else {
//         removeError("isActiveCb");
//     }

//     $.ajax({
//         headers: {
//             "RequestVerificationToken":
//                 $('input[name="__RequestVerificationToken"]').val()
//         },
//         type: "POST",
//         datatype: "JSON",
//         url: '/' + namecontroller + '?handler=Add',
//         data: {

//             Name: fullNametext,

//             Id: idEmp,

//             Noted: txtNotedText,

//             IsActive: isActiveCb,
//             typeData:  txtTypeDataText
//         },
//         success: function (data) {

//             successAdd(idEmp);
//         },
//         error: function (jqXHR, exception) {
//             showError(jqXHR);
//         },
//         complete: function () {

//         }
//     });
// }



function openFormMasterData(id = -1, controller = "partner", type = "") {

    var dataString = "id=" + id;

    if (type != "") {
        dataString += "&typeData=" + type;
    }



    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/' + controller + '?handler=FormEdit&' + dataString,
        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');



        },
        error: function (jqXHR, exception) {
            debugger;
        },
        complete: function () {

        },
    });
}


function OpenFormImportCandidate() {
    var controller =  "Candidate";
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "GET",

        url: '/' + controller + '?handler=FormImportCandidate',
        success: function (data) {
            $("#contentModal").empty();
            $("#contentModal").append(data);
            $('#formModal').modal('show');



        },
        error: function (jqXHR, exception) {
            
        },
        complete: function () {

        },
    });
}

function saveMasterTypeData(idEmp, namecontroller = "MasterDataPage") {


    namecontroller = "MasterDataPage";

    var fullNametext = getValueControl("txtFullName");
    var isActiveCb = getValueControl("isActive");
    var txtNotedText = getValueControl("txtNoted");

    var txtExtraText = getValueControl("txtExtra");
    var applyFor = getValueControl("applyFor");
    var txtTypeDataValue = getValueControl("txtTypeData");


    removeAllEror("mainForm");

    if (fullNametext == "") {
        addError("txtFullName", "yêu cầu nhập tên đối tác");
        return;
    }
    else {
        removeError("txtFullName");
    }



    if (isActiveCb == "") {
        addError("isActiveCb", "Yêu cầu chọn trạng thái");
        return;
    }
    else {
        removeError("isActiveCb");
    }

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/' + namecontroller + '?handler=Add',
        data: {

            Name: fullNametext,

            Id: idEmp,

            Noted: txtNotedText,
            Extra: txtExtraText,
            applyFor: applyFor,
            TypeData: txtTypeDataValue,

            IsActive: isActiveCb
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


var saveImport = true;
function importCandidateToEmployee() {
    // saveImport =false;
  
     var  namecontroller = "Employee";
    var selectCandidateid = getValueControl("cbSelectCandidate");
  
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/' + namecontroller + '?handler=ConvertToEmployee',
        data: {
           
            Id: selectCandidateid
        },
        success: function (data) {
            saveImport = true;
            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}




function saveSchedule(idEmp) {

    var dateScheduleDateInput = getValueControl("dateScheduleDate");
    var txtAddressInfoInput = getValueControl("txtAddressInfo");
    var txtScheduleNotedInput = getValueControl("txtScheduleNoted");

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/CandidateDetail?handler=AddSchedule',
        data: {
           
            RelId :  idEmp,
            Noted : txtScheduleNotedInput,
            ScheduleDate :  dateScheduleDateInput,
            AddressInfo : txtAddressInfoInput
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}


function addDocumentSelect() {

    var cbSelected = document.getElementById("cbDocumentType");
    var valueSelected = cbSelected.options[cbSelected.selectedIndex].value;
    var textSelected = cbSelected.options[cbSelected.selectedIndex].text;
    var labelText = textSelected;
    var nameCotrol = valueSelected  ;
    var idControl = valueSelected + "id" ;
    var htmlAppend =  `<div class="col-12 form-group"> <label class="form-label">`+labelText+`</label> <input type="file" onchange="UploadImage(this)" name="`+nameCotrol+`" class="form-control" required="" placeholder=""> <div class="invalid-feedback-cs"> </div> <div class="fileResult"> </div> <div class="fileValue"> <input type="hidden" id="`+idControl+`" class="valuefile" value=""> </div> </div>`;

    document.getElementById("documnetList").insertAdjacentHTML('afterbegin',htmlAppend);

}


function getDataDocumentUpdate()
{
    var dataRecorded = document.getElementById("documnetList").children;
    var arrayoutput = [];
    for (let indexLoop = 0; indexLoop < dataRecorded.length; indexLoop++) {
        const itemRecord = dataRecorded[indexLoop];
        var labelText = itemRecord.firstElementChild.textContent;
        var valueFile = "";
        var itemValue = itemRecord.querySelector(".valuefile");
        if(itemValue != null)
        {
            valueFile = itemValue.value;
        }
        var fileInput = itemRecord.querySelector('input[type="file"]');
        var valueIdItem = itemRecord.querySelector(".ValueId");
        var inputId = 0;
        if(valueIdItem != null )
        {
            inputId = valueIdItem.value;
        }

        if(inputId == "")
        {
            inputId =0;
        }

        var  keyVaueContent = fileInput.name;
        var itemData = {
            Code :  keyVaueContent,
            DisplayText :  labelText,
            ValueFile:  valueFile,
            Id: inputId  
        };
        arrayoutput.push(itemData);
    }
    return arrayoutput;
}

function AddDocument( idCandidate, dataType =1)
{

   
    var data =getDataDocumentUpdate();

    var bodyRequest = {
        RelId : idCandidate,
        Data :data,
        DataType : dataType

    };
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/CandidateDetail?handler=AddDocument',
        data: bodyRequest,
        success: function (data) {

            successAdd(idCandidate);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}



function saveEmployeeDetail(idEmp) {

    var employeeIdIdInput = getValueControl("inputId");
    var txtFullName = getValueControl("txtFullName");
    var txtNationnalInput = getValueControl("txtNationnal");
    var txtNationnalDateInput = getValueControl("txtNationnalDate");
    var txtNationalPlaceInput = getValueControl("txtNationalPlace");
    var dobInput = getValueControl("dob");
    var onboardDateInput = getValueControl("onboardDate");
    var txtPhoneInput = getValueControl("txtPhone");
    var cbManagerInput = getValueControl("cbManager");
    var cbDepartmentIdInput = getValueControl("cbDepartmentId");
    var cbPositionInput = getValueControl("cbPosition");
    var txtEmailInput = getValueControl("txtEmail");
    var txtfileCV = getValueControl("inputCvlink");
    var cbRoleCodeInput = getValueControl("cbRoleCode");
    
    var txtNotedCandInput = getValueControl("txtNotedCand");
    var txtPermanentAddressInput = getValueControl("txtPermanentAddress");
    var txtTemporaryAddressInput = getValueControl("txtTemporaryAddress");
     var cbStatusHumanInput = getValueControl("cbStatusHuman");
     var txtStatusInput = getValueControl("cbStatus");

    var txtBankAccountInput = getValueControl("txtBankAccount");
    var txtBankNameInput = getValueControl("txtBankName");

    var inputcBEducationLevel = getValueControl("cBEducationLevel");
    var intpucBMaritalStatus = getValueControl("cBMaritalStatus");

      var cbStatusWorkInput = getValueControl("cbStatusWork");

    const selectedDocuments = Array.from(document.querySelectorAll('input[name="Documents"]:checked'))
    .map(checkbox => checkbox.value);

    var valueSelectCheck = selectedDocuments.join(',');
    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    
    var bodyRequest = {
        EmployeeId: employeeIdIdInput,
        id: employeeIdIdInput,
        fullName: txtFullName,
        NationalId : txtNationnalInput,
        NationalDate: txtNationnalDateInput,
        NationalPlace : txtNationalPlaceInput,
        PermanentAddress : txtPermanentAddressInput,
        TemporaryAddress : txtTemporaryAddressInput,
        Dob: dobInput,
        Onboard : onboardDateInput,
        Phone:  txtPhoneInput,
        ManagerId: cbManagerInput,
        DepartmentCode :  cbDepartmentIdInput,
        Email: txtEmailInput,
        CVLink:  txtfileCV,
        PositionCode  : cbPositionInput,
        Noted : txtNotedCandInput,
        DocumentStatus :cbStatusHumanInput,
        Status  : txtStatusInput,
        StatusWork : cbStatusWorkInput,
        RoleCode : cbRoleCodeInput,
         BankName :txtBankNameInput,
        BankAccount : txtBankAccountInput ,
        EducationLevel :inputcBEducationLevel,
        Maritalstatus : intpucBMaritalStatus ,
        DocumentCheck:  valueSelectCheck
    };

      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/EmployeeInfo?handler=Update',
        data:  bodyRequest,
        success: function (data) {
            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
        }
    });
}


function changePassword(reloadPage =true) {
    var currentPassword = getValueControl("txtPasswordCurrent");
    var newPassword = getValueControl("txtnewPassword");
    var renewPassword = getValueControl("txtRepeatPasswordNew");

    if (newPassword == "") {
        addError("txtnewPassword", "yêu cầu nhập mật khẩu mới");
        return;
    }
    else {
        removeError("txtnewPassword");
    }

    if (renewPassword == "") {
        addError("txtRepeatPasswordNew", "yêu cầu nhập lại mật khẩu mới");
        return;
    }
    else {
        removeError("txtRepeatPasswordNew");
    }

    if (newPassword != renewPassword) {
        addError("txtRepeatPasswordNew", "Hai mật khẩu không trùng khớp");
    }
    var bodyRequest = {
    newPassword: newPassword, 
    id: getValueControl("inputId")
    };

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/EmployeeInfo?handler=ChangePassword',
        data:bodyRequest,
        success: function (data) {
                Swal.fire({
                    position: "center",
                    icon: "success",
                    title: "Đổi mật khẩu thành công",
                    showConfirmButton: false,
                    timer: 5000
                });

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        }
    });
}


function saveRelaionItem(idEmp) {

    var cbcandidateId = getValueControl("inputId");
    var txtRelaFullNameInput = getValueControl("txtRelaFullName");
    var txtRelaRelationCodeInput = getValueControl("txtRelaRelationCode");
    var txtRelaPhoneInput = getValueControl("txtRelaPhone");
    var txtRelaAddressInput = getValueControl("txtRelaAddress");
  
    if (txtRelaFullNameInput == "") {
        addError("txtRelaFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtRelaFullName");
    }
    if (txtRelaRelationCodeInput == "") {
        addError("txtRelaRelationCode", "Yều cầu nhập trường này");
        return;
    }
    else {
        removeError("txtRelaRelationCode");
    }
    var bodyRequest = {
      
        Name: txtRelaFullNameInput,
        UserName :  getValueControl("txtUserName"),
        Relationcode : txtRelaRelationCodeInput,
        Phone :txtRelaPhoneInput,
        Noted: "",
        AddressInfo: txtRelaAddressInput
       
    };


      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/EmployeeInfo?handler=AddRelationItem',
        data:  bodyRequest,
        success: function (data) {
            successAdd(idEmp, false, false);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
        }
    });
}

function SaveHDLD(idEmp) {

    var cbcandidateId = getValueControl("inputId");
    var txtNumberHDLD = getValueControl("txtSoHDLD");
    var htdldBegin = getValueControl("dtpHDLDBeginDate");
    var hdldEdndInput = getValueControl("dtpHDLDEndDate");
     var hdldLoaiHDInput = getValueControl("cbLoaiHopDong"); 
    if (txtNumberHDLD == "") {
        addError("txtSoHDLD", "yêu cầu nhập số hợp đồng");
        return;
    }
    else {
        removeError("txtSoHDLD");
    }
    if (htdldBegin == "") {
        addError("dtpHDLDBeginDate", "Yều cầu nhập  ngày hợp đồng");
        return;
    }
    else {
        removeError("dtpHDLDBeginDate");
    }

     if (hdldEdndInput == "") {
        addError("dtpHDLDEndDate", "Yêu cầu nhập hạn ngày hợp đồng");
        return;
    }
    else {
        removeError("dtpHDLDEndDate");
    }
    var bodyRequest = {
            UserId :  cbcandidateId,
            NoAgree :  txtNumberHDLD,
            Start : htdldBegin,
            End :hdldEdndInput,
            CodeId:  hdldLoaiHDInput
    };

    
    
      $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/EmployeeInfo?handler=AddHDLDItem',
        data:  bodyRequest,
        success: function (data) {
            successAdd(idEmp, false, false);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
        }
    });
}

function UpdateOtherInfoEmployee(employId, typeUpdate) {
    //LUONG
    var txtBankAccountInput = getValueControl("txtBankAccount");
    var txtBankNameInput = getValueControl("txtBankName");
    // 
    var masobaohiemInput = getValueControl("txtMaSoBaoHiem");
    var cbToroiBaoHiemInput = getValueControl("cbToroiBaoHiem");
    var txtBiaSoBaoHiem = getValueControl("txtBiaSoBaoHiem");
    // tax code
    var inputTaxCode = getValueControl("txtTaxCodeInput");
    var inputCbThuXacNhan = getValueControl("cbThuXacNhan");
    var intputCbChungTuThue = getValueControl("cbChungTuThue");
    var inputDependent = getValueControl("txtDependentNumber");
    var inputDependentName = getValueControl("txtDependentName");
    var inptutxtRegBHYT = getValueControl("txtRegBHYT");
    var bodyRequest =  {
            EmployeeId: employId,
            TypeUpdate: typeUpdate,
            PageTax : cbToroiBaoHiemInput,
            BiaSo: txtBiaSoBaoHiem,
            CodeBHXH: masobaohiemInput,
            TaxCode: inputTaxCode,
            IsThuXacNhan:  inputCbThuXacNhan,
            ChungTuThue :intputCbChungTuThue,
            BankAccount :txtBankAccountInput, 
            BankName :txtBankNameInput,
            Dependent : inputDependent ,
            RegBHYT : inptutxtRegBHYT,
            DependentName : inputDependentName
    };

debugger;

    $.ajax({
    headers: {
        "RequestVerificationToken":
            $('input[name="__RequestVerificationToken"]').val()
    },
    type: "POST",
    datatype: "JSON",
    url: '/employeeInfo?handler=AddOtherInfomation',
    data:bodyRequest,
    success: function (data) {
        successAdd(employId, false, false);
    },
    error: function (jqXHR, exception) {
        showError(jqXHR);
    },
    complete: function () {
    }
    });
}

function OpenCreateNew(){
    window.open("/EmployeeInfo?id=-1");
}

function updateOther(idEmp) {

    var txtFullName = getValueControl("txtFullName");
    var txtPhone = getValueControl("txtPhone");
    var dobcb = getValueControl("dob");
    var txtNoted = getValueControl("txtNotedCand");

    var txtEmail = getValueControl("txtEmail");
    var txtSourceCode = 0;
    if(txtSourceCode =="")
        {
            txtSourceCode = 0;
        }
    var txtShortDes = getValueControl("txtNotedCand");
    var cbisActive = 1;
    var fileCvLinkInput = getValueControl("inputCvlink1");
    removeAllEror("mainForm");
    if (txtFullName == "") {
        addError("txtFullName", "yêu cầu nhập họ và tên");
        return;
    }
    else {
        removeError("txtFullName");
    }
    if (txtPhone == "") {
        addError("txtPhone", "Yêu cầu nhập số điện thoại");
        return;
    }
    else {
        removeError("txtPhone");
    }
    


    if (cbisActive == "") {
        addError("cbisActive", "yêu cầu nhập trạng thái");
        return;
    }
    else {
        removeError("cbisActive");
    }

    return;

    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/candidate?handler=Add',
        data: {
           
            Phone: txtPhone,
            Name: txtFullName,
            Email: txtEmail,
            Id: idEmp,
            Dob: dobcb,
            AvatarLink: "",
            CVLink: fileCvLinkInput,
            ShortDes:"",
            Noted: txtNoted ,
            Source : 0,
            IsActive: 1
        },
        success: function (data) {

            successAdd(idEmp);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}







