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
});
