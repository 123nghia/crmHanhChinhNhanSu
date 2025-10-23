function loadDataPartner() 
{
  
    if( window.location.pathname.includes("OrderDetail") ==true)
        {
            return;
        }
    var elementchange = document.getElementById("cbpartnerId2");
    if(elementchange == null)
        {
            elementchange = document.getElementById("cbpartnerId4");
        }

        if(elementchange == null)
            {
               return;
            }
        elementchange.onchange();
    setTimeout(() => {
        
            var projectIdcb = document.getElementById("cbProject");
            if(  document.getElementById("txtProjectId")  == null)
                {
                    return;
                }
            var valueProject = document.getElementById("txtProjectId").value;

            
        if(projectIdcb != null && valueProject  > 0)
        {
            projectIdcb.value  = valueProject;
            $("#cbProject").trigger('change'); 
            
        }
    }, 1000);
}
$(document).ready(function () {
    activeMenu();
    if (typeof(extraSearchRequest) != "undefined")
    {
        getAllGroup();
    }
 
    getAllStatus();
    getAllPartner();
    getAllPartner("cbpartnerId3");
    if(window.location.href.includes("Detail")==true ||  window.location.href.includes("job")==true )
    {
        loadDataPartner();
    }
    if( window.location.pathname.includes("OrderDetail") ==true)
    {
        setTimeout(() => {
            $("#cbJobId1").change();
        }, 1000);
    }
   var cbLimit = document.getElementById("cbLimit");
   if(cbLimit)
    {
     cbLimit.dispatchEvent(new Event('change'));
    }
  
});

function logout() {
    $("#logoutSubmit").click();
}

function activeMenu() {

    var temp = $( '.sidebar-nav a[href*="'+ window.location.pathname+'"]' );
    var activeMenu = null;
    if(temp.length >0) 
    {
        activeMenu  = temp[0];
        activeMenu.classList.add("active");
    }

    if(activeMenu == null)
        return;
    var ulParrent = activeMenu.closest("ul");

    if(ulParrent)
        {
            ulParrent.classList.add("show");

        }
  
}

function getAllMemberOfGroup(groupId) 
{

    var datacbGroup1 = document.getElementById("cbmember");
    datacbGroup1.innerText = null;
    var optionSelect2 = document.createElement("option");
    optionSelect2.text = "Tất cả";
    optionSelect2.value = "-1";
    datacbGroup1.appendChild(optionSelect2);
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: '/GroupPage?handler=ListMember&&groupId='+groupId,
        data: {
        
        },
        success: function (data) {
            var dataGroup = data.data;
            var datacbGroup = document.getElementById("cbmember");
            datacbGroup.innerText = null;
            var optionSelect1 = document.createElement("option");
            optionSelect1.text = "Tất cả";
            optionSelect1.value = "-1";
            datacbGroup.appendChild(optionSelect1);
            if(datacbGroup != null)
                {
                
                    for (let index = 0; index < dataGroup.length; index++) {
                        const elementItem = dataGroup[index];
                        var optionSelect = document.createElement("option");
                        optionSelect.text = elementItem.memberName;
                        optionSelect.value = elementItem.memberId;
                        if( optionSelect.value == extraSearchRequest.memberSearch)
                        {
                            optionSelect.selected = true;
                            extraSearchRequest.memberSearch = -2;
                        }
                        datacbGroup.appendChild(optionSelect);
                        
                    }
                    datacbGroup.dispatchEvent(new Event('change'));
                    
            }
            
    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function  getAllGroup (){
    
        $.ajax({
            headers: {
                "RequestVerificationToken":
                    $('input[name="__RequestVerificationToken"]').val()
            },
            type: "get",
            datatype: "JSON",
            url: '/GroupPage?handler=AllGroup',
            data: {
            
            },
            success: function (data) {
       

            var dataGroup = data.data;
                var datacbGroup = document.getElementById("cbgroup");
                if (datacbGroup == null) {
                    return;
                }
            datacbGroup.innerText = null;
            var optionSelect1 = document.createElement("option");
            optionSelect1.text = "Tất cả";
            optionSelect1.value = "-1";
            datacbGroup.appendChild(optionSelect1);
            if(datacbGroup != null)
            {
                   
                    for (let index = 0; index < dataGroup.length; index++) {
                        const elementItem = dataGroup[index];
                        var optionSelect = document.createElement("option");
                        optionSelect.text = elementItem.name;
                        optionSelect.value = elementItem.id;
                        if(optionSelect.value == extraSearchRequest.groupSearch)
                        {
                            optionSelect.selected = true;
                            extraSearchRequest.groupSearch =-2;
                        }
                        datacbGroup.appendChild(optionSelect);
                        
                    }
                    datacbGroup.addEventListener("change", getmemberOfThis);
                    function getmemberOfThis() {
                    
                        var temp = document.getElementById("cbgroup");
                        var groupIdSelect = temp.value;
                        getAllMemberOfGroup(groupIdSelect);
                    }
                    datacbGroup.dispatchEvent(new Event('change'));
                 
                    
             }


        

            },
            error: function (jqXHR, exception) {
                showError(jqXHR);
            },
            complete: function () {

            }
        });
}

function UploadImage(fileInput, type ="candidate") {

 
    if (fileInput.files.length < 1)
        return;
    var fileAccess = fileInput.files[0];
    var formData = new FormData();
    formData.append('FileRequest', fileAccess);
    formData.append('Type', "candidate");
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        processData: false,  // tell jQuery not to process the data
        contentType: false,
        url: '/file?handler=Upload',
        data: formData ,
        success: function (data, reponse) {

            successUpload(data, fileInput);
        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });

}

function successUpload(dataReponse, fileInput) {

 
    var linkResult = dataReponse.linkResult;
    var x = fileInput.closest(".form-group");
    var fileResult1 = x.querySelector('.fileResult');
    fileResult1.innerHTML = "";

    var taga = document.createElement('a');
    taga.href = linkResult;
    taga.target = "_blank";
    taga.textContent = "Thông tin file";
    fileResult1.appendChild(taga);
    var fileValue = x.querySelector(".valuefile");
    fileValue.value = linkResult;

}


function callFrom(typeCall,idRel,elementClick) {

 
     var errorText ="";
     
     if(idRel == "-1")
    {
        errorText = "Không có đối tượng để gọi, vui lòng tạo thông tin trước";
        Swal.fire({
         icon: "error",
         title: errorText,
         text: errorText,
         footer: '<a >Vi phạm gọi</a>'
         });
         return;
    }

     
    if (typeCall == "") {
        errorText = "Không có đối tượng để gọi";
        Swal.fire({
            icon: "error",
            title: errorText,
            text: errorText,
            footer: '<a >Có lỗi trong thao tác gọi</a>'
            });
        return;
    }

    if (idRel == "") {
       errorText = "Không có đối tượng để gọi";
       Swal.fire({
        icon: "error",
        title: errorText,
        text: errorText,
        footer: '<a >Có lỗi trong thao tác gọi</a>'
        });
      return;
    }
    
    var parrentNumber = elementClick.closest(".input-group");
    var phoneInputValue = parrentNumber.querySelector('.phonecall');
    var valuePhoneNumber = phoneInputValue.value;
    if(valuePhoneNumber.length < 10) 
    {
        errorText = "Số điện thoại không chính xác hoặc không có";
        Swal.fire({
         icon: "error",
         title: errorText,
         text: errorText,
         footer: '<a >Có lỗi số điện thoại</a>'
         });
       return;
      
    }



    Swal.fire({
        title: "Đang thực hiện cuộc gọi, vui lòng chờ đợi",
        width: 800,
      
        padding: "3em",
        color: "#716add",
        background: "#fff url(/assets/trees.png)",
        backdrop: `
          rgba(0,0,123,0.4)
         
          left top
          no-repeat
        `
      });
   
    
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "POST",
        datatype: "JSON",
        url: '/Call?handler=MakeCall',
        data: {
            
            typecall: typeCall,
            phonecall: valuePhoneNumber,
            idrel: idRel
        },
        success: function (data) {
            
            Swal.fire({
                title: "Đang chuyển tiếp đến microsip, thao tác thành công",
                width: 800,
                padding: "3em",
                color: "#716add",
                timer: 4000,
                background: "#fff url(/assets/trees.png)",
                backdrop: `
                  rgba(0,0,123,0.4)
                 
                  left top
                  no-repeat
                `
              });
           
            
        },
        error: function (jqXHR, exception) {
            Swal.fire({
                icon: "error",
                title: "Có lỗi trong quá trình gọi",
                text: "Vui lòng liên hệ IT!",
                footer: 'Gọi thất bại'
              });
        },
        complete: function () {
           
        }
    });
}
function addNewAddress (elementClick, lableText = "Địa chỉ") {

    var  htmlTemplate = '<div class="col-12 form-group">\
                               <label class="form-label">' + lableText + '</label>\
                                <input type="text" customvalue ="-1"  class="form-control"  placeholder="" />\
                        </div>';
    var elementParrent = elementClick.closest("div");
    elementParrent.innerHTML+= htmlTemplate;
 

}
function  getAllPartner (idPartner = "cbpartnerId2"){

    if( window.location.pathname.includes("OrderDetail") ==true)
        {  

            // setTimeout(() => {
            //     var elementClick = document.getElementById("cbpartnerId2");
            //     elementClick.onchange();
            // }, 1000);
           
            // setTimeout(() => {
                
            //         var projectIdcb = document.getElementById("cbProject");
            //         if(  document.getElementById("txtProjectId")  == null)
            //             {
            //                 return;
            //             }
            //         var valueProject = document.getElementById("txtProjectId").value;
        
                    
            //     if(projectIdcb != null && valueProject  > 0)
            //     {
            //         projectIdcb.value  = valueProject;
            //         $("#cbProject").trigger('change'); 
                    
            //     }
            //     document.getElementById('cbProject').onchange =  getAllJobFilter;
            //     document.getElementById('cbField').onchange =  getAllJobFilter;
            // }, 3000);
            // return;
        }
    var datacbGroup1 = document.getElementById(idPartner);
    

    if (datacbGroup1 == null) {
       return;
        
    }
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: '/Partner?handler=AllData',
        data: {
        
        },
        success: function (data) {
   

        var dataGroup = data.data;
            var datacbGroup = document.getElementById(idPartner);
            if (datacbGroup == null) {
                return;
            }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.shortName;
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
               
                
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function  getAllJob (){
    
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: '/Job?handler=AllData',
        data: {
        
        },
        success: function (data) {
   

        var dataGroup = data.data;
            var datacbGroup = document.getElementById("cbJobId");
            if (datacbGroup == null) {
                return;
            }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.name;
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
                  setTimeout(() => {
                    datacbGroup.addEventListener("change", getmemberOfThis);
        
                    function getmemberOfThis() {
                      
                          var temp = document.getElementById("cbgroup");
                          if(temp != null)
                            {
                                var groupIdSelect = temp.value;
                                getAllMemberOfGroup(groupIdSelect);
                            }
                
                       
                    }
                    }, 200);
                
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}




function  getProject (elementChange, targetElement ="cbProject"){
    
    var urlget =  '/common?handler=GetAllParrentChild&idRel=' + elementChange.value;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var dataGroup = data.data;
        var datacbGroup = document.getElementById(targetElement);
        if (datacbGroup == null) {
            return;
        }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.text;
                    
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
                 
                
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {
            setTimeout(() => {
                // getAllJobFilter();
                
            }, 1000);
            

        }
    });
}






function  getProjectDetailOrder (elementChange){


    
    var urlget =  '/common?handler=GetAllParrentChild&idRel=' + elementChange.value;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var dataGroup = data.data;
        var datacbGroup = document.getElementById("cbProject");
        if (datacbGroup == null) {
            return;
        }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.text;
                    
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
        
            setTimeout(() => {

                if( dataGroup != null)

                    {
                       
                        getAllAddress(elementChange);
                        // getAllJobFilter();
                    
                    }                    
            }, 1000);
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}





function  getAllAddress (elementChange){
    
    var urlget =  '/common?handler=GetAllParrentAddress&idRel=' + elementChange.value;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var dataGroup = data.data;
        var datacbGroup = document.getElementById("cbAddress");
        if (datacbGroup == null) {
            return;
        }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Chọn địa chỉ";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.text;
                    
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
        
            setTimeout(() => {

                if(dataGroup.length >0)
                    {
                       var  checkboxAddress =   document.getElementById('checkDefaultAddress');

                       if( checkboxAddress != null)
                        {
                            document.getElementById('checkDefaultAddress').checked = true;
                            changeSelect("",1);
                        }
                        
                    }
                    else 
                    {
                        var  checkboxAddress2 =   document.getElementById('checkOtherAddress');
                        if( checkboxAddress2 != null)
                            {
                                document.getElementById('checkOtherAddress').checked = true;
                                changeSelect("",2);
                            }
                      
                    }

                if( dataGroup != null)

                            {datacbGroup.onchange();}                    
            }, 1000);
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function  getAllAddress2 (valueChange){
    
    var urlget =  '/common?handler=GetAllParrentAddress&idRel=' + valueChange;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var dataGroup = data.data;
        var datacbGroup = document.getElementById("cbAddress");
        if (datacbGroup == null) {
            return;
        }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Chọn địa chỉ";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.text;
                    
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
        
            setTimeout(() => {

                if(dataGroup.length >0)
                    {
                       var  checkboxAddress =   document.getElementById('checkDefaultAddress');

                       if( checkboxAddress != null)
                        {
                            document.getElementById('checkDefaultAddress').checked = true;
                            changeSelect("",1);
                        }
                        
                    }
                    else 
                    {
                        var  checkboxAddress2 =   document.getElementById('checkOtherAddress');
                        if( checkboxAddress2 != null)
                            {
                                document.getElementById('checkOtherAddress').checked = true;
                                changeSelect("",2);
                            }
                      
                    }

                // if( dataGroup != null)

                // {
                //     datacbGroup.onchange();
                
                // }                    
            }, 1000);
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}
function changeSelect(elementChange,valueSelect) {
 
    if(valueSelect ==1)
        {
            $("#divAdddressDefault").show();
            $("#divAdddressOther").hide();
            return;
        }
        $("#divAdddressDefault").hide();
        $("#divAdddressOther").show();
}



function  getAllJobFilter (){

    var cbpartnerIdValue =  getValueControl("cbpartnerId2");
    var cbProjectValue =  getValueControl("cbProject");
    var cbFieldValue =  getValueControl("cbField");
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: '/job?handler=AllJob',
        data: {
            PartnerId :  cbpartnerIdValue,
            ProjectId :  cbProjectValue,
            LinhvucId :  cbFieldValue
        },
        success: function (data) {
            var dataGroup = data.data;
            var datacbGroup = document.getElementById("cbJobId");
            if (datacbGroup == null) {
                return;
            }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.name;
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
                
                
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}




function  getAllStatus (){
    var datacbGroup1 = document.getElementById("cbstatus");
    if (datacbGroup1 == null) {
        return;
    }

    
    var urlget =  '/StatusPage?handler=AllData'
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var dataGroup = data.data;
        var datacbGroup = document.getElementById("cbstatus");
        if (datacbGroup == null) {
            return;
        }
        datacbGroup.innerText = null;
        var optionSelect1 = document.createElement("option");
        optionSelect1.text = "Tất cả";
        optionSelect1.value = "-1";
        datacbGroup.appendChild(optionSelect1);
        if(datacbGroup != null)
            {
               
                for (let index = 0; index < dataGroup.length; index++) {
                    const elementItem = dataGroup[index];
                    var optionSelect = document.createElement("option");
                    optionSelect.text = elementItem.name;
                    
                    optionSelect.value = elementItem.id;
                    datacbGroup.appendChild(optionSelect);
                    
                }
        
            setTimeout(() => {

                if(dataGroup.length >0)
                    {    
                        
                        var  checkDefaultAddress1 =  document.getElementById('checkDefaultAddress');
                        if(checkDefaultAddress1 != null )
                        {
                            document.getElementById('checkDefaultAddress').checked = true;
                            changeSelect("",1);
                        }
                
                    }
                    else 
                    {
                        var  checkDefaultAddress2 =    document.getElementById('checkOtherAddress');
                        if(checkDefaultAddress2 != null )
                        {
                            document.getElementById('checkOtherAddress').checked = true;
                            changeSelect("",2);
                        }
                    }

                if( dataGroup != null)

                   {  
                    try {
                        datacbGroup.onchange();
                    } catch (error ) {
                        
                    }
                 
                
                }                    
            }, 1000);
         }


    

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function openFileNewTag(elementChange) {

    var x = elementChange.closest(".form-group");
    var inputSelet = x.querySelector('input');

    var linkHref = inputSelet.value;
    window.open(linkHref, '_blank').focus();
   
}


function  getFullPathJob (elementChange){
    
    var urlget =  '/job?handler=FullText&id=' + elementChange.value;
    $.ajax({
        headers: {
            "RequestVerificationToken":
                $('input[name="__RequestVerificationToken"]').val()
        },
        type: "get",
        datatype: "JSON",
        url: urlget,
        data: {

        },
        success: function (data) {
        var pathFullText = data.pathFullText;
        var patnerId = data.partnerId;
        var pFullTag = document.createElement("p");
        pFullTag.innerHTML = pathFullText;
        var fullPathJobDiv = document.getElementById("fullPathJob");
        fullPathJobDiv.innerHTML = "";
        pFullTag.style.display = "none";
        fullPathJobDiv.append(pFullTag);
        var partnerCb =   document.getElementById("cbpartnerId3");
        if(partnerCb)
            {
                partnerCb.value = patnerId;
            }
            setTimeout(() => {
                getAllAddress2(patnerId);
            }, 1000);


        
    //     setTimeout(() => {

    //         if( dataGroup != null)

    //             {
                   
    //                 getAllAddress(elementChange);
    //                 // getAllJobFilter();
                
    //             }                    
    //     }, 1000);
    //  }

        },
        error: function (jqXHR, exception) {
            showError(jqXHR);
        },
        complete: function () {

        }
    });
}

function showOrderList(idInput) {
  
    var linkHref = "/order?status="+idInput;
    window.open(linkHref, '_blank').focus();
  
}



function isBlank(str) {
    return (!str || /^\s*$/.test(str));
}

function changeStatusQuick(cbbook){
     var valuestatus = cbbook.value;
      if(valuestatus == 12 || valuestatus == 9)
      {
         document.getElementById("div_Status").style.display ="block";
      }
      else 
      {
        document.getElementById("div_Status").style.display ="none";
      }
    }


    function loadCombobx()
    {
        var candTextinput = document.getElementById("cbcandidateId");
        var input = document.getElementById("input");
        input.onfocus = function () {
            browsers.style.display = "block";
            input.style.borderRadius = "5px 5px 0 0";
          };
          for (let option of browsers.options) {
            option.onclick = function () {
              input.value = option.text;
              candTextinput.value = option.value;
              browsers.style.display = "none";
              input.style.borderRadius = "5px";
            };
          }
          
          input.oninput = function () {
            currentFocus = -1;
            var text = input.value.toUpperCase();
            for (let option of browsers.options) {
              if (option.value.toUpperCase().indexOf(text) > -1 || option.text.toUpperCase().indexOf(text) > -1 ) {
                option.style.display = "block";
              } else {
                option.style.display = "none";
              }
            }
          };
          var currentFocus = -1;
          input.onkeydown = function (e) {
            if (e.keyCode == 40) {
              currentFocus++;
              addActive(browsers.options);
            } else if (e.keyCode == 38) {
              currentFocus--;
              addActive(browsers.options);
            } else if (e.keyCode == 13) {
              e.preventDefault();
              if (currentFocus > -1) {
                /*and simulate a click on the "active" item:*/
                if (browsers.options) browsers.options[currentFocus].click();
              }
            }
          };
          
          function addActive(x) {
            if (!x) return false;
            removeActive(x);
            if (currentFocus >= x.length) currentFocus = 0;
            if (currentFocus < 0) currentFocus = x.length - 1;
            x[currentFocus].classList.add("active");
          }
          function removeActive(x) {
            for (var i = 0; i < x.length; i++) {
              x[i].classList.remove("active");
            }
          }
          
    }
   function formatDateTime (datetimeConvert)
    {
        return datetimeConvert;
    }

    function dowloadfileRecord (likFile ='') {

        const a = document.createElement('a')
        a.href = "http://192.168.1.3:7224/api/file/getaudio9?filePath="  + likFile;
        a.target ="_blank";
        document.body.appendChild(a)
        a.click()
        document.body.removeChild(a)

}


$(document).ready(function () {
    var todayDate = new Date().getDate();
    var endD = new Date(new Date().setDate(todayDate - 360));
    var currDate = new Date();
    //$('.datepickerInput').datepicker({
    //    format: 'dd/mm/yyyy',
    //    clear: "Clear",
    //    autoclose: true

    //})


});



