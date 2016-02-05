$(function () {
    $(document).ready(function () {
        $("input:text").first().focus();
    });

    window.hasOwnProperty("$content") || (window["$content"] = "/Content");
    function showAvailabilityImage(s) {
        var url = s === -1 ? $content + "/img/unknown.png" : "";
        (s === 0) && (url = $content + "/img/notok.png");
        (s === 1) && (url = $content + "/img/ok.png");
        $("#TenantName").css("background", "url(" + url + ") no-repeat scroll right 12px center");
    };

    function clearTenantAvailabilityIndicator() {
        $("#tenantNameGroup").removeClass("has-error").removeClass("has-success"), $("#tenantNameStatus").css("display", "none");
        !$("#TenantName").val() && (showAvailabilityImage(-2));
    }

    var $t = $("#TenantName");
    var validated = false;
    ($t.length !== 0) &&
    (
        $t.on("input", function() {
                validated = false;
                $t.removeData("previousValue");
                showAvailabilityImage(-2);
                clearTenantAvailabilityIndicator();
            })
            .on("blur", function() {
                validated || ($("form").validate().element("#TenantName"));
            })
            .rules().remote.complete = function(r) {
                r.responseJSON !== undefined && showAvailabilityImage(r.responseJSON === true ? 1 : 0);
                $("#tenantNameGroup").removeClass("has-error").removeClass("has-success");
                r.responseJSON === true && $("#tenantNameGroup").addClass("has-success");
                r.responseJSON === false && $("#tenantNameGroup").addClass("has-error");
            },
        $t.rules().remote.beforeSend = function() {
            validated = true;
            showAvailabilityImage(-1);
        });

    var doReplace;

    function replace(s) {
        return s.replace(/[^\w]/g, "");
    };

    $("#CompanyName")
        .focus(function() {
            var v = $("#TenantName").val();
            doReplace = (replace(this.value) === v) || v.trim() === "";
        })
        .blur(function() {
            doReplace && (this.value && $("#TenantName").val(replace(this.value)), $("form").validate().element("#TenantName"));
        });
});
