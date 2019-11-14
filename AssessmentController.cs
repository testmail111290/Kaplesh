@model Humana.WebClaim.CASUI.Web.Models.DBAR
<script src="~/Scripts/DBAR.js"></script>
<script src="~/Scripts/jquery.validate.js"></script>
<script src="~/Scripts/jquery.validate.unobtrusive.js"></script>
<script src="~/Scripts/jquery.OverwriteText.js"></script>
@using (Html.BeginForm("CreateDBAR", "DBAR", FormMethod.Post, new { @id = "frmCreateDBAR" }))
{
    @Html.AntiForgeryToken()

    <div class="form-horizontal">
        @Html.ValidationSummary(true, "", new { @class = "text-danger", @id = "summary" })
        <div class="form-group">
            @Html.LabelFor(model => model.client, htmlAttributes: new { @class = "control-label col-md-2 text-uppercase" })
            <div class="col-md-6">
                @Html.EditorFor(model => model.client, new { htmlAttributes = new { @class = "form-control", @maxlength = 3, @id = "txtDBARClient", @style = "width:710px;" } })
                @Html.ValidationMessageFor(model => model.client, "", new { @class = "text-danger" })
            </div>
        </div>

        <div class="form-group">
            @Html.LabelFor(model => model.name, htmlAttributes: new { @class = "control-label col-md-2" })
            <div class="col-md-6">
                @Html.EditorFor(model => model.name, new { htmlAttributes = new { @class = "form-control", @id = "txtDBARName", @maxlength = 4, @style = "width:710px;" } })
                @Html.ValidationMessageFor(model => model.name, "", new { @class = "text-danger" })
            </div>
        </div>

        <div class="form-group">
            @Html.LabelFor(model => model.entry, htmlAttributes: new { @class = "control-label col-md-2" })
            <div class="col-md-6">
                @Html.EditorFor(model => model.entry, new { htmlAttributes = new { @class = "form-control overWrite ", @id = "txtDBAREntry", @maxlength = 12, @style = "width:710px;" } })
                @Html.ValidationMessageFor(model => model.entry, "", new { @class = "text-danger" })
            </div>
        </div>

        <div class="form-group">
            @Html.LabelFor(model => model.data, htmlAttributes: new { @class = "control-label col-md-2 " })
            <div class="col-md-6">
                @Html.EditorFor(model => model.data, new { htmlAttributes = new { @class = "form-control overWrite", @id = "txtDBARData", @rows = "6", @cols = "98", @style = "height:100px;width:710px;-ms-word-break: break-all;" } })
                @Html.ValidationMessageFor(model => model.data, "", new { @class = "text-danger" })

            </div>
        </div>


        <div class="form-group">
            @Html.Label(" ", htmlAttributes: new { @class = "control-label col-md-2" })
            <div class="col-md-1">
                @Html.Editor("RIndex", new { htmlAttributes = new { @class = "form-control", @id = "txtRIndex", @style = "width:50px;" } })
            </div>
            <div class="col-md-1">
                @Html.Editor("CIndex", new { htmlAttributes = new { @class = "form-control", @id = "txtCIndex", @style = "width:50px;" } })
            </div>
        </div>
        @*<div class="form-group">
                @Html.Label("Data Index", htmlAttributes: new { @class = "control-label col-md-2" })
                <div class="col-md-1">
                    @Html.Editor("RowIndex", new { htmlAttributes = new { @class = "form-control", @id = "txtDBARRowIndex", @style = "width:50px;display:none" } })
                </div>
                <div class="col-md-1">
                    @Html.Editor("ColIndex", new { htmlAttributes = new { @class = "form-control", @id = "txtDBARColIndex", @style = "width:50px;" } })
                </div>
            </div>*@
        <div class="form-group">
            <div class="col-md-offset-4 col-md-8" style="align-items:center">
                @*<input type="button" value="Save" class="btn btn-default" id="SaveTableRecord" />*@
                <input tabindex="-1" class="button-1" id="SaveTableRecord" type="button" value="Save" style="align-self:center">
                <input tabindex="-1" class="button-1" data-dismiss="modal" id="btnCancel" type="button" value="Cancel" style="align-self:center">

                <input type="submit" id="btnSubmit" style="display:none" />
            </div>
        </div>
        <label id="lblError" style="color: red"></label>
        <input type="hidden" id="hdnValidation" value="true" />
    </div>
}

<script type="text/javascript">
    $(document).ready(function () {
        $("#txtDBARClient").keyup(function (event) {
            $("#txtDBARClient").val($("#txtDBARClient").val().toUpperCase());
        });
        $("#txtDBARName").keyup(function (event) {
            $("#txtDBARName").val($("#txtDBARName").val().toUpperCase());
        });
        $("#txtDBAREntry").keyup(function (event) {
            $("#txtDBAREntry").val($("#txtDBAREntry").val().toUpperCase());
        });
        $("#txtDBARData").keyup(function (event) {
            //$("#txtDBARRowIndex").val((getCaret(el("txtDBARData"))));
            //$("#txtDBARColIndex").val((getCaret(el("txtDBARData"))));
            var data = $("#txtDBARData")[0];
            var arraydata = data.value.substr(0, data.selectionStart).split("\n");
            $("#txtRIndex").val(arraydata.length);
            $("#txtCIndex").val(arraydata[arraydata.length - 1].length);


        });
        $("#txtDBARData").click(function (event) {
            //$("#txtDBARRowIndex").val((getCaret(el("txtDBARData"))));
            //$("#txtDBARColIndex").val((getCaret(el("txtDBARData"))));
            var data = $("#txtDBARData")[0];
            var arraydata = data.value.substr(0, data.selectionStart).split("\n");
            $("#txtRIndex").val(arraydata.length);
            $("#txtCIndex").val(arraydata[arraydata.length - 1].length);
        });
        //$("#txtDBARRowIndex").keyup(function (event) {
        //    setCaretToPos(el("txtDBARData"), $("#txtDBARRowIndex").val());
        //});
        $("#txtRIndex").keyup(function (e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 9) {
                var linenumber = $("#txtRIndex").val() - 1;
                var Columnnumber = $("#txtCIndex").val();

                var data = $("#txtDBARData")[0];
                var arraydata = data.value.substr(0, data.selectionStart).split("\n");
                var rowindex = 0;
                $.each(arraydata, function (key, value) {
                    if (key === linenumber) {
                        return false; // breaks
                    }
                    rowindex += value.length;
                });
                rowindex = rowindex + 1 + Number(Columnnumber);
                setCaretToPos(el("txtDBARData"), rowindex);
            }
            else {
                console.log('Not tabbed');
            }


        });
        $("#txtCIndex").keyup(function (e) {
            var code = (e.keyCode ? e.keyCode : e.which);
            if (code == 9) {
                var linenumber = $("#txtRIndex").val() - 1;
                var Columnnumber = $("#txtCIndex").val();

                var data = $("#txtDBARData")[0];
                var arraydata = data.value.substr(0, data.selectionStart).split("\n");
                var rowindex = 0;
                $.each(arraydata, function (key, value) {
                    if (key === linenumber) {
                        return false; // breaks
                    }
                    rowindex += value.length;
                });
                rowindex = rowindex + 1 + Number(Columnnumber);
                setCaretToPos(el("txtDBARData"), rowindex);
            }
            else {
                console.log('Not tabbed');
            }


        });
        var textarea = document.getElementById("txtDBARData");
        //textarea.onkeyup = function() {
        $('#txtDBARData').on('input focus keydown keyup', function () {
            var lines = textarea.value.split("\n");
            var start = textarea.selectionStart;
            var end = textarea.selectionEnd;
            for (var i = 0; i < lines.length; i++) {
                if (lines[i].length <= 80) continue;
                var j = 0; space = 80;
                while (j++ <= 80) {
                    if (lines[i].charAt(j) === " ") space = j;
                }
                lines[i + 1] = lines[i].substring(space + 1) + (lines[i + 1] ? " " + lines[i + 1] : "");
                lines[i] = lines[i].substring(0, space);
            }
            textarea.value = lines.slice(0, 5).join("\n");
            if (start == end) {
                textarea.setSelectionRange(start, end);
            }
        });

    });
    function el(id) { if (document.getElementById) return document.getElementById(id); return null; }
    function appentNewline(f) {
        appendText(f, '\n');
    }
    function appendText(f, c) {
        var t = el(f);
        //t.value = t.value + c;

        var strPos = getCaret(t);

        var front = (t.value).substring(0, strPos);
        var back = (t.value).substring(strPos, t.value.length);
        t.value = front + c + back;
        strPos = strPos + c.length;
        setCaretToPos(t, strPos);

        if (!iphone) t.focus();
    }
    function getCaret(ell) {
        if (ell.selectionStart) {
            return ell.selectionStart;
        } else if (document.selection) {
            ell.focus();

            var r = document.selection.createRange();
            if (r == null) {
                return 0;
            }

            var re = ell.createTextRange(),
                rc = re.duplicate();
            re.moveToBookmark(r.getBookmark());
            rc.setEndPoint('EndToStart', re);

            return rc.text.length;
        }

        return 0;
    }

    function doGetCaretPosition(ctrl) {
        var CaretPos = 0;

        if (ctrl.selectionStart || ctrl.selectionStart == 0) {// Standard.
            CaretPos = ctrl.selectionStart;
        }
        else if (document.selection) {// Legacy IE
            ctrl.focus();
            var Sel = document.selection.createRange();
            Sel.moveStart('character', -ctrl.value.length);
            CaretPos = Sel.text.length;
        }

        return (CaretPos);
    }
    function setSelectionRange(input, selectionStart, selectionEnd) {
        if (input.setSelectionRange) {
            input.focus();
            input.setSelectionRange(selectionStart, selectionEnd);
        }
        else if (input.createTextRange) {
            var range = input.createTextRange();
            range.collapse(true);
            range.moveEnd('character', selectionEnd);
            range.moveStart('character', selectionStart);
            range.select();
        }
    }
    function setCaretToPos(input, pos) {
        setSelectionRange(input, pos, pos);
    }
</script>
