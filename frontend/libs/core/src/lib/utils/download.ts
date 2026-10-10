/** Triggers a browser download for a URL returned by an `export` endpoint. */
export function downloadBlob(url: string, fileName: string): void {
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.rel = 'noopener';
  a.click();
}
