var CUDLR_Relay = {
  $cr_callback: function(){},

  SetCudlrCallbackAndCreateInput: function(callbackPtr){
    cr_callback = callbackPtr;
    
    window.cc = function(command) {
      var size = lengthBytesUTF8(command) + 1;
      var buffer = _malloc(size);
      stringToUTF8(command, buffer, size);
      
      var result = Runtime.dynCall('ii', cr_callback, [buffer]);
      var text = UTF8ToString(result);
      _free(buffer);
      
      return text;
    }

    document.onclick = function(){
      if(!window.event.getModifierState("Shift")
      || !window.event.getModifierState("Alt")) return true;
    
      var text = cc(prompt());
      console.log(text);
      
      return true;
    };
  
  },

  ShowLog: function(text){
    console.log(text);
  },

}
autoAddDeps(CUDLR_Relay, '$cr_callback');
mergeInto(LibraryManager.library, CUDLR_Relay);
