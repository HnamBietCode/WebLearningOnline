$(document).ready(function () {
    // Auto dismiss alerts after 5 seconds
    setTimeout(function () {
        $(".alert").fadeOut(500, function () {
            $(this).remove();
        });
    }, 5000);

    // Add animation to the main container when page loads
    $(".grading-selection-container").css("opacity", "0").css("transform", "translateY(20px)");
    setTimeout(function () {
        $(".grading-selection-container").css({
            "opacity": "1",
            "transform": "translateY(0)",
            "transition": "opacity 0.5s ease, transform 0.5s ease"
        });
    }, 200);

    // Add staggered animation to options
    $(".grading-option-row").each(function (index) {
        $(this).css("opacity", "0").css("transform", "translateY(20px)");
        setTimeout(function () {
            $(".grading-option-row").eq(index).css({
                "opacity": "1",
                "transform": "translateY(0)",
                "transition": "opacity 0.5s ease, transform 0.5s ease"
            });
        }, 400 + (index * 150));
    });

    // Add pulse animation to header icon
    function pulseAnimation() {
        $(".icon-circle").addClass("pulse");
        setTimeout(function () {
            $(".icon-circle").removeClass("pulse");
            setTimeout(pulseAnimation, 1500);
        }, 1600);
    }

    setTimeout(pulseAnimation, 1500);
});