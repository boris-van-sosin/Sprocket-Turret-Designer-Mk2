mergeInto(LibraryManager.library, {
DownloadStringAsFile : function (text, fileType, fileName)
	{
		let blob = new Blob([UTF8ToString(text)], { type: UTF8ToString(fileType) });
		
		let a = document.createElement('a');
		a.download = UTF8ToString(fileName);
		a.href = URL.createObjectURL(blob);
		a.dataset.downloadurl = [UTF8ToString(fileType), a.download, a.href].join(':');
		a.style.display = "none";
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);
		setTimeout(function() { URL.revokeObjectURL(a.href); }, 1500);
	},

SetTurretData: function (tankBlueprint_stringPtr, structData_stringPtr)
	{
		let tankObj = JSON.parse(UTF8ToString(tankBlueprint_stringPtr));
		let structDataObj = JSON.parse(UTF8ToString(structData_stringPtr));
		
		console.log("Got structure for export:");
		console.log(structDataObj);
		
		for (let i in tankObj.blueprints)
		{
			if (tankObj.blueprints[i].id == "Compartment")
			{
				let compartmentData = JSON.parse(tankObj.blueprints[i].data);
				if (compartmentData.name == "Turret 1")
				{
					console.log("Tank blueprint before:");
					console.log(tankObj);
					
					let modifiedTurret = SetCompartmentGeometry(compartmentData, structDataObj.Turret);
					tankObj.blueprints[i].data = JSON.stringify(modifiedTurret);
					console.log("Tank blueprint after:");
					console.log(tankObj);
				}
				else if ("Hull" in structDataObj && structDataObj.Hull.Faces.length > 0 && compartmentData.name == "Hull")
				{
					console.log("Tank blueprint before:");
					console.log(tankObj);
					let modifiedTurret = SetCompartmentGeometry(compartmentData, structDataObj.Hull);
					tankObj.blueprints[i].data = JSON.stringify(modifiedTurret);
					console.log("Tank blueprint after:");
					console.log(tankObj);
				}
			}
		}
		
		let fileName = tankObj.name + "_withTurret.blueprint";
		
		// Copy of DownloadStringAsFile, because JavaScript is stupid like this.
		{
			let blob = new Blob([JSON.stringify(tankObj, null, 2)], { type: "text/json" });
			
			let a = document.createElement('a');
			a.download = fileName;
			a.href = URL.createObjectURL(blob);
			a.dataset.downloadurl = ["text/json", a.download, a.href].join(':');
			a.style.display = "none";
			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);
			setTimeout(function() { URL.revokeObjectURL(a.href); }, 1500);
		}
		
		function SetCompartmentGeometry(compartmentObj, exportDataObj)
		{
			console.log("Will set compartment data to " + compartmentObj.name);
			console.log(exportDataObj);
			
			let vertices = new Array(exportDataObj.Vertices.length * 3);
			for (let i in exportDataObj.Vertices)
			{
				vertices[i * 3 + 0] = exportDataObj.Vertices[i].x;
				vertices[i * 3 + 1] = exportDataObj.Vertices[i].y;
				vertices[i * 3 + 2] = exportDataObj.Vertices[i].z;
			}
			
			let dups = new Array(exportDataObj.Dups.length);
			for (let i in exportDataObj.Dups)
			{
				dups[i] = exportDataObj.Dups[i].Array;
			}
			
			let thicknessMap = exportDataObj.ThicknessMap.slice();
			
			let faces = new Array(exportDataObj.Faces.length);
			for (let i in exportDataObj.Faces)
			{
				faces[i] = exportDataObj.Faces[i].Array;
			}
			
			compartmentObj.compartment.points = vertices;
			compartmentObj.compartment.sharedPoints = dups;
			compartmentObj.compartment.thicknessMap = thicknessMap;
			compartmentObj.compartment.faceMap = faces;
			
			return compartmentObj;
		}
	},

/*
 * The function GetFileFromBrowser was adapted with modifications from Gregg Tavares's getuserimage-unity-webgl project, as permitted by its copyright notice.
 * Original gitHub link: https://github.com/greggman/getuserimage-unity-webgl

 * Original copyright notice:
 * * Copyright 2016, Gregg Tavares.
 * * All rights reserved.
 * *
 * * Redistribution and use in source and binary forms, with or without
 * * modification, are permitted provided that the following conditions are
 * * met:
 * *
 * *     * Redistributions of source code must retain the above copyright
 * * notice, this list of conditions and the following disclaimer.
 * *     * Redistributions in binary form must reproduce the above
 * * copyright notice, this list of conditions and the following disclaimer
 * * in the documentation and/or other materials provided with the
 * * distribution.
 * *     * Neither the name of Gregg Tavares. nor the names of its
 * * contributors may be used to endorse or promote products derived from
 * * this software without specific prior written permission.
 * *
 * * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

GetFileFromBrowser: function(objectNamePtr, funcNamePtr, taskIdPtr)
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

		var objectName = UTF8ToString(objectNamePtr);
		var funcName = UTF8ToString(funcNamePtr);
		var taskId = UTF8ToString(taskIdPtr);
		console.log("In JavaScript. taskId=" + taskId);
		
		g.taskId = taskId;
		
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
			input.addEventListener('change', LoadFile);

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
		
		function preventOtherClicks(evt) {
			evt.stopPropagation();
		}
		
		function HandleCancel(evt) {
			evt.stopPropagation();
			evt.preventDefault();
			sendError(g.taskId, "cancelled");
			g.busy = false;
			hide();
		}
		
		function LoadFile(evt) {
			evt.stopPropagation();
			let fileInput = evt.target.files;
			if (fileInput.length == 0) {
				sendError("Did not get any file");
				return;
			}
			let file = fileInput[0];
			let reader = new FileReader();

			console.log("In LoadFile. taskId=" + g.taskId);
			reader.addEventListener("load", () => {
				// send the uploaded text
				sendResult(g.taskId, reader.result);
			}, false);

			if (file) {
				reader.readAsText(file);
			}
		}
		
		function hide() {
			g.root.style.display = "none";
		}

		function sendResult(taskId, result) {
			hide();
			g.busy = false;
			console.log("In sendResult. taskId=" + taskId);
			SendMessage(objectName, funcName, JSON.stringify({"Id":taskId, "Success":true, "Data":result}));
		}
		
		function sendError(taskId, msg) {
			SendMessage(objectName, funcName, JSON.stringify({"Id":taskId, "Success":false, "Data":msg}));
		}
	}
});
