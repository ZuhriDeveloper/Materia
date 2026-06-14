/**
 * downloadFileFromStream(fileName, contentStreamReference)
 *
 * Saves a file from a Blazor DotNetStreamReference by building a Blob and clicking a
 * temporary anchor. Used for the products Excel export, which streams the .xlsx bytes
 * over the SignalR circuit rather than embedding them as base64.
 */
window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: 'application/octet-stream' });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? 'download';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();

    URL.revokeObjectURL(url);
};
