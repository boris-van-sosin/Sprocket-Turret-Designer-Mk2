mergeInto(LibraryManager.library, {
DownloadStringAsFile : function (text, fileType, fileName)
	{
		let blob = new Blob([Pointer_stringify(text)], { type: Pointer_stringify(fileType) });
		
		let a = document.createElement('a');
		a.download = Pointer_stringify(fileName);
		a.href = URL.createObjectURL(blob);
		a.dataset.downloadurl = [Pointer_stringify(fileType), a.download, a.href].join(':');
		a.style.display = "none";
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		setTimeout(function() { URL.revokeObjectURL(a.href); }, 1500);
	}
	
GetFileFromBrowser: function(objectNamePtr, funcNamePtr)
	{
		// Because unity is currently bad at JavaScript we can't use standard
		// JavaScript idioms like closures so we have to use global variables :(
		window.becauseUnitysBadWithJavacript_getImageFromBrowser =
			window.becauseUnitysBadWithJavacript_getImageFromBrowser || {
			busy: false,
			initialized: false,
			rootDisplayStyle: null,  // style to make root element visible
			root_: null,             // root element of form
		};
		var g = window.becauseUnitysBadWithJavacript_getImageFromBrowser;
		if (g.busy) {
			// Don't let multiple requests come in
			return;
		}
		g.busy = true;

		var objectName = Pointer_stringify(objectNamePtr);
		var funcName = Pointer_stringify(funcNamePtr);

		if (!g.initialized)
		{
			g.initialized = true;

			// Append a form to the page (more self contained than editing the HTML?)
			g.root = window.document.createElement("div");
			g.root.innerHTML = [
				'<style>                                                    ',
				'.getimage {                                                ',
				'    position: absolute;                                    ',
				'    left: 0;                                               ',
				'    top: 0;                                                ',
				'    width: 100%;                                           ',
				'    height: 100%;                                          ',
				'    display: -webkit-flex;                                 ',
				'    display: flex;                                         ',
				'    -webkit-flex-flow: column;                             ',
				'    flex-flow: column;                                     ',
				'    -webkit-justify-content: center;                       ',
				'    -webkit-align-content: center;                         ',
				'    -webkit-align-items: center;                           ',
				'                                                           ',
				'    justify-content: center;                               ',
				'    align-content: center;                                 ',
				'    align-items: center;                                   ',
				'                                                           ',
				'    z-index: 2;                                            ',
				'    color: white;                                          ',
				'    background-color: rgba(0,0,0,0.8);                     ',
				'    font: sans-serif;                                      ',
				'    font-size: x-large;                                    ',
				'}                                                          ',
				'.getimage a,                                               ',
				'.getimage label {                                          ',
				'   font-size: x-large;                                     ',
				'   background-color: #666;                                 ',
				'   border-radius: 0.5em;                                   ',
				'   border: 1px solid black;                                ',
				'   padding: 0.5em;                                         ',
				'   margin: 0.25em;                                         ',
				'   outline: none;                                          ',
				'   display: inline-block;                                  ',
				'}                                                          ',
				'.getimage input {                                          ',
				'    display: none;                                         ',
				'}                                                          ',
				'</style>                                                   ',
				'<div class="getimage">                                     ',
				'    <div>                                                  ',
				'      <label for="photo">click to choose a file</label>  ',
				'      <input id="photo" type="file"/><br/>',
				'      <a>cancel</a>                                        ',
				'    </div>                                                 ',
				'</div>                                                     ',
			].join('\n');
			var input = g.root.querySelector("input");
			input.addEventListener('change', getPic);

			// prevent clicking in input or label from canceling
			input.addEventListener('click', preventOtherClicks);
			var label = g.root.querySelector("label");
			label.addEventListener('click', preventOtherClicks);

			// clicking cancel or outside cancels
			var cancel = g.root.querySelector("a");  // there's only one
			cancel.addEventListener('click', HandleCancel);
			var getImage = g.root.querySelector(".getimage");
			getImage.addEventListener('click', HandleCancel);

			// remember the original style
			g.rootDisplayStyle = g.root.style.display;

			window.document.body.appendChild(g.root);
		}

		g.root.style.display = g.rootDisplayStyle;
		
		function HandleCancel(evt) {
			evt.stopPropagation();
			evt.preventDefault();
			sendError("cancelled");
		}
		
		function LoadFile(evt) {
			evt.stopPropagation();
			var fileInput = evt.target.files;
		}
	}
});
