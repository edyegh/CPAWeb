// Ֆայլ ներբեռնելու օգնական՝ Blazor-ը բայթերն ուղարկում է base64-ով:
// Անհրաժեշտ է, որովհետև API-ն [Authorize] է, իսկ սովորական <a href>-ը
// Bearer token չի կրում — ֆայլը վերցնում ենք HttpClient-ով և տալիս բրաուզերին:
window.cpaDownloadFile = (fileName, contentType, base64) => {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);

    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    URL.revokeObjectURL(url);
};
