export function initializeUpload(instance) {
    const input = document.getElementById("filePicker");
    const chunk = 25 * 1024 * 1024;

    input.addEventListener("change", async function (e) {
        const file = e.target.files[0];

        console.log("Uploading file");

        const jwt = await instance.invokeMethodAsync("GetJwt");
        await instance.invokeMethodAsync("UploadStarted");
        
        const upload = new tus.Upload(file,
        {
            endpoint: "http://squadtalk.ddns.net/tus",
            retryDelays: [0, 3000, 5000, 10000, 20000],
            metadata: {
                filename: file.name,
                filetype: file.type,
                filesize: file.size
            },
            chunkSize: chunk,
            headers: {Authorization: `Bearer ${jwt}`},
            onProgress: async function (bytesUploaded, bytesTotal) {
                const percentage = ((bytesUploaded / bytesTotal) * 100).toFixed(2);
                await instance.invokeMethodAsync("UploadProgressed", percentage);
                console.log(`Progress: ${percentage}`);
            },
            onSuccess: async function () {
                await instance.invokeMethodAsync("UploadStopped");
                console.log("File uploaded");

            },
            onError: async function (error) {
                await instance.invokeMethodAsync("UploadStopped");
                console.log(`Error: ${error}`);
            }
        });

        upload.findPreviousUploads().then(function (previousUploads) {
            // Found previous uploads so we select the first one.
            if (previousUploads.length) {
                upload.resumeFromPreviousUpload(previousUploads[0])
            }

            // Start the upload
            upload.start()
        })
    });
}

function askToResumeUpload(previousUploads) {
    if (previousUploads.length === 0) return null;

    var text = "You tried to upload this file previously at these times:\n\n";
    previousUploads.forEach((previousUpload, index) => {
        text += "[" + index + "] " + previousUpload.creationTime + "\n";
    });
    text += "\nEnter the corresponding number to resume an upload or press Cancel to start a new upload";

    var answer = prompt(text);
    var index = parseInt(answer, 10);

    if (!isNaN(index) && previousUploads[index]) {
        return previousUploads[index];
    }
}