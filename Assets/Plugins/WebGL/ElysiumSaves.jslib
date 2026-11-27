mergeInto(LibraryManager.library, {
  ElysiumReadSave: function (key) {
    try {
      var value = window.localStorage.getItem(UTF8ToString(key)) || "";
      var length = lengthBytesUTF8(value) + 1;
      var result = _malloc(length);
      stringToUTF8(value, result, length);
      return result;
    } catch (error) {
      return 0;
    }
  },

  ElysiumWriteSave: function (key, value) {
    try {
      window.localStorage.setItem(UTF8ToString(key), UTF8ToString(value));
      return 1;
    } catch (error) {
      return 0;
    }
  }
});
