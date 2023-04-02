mergeInto(LibraryManager.library, {
  getDevices: function() {
    if (typeof fetchingAudioInputs === 'undefined' || !fetchingAudioInputs) {
      if (typeof audioInputNames === 'undefined') {
        audioInputNames = "";
        audioDeviceIDs = {};
      }
      fetchingAudioInputs = true;
      navigator.mediaDevices.getUserMedia({ audio: true })
        .then(stream => {
          stream.getTracks().forEach(track => track.stop());
          navigator.mediaDevices.enumerateDevices()
            .then(devices => {
              const audioDevices = devices.filter(device => device.kind === 'audioinput');
              audioInputNames = audioDevices.map(device => device.label).join('\n');
              audioDeviceIDs = audioDevices.map(device => audioDeviceIDs[device.label] = device.deviceId);
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

  // Start recording by device name, cache the device ID in 'recording', and the stream in 'audioStream'.
  startDevice: function(deviceName) {
    if (typeof audioDeviceIDs === 'undefined' || !audioDeviceIDs.hasOwnProperty(deviceName)) {
      return false; // Device doesn't exist
    }

    if (typeof recording !== 'undefined') {
      return false; // Already recording, can't start again
    }

    recording = audioDeviceIDs[deviceName];
    navigator.mediaDevices.getUserMedia({ audio: { deviceId: { exact: recording } } })
      .then(stream => {
        audioStream = stream;
      })
      .catch(error => {
        delete recording;
      });
    return true;
  },
  
  endDevice: function(deviceName) {
    if (typeof audioStream !== 'undefined') {
      audioStream.getTracks().forEach(track => track.stop());
      delete audioStream;
      delete recording;
    }
  },
  
  dispose: function(pointer) {
    _free(pointer);
  },

  getMicrophoneJslib: function() { }
});
