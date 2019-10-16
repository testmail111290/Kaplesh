/*DeopDown Navigation*/
function DropDown(el) {
    this.dd = el;
    this.initEvents();
}

DropDown.prototype = {
    initEvents: function () {
        var obj = this;

        obj.dd.on('click', function (event) {
           
            $(this).toggleClass('active-menu');

            /*niceScroll*/
            $(".dropdown").niceScroll({
                cursoropacitymin: .5,
                cursorcolor: "#8b7cb7",
                cursorwidth: "5px",
                cursorborder: "0px solid #fff",
                cursorborderradius: "0px"
            });

            if ($(this).attr("class").indexOf("active-menu") > -1) {
                $("ul",this).niceScroll().show();
            }
            else { $("ul", this).niceScroll().hide(); }
            event.stopPropagation();
        });
    }
}

$(function () {
    var dd = new DropDown($('.dashBoardMenu'));
    $(document).click(function () {
        // all dropdowns
        $('.wrapper-dropdown').removeClass('active-menu');
       
        $(".dropdown").niceScroll().hide();

    });
});


$(document).ready(function () {


    $('.table-scroll').jScrollPane();
    $('.scrollTbody').jScrollPane();

    $(window).resize(function () {
        $('.table-scroll').jScrollPane();
        $('.scrollTbody').jScrollPane();
    });

    /*Responsive Menu*/
    $('#menu-toggle').click(function (e) {
        $('body').toggleClass('active');
        e.preventDefault();
    });

    initializeCSS();
    $(".collapse-button").click(function () {
        $('.start-training-wrapper').toggleClass('collapse-sidebar');
        $(".scroll-training").getNiceScroll().resize();
    });

    $('.training-left-col > .panel-group .scroll-tbl > .panel').click(function () {
        if ($('.start-training-wrapper').hasClass('collapse-sidebar')) {
            $('.start-training-wrapper').removeClass('collapse-sidebar');
        }
    });

    $('.training-left-col > .panel-group .scroll-tbl > .panel .panel-collapse').click(function () {
        $('.training-content-body').addClass('open');
    });
	
});

function resetCSS() {

    $(".scroll-cal-item").niceScroll().hide();
  
}

function initializeCSS()
{
    $(".table-selector tr:even").addClass("even");

    $("select, input[type='file'], input[type='checkbox'], input[type='radio']").uniform({
        fileButtonHtml: "Browse"
    });

    $(".upl input[type='file']").uniform({
        fileButtonHtml: " "
    });


    
    $("[title]").tooltip();



    
    


    /*niceScroll*/

   
    $(".scroll-tbl, .scroll-cal-item").niceScroll({
        cursoropacitymin: .5,
        cursorcolor: "#8b7cb7",
        cursorwidth: "5px",
        cursorborder: "0px solid #fff",
        cursorborderradius: "0px",
        horizrailenabled: false
    });

  
    $(".scroll-training").niceScroll({
        cursoropacitymin: .5,
        cursorcolor: "#8b7cb7",
        cursorwidth: "5px",
        cursorborder: "0px solid #fff",
        cursorborderradius: "0px",
        horizrailenabled: false
    });
    


    $(".table-responsive-desktop").niceScroll({
        horizrailenabled: true,
        cursoropacitymin: .5,
        cursorcolor: "#8b7cb7",
        cursorwidth: "5px",
        cursorborder: "0px solid #fff",
        cursorborderradius: "0px",
        touchbehavior: false
    });


    if ($(window).width() < 900) {
        $(".table-responsive").niceScroll({
            horizrailenabled: true,
            cursoropacitymin: .5,
            cursorcolor: "#8b7cb7",
            cursorwidth: "5px",
            cursorborder: "0px solid #fff",
            cursorborderradius: "0px",
            touchbehavior: false
        });
    }
}




