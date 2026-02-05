mergeInto(LibraryManager.library, {
    GetUrlParam: function(paramName) {
        var paramNameStr = UTF8ToString(paramName);
        var urlParams = new URLSearchParams(window.location.search);
        var value = urlParams.get(paramNameStr) || "";
        var bufferSize = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(value, buffer, bufferSize);
        return buffer;
    }
});
