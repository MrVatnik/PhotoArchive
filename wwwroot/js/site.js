// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(window).on('load', function () {
    $('#photoItem img').each(function () { //you need to put this inside the window.onload-function (not document.ready), otherwise the image dimensions won't be available yet
        if ($(this).width() / $(this).height() >= 1) {
            $(this).addClass('photo');
        } else {
            $(this).addClass('photo_vertical');
        }
        $(this).removeClass('temp');
        $(this).addClass('block');
    });
});

$(window).on('load', function () {
    $('#details img').each(function () { //you need to put this inside the window.onload-function (not document.ready), otherwise the image dimensions won't be available yet
        if ($(this).width() / $(this).height() >= 1) {
            $(this).addClass('photoBig');
        } else {
            $(this).addClass('photoBig_vertical');
        }
        $(this).removeClass('temp');
        $(this).addClass('block');
    });
});


$(window).on('load', function () {
    $('#FilmPhotosBlock img').each(function () { //you need to put this inside the window.onload-function (not document.ready), otherwise the image dimensions won't be available yet
        if ($(this).height() < $(this).width()) {
            $(this).addClass('photo');
        } else {
            $(this).addClass('photo_ver_rot');
        }
        $(this).removeClass('temp');
        $(this).addClass('block');
    });
});
