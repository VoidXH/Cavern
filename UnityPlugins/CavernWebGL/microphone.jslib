mergeInto(LibraryManager.library, {
  getDevices: function () {
    if (typeof fetchingAudioInputs === 'undefined' || !fetchingAudioInputs) {
      if (typeof audioInputNames === 'undefined') {
        audioInputNames = "";
      }
      fetchingAudioInputs = true;
      navigator.mediaDevices.getUserMedia({ audio: true })
        .then(stream => {
          stream.getTracks().forEach(track => track.stop());
          navigator.mediaDevices.enumerateDevices()
            .then(devices => {
              const audioDevices = devices.filter(device => device.kind === 'audioinput');
              audioInputNames = audioDevices.map(device => device.label).join('\n');
              fetchingAudioInputs = false;
            })
            .catch(error => {
              console.error(error);
              audioInputNames = "ERROR";
              fetchingAudioInputs = false;
            });
        })
        .catch(error => {
          console.error(error);
          audioInputNames = "DENIED";
          fetchingAudioInputs = false;
        });
    }

    var length = lengthBytesUTF8(audioInputNames) + 1;
    var buffer = _malloc(length);
    stringToUTF8(audioInputNames, buffer, length);
    return buffer;
  },
  
  dispose: function(pointer) {
    _free(pointer);
  },

  getMicrophoneJslib: function() { }
});
