// Images start hidden (class "temp") and get an orientation class once their size is known.
// Each image is revealed as soon as it has loaded, instead of waiting for the whole page.

function onImageReady(img, apply) {
    var done = function () {
        var landscape = img.naturalWidth >= img.naturalHeight;
        apply(img, landscape);
        img.classList.remove('temp');
    };
    if (img.complete && img.naturalWidth) {
        done();
    } else {
        img.addEventListener('load', done, { once: true });
        img.addEventListener('error', function () { img.classList.remove('temp'); }, { once: true });
    }
}

function setupImages(selector, apply) {
    document.querySelectorAll(selector).forEach(function (img) { onImageReady(img, apply); });
}

document.addEventListener('DOMContentLoaded', function () {
    setupImages('#photoItem img', function (img, landscape) {
        img.classList.add(landscape ? 'photo' : 'photo_vertical', 'block');
    });

    setupImages('#details img', function (img, landscape) {
        img.classList.add(landscape ? 'photoBig' : 'photoBig_vertical');
    });

    // gallery thumbnails: portrait frames are rotated so every frame in a line has the same height
    var thumbs = function (img, landscape) {
        img.classList.add(img.naturalHeight < img.naturalWidth ? 'photo_hor' : 'photo_ver_rot', 'block');
    };
    setupImages('#FilmPhotosBlock img', thumbs);
    setupImages('#PhotoPage img', thumbs);
});
