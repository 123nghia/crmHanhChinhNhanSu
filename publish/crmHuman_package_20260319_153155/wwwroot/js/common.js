function addError(idInput, message) {
    var input = document.getElementById(idInput);
    if (input == null) {
        return;
    }
    var containerdiv = input.closest(".form-group");
    if (containerdiv == null) {
        return;
    }
    containerdiv.classList.add("hasError");
    if (message != "") {
        var childElement = containerdiv.getElementsByClassName("invalid-feedback-cs");
        if (childElement.length > 0) {
            childElement[0].textContent = message;
        }

    }

}

function removeError(idInput) {
    var input = document.getElementById(idInput);
    if (input == null) {
        return;
    }
    var containerdiv = input.closest(".form-group");
    containerdiv
    if (containerdiv == null) {
        return;
    }
    containerdiv.classList.remove("hasError");
}

function removeAllEror(idForm) {
    var formEle = document.getElementById(idForm);
    var allError = formEle.querySelectorAll(".hasError");
    for (var i = 0; i < allError.length; i++) {
        allError[i].classList.remove("hasError");
    }
}

function validateForm() {
    return true;
}

function successAdd(id = -1, deleteRecord = false, isReload=true) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0 ) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord ==true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        if(isReload ==false)
        {

        }
        else 
        {
            window.location.reload();
        }
        
    });
}
function khoiphucXoa() {
    var timer1 = 1500;
    var textMess  = "khôi phục thành công";
 
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: 3000
    }).then((result) => {
        window.location.reload();
    });
}

function openAlertAndClosePopup(id = -1, deleteRecord = false) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Đổi mật khẩu thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {

        $("#closePopup").click();

    });
}
function successAddMember(id = -1, deleteRecord = false,groupId =-1) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        loadData(groupId);
    });
}
function successDeleteMember(id = -1, deleteRecord = false, groupId = -1) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord == true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        loadData(groupId);
    });
}
function showError(jqXHRItem) {
    var statusCode = jqXHRItem.status;
    if (statusCode == 400) {
        var dataEror = jqXHRItem.responseJSON;
        for (var i = 0; i < dataEror.length; i++) {
            var itemName = dataEror[i].name;
            addError(itemName, dataEror[i].content);
        }
    }

}

function showErrorDelete(jqXHRItem) {

    Swal.fire({
        icon: "error",
        title: "Không thành công",
        text: "Xoá thất bại!"
        
    });
}

function getValueControl(idControl) {
    var valueCon = document.getElementById(idControl);
    if (valueCon == null || valueCon == undefined || valueCon == "") {

        return "";
    }
    return valueCon.value;
}
function addErorControl(idControl) {
       
}

function submitForm(formId) {

    document.getElementById(formId).submit();
}

function successAddImpact(id = -1, deleteRecord = false) {
    var timer1 = 1500;
    var textMess = "Thêm mới thành công";
    if (id > 0 ) {
        textMess = "Cập nhật thành công";
    }
    if (deleteRecord ==true) {
        textMess = "Xoá thành công";
        timer1 = 3000;
    }
    Swal.fire({
        position: "center",
        icon: "success",
        title: textMess,
        showConfirmButton: false,
        timer: timer1
    }).then((result) => {
        window.location.reload(true);
    });
}