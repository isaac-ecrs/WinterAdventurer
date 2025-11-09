function downloadFile(filename, base64Content) {
    const linkSource = `data:application/pdf;base64,${base64Content}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.click();
}
