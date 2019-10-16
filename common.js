
function C(name) {
    return $("." + name);
}
function Cv(name) {
    return $("." + name).val();
}
function CSH(name) {
    return C(name).show();
}
function CHD(name) {
    return C(name).hide();
}
function G(id) {
    return $("#" + id);
}
function Gv(id) {
    return G(id).val();
}
function SH(id) {
    return G(id).show();
}
function HD(id) {
    return G(id).hide();
}
function DDT(id) {
    return G(id).find("option:selected").text();
}
function DDS(id) {
    return G(id).find("option:selected");
}

function movelist(frmid, toid) {
    var selectedOpts = DDS(frmid);
    if ($(selectedOpts).val() == 0) {
        alert("Nothing to move.");
        return;
    }
    G(toid).append($(selectedOpts).clone());
    $(selectedOpts).remove();
    return;
}

function offevent(id, eventname, func) {
    //G(id).unbind(eventname);
    G(id).unbind(eventname, func);
}
function onevent(id, eventname, func) {
    //G(id).bind(eventname);
    G(id).bind(eventname, func);
}

function clearDD(id) {
    var select = G(id);
    select.empty();
    select.append($('<option/>', {
        value: "0",
        text: "--Select--",
        selected: 0
    }));
}

function RCCheck(id, bool) {
    G(id).prop("checked", bool)
}

function EDC(id, bool) {
    G(id).attr("disabled", bool);
}
function CEDC(id, bool) {
    C(id).attr("disabled", bool);
}
function AllDDV(ddid) {
    var values = "";

    G(ddid + " option").each(function (index, value) {
        if (index != 0)
            values += $(this).val() + ",";
    });

    if (values.length > 0) values = values.slice(0, -1);

    return values;
}

function AllDDT(ddid) {
    var values = "";

    G(ddid + " option").each(function (index, value) {
        if (index != 0)
            values += $(this).text() + ",";
    });

    if (values.length > 0) values = values.slice(0, -1);

    return values;
}

function tblSearchId(tblid, searchid, colIndex) {
    var ischecx = true;
    G(tblid + " tr").each(function (index, value) {
        if (!this.rowIndex) return;

        if ($(this).find("td").eq(colIndex).attr('name') == searchid)
        { ischecx = false; return; }
    });

    return ischecx;
}

function getaramvalues() {
    // Normal Asp.net
    //    var query, parms, i, pos, key, val, qsp;
    //    qsp = {};
    //    query = location.search.substring(1);
    //    parms = query.split('&');
    //    for (i = parms.length - 1; i >= 0; i--) {
    //        pos = parms[i].indexOf('=');
    //        if (pos > 0) {
    //            key = parms[i].substring(0, pos);
    //            val = parms[i].substring(pos + 1);
    //            qsp[key] = val;
    //        }
    //    }
    // MVC 
    var URL = window.location.toString();
    var number = URL.match(/(\d+)$/g);

    return number;
}

function getcheckedchkBox() {
    var values = "";

    $("input[type='checkbox']").each(function () {
        if (this.checked) values += this.value + ",";
    });

    if (values.length > 0) values = values.slice(0, -1);
    return values;
}

function formatDateTime(input) {
    if (input.length > 0) {
        var datePart = input.match(/\d+/g),
                year = datePart[2],
                month = datePart[1],
                day = datePart[0];
        hour = datePart[3];
        minute = datePart[4];

        return month + '/' + day + '/' + year + ' ' + hour + ':' + minute;
    }
}

function getDropDownYNString(ctrlID, selectedvalue, events) {
    var ctrl = '<select style="width: 100%;" id="' + ctrlID + '"' + events + '>';
    if (selectedvalue == null) {
        ctrl = ctrl + '<option selected="selected" value="">---Select---</option>';
    }
    else {
        ctrl = ctrl + '<option value="">---Select---</option>';
    }

    if (selectedvalue == '1') {
        ctrl = ctrl + '<option selected="selected" value="1">Yes</option>';
    }
    else {
        ctrl = ctrl + '<option value="1">Yes</option>';
    }

    if (selectedvalue == '0') {
        ctrl = ctrl + '<option selected="selected" value="0">No</option>';
    }
    else {
        ctrl = ctrl + '<option value="0">No</option>';
    }
    ctrl = ctrl + '</select>';
    return ctrl;
}

function getDropDownString(ctrlID) {
    var ctrl = '<select id="' + ctrlID + '">';
    ctrl = ctrl + '<option selected="selected" value="">---Select---</option>';
    ctrl = ctrl + '</select>';
    return ctrl;
}
function TextAreaMaxLength(textControl, maxlen) {
    if (textControl.value.length >= maxlen) {
        textControl.value = textControl.value.substring(0, maxlen);
    }
}

var funExeSessionTimeOut;
function SetSessionOutRedirection(newSessionTimeOut,preLoginURL) {
    clearTimeout(funExeSessionTimeOut);
    //Variable funExeSessionTimeOut define globally at the top of Method
    funExeSessionTimeOut = setTimeout(function () {
        window.location = preLoginURL;
    }, newSessionTimeOut - 10000);
}

//This method of file upload is tested & modify as per IE8 requirement
//, Not for HTML5 Enabled browser Like Chrome, Mozilla & IE10 to do so need modifications in code handling & processing FormData Object.
function upload_myFile_file(ctrlID, actionURL, ctrlContainerID, gridID, gridURL, errorActionMethodURL) {
    //debugger;
    var upload_html = "<input type=\"file\" name=\"" + ctrlID + "\" id = \"" + ctrlID + "\" \/>";

    if (!isAjaxUploadSupported()) { //IE fallfack
        var iframe = document.createElement("<iframe name='upload_iframe_" + ctrlID + "' id='upload_iframe_" + ctrlID + "'>");
        iframe.setAttribute("width", "0");
        iframe.setAttribute("height", "0");
        iframe.setAttribute("border", "0");
        iframe.setAttribute("src", "javascript:false;");
        //iframe.style.display = "none";

        var form = document.createElement("form");
        form.setAttribute("target", "upload_iframe_" + ctrlID);
        form.setAttribute("action", actionURL);
        form.setAttribute("method", "post");
        form.setAttribute("enctype", "multipart/form-data");
        form.setAttribute("encoding", "multipart/form-data");
        //form.style.display = "none";

        var files = document.getElementById(ctrlID);
        form.appendChild(files);
        //$conv("#container_myFile").html("Uploading...");

        //document.body.appendChild(form);
        //document.body.appendChild(iframe);
        document.getElementById(ctrlContainerID).appendChild(form);
        document.getElementById(ctrlContainerID).appendChild(iframe);

        iframeIdmyFile = document.getElementById("upload_iframe_" + ctrlID);

        // Add event...
        var eventHandlermyFile = function () {
            if (iframeIdmyFile.detachEvent)
                iframeIdmyFile.detachEvent("onload", eventHandlermyFile);
            else
                iframeIdmyFile.removeEventListener("load", eventHandlermyFile, false);

            response = getIframeContentJSON(iframeIdmyFile);
            uploaded_file_myFile(response);
        }

        if (iframeIdmyFile.addEventListener)
            iframeIdmyFile.addEventListener("load", eventHandlermyFile, true);
        if (iframeIdmyFile.attachEvent)
            iframeIdmyFile.attachEvent("onload", eventHandlermyFile);

        form.submit();
        return;
    }
    var data = new FormData();
    data.append("files[]", $conv(this).prop("files")[0]);

    $conv("#container_myFile").html("Uploading...");
    $conv.ajax({
        url: "upload.php",
        data: data,
        type: "POST",
        dataType: "json",
        cache: false,
        contentType: false,
        processData: false,
        enctype: "multipart/form-data",
        xhr: function () {
            myXhr = $conv.ajaxSettings.xhr();
            if (myXhr.upload) {
                myXhr.upload.addEventListener("progress", function (e) {
                    if (e.lengthComputable) {
                        $conv("#container_myFile").html("<div class=\"progress\">Uploading..." + parseInt((e.loaded / e.total) * 100) + " % complete</div>");
                    }
                }, false);
            }
            return myXhr;
        },
        success: function uploaded_file_myFile(result) {
            $.ajax({
                type: "POST",
                url: errorActionMethodURL,
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (data) {
                    alert(data);
                    G(gridID).jqGrid('setGridParam', { url: gridURL, page: 1 }).trigger("reloadGrid");
                },
                error: function (jqXHR, exception) {
                    alert('exception123: ' + exception + ', jqXHR: ' + jqXHR);
                }
            });
        },
        failure: function (jqXHR, textStatus) {
            alert("Oops!! Something went wrong!: " + textStatus);
            $conv("#container_myFile").html(upload_html);
            $conv("#myFile").change(upload_myFile_file);

            return;
        }
    });
}
function isAjaxUploadSupported() {
    //return false;
    var input = document.createElement("input");
    input.type = "file";

    return (
		        "multiple" in input &&
		            typeof File != "undefined" &&
		            typeof FormData != "undefined" &&
		            typeof (new XMLHttpRequest()).upload != "undefined");
}
function getIframeContentJSON(iframe) {
    //IE may throw an "access is denied" error when attempting to access contentDocument on the iframe in some cases
    try {
        // iframe.contentWindow.document - for IE<7
        var doc = iframe.contentDocument ? iframe.contentDocument : iframe.contentWindow.document,
	                response;

        var innerHTML = doc.body.innerHTML;
        //plain text response may be wrapped in <pre> tag
        if (innerHTML.slice(0, 5).toLowerCase() == "<pre>" && innerHTML.slice(-6).toLowerCase() == "</pre>") {
            innerHTML = doc.body.firstChild.firstChild.nodeValue;
        }
        response = eval("(" + innerHTML + ")");
    } catch (err) {
        response = { success: false };
    }
    return response;
}
//Added by niklank on 12 May 2014, as set focus on page
$(function () {
    $('.setfocus :input').focus();
});



/*---------------------------Custom Paging and sortings-----------------------------------------*/
//Added by Harsh
function bindSort(obj, sortByID, sortOrderID, actionTypeID, fnCallback) {


    var sortby = ($(obj).attr("sortby"));
    var previouseSortBy = $("#" + sortByID).val();
    var sortOrder = $("#" + sortOrderID).val();

    if (sortby == previouseSortBy) {
        if (sortOrder == '0') {
            $("#" + sortOrderID).val("1");
            $(obj).addClass("desc");
        }
        else {
            $("#" + sortOrderID).val("0");
            $(obj).addClass("asc");
        }
        $("#" + sortByID).val(sortby);
    }
    else {
        $("#" + sortOrderID).val("1");
        $("#" + sortByID).val(sortby);
    }
    $("#" + actionTypeID).val("sortList");
    //$("#currentPage").val("1");
    fnCallback();

}



function createFooter(footerContainer, totalPages, rowsPerPage, ActionTypeID, CurrentPageID, fnCallback) {

   if (totalPages > 1) {

        var currentPage = $("#" + CurrentPageID).val();

        var footer = "<ul class='pagination pagination-sm'>";
        if (parseInt(currentPage) > 1) {
            footer = footer + "<li class='prev-btn'><a href='javascript:void(0)' title='previous' class='page' id=" + (parseInt(currentPage) - 1) + "></a></li>"
        }
       
       // for (var i = 1; i < (parseInt(totalPages) + 1); i++) {
        var startIndex = 0;
        
        if (currentPage > 6)
            startIndex = currentPage - 6;
      
        for (var i = 1; i <= 11; i++) {

            if ((i + startIndex) == currentPage)
                footer = footer + "<li class='active'><a >" + (i + startIndex) + "</a></li>";
            else
                footer = footer + "<li><a href='javascript:void(0);' class='page' id=" + (i + startIndex) + ">" + (i + startIndex) + "</a></li>";
            if (parseInt(totalPages) == (i + startIndex))
                break;
        }
        if (parseInt(currentPage) <parseInt(totalPages)) {
            footer = footer + "<li class='next-btn'><a href='javascript:void(0)' title='next' class='page' id=" + (parseInt(currentPage) + 1) + "></a></li>"
        }
        footer=footer+"</ul>"
        footer = footer;
        $("#" + footerContainer).html('');
        $("#" + footerContainer).append(footer);
        $("a.page").click(function () {

            pageNumber = $(this).attr("id");
            {

                $("#" + ActionTypeID).val("pagechange");
                $("#" + CurrentPageID).val(pageNumber);

                fnCallback();
            }
        });
    }
}

function setSortClass(sortByID, sortOrderID) {
    var previousSortBy = $("#" + sortByID).val();
    var sortOrder = $("#" + sortOrderID).val();
    $("a.sort").each(function () {
        var sortby = ($(this).attr("sortby"));
        if (sortby == previousSortBy) {
            if (sortOrder == '0') {
                $(this).removeClass("desc").addClass("asc");
            }
            else {
                $(this).removeClass("asc").addClass("desc");
            }
        }
    });
}

/*---------------------------End Custom Paging and sortings-----------------------------------------*/
/*---------------------Loader------------------------------------------Added By Harsh*/
function beginRequest() {
    $.blockUI({ message: '<h6><img width="25" height="25" src="/Images/loading.gif"/>Please wait...</h6>' });
    resetCSS();
 }
function endRequest() { $.unblockUI();
initializeCSS(); }

/*------------------------------Open Edit Dialog-----------------------------------------Added By Harsh */
function openEditDialog(url, titleText, dialogID) {
    beginRequest();
    $.ajax({
        type: "GET",
        url: encodeURI(url),
        cache: false,
        dataType: 'html',
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            $("#modal-body-content").html(XMLHttpRequest.responseText);
        },
        success: function (data, textStatus, XMLHttpRequest) {
            $("#modal-body-content").html(data);
        },
        complete: function (XMLHttpRequest, textStatus) {
            //            $("#modal-body-content").html($("#" + dialogID).html());
            $("#modal-body-title").addClass("modelbox-title").html(titleText);

            $('#light-box').modal('toggle');
            endRequest();

            //            $("#" + dialogID).dialog({
            //                title: titleText,
            //                width: '500px',
            //                modal: true
            //            });
        }
    });

}
/*------------------------------Open Edit Dialog-----------------------------------------Added By Vashistha */
function openConfirmDialog(deletedId, titleText, okCallBackFunction, cancelCallBackFunction) {
    // e.preventDefault(); use this or return false
    bootbox.confirm(titleText, function (result) {
        if (result) {
           if (okCallBackFunction != null)
                okCallBackFunction(deletedId);
        }
       else { 
        if (cancelCallBackFunction != null)
                cancelCallBackFunction();
        }
    }); 
   

    return false;
}

function openConfirmSaveDialog(content, titleText,sender, yesCallBackFunction, noCallBackFunction) {
    bootbox.dialog({
        message: content,
        title: titleText,
        buttons: {
            Yes: {
                label: "Yes",
                className: "btn-primary",
                callback: function () {
                     if (yesCallBackFunction != null)
                         yesCallBackFunction(sender);

                }
            },
            No: {
                label: "No",
                className: "btn-primary",
                callback: function () {
                    if (noCallBackFunction != null)
                        noCallBackFunction();
                }
            }
        }
    });
    return false;
}


function showMessage(divID, className, message) {

    if (message != null && message != "") {
       // bootbox.alert(message);
        
       $("#" + divID).html(message);
        $("#" + divID).removeAttr('class').addClass("alert").addClass(className);
//        $("#" + divID).slideDown();
        //        setTimeout(function () { $("#" + divID).slideUp(); setTimeout(function () { $("#" + divID).html("").removeAttr('class'); }, 2000); }, 5000);
        $(window).scrollTop(0);
    }
}

function showSpinnerForInteger(id, minvalue, maxvalue) {
    $("#" + id).TouchSpin({
        verticalbuttons: true,
        min: parseInt(minvalue),
        max: parseInt(maxvalue)
    });
}

function showSpinnerForDecimal(id, minvalue, maxvalue) {
    $("#" + id).TouchSpin({
        verticalbuttons: true,
        min: parseFloat(minvalue),
        max: parseFloat(maxvalue),
       decimals: 2

    });
 }

 /*---------------------add and remove value from comma seprated string------------------------------------------Added By vashistha*/
 var addValue = function (list, value, separator) {
     separator = separator || ",";
     var values = list.split(separator);

     if (list != "") {
         values.push(value);
         return values;
     }
     else {
         return value;
     }
     return list;
 }

 var removeValue = function (list, value, separator) {
     separator = separator || ",";
     var values = list.split(separator);
     for (var i = 0; i < values.length; i++) {
         if (values[i] == value) {
             values.splice(i, 1);
             values.join(separator);
             return values;
         }
     }
     return list;
 }

 /*---------------------Check Decimal Place with exect count ------------------------------------------Added By vashistha*/
 function CheckDecimalPlace(obj, count) {
     var val = $(obj).val();
     var ex = /^[0-9]+\.?[0-9]*$/;
     if (($.trim($(obj).val())) != "" && ex.test(obj.value) == false) {
         $(obj).val('');
         //$(obj).select();
         $(obj).focus();
         return false;
     }
     else {

         if (val.indexOf(".") > -1 && val.split(".")[1].length > count) {
             var substr = val.split(".")[1].substring(0, count);
             val = val.split(".")[0] + "." + substr;
             $(obj).val(val);
         }
         else {
             var valnew = $(obj).val();
             if (valnew != val) {
                 $(obj).select();
             }
         }
     }
     //$(obj).val(val);
 }



 function MaximizePopup() {
     newwin = window.showModalDialog($("#myIframe").attr("src"), 'a', "dialogwidth: " + screen.width + "; dialogheight: " + screen.height + "; resizable: 0; dialogLeft:10; dialogTop:10; center:1; status:0; help:0;menubar=no");
     if (window.focus) { newwin.focus() }
     return false;


 }